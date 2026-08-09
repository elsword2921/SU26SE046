using BLL.DTOs;
using BLL.Services.Implements.Notifications;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Net.Http;
using System.Text.Json;

namespace BLL.Services.Implements.DistributionOperations;

public class DistributionOperationsService(AppDbContext context, HttpClient ghnClient, IConfiguration configuration)
{
    public async Task<object> CatalogAsync(Guid? warehouseId)
    {
        var warehouses = await context.Warehouses.AsNoTracking().Where(x => x.IsActive != false)
            .OrderBy(x => x.WarehouseName).Select(x => new { x.Id, x.WarehouseName, x.Address }).ToListAsync();
        var query = context.Inventories.AsNoTracking().Include(x => x.ClassifiedBatch)!.ThenInclude(x => x!.Items)
            .Where(x => x.IsActive != false && x.Status == "Available" && x.ProcessingDirection == "Charity"
                && x.Quantity > x.ReservedQuantity);
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId);
        var rows = await query.OrderBy(x => x.Sku).ToListAsync();
        var items = rows.Select(x => new DistributionCatalogItemDto(x.Id, x.ClassifiedBatchId!.Value,
            x.ClassifiedBatch!.BatchCode, x.Sku, x.ClothingType, x.FabricType, x.Gender, x.TargetUser, x.Size,
            Grade(x.ConditionRating), x.Quantity - x.ReservedQuantity, x.TotalWeight - x.ReservedWeight,
            x.ClassifiedBatch.Items.Where(i => i.IsActive != false).Select(i => new DistributionCatalogImageDto(
                i.ItemCode, i.ClothingType, i.FabricType, i.Gender, i.TargetUser, i.Size, i.ImageUrls ?? [], i.Notes)).ToList())).ToList();
        return new { warehouses, items };
    }

    public async Task<Guid> CreateAsync(Guid organizationId, CreateDistributionRequestDto dto)
    {
        ValidateRequest(dto);
        if (dto.Items.Count == 0) throw new InvalidOperationException("Select at least one batch.");
        var ids = dto.Items.Select(x => x.InventoryId).Distinct().ToList();
        if (ids.Count != dto.Items.Count) throw new InvalidOperationException("A batch can only appear once.");
        var inventories = await context.Inventories.Where(x => ids.Contains(x.Id) && x.IsActive != false
            && x.WarehouseId == dto.WarehouseId && x.ProcessingDirection == "Charity" && x.Status == "Available").ToListAsync();
        if (inventories.Count != ids.Count) throw new InvalidOperationException("One or more batches are unavailable or belong to another warehouse.");
        var requestId = Guid.NewGuid();
        var request = new DistributionRequest { Id=requestId, RequestCode=BuildRequestCode(requestId), UserId=organizationId, WarehouseId=dto.WarehouseId,
            RecipientName=dto.RecipientName.Trim(), RecipientPhone=dto.RecipientPhone.Trim(), ToAddress=dto.ToAddress.Trim(),
            RequestNotes=dto.Notes, RequestedAt=DateTime.UtcNow, Status="PendingManagerApproval", CreateAt=DateTime.UtcNow, IsActive=true };
        foreach (var input in dto.Items)
        {
            var inventory=inventories.Single(x=>x.Id==input.InventoryId); var available=inventory.Quantity-inventory.ReservedQuantity;
            if(input.Quantity<=0||input.Quantity>available) throw new InvalidOperationException($"Invalid quantity for {inventory.Sku}.");
            var unitWeight=inventory.Quantity==0?0:inventory.TotalWeight/inventory.Quantity;
            request.Items.Add(new DistributionItem { Id=Guid.NewGuid(), InventoryId=inventory.Id,
                ConditionRating=inventory.ConditionRating, RequestedQuantity=input.Quantity,
                RequestedWeight=Math.Round(unitWeight*input.Quantity,2), CreateAt=DateTime.UtcNow, IsActive=true });
        }
        context.DistributionRequests.Add(request);
        var managers=await context.Users.Where(x=>x.IsActive!=false&&x.Role.RoleName=="Manager").Select(x=>x.Id).ToListAsync();
        foreach(var id in managers) NotificationWriter.NotifyUser(context,id,"DistributionRequested","Yêu cầu nhận đồ từ thiện mới",
            $"Tổ chức {request.RecipientName} vừa tạo yêu cầu gồm {request.Items.Sum(x=>x.RequestedQuantity)} item.",
            $"/manager/distributions?requestId={request.Id}",organizationId);
        await context.SaveChangesAsync(); return request.Id;
    }

    public async Task<Guid> CreateManagerRequestAsync(Guid managerId, CreateManagerRequestDto dto)
    {
        var organization = await context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == dto.OrganizationId && x.IsActive != false)
            ?? throw new KeyNotFoundException("Recycling or disposal organization not found.");
        var processingDirection = GetProcessingDirection(organization.Role.RoleName);
        ValidateRequest(new CreateDistributionRequestDto(dto.WarehouseId, organization.FullName, organization.PhoneNumber, organization.Address, dto.Notes, dto.Items));
        if (dto.Items.Count == 0) throw new InvalidOperationException("Select at least one batch.");
        var ids = dto.Items.Select(x => x.InventoryId).Distinct().ToList();
        if (ids.Count != dto.Items.Count)throw new InvalidOperationException("A batch can only appear once.");
        var inventories = await context.Inventories.Where(x =>ids.Contains(x.Id) && x.IsActive != false
            && x.WarehouseId == dto.WarehouseId && x.ProcessingDirection == processingDirection && x.Status == "Available").ToListAsync();
        if (inventories.Count != ids.Count)throw new InvalidOperationException("One or more batches are unavailable or belong to another warehouse.");
        var requestId = Guid.NewGuid();
        var request = new DistributionRequest{ Id = requestId, RequestCode = BuildRequestCode(requestId), UserId = organization.Id, WarehouseId = dto.WarehouseId,
            RecipientName = organization.FullName.Trim(), RecipientPhone = organization.PhoneNumber.Trim(), ToAddress = organization.Address.Trim(),
            RequestNotes = dto.Notes, RequestedAt = DateTime.UtcNow, Status = "PendingOrganizationApproval", CreateAt = DateTime.UtcNow, IsActive = true };
        foreach (var input in dto.Items)
        {
            var inventory = inventories.Single(x => x.Id == input.InventoryId); var available = inventory.Quantity - inventory.ReservedQuantity;
            if (input.Quantity <= 0 || input.Quantity > available) throw new InvalidOperationException($"Invalid quantity for {inventory.Sku}.");
            var unitWeight = inventory.Quantity==0?0:inventory.TotalWeight/inventory.Quantity;
            request.Items.Add(new DistributionItem { Id = Guid.NewGuid(), InventoryId = inventory.Id,
                ConditionRating=inventory.ConditionRating, RequestedQuantity = input.Quantity,
                RequestedWeight=Math.Round(unitWeight*input.Quantity,2), CreateAt=DateTime.UtcNow, IsActive=true });
        }
        context.DistributionRequests.Add(request);
        var actionText = processingDirection == "Recycling"
            ? "tái chế"
            : "tiêu hủy";
        NotificationWriter.NotifyUser(context,organization.Id,"DistributionRequested",$"Yêu cầu {actionText} mới",
            $"Manager đã gửi yêu cầu {actionText} gồm " +
            $"{request.Items.Sum(x => x.RequestedQuantity)} sản phẩm.",
            $"/organization/distributions/{request.Id}",
            managerId);
        await context.SaveChangesAsync(); return request.Id;
    }

    public async Task UpdateAsync(Guid organizationId, Guid id, CreateDistributionRequestDto dto)
    {
        ValidateRequest(dto);
        var request = await context.DistributionRequests.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == organizationId && x.IsActive != false)
            ?? throw new KeyNotFoundException("Distribution request not found.");
        if (request.Status != "PendingManagerApproval")
            throw new InvalidOperationException("Only requests awaiting manager approval can be edited.");
        if (dto.Items.Count == 0) throw new InvalidOperationException("Select at least one batch.");
        var ids = dto.Items.Select(x => x.InventoryId).Distinct().ToList();
        if (ids.Count != dto.Items.Count) throw new InvalidOperationException("A batch can only appear once.");
        var inventories = await context.Inventories.Where(x => ids.Contains(x.Id) && x.IsActive != false
            && x.WarehouseId == dto.WarehouseId && x.ProcessingDirection == "Charity" && x.Status == "Available").ToListAsync();
        if (inventories.Count != ids.Count) throw new InvalidOperationException("One or more batches are unavailable or belong to another warehouse.");

        context.DistributionItems.RemoveRange(request.Items);
        request.Items.Clear();
        foreach (var input in dto.Items)
        {
            var inventory = inventories.Single(x => x.Id == input.InventoryId);
            var available = inventory.Quantity - inventory.ReservedQuantity;
            if (input.Quantity <= 0 || input.Quantity > available)
                throw new InvalidOperationException($"Invalid quantity for {inventory.Sku}.");
            var unitWeight = inventory.Quantity == 0 ? 0 : inventory.TotalWeight / inventory.Quantity;
            request.Items.Add(new DistributionItem { Id = Guid.NewGuid(), InventoryId = inventory.Id,
                ConditionRating = inventory.ConditionRating, RequestedQuantity = input.Quantity,
                RequestedWeight = Math.Round(unitWeight * input.Quantity, 2), CreateAt = DateTime.UtcNow, IsActive = true });
        }
        request.WarehouseId = dto.WarehouseId;
        request.RecipientName = dto.RecipientName.Trim();
        request.RecipientPhone = dto.RecipientPhone.Trim();
        request.ToAddress = dto.ToAddress.Trim();
        request.RequestNotes = dto.Notes;
        request.UpdateAt = DateTime.UtcNow;
        var managers = await context.Users.Where(x => x.IsActive != false && x.Role.RoleName == "Manager").Select(x => x.Id).ToListAsync();
        foreach (var managerId in managers) NotificationWriter.NotifyUser(context, managerId, "DistributionUpdated",
            "Yêu cầu nhận đồ từ thiện đã cập nhật", $"Tổ chức {request.RecipientName} đã chỉnh sửa yêu cầu {request.RequestCode}.",
            $"/manager/distributions?requestId={request.Id}", organizationId);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid organizationId, Guid id)
    {
        var request = await context.DistributionRequests
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == organizationId && x.IsActive != false)
            ?? throw new KeyNotFoundException("Distribution request not found.");
        if (request.Status != "PendingManagerApproval")
            throw new InvalidOperationException("Only requests awaiting manager approval can be deleted.");
        request.IsActive = false;
        request.DeleteAt = DateTime.UtcNow;
        request.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public Task<List<DistributionRequestViewDto>> MineAsync(Guid userId)=>Query().Where(x=>x.UserId==userId).Select(Map()).ToListAsync();
    public Task<List<DistributionRequestViewDto>> ManagerAsync()=>Query().Select(Map()).ToListAsync();
    public async Task<List<DistributionRequestViewDto>> WarehouseAsync(Guid staffId)
    {
        var warehouseId=await context.Users.Where(x=>x.Id==staffId).Select(x=>x.WarehouseId).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Warehouse staff is not assigned to a warehouse.");
        return await Query().Where(x=>x.WarehouseId==warehouseId&&x.Status!="PendingManagerApproval").Select(Map()).ToListAsync();
    }

    public async Task ApproveAsync(Guid managerId,Guid id,ApproveDistributionDto dto)
    {
        await using var tx=await context.Database.BeginTransactionAsync();
        var request=await context.DistributionRequests.Include(x=>x.Items).ThenInclude(x=>x.Inventory)
            .FirstOrDefaultAsync(x=>x.Id==id&&x.Status=="PendingManagerApproval")??throw new InvalidOperationException("Pending request not found.");
        if(!dto.Approved){request.Status="Rejected";request.RejectReason=dto.Notes;NotificationWriter.NotifyUser(context,request.UserId,"DistributionRejected","Yêu cầu chưa được duyệt",dto.Notes??"Manager đã từ chối yêu cầu.",$"/organization/distributions/{id}",managerId);await context.SaveChangesAsync();await tx.CommitAsync();return;}
        foreach(var item in request.Items){var inv=item.Inventory;var available=inv.Quantity-inv.ReservedQuantity;if(item.RequestedQuantity>available)throw new InvalidOperationException($"Insufficient inventory for {inv.Sku}.");var weight=inv.Quantity==0?0:Math.Round(inv.TotalWeight/inv.Quantity*item.RequestedQuantity,2);inv.ReservedQuantity+=item.RequestedQuantity;inv.ReservedWeight+=weight;item.ApprovedQuantity=item.RequestedQuantity;item.RequestedWeight=weight;}
        request.Status="ApprovedAwaitingWarehouse";request.ApprovedAt=DateTime.UtcNow;request.ApprovedByManagerId=managerId;
        var staff=await context.Users.Where(x=>x.WarehouseId==request.WarehouseId&&x.IsActive!=false&&x.Role.RoleName=="WarehouseStaff").Select(x=>x.Id).ToListAsync();
        foreach(var userId in staff)NotificationWriter.NotifyUser(context,userId,"DistributionApproved","Có yêu cầu xuất kho mới",$"Yêu cầu của {request.RecipientName} đã được duyệt.",$"/warehouse/distributions?requestId={id}",managerId);
        NotificationWriter.NotifyUser(context,request.UserId,"DistributionApproved","Yêu cầu đã được duyệt","Kho đang chuẩn bị hàng theo yêu cầu của bạn.",$"/organization/distributions/{id}",managerId);
        await context.SaveChangesAsync();await tx.CommitAsync();
    }

    public async Task RespondDistributionRequestAsync(Guid organizationId, Guid id, RespondDistributionRequestDto dto)
    {
        var request = await context.DistributionRequests
            .FirstOrDefaultAsync(x=>x.Id == id && x.UserId == organizationId && x.IsActive != false && x.Status == "PendingOrganizationApproval")
            ?? throw new InvalidOperationException("Pending request not found or does not belong to this organization.");
        if (!dto.Accepted)
        {
            if (string.IsNullOrWhiteSpace(dto.Notes))
                throw new InvalidOperationException(
                    "Decline reason is required.");
            request.Status = "DeclinedByOrganization";
            request.RejectReason = dto.Notes.Trim();
        }
        else
        {
            request.Status = "PendingManagerApproval";
        }
        request.UpdateAt = DateTime.UtcNow;
        var managerIds = await context.Users.Where(x => x.IsActive != false && x.Role.RoleName == "Manager").Select(x => x.Id).ToListAsync();
        foreach (var managerId in managerIds)
            NotificationWriter.NotifyUser(context, managerId, dto.Accepted ? "OrganizationAcceptedRequest" : "OrganizationDeclinedRequest",
                dto.Accepted ? "Tổ chức đã chấp nhận yêu cầu" : "Tổ chức đã từ chối yêu cầu",
                dto.Accepted ? $"{request.RecipientName} đã chấp nhận yêu cầu {request.RequestCode}." : $"{request.RecipientName} đã từ chối yêu cầu {request.RequestCode}: {request.RejectReason}",
                $"/manager/distributions?requestId={request.Id}", organizationId);
        await context.SaveChangesAsync();
    }

    public async Task IssueAsync(Guid staffId,Guid id,IssueDistributionDto dto)
    {
        await using var tx=await context.Database.BeginTransactionAsync();
        var request=await context.DistributionRequests.Include(x=>x.Warehouse)
            .Include(x=>x.Items).ThenInclude(x=>x.Inventory).ThenInclude(x=>x.StorageLocation)!.ThenInclude(x=>x!.Area)
            .Include(x=>x.Items).ThenInclude(x=>x.Inventory).ThenInclude(x=>x.ClassifiedBatch).ThenInclude(x=>x!.DonationRequestSources)
            .FirstOrDefaultAsync(x=>x.Id==id&&x.Status=="ApprovedAwaitingWarehouse")??throw new InvalidOperationException("Approved request not found.");
        var staff=await context.Users.FirstAsync(x=>x.Id==staffId);if(staff.WarehouseId!=request.WarehouseId)throw new InvalidOperationException("Request belongs to another warehouse.");
        var transaction=new InventoryTransaction{Id=Guid.NewGuid(),WarehouseId=request.WarehouseId,TransactionCode=$"TX-OUT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(),TransactionType="OUT",ReferenceType="DistributionRequest",ReferenceId=request.Id,Status="Posted",Notes=dto.Notes,PerformedByStaffId=staffId,PerformedAt=DateTime.UtcNow,CreateAt=DateTime.UtcNow,IsActive=true};
        var donorIds=new HashSet<Guid>();
        foreach(var item in request.Items){var inv=item.Inventory;var qty=item.ApprovedQuantity;var weight=item.RequestedWeight;var beforeQty=inv.Quantity;var beforeWeight=inv.TotalWeight;inv.ReservedQuantity-=qty;inv.ReservedWeight-=weight;inv.Quantity-=qty;inv.TotalWeight-=weight;inv.Status=inv.Quantity==0?"Depleted":"Available";if(inv.StorageLocation!=null){inv.StorageLocation.CurrentWeightKg=Math.Max(0,inv.StorageLocation.CurrentWeightKg-weight);inv.StorageLocation.Area.CurrentKg=Math.Max(0,inv.StorageLocation.Area.CurrentKg-weight);}request.Warehouse.CurrentWeight=Math.Max(0,request.Warehouse.CurrentWeight-weight);item.IssuedQuantity=qty;item.IssuedWeight=weight;transaction.Items.Add(new TransactionItem{Id=Guid.NewGuid(),InventoryId=inv.Id,ClassifiedBatchId=inv.ClassifiedBatchId,Quantity=qty,Weight=weight,QuantityBefore=beforeQty,QuantityAfter=inv.Quantity,WeightBefore=beforeWeight,WeightAfter=inv.TotalWeight,SourceLocationId=inv.StorageLocationId,CreateAt=DateTime.UtcNow,IsActive=true});foreach(var source in inv.ClassifiedBatch!.DonationRequestSources)donorIds.Add(source.DonationRequestId);}
        context.InventoryTransactions.Add(transaction);request.Status="ReadyForGhn";request.IssueSlipCode=$"PXK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();request.WarehouseIssuedAt=DateTime.UtcNow;request.WarehouseIssuedByStaffId=staffId;
        var donors=await context.DonationRequests.Where(x=>donorIds.Contains(x.Id)).ToListAsync();foreach(var donor in donors)NotificationWriter.NotifyDonor(context,donor,"DonationDistributed","Món quà đã được chuyển đến tổ chức từ thiện",$"một phần đóng góp của bạn đã được xuất kho gửi đến {request.RecipientName}. Cảm ơn bạn đã lan tỏa yêu thương — hãy tiếp tục đồng hành cùng ReThreads!",staffId);
        NotificationWriter.NotifyUser(context,request.UserId,"DistributionIssued","Kho đã chuẩn bị xong hàng",$"Phiếu xuất {request.IssueSlipCode} đã được lập, đang chờ GHN đến lấy.",$"/organization/distributions/{id}",staffId);
        await context.SaveChangesAsync();await tx.CommitAsync();
    }

    public async Task CreateGhnShipmentAsync(Guid staffId, Guid id, CreateGhnShipmentDto dto)
    {
        var request=await context.DistributionRequests.Include(x=>x.Warehouse).Include(x=>x.Items).ThenInclude(x=>x.Inventory)
            .FirstOrDefaultAsync(x=>x.Id==id&&x.Status=="ReadyForGhn")??throw new InvalidOperationException("Issue the goods before booking GHN.");
        var staff=await context.Users.FirstAsync(x=>x.Id==staffId);
        if(staff.WarehouseId!=request.WarehouseId)throw new InvalidOperationException("Request belongs to another warehouse.");
        var token=configuration["Ghn:Token"]??configuration["GHN:Key"];var shopId=configuration["Ghn:ShopId"];
        if(string.IsNullOrWhiteSpace(token)||string.IsNullOrWhiteSpace(shopId))
            throw new InvalidOperationException("GHN Token and ShopId are not configured on the server.");
        var client=ghnClient;
        client.DefaultRequestHeaders.TryAddWithoutValidation("Token",token);
        client.DefaultRequestHeaders.TryAddWithoutValidation("ShopId",shopId);
        _=int.TryParse(configuration["Ghn:PickupDistrictId"],out var pickupDistrictId);
        var pickupWardCode=configuration["Ghn:PickupWardCode"];
        if(pickupDistrictId<=0||string.IsNullOrWhiteSpace(pickupWardCode))
            throw new InvalidOperationException("GHN pickup district and ward are not configured on the server.");
        var weight=(int)Math.Ceiling(request.Items.Sum(x=>x.IssuedWeight)*1000);
        var payload=new {payment_type_id=dto.PaymentTypeId,service_type_id=dto.ServiceTypeId<=0?2:dto.ServiceTypeId,
            required_note=dto.RequiredNote??"KHONGCHOXEMHANG",
            from_name=configuration["Ghn:PickupName"]??request.Warehouse.WarehouseName,
            from_phone=configuration["Ghn:PickupPhone"]??request.Warehouse.PhoneNumber??"0900000000",
            from_address=configuration["Ghn:PickupAddress"]??request.Warehouse.Address,
            from_district_id=pickupDistrictId,from_ward_code=pickupWardCode,
            to_name=request.RecipientName,to_phone=request.RecipientPhone,to_address=request.ToAddress,to_district_id=dto.ToDistrictId,to_ward_code=dto.ToWardCode,
            weight=Math.Max(weight,1),length=40,width=40,height=40,client_order_code=request.IssueSlipCode,
            items=request.Items.Select(x=>new{name=x.Inventory.ClothingType,code=x.Inventory.Sku,quantity=x.IssuedQuantity,price=0}).ToList()};
        var response=await client.PostAsJsonAsync("v2/shipping-order/create",payload);
        var json=await response.Content.ReadAsStringAsync();
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException($"GHN rejected shipment: {json}");
        using var document=JsonDocument.Parse(json);var data=document.RootElement.GetProperty("data");
        request.GhnOrderCode=data.GetProperty("order_code").GetString();request.TrackingCode=request.GhnOrderCode;
        request.CarrierName="Giao Hàng Nhanh";request.GhnStatus="ready_to_pick";request.Status="GhnBooked";request.GhnUpdatedAt=DateTime.UtcNow;
        context.ShipmentStatusHistories.Add(new ShipmentStatusHistory{Id=Guid.NewGuid(),DistributionRequestId=request.Id,Status="ready_to_pick",Description="GHN đã nhận yêu cầu đến lấy hàng.",Source="GHN",OccurredAt=DateTime.UtcNow,CreateAt=DateTime.UtcNow,IsActive=true});
        NotificationWriter.NotifyUser(context,request.UserId,"GhnShipmentCreated","Đã tạo vận đơn GHN",$"Mã vận đơn {request.GhnOrderCode}. GHN đang chuẩn bị đến kho lấy hàng.",$"/organization/distributions/{id}",staffId);
        await context.SaveChangesAsync();
    }

    public async Task RefreshGhnAsync(Guid userId, Guid id)
    {
        var request=await context.DistributionRequests.FirstOrDefaultAsync(x=>x.Id==id&&x.GhnOrderCode!=null)
            ??throw new InvalidOperationException("GHN shipment not found.");
        var user=await context.Users.Include(x=>x.Role).FirstAsync(x=>x.Id==userId);
        if(user.Role.RoleName=="CharityOrganization"&&request.UserId!=userId)
            throw new UnauthorizedAccessException("This shipment belongs to another organization.");
        if(user.Role.RoleName=="WarehouseStaff"&&request.WarehouseId!=user.WarehouseId)
            throw new UnauthorizedAccessException("This shipment belongs to another warehouse.");
        var token=configuration["Ghn:Token"]??configuration["GHN:Key"]??throw new InvalidOperationException("GHN Token is not configured.");
        var client=ghnClient;client.DefaultRequestHeaders.TryAddWithoutValidation("Token",token);
        var response=await client.PostAsJsonAsync("v2/shipping-order/detail",new{order_code=request.GhnOrderCode});
        var json=await response.Content.ReadAsStringAsync();if(!response.IsSuccessStatusCode)throw new InvalidOperationException($"Cannot query GHN: {json}");
        using var document=JsonDocument.Parse(json);var status=document.RootElement.GetProperty("data").GetProperty("status").GetString()??"unknown";
        if(status!=request.GhnStatus){request.GhnStatus=status;request.GhnUpdatedAt=DateTime.UtcNow;context.ShipmentStatusHistories.Add(new ShipmentStatusHistory{Id=Guid.NewGuid(),DistributionRequestId=id,Status=status,Description="Cập nhật trạng thái từ GHN.",Source="GHN",OccurredAt=DateTime.UtcNow,CreateAt=DateTime.UtcNow,IsActive=true});}
        await context.SaveChangesAsync();
    }

    private IQueryable<DistributionRequest> Query()=>context.DistributionRequests.AsNoTracking().Where(x=>x.IsActive!=false)
        .Include(x=>x.User).Include(x=>x.Warehouse).Include(x=>x.WarehouseIssuedByStaff).Include(x=>x.Items).ThenInclude(x=>x.Inventory)
        .Include(x=>x.ShipmentHistory).OrderByDescending(x=>x.RequestedAt);
    private static System.Linq.Expressions.Expression<Func<DistributionRequest,DistributionRequestViewDto>> Map()=>x=>new DistributionRequestViewDto(x.Id,x.RequestCode,x.UserId,x.User.FullName,x.WarehouseId,x.Warehouse.WarehouseName,x.RecipientName,x.RecipientPhone,x.ToAddress,x.Status,x.RequestNotes,x.RejectReason,x.RequestedAt,x.ApprovedAt,x.IssueSlipCode,x.WarehouseIssuedAt,x.WarehouseIssuedByStaff!=null?x.WarehouseIssuedByStaff.FullName:null,x.GhnOrderCode,x.GhnStatus,x.GhnUpdatedAt,x.Items.Select(i=>new DistributionItemViewDto(i.Id,i.InventoryId,i.Inventory.ClassifiedBatch!.BatchCode,i.Inventory.Sku,i.Inventory.ClothingType,i.Inventory.FabricType,i.Inventory.Gender,i.Inventory.TargetUser,i.Inventory.Size,i.RequestedQuantity,i.ApprovedQuantity,i.IssuedQuantity,i.RequestedWeight,i.IssuedWeight)).ToList(),x.ShipmentHistory.OrderByDescending(h=>h.OccurredAt).Select(h=>new ShipmentEventDto(h.Status,h.Description,h.Source,h.OccurredAt)).ToList());
    private static string BuildRequestCode(Guid id)=>$"DIST-{id.ToString("N")[..8].ToUpperInvariant()}";
    private static string Grade(int value)=>value==1?"A":value==2?"B":"C";
    private static void ValidateRequest(CreateDistributionRequestDto dto)
    {
        if (dto.WarehouseId == Guid.Empty) throw new InvalidOperationException("Warehouse is required.");
        if (string.IsNullOrWhiteSpace(dto.RecipientName)) throw new InvalidOperationException("Recipient name is required.");
        if (string.IsNullOrWhiteSpace(dto.RecipientPhone)) throw new InvalidOperationException("Recipient phone is required.");
        if (string.IsNullOrWhiteSpace(dto.ToAddress)) throw new InvalidOperationException("Delivery address is required.");
        if (string.IsNullOrWhiteSpace(dto.Notes)) throw new InvalidOperationException("Purpose and notes are required.");
        var phone = System.Text.RegularExpressions.Regex.Replace(dto.RecipientPhone, @"[\s.\-()]", "");
        if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:\+84|0)(?:3|5|7|8|9)\d{8}$"))
            throw new InvalidOperationException("Recipient phone is invalid.");
    }

    private static string GetProcessingDirection(string roleName)
    {
        return roleName switch
        {
            "RecyclingOrganization" => "Recycling",
            "DisposalOrganization" => "Disposal",

            _ => throw new InvalidOperationException(
                "The selected user is not a recycling or disposal organization.")
        };
    }
}
