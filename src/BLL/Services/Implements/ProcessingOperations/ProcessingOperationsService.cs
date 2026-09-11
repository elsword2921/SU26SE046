using System.Data;
using System.Security.Authentication;
using BLL.DTOs;
using BLL.Services.Implements.Notifications;
using BLL.Services.Interfaces.ProcessingOperations;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BLL.Services.Implements.ProcessingOperations;

public partial class ProcessingOperationsService(AppDbContext context, HttpClient ghnClient, IConfiguration configuration)
    : IProcessingOperationsService
{
    public async Task<ProcessingOperationCreatedDto> CreateAsync(Guid userId,CreateProcessingOperationDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await RequireUserAsync(userId, "Manager");
        ValidateOperationType(dto.OperationType);
        if (dto.Inputs is null || dto.Inputs.Count == 0)
            throw new InvalidOperationException(
                "Select at least one inventory item.");
        if (dto.Inputs.Any(x => x is null)) throw new InvalidOperationException("Danh sách batch không hợp lệ.");
        var inputIds = dto.Inputs
            .Select(x => x.InventoryId)
            .Distinct()
            .ToList();
        if (inputIds.Count != dto.Inputs.Count)
            throw new InvalidOperationException(
                "An inventory item can only appear once.");
        var warehouse = await context.Warehouses
            .FirstOrDefaultAsync(x => x.Id == dto.WarehouseId && x.IsActive != false);
        if (warehouse is null)
            throw new KeyNotFoundException("Warehouse not found.");
        var organization = await context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x =>x.Id == dto.OrganizationId && x.IsActive != false);
        if (organization is null)
            throw new KeyNotFoundException(
                "Processing organization not found.");
        if (organization.Role.RoleName != "RecyclingOrganization"
            && organization.Role.RoleName != "DisposalOrganization")
        {
            throw new InvalidOperationException(
                "Selected organization is not a valid processing organization.");
        }
        if (dto.OperationType == "Recycling"
            && organization.Role.RoleName != "RecyclingOrganization")
        {
            throw new InvalidOperationException(
                "Recycling operations must use a RecyclingOrganization.");
        }
        if (dto.OperationType == "Disposal"
            && organization.Role.RoleName != "DisposalOrganization")
        {
            throw new InvalidOperationException(
                "Disposal operations must use a DisposalOrganization.");
        }
        var inventories = await context.Inventories
            .Include(x => x.ClassifiedBatch)
            .Where(x => inputIds.Contains(x.Id) && x.IsActive != false && x.WarehouseId == dto.WarehouseId)
            .ToListAsync();
        if (inventories.Count != inputIds.Count)
        {
            throw new InvalidOperationException(
                "One or more inventory items are unavailable or belong to another warehouse.");
        }
        foreach (var input in dto.Inputs)
        {
            var inventory = inventories.Single(x => x.Id == input.InventoryId);
            ValidateInventory(inventory, dto.WarehouseId, dto.OperationType);
            if (inventory.ReservedQuantity != 0 || inventory.ReservedWeight != 0)
                throw new InvalidOperationException("Batch đang được giữ cho yêu cầu khác.");
            if ((input.RequestedQuantity.HasValue && input.RequestedQuantity != inventory.Quantity)
                || (input.RequestedWeight.HasValue && input.RequestedWeight != inventory.TotalWeight))
                throw new InvalidOperationException("Yêu cầu xử lý phải chọn nguyên batch theo tồn kho hiện tại. Vui lòng tải lại danh sách.");
        }
        var lockedInventoryIds = await context.ProcessingOperationInputs
            .Where(input =>
                input.IsActive != false &&
                inputIds.Contains(input.InventoryId) &&
                input.ProcessingOperation.IsActive != false &&
                (input.ProcessingOperation.Status == "PendingOrganizationApproval" ||
                 input.ProcessingOperation.Status == "PendingManagerApproval" ||
                 input.ProcessingOperation.Status == "Approved"))
            .Select(input => input.InventoryId)
            .Distinct()
            .ToListAsync();
        if (lockedInventoryIds.Count > 0)
        {
            throw new InvalidOperationException(
                "One or more inventory items are already assigned to another active processing operation.");
        }
        var operationId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var operation = new ProcessingOperation
        {
            Id = operationId, OperationCode = BuildOperationCode(operationId), OperationType = dto.OperationType, Status = "PendingOrganizationApproval",
            WarehouseId = dto.WarehouseId, OrganizationId = dto.OrganizationId, CreatedByUserId = userId, RequestedAt = now, RequestNotes = dto.RequestNotes?.Trim(),
            CreateAt = now, CreatedBy = userId, IsActive = true
        };
        foreach (var input in dto.Inputs)
        {
            var inventory = inventories.Single(x =>
                x.Id == input.InventoryId);
            operation.Inputs.Add(new ProcessingOperationInput
            {
                Id = Guid.NewGuid(), ProcessingOperationId = operationId, InventoryId = inventory.Id, ClassifiedBatchId = inventory.ClassifiedBatchId,
                RequestedQuantity = inventory.Quantity, RequestedWeight = inventory.TotalWeight, IssuedQuantity = 0,
                IssuedWeight = 0, CreateAt = now, CreatedBy = userId, IsActive = true
            });
        }
        context.ProcessingOperations.Add(operation);
        NotificationWriter.NotifyUser(context,organization.Id,"ProcessingOperationCreated","Có yêu cầu xử lý mới",
            $"Yêu cầu {operation.OperationCode} vừa được gửi đến tổ chức để xem xét.",
            $"/organization/processing-operations/{operation.Id}",userId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return new ProcessingOperationCreatedDto(
            operation.Id,
            operation.OperationCode,
            operation.OperationType,
            operation.Status);
    }

    public async Task ApproveByOrganizationAsync(Guid organizationId,Guid operationId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await RequireUserAsync(organizationId, "RecyclingOrganization", "DisposalOrganization");
        var operation = await context.ProcessingOperations
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false);
        if (operation is null)
            throw new KeyNotFoundException(
                "Processing operation not found.");
        if (operation.OrganizationId != organizationId)
            throw new InvalidOperationException(
                "You are not the organization assigned to this operation.");
        if (operation.Status != "PendingOrganizationApproval")
            throw new InvalidOperationException(
                "This processing operation is not awaiting organization approval.");
        operation.Status = "PendingManagerApproval";
        operation.ApprovedByOrganizationId = organizationId;
        operation.OrganizationRespondedAt = DateTime.UtcNow;
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = organizationId;
        await NotifyManagersAsync(context,operation,"ProcessingOperationOrganizationApproved","Yêu cầu xử lý chờ Manager duyệt",
            $"Tổ chức {operation.Organization.FullName} đã chấp thuận yêu cầu {operation.OperationCode}. Yêu cầu đang chờ Manager phê duyệt.",organizationId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RejectByOrganizationAsync(Guid organizationId,Guid operationId,ProcessingOperationDecisionDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await RequireUserAsync(organizationId, "RecyclingOrganization", "DisposalOrganization");
        var operation = await context.ProcessingOperations
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false);
        if (operation is null)
            throw new KeyNotFoundException(
                "Processing operation not found.");
        if (operation.OrganizationId != organizationId)
            throw new InvalidOperationException(
                "You are not the organization assigned to this operation.");
        if (operation.Status != "PendingOrganizationApproval")
            throw new InvalidOperationException(
                "This processing operation is not awaiting organization approval.");
        if (string.IsNullOrWhiteSpace(dto.RejectionReason))
            throw new InvalidOperationException(
                "Rejection reason is required.");
        operation.Status = "RejectedByOrganization";
        operation.OrganizationRespondedAt = DateTime.UtcNow;
        operation.OrganizationRejectionReason = dto.RejectionReason.Trim();
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = organizationId;
        await NotifyManagersAsync(context,operation,"ProcessingOperationOrganizationRejected","Yêu cầu xử lý bị từ chối",
            $"Yêu cầu {operation.OperationCode} đã bị tổ chức từ chối. Lý do: {operation.OrganizationRejectionReason}",organizationId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ApproveByManagerAsync(Guid managerId,Guid operationId)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var operation = await context.ProcessingOperations
            .Include(x => x.Inputs)
                .ThenInclude(x => x.Inventory)
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false);
        if (operation is null)
            throw new KeyNotFoundException(
                "Processing operation not found.");
        if (operation.Status != "PendingManagerApproval")
            throw new InvalidOperationException(
                "This processing operation is not awaiting manager approval.");
        var manager = await context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == managerId && x.IsActive != false);
        if (manager is null)
            throw new KeyNotFoundException("Manager not found.");
        if (manager.Role.RoleName != "Manager")
            throw new AuthenticationException(
                "Only a manager can approve a processing operation.");
        if (!operation.Inputs.Any(x => x.IsActive != false))
            throw new InvalidOperationException("Yêu cầu không có batch để duyệt.");
        foreach (var input in operation.Inputs
            .Where(x => x.IsActive != false))
        {
            var inventory = input.Inventory;
            ValidateInventory(inventory, operation.WarehouseId, operation.OperationType);
            if (input.RequestedQuantity != inventory.Quantity || input.RequestedWeight != inventory.TotalWeight)
                throw new InvalidOperationException("Tồn kho đã thay đổi. Hãy hủy và tạo lại yêu cầu cho nguyên batch.");
            var availableQuantity = inventory.Quantity - inventory.ReservedQuantity;
            var availableWeight = inventory.TotalWeight - inventory.ReservedWeight;
            if (input.RequestedQuantity > availableQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient available quantity for {inventory.Sku}.");
            }
            if (input.RequestedWeight > availableWeight)
            {
                throw new InvalidOperationException(
                    $"Insufficient available weight for {inventory.Sku}.");
            }
            inventory.ReservedQuantity += input.RequestedQuantity;
            inventory.ReservedWeight += input.RequestedWeight;
        }
        var now = DateTime.UtcNow;
        operation.Status = "Approved";
        operation.ApprovedByManagerId = managerId;
        operation.ManagerRespondedAt = now;
        operation.ApprovedAt = now;
        operation.UpdateAt = now;
        operation.UpdatedBy = managerId;
        NotificationWriter.NotifyUser(context,operation.OrganizationId,"ProcessingOperationApproved","Yêu cầu xử lý đã được duyệt",
            $"Yêu cầu {operation.OperationCode} đã được Manager phê duyệt. Kho đang chuẩn bị xuất hàng.",
            $"/organization/processing-operations/{operation.Id}",managerId);
        await NotifyWarehouseStaffAsync(context,operation,"ProcessingOperationApproved","Có yêu cầu xuất kho xử lý mới",
            $"Yêu cầu {operation.OperationCode} đã được duyệt và đang chờ kho xuất hàng.",managerId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RejectByManagerAsync(Guid managerId,Guid operationId,ProcessingOperationDecisionDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await RequireUserAsync(managerId, "Manager");
        var operation = await context.ProcessingOperations
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false);
        if (operation is null)
            throw new KeyNotFoundException(
                "Processing operation not found.");
        if (operation.Status != "PendingManagerApproval")
            throw new InvalidOperationException(
                "This processing operation is not awaiting manager approval.");
        if (string.IsNullOrWhiteSpace(dto.RejectionReason))
            throw new InvalidOperationException(
                "Rejection reason is required.");
        operation.Status = "RejectedByManager";
        operation.RejectedByManagerId = managerId;
        operation.RejectedAt = DateTime.UtcNow;
        operation.ManagerRespondedAt = DateTime.UtcNow;
        operation.ManagerRejectionReason = dto.RejectionReason.Trim();
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = managerId;
        NotificationWriter.NotifyUser(context,operation.OrganizationId,"ProcessingOperationManagerRejected","Yêu cầu xử lý bị từ chối",
            $"Yêu cầu {operation.OperationCode} đã bị Manager từ chối. Lý do: {operation.ManagerRejectionReason}",
            $"/organization/processing-operations/{operation.Id}",managerId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<List<ProcessingOperationListDto>> GetListAsync(Guid userId,string? status)
    {
        var user = await context.Users
            .Include(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive != false);
        if (user is null)
            throw new KeyNotFoundException("User not found.");
        var roleName = user.Role.RoleName;
        if (roleName != "Manager" && roleName != "RecyclingOrganization" && roleName != "DisposalOrganization" && roleName != "WarehouseStaff")
        {
            throw new InvalidOperationException(
                "You are not allowed to view processing operations.");
        }
        var query = context.ProcessingOperations
            .AsNoTracking()
            .Where(x => x.IsActive != false);
        if (roleName == "RecyclingOrganization" || roleName == "DisposalOrganization")
        {
            query = query.Where(x => x.OrganizationId == userId);
        }
        if (roleName == "WarehouseStaff")
            query = query.Where(x => x.WarehouseId == user.WarehouseId && (x.Status == "Approved" || x.IssuedAt != null));
        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim();
            query = query.Where(x => x.Status == status);
        }
        return await query
            .OrderByDescending(x => x.RequestedAt)
            .Select(x => new ProcessingOperationListDto(x.Id,x.OperationCode,x.OperationType,x.Status,x.WarehouseId,x.Warehouse.WarehouseName,x.OrganizationId,
                x.Organization.FullName,x.RequestedAt,x.Inputs.Count(i => i.IsActive != false),x.Inputs.Where(i => i.IsActive != false).Sum(i => i.RequestedQuantity),
                x.Inputs.Where(i => i.IsActive != false).Sum(i => i.RequestedWeight)
            ))
            .ToListAsync();
    }

    public async Task<ProcessingOperationDetailDto> GetByIdAsync(Guid userId,Guid operationId)
    {
        var user = await context.Users
            .Include(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive != false);
        if (user is null)
            throw new KeyNotFoundException("User not found.");
        var roleName = user.Role.RoleName;
        if (roleName != "Manager" && roleName != "RecyclingOrganization" && roleName != "DisposalOrganization" && roleName != "WarehouseStaff")
        {
            throw new InvalidOperationException(
                "You are not allowed to view processing operations.");
        }
        var operation = await context.ProcessingOperations
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Organization)
            .Include(x => x.Inputs)
                .ThenInclude(x => x.Inventory)
            .Include(x => x.Inputs)
                .ThenInclude(x => x.ClassifiedBatch)
            .Include(x => x.Outputs)
            .Include(x => x.ShipmentHistory)
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false);
        if (operation is null)
            throw new KeyNotFoundException(
                "Processing operation not found.");
        if ((roleName == "RecyclingOrganization" || roleName == "DisposalOrganization") && operation.OrganizationId != userId)
        {
            throw new InvalidOperationException(
                "You are not the organization assigned to this operation.");
        }
        if (roleName == "WarehouseStaff" && user.WarehouseId != operation.WarehouseId)
            throw new AuthenticationException("Yêu cầu thuộc kho khác.");
        return new ProcessingOperationDetailDto(operation.Id,operation.OperationCode,operation.OperationType,operation.Status,operation.WarehouseId,
            operation.Warehouse.WarehouseName,operation.OrganizationId,operation.Organization.FullName,operation.CreatedByUserId,operation.ApprovedByOrganizationId,
            operation.ApprovedByManagerId,operation.IssuedByStaffId,operation.RequestedAt,operation.OrganizationRespondedAt,operation.ManagerRespondedAt,
            operation.ApprovedAt,operation.IssuedAt,operation.OrganizationReceivedAt,operation.ProcessingStartedAt,operation.ProcessingCompletedAt,
            operation.OutputReturnedAt,operation.TrackingCode,operation.CarrierName,operation.RequestNotes,operation.OrganizationRejectionReason,
            operation.ManagerRejectionReason,operation.CompletionNotes,
            operation.Inputs.Where(x => x.IsActive != false).Select(x => new ProcessingOperationInputDetailDto(x.Id,x.InventoryId,x.Inventory.Sku,
                    x.ClassifiedBatchId,x.ClassifiedBatch != null ? x.ClassifiedBatch.BatchCode : null,x.RequestedQuantity,x.RequestedWeight,
                    x.IssuedQuantity,x.IssuedWeight,x.Notes)).ToList(),
            operation.Outputs.Where(x => x.IsActive != false).Select(x => new ProcessingOperationOutputDetailDto(x.Id,x.OutputType,x.Quantity,x.Weight,
                    x.ReturnedQuantity,x.ReturnedWeight,x.RecordedByStaffId,x.RecordedAt,x.Notes)).ToList()
        )
        {
            RecyclingReturn = operation.OperationType == "Recycling" ? await GetReturnDetailAsync(operation) : null,
            Shipping = new ProcessingShippingDto(operation.Warehouse.Address, operation.Warehouse.PhoneNumber,
                operation.Organization.PhoneNumber, operation.Organization.Address, operation.GhnOrderCode,
                operation.GhnStatus, operation.GhnUpdatedAt, operation.ShipmentHistory
                    .OrderByDescending(x => x.OccurredAt)
                    .Select(x => new ShipmentEventDto(x.Status, x.Description, "GHN", x.OccurredAt)).ToList())
        };
    }

    public async Task IssueAsync(Guid staffId,Guid operationId,IssueProcessingOperationDto dto)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var operation = await context.ProcessingOperations.Include(x => x.Warehouse).Include(x => x.Inputs).ThenInclude(x => x.Inventory).ThenInclude(x => x.StorageLocation)!.ThenInclude(x => x!.Area)
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false);
        if (operation is null)
            throw new KeyNotFoundException(
                "Processing operation not found.");
        if (operation.Status != "Approved")
            throw new InvalidOperationException(
                "Only approved processing operations can be issued.");
        var staff = await context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == staffId && x.IsActive != false);
        if (staff is null)
            throw new KeyNotFoundException("Staff not found.");
        if (staff.Role.RoleName != "WarehouseStaff")
            throw new AuthenticationException(
                "Only warehouse staff can issue processing operations.");
        if (staff.WarehouseId != operation.WarehouseId)
            throw new AuthenticationException(
                "This processing operation belongs to another warehouse.");
        var inputs = operation.Inputs.Where(x => x.IsActive != false).ToList();
        if (inputs.Count == 0)
            throw new InvalidOperationException(
                "Processing operation has no inputs.");
        var now = DateTime.UtcNow;
        var inventoryTransaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            WarehouseId = operation.WarehouseId,
            TransactionCode =
                $"TX-OUT-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"
                    .ToUpperInvariant()[..30],
            TransactionType = "OUT",
            ReferenceType = "ProcessingOperation",
            ReferenceId = operation.Id,
            Status = "Posted",
            Notes = dto.Notes?.Trim(),
            PerformedByStaffId = staffId,
            PerformedAt = now,
            CreateAt = now,
            IsActive = true
        };
        foreach (var input in inputs)
        {
            var inventory = input.Inventory;
            var issueQuantity = input.RequestedQuantity;
            var issueWeight = input.RequestedWeight;
            ValidateInventory(inventory, operation.WarehouseId, operation.OperationType);
            if (issueQuantity != inventory.Quantity || issueWeight != inventory.TotalWeight)
                throw new InvalidOperationException("Tồn kho không khớp nguyên batch đã duyệt. Vui lòng kiểm tra lại yêu cầu.");
            if (issueQuantity < 0)
            {
                throw new InvalidOperationException(
                    $"Invalid requested quantity for {inventory.Sku}.");
            }
            if (issueWeight <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid requested weight for {inventory.Sku}.");
            }
            if (issueQuantity > inventory.ReservedQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient reserved quantity for {inventory.Sku}.");
            }
            if (issueWeight > inventory.ReservedWeight)
            {
                throw new InvalidOperationException(
                    $"Insufficient reserved weight for {inventory.Sku}.");
            }
            if (issueQuantity > inventory.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient inventory quantity for {inventory.Sku}.");
            }
            if (issueWeight > inventory.TotalWeight)
            {
                throw new InvalidOperationException(
                    $"Insufficient inventory weight for {inventory.Sku}.");
            }
            var quantityBefore = inventory.Quantity;
            var weightBefore = inventory.TotalWeight;
            inventory.Quantity -= issueQuantity;
            inventory.ReservedQuantity -= issueQuantity;
            inventory.TotalWeight -= issueWeight;
            inventory.ReservedWeight -= issueWeight;
            inventory.Status = inventory.Quantity <= 0 || inventory.TotalWeight <= 0 ? "Depleted" : "Available";
            if (inventory.StorageLocation != null)
            {
                inventory.StorageLocation.CurrentWeightKg = Math.Max(0,inventory.StorageLocation.CurrentWeightKg - issueWeight);
                inventory.StorageLocation.Area.CurrentKg = Math.Max(0,inventory.StorageLocation.Area.CurrentKg - issueWeight);
            }
            operation.Warehouse.CurrentWeight =
                Math.Max(0,operation.Warehouse.CurrentWeight - issueWeight);
            input.IssuedQuantity = issueQuantity;
            input.IssuedWeight = issueWeight;
            inventoryTransaction.Items.Add(new TransactionItem
            {
                Id = Guid.NewGuid(),
                InventoryId = inventory.Id,
                ClassifiedBatchId = inventory.ClassifiedBatchId,
                Quantity = issueQuantity,
                Weight = issueWeight,
                QuantityBefore = quantityBefore,
                QuantityAfter = inventory.Quantity,
                WeightBefore = weightBefore,
                WeightAfter = inventory.TotalWeight,
                SourceLocationId = inventory.StorageLocationId,
                CreateAt = now,
                IsActive = true
            });
        }
        context.InventoryTransactions.Add(inventoryTransaction);
        operation.Status = "ReadyForGhn";
        operation.IssuedByStaffId = staffId;
        operation.IssuedAt = now;
        operation.UpdateAt = now;
        operation.UpdatedBy = staffId;
        NotificationWriter.NotifyUser(context,operation.OrganizationId,"ProcessingOperationIssued","Kho đã xuất hàng xử lý",
            $"Yêu cầu {operation.OperationCode} đã lập phiếu xuất, đang chờ kho tạo vận đơn GHN đến lấy hàng.",
            $"/organization/processing-operations/{operation.Id}",staffId);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static void ValidateOperationType(string operationType)
    {
        if (string.IsNullOrWhiteSpace(operationType))
            throw new InvalidOperationException(
                "Operation type is required.");
        if (operationType is not ("Recycling" or "Disposal"))
            throw new InvalidOperationException(
                "Operation type must be Recycling or Disposal.");
    }

    private static string BuildOperationCode(Guid id) =>
        $"PROC-{id.ToString("N")[..8].ToUpperInvariant()}";

    private static async Task<List<Guid>> GetManagerIdsAsync(AppDbContext context)
    {
        return await context.Users
            .AsNoTracking()
            .Where(x =>
                x.IsActive != false &&
                x.Role.RoleName == "Manager")
            .Select(x => x.Id)
            .ToListAsync();
    }

    private static async Task NotifyManagersAsync(AppDbContext context,ProcessingOperation operation,string type,string title,string message,Guid? actorId = null)
    {
        var managerIds = await GetManagerIdsAsync(context);
        foreach (var managerId in managerIds)
        {
            NotificationWriter.NotifyUser(context,managerId,type,title,message,$"/manager/processing-operations/{operation.Id}",actorId);
        }
    }

    private static async Task NotifyWarehouseStaffAsync(AppDbContext context,ProcessingOperation operation,string type,string title,string message,Guid? actorId = null)
    {
        var staffIds = await context.Users
            .AsNoTracking()
            .Where(x =>x.IsActive != false && x.WarehouseId == operation.WarehouseId && x.Role.RoleName == "WarehouseStaff")
            .Select(x => x.Id)
            .ToListAsync();
        foreach (var staffId in staffIds)
        {
            NotificationWriter.NotifyUser(context,staffId,type,title,message,$"/warehouse/processing-operations/{operation.Id}",actorId);
        }
    }
}
