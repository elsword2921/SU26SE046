using System.Data;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json;
using BLL.DTOs;
using BLL.Services.Implements.Notifications;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ProcessingOperations;

public partial class ProcessingOperationsService
{
    public async Task CreateGhnShipmentAsync(Guid staffId, Guid operationId, CreateProcessingGhnShipmentDto dto)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var staff = await RequireUserAsync(staffId, "WarehouseStaff");
        var operation = await context.ProcessingOperations.Include(x => x.Organization)
            .Include(x => x.Inputs).ThenInclude(x => x.Inventory)
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu xử lý.");
        if (staff.WarehouseId != operation.WarehouseId)
            throw new AuthenticationException("Chỉ nhân viên thuộc kho xuất mới được tạo vận đơn.");
        // Idempotent even after the operation has advanced past the booking step.
        if (!string.IsNullOrWhiteSpace(operation.GhnOrderCode)) return;
        if (operation.Status is not ("ReadyForGhn" or "Issued") || operation.IssuedAt is null)
            throw new InvalidOperationException("Cần lập phiếu xuất kho trước khi tạo vận đơn GHN.");
        ValidateGhnForm(dto, operation.Organization);
        var inputs = operation.Inputs.Where(x => x.IsActive != false).ToList();
        if (inputs.Count == 0 || inputs.Any(x => x.IssuedWeight <= 0))
            throw new InvalidOperationException("Phiếu xuất chưa có khối lượng hợp lệ.");
        var grams = decimal.Ceiling(inputs.Sum(x => x.IssuedWeight) * 1000);
        if (grams > int.MaxValue) throw new InvalidOperationException("Khối lượng vượt giới hạn tạo vận đơn.");
        var weight = (int)grams;
        var serviceType = weight >= 20000 ? 5 : dto.ServiceTypeId;
        var packages = new List<object>();
        foreach (var input in inputs)
        {
            var remaining = (int)decimal.Ceiling(input.IssuedWeight * 1000);
            var index = 1;
            do
            {
                var parcelWeight = serviceType == 5 ? Math.Min(remaining, 30000) : remaining;
                packages.Add(new
                {
                    name = $"{operation.OperationType} - {input.Inventory.Sku} - kiện {index}",
                    code = $"{input.InventoryId:N}-{index}", quantity = 1, price = 0,
                    weight = parcelWeight, length = dto.Length, width = dto.Width, height = dto.Height
                });
                remaining -= parcelWeight;
                index++;
            } while (remaining > 0);
        }
        var payload = new
        {
            // GHN reuses the existing order for this stable code, including after a network timeout
            // or a database rollback. Never use a random value per attempt.
            client_order_code = $"PROC-{operation.Id:N}",
            payment_type_id = dto.PaymentTypeId, service_type_id = serviceType,
            required_note = dto.RequiredNote, cod_amount = 0,
            from_name = dto.FromName.Trim(), from_phone = dto.FromPhone.Trim(), from_address = dto.FromAddress.Trim(),
            from_district_id = dto.FromDistrictId, from_ward_code = dto.FromWardCode.Trim(),
            from_province_name = dto.FromProvinceName.Trim(), from_district_name = dto.FromDistrictName.Trim(), from_ward_name = dto.FromWardName.Trim(),
            to_name = operation.Organization.FullName.Trim(), to_phone = operation.Organization.PhoneNumber.Trim(),
            to_address = operation.Organization.Address.Trim(), to_district_id = dto.ToDistrictId, to_ward_code = dto.ToWardCode.Trim(),
            to_province_name = dto.ToProvinceName.Trim(), to_district_name = dto.ToDistrictName.Trim(), to_ward_name = dto.ToWardName.Trim(),
            content = $"Hàng { (operation.OperationType == "Recycling" ? "tái chế" : "tiêu hủy") } - {operation.OperationCode}",
            weight, length = dto.Length, width = dto.Width, height = dto.Height, items = packages
        };
        var data = await SendGhnAsync("v2/shipping-order/create", payload);
        var orderCode = data.TryGetProperty("order_code", out var order) ? order.GetString() : null;
        if (string.IsNullOrWhiteSpace(orderCode)) throw new InvalidOperationException("GHN chưa trả mã vận đơn. Vui lòng thử lại để đối soát cùng mã yêu cầu.");
        operation.GhnOrderCode = orderCode;
        operation.TrackingCode = orderCode;
        operation.CarrierName = "Giao Hàng Nhanh";
        operation.Status = "GhnBooked";
        SetGhnStatus(operation, data.TryGetProperty("status", out var status) ? status.GetString() ?? "ready_to_pick" : "ready_to_pick",
            "GHN đã tiếp nhận yêu cầu đến kho lấy hàng.", staffId);
        NotificationWriter.NotifyUser(context, operation.OrganizationId, "ProcessingGhnBooked", "Đã tạo vận đơn GHN",
            $"{operation.OperationCode}: vận đơn {orderCode}. GHN sẽ đến kho lấy hàng giao tới tổ chức.",
            $"/organization/processing-operations/{operation.Id}", staffId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task RefreshGhnAsync(Guid userId, Guid operationId)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await RequireUserAsync(userId, "Manager", "WarehouseStaff", "RecyclingOrganization", "DisposalOrganization");
        var operation = await context.ProcessingOperations.FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu xử lý.");
        if ((user.Role.RoleName == "WarehouseStaff" && user.WarehouseId != operation.WarehouseId)
            || (user.Role.RoleName is "RecyclingOrganization" or "DisposalOrganization" && operation.OrganizationId != userId))
            throw new AuthenticationException("Bạn không có quyền xem vận đơn này.");
        if (string.IsNullOrWhiteSpace(operation.GhnOrderCode)) throw new InvalidOperationException("Yêu cầu chưa có vận đơn GHN.");
        var data = await SendGhnAsync("v2/shipping-order/detail", new { order_code = operation.GhnOrderCode });
        if (!data.TryGetProperty("status", out var status) || string.IsNullOrWhiteSpace(status.GetString()))
            throw new InvalidOperationException("GHN chưa trả trạng thái vận đơn hợp lệ.");
        SetGhnStatus(operation, status.GetString()!, "Cập nhật trạng thái từ GHN.", userId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private void SetGhnStatus(ProcessingOperation operation, string status, string description, Guid actorId)
    {
        if (operation.GhnStatus != status)
            context.ProcessingShipmentEvents.Add(new()
            {
                Id = Guid.NewGuid(), ProcessingOperationId = operation.Id, Status = status, Description = description,
                OccurredAt = DateTime.UtcNow, CreateAt = DateTime.UtcNow, CreatedBy = actorId, IsActive = true
            });
        operation.GhnStatus = status;
        operation.GhnUpdatedAt = DateTime.UtcNow;
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = actorId;
        // Refreshing carrier tracking must not undo the organization's receipt/completion.
        if (operation.OrganizationReceivedAt.HasValue || operation.Status is "OrganizationReceived" or "Completed") return;
        operation.Status = status switch
        {
            "delivered" => "Delivered",
            "cancel" => "ShipmentCancelled",
            "returned" => "Returned",
            "delivery_fail" or "return" or "return_transporting" or "return_sorting" or "returning" or "return_fail" or "exception" or "damage" or "lost" => "DeliveryException",
            "ready_to_pick" or "picking" or "money_collect_picking" => "GhnBooked",
            _ => "InTransit"
        };
    }

    private async Task<JsonElement> SendGhnAsync(string path, object payload)
    {
        var token = configuration["Ghn:Token"] ?? configuration["GHN:Key"];
        var shopId = configuration["Ghn:ShopId"];
        if (string.IsNullOrWhiteSpace(token) || !int.TryParse(shopId, out var shop) || shop <= 0)
            throw new InvalidOperationException("Máy chủ chưa cấu hình GHN Token và ShopId hợp lệ.");
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.TryAddWithoutValidation("Token", token);
        request.Headers.TryAddWithoutValidation("ShopId", shopId);
        try
        {
            using var response = await ghnClient.SendAsync(request);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            if (!response.IsSuccessStatusCode || !root.TryGetProperty("code", out var code) || code.GetInt32() != 200
                || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                var message = root.TryGetProperty("message_display", out var display) ? display.GetString()
                    : root.TryGetProperty("message", out var error) ? error.GetString() : "Phản hồi không hợp lệ.";
                throw new InvalidOperationException($"GHN: {message}");
            }
            return data.Clone();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Chưa xác nhận được kết quả từ GHN. Hãy thử lại; hệ thống dùng cùng mã yêu cầu để tránh tạo trùng vận đơn.", ex);
        }
    }

    private static void ValidateGhnForm(CreateProcessingGhnShipmentDto dto, User recipient)
    {
        if (dto.PaymentTypeId is not (1 or 2) || dto.ServiceTypeId is not (2 or 5))
            throw new InvalidOperationException("Bên thanh toán hoặc dịch vụ GHN không hợp lệ.");
        if (dto.RequiredNote is not ("KHONGCHOXEMHANG" or "CHOXEMHANGKHONGTHU" or "CHOTHUHANG"))
            throw new InvalidOperationException("Yêu cầu giao hàng không hợp lệ.");
        if (dto.Length is < 1 or > 200 || dto.Width is < 1 or > 200 || dto.Height is < 1 or > 200)
            throw new InvalidOperationException("Kích thước mỗi chiều phải từ 1 đến 200 cm.");
        if (dto.FromDistrictId <= 0 || dto.ToDistrictId <= 0 || new[] { dto.FromName, dto.FromPhone, dto.FromAddress,
                dto.FromWardCode, dto.ToWardCode, dto.FromProvinceName, dto.FromDistrictName, dto.FromWardName,
                dto.ToProvinceName, dto.ToDistrictName, dto.ToWardName }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Vui lòng nhập đủ liên hệ lấy hàng và chọn địa chỉ GHN của hai bên.");
        if (new[] { recipient.FullName, recipient.PhoneNumber, recipient.Address }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Tổ chức tiếp nhận chưa có đủ tên, điện thoại và địa chỉ. Vui lòng cập nhật hồ sơ tổ chức.");
    }
}
