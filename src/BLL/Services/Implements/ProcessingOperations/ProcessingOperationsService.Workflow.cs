using System.Data;
using System.Security.Authentication;
using BLL.DTOs;
using BLL.Services.Implements.Notifications;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ProcessingOperations;

public partial class ProcessingOperationsService
{
    private async Task<User> RequireUserAsync(Guid id, params string[] roles)
    {
        var user = await context.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false);
        if (user is null || !roles.Contains(user.Role.RoleName))
            throw new AuthenticationException("Bạn không có quyền thực hiện thao tác này.");
        return user;
    }

    private static void ValidateInventory(Inventory inventory, Guid warehouseId, string operationType)
    {
        var expectedGrade = operationType == "Recycling" ? 2 : 3;
        if (inventory.IsActive == false || inventory.Status != "Available"
            || inventory.WarehouseId != warehouseId || inventory.ProcessingDirection != operationType
            || inventory.ConditionRating != expectedGrade || inventory.TotalWeight <= 0 || inventory.Quantity < 0)
            throw new InvalidOperationException($"Batch {inventory.Sku} không còn hợp lệ cho hướng xử lý này. Tái chế chỉ nhận nhãn B, tiêu hủy chỉ nhận nhãn C.");
    }

    public async Task<ProcessingCatalogDto> CatalogAsync(Guid userId, Guid? warehouseId, string? operationType)
    {
        await RequireUserAsync(userId, "Manager");
        if (!string.IsNullOrWhiteSpace(operationType)) ValidateOperationType(operationType);
        var warehouses = await context.Warehouses.AsNoTracking().Where(x => x.IsActive != false)
            .OrderBy(x => x.WarehouseName).Select(x => new ProcessingWarehouseDto(x.Id, x.WarehouseName)).ToListAsync();
        var organizations = await context.Users.AsNoTracking().Where(x => x.IsActive != false
            && (x.Role.RoleName == "RecyclingOrganization" || x.Role.RoleName == "DisposalOrganization"))
            .OrderBy(x => x.FullName).Select(x => new ProcessingOrganizationDto(x.Id, x.FullName,
                x.Role.RoleName == "RecyclingOrganization" ? "Recycling" : "Disposal")).ToListAsync();
        var query = context.Inventories.AsNoTracking().Where(x => x.IsActive != false
            && x.Warehouse.IsActive != false && x.Status == "Available" && x.TotalWeight > 0
            && ((x.ProcessingDirection == "Recycling" && x.ConditionRating == 2)
                || (x.ProcessingDirection == "Disposal" && x.ConditionRating == 3)));
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId);
        if (!string.IsNullOrWhiteSpace(operationType)) query = query.Where(x => x.ProcessingDirection == operationType);
        var rows = await query.OrderBy(x => x.Sku).Select(x => new
        {
            x.Id, x.Sku, BatchCode = x.ClassifiedBatch == null ? null : x.ClassifiedBatch.BatchCode,
            x.ProcessingDirection, x.ConditionRating, x.Quantity, x.TotalWeight,
            LocationCode = x.StorageLocation == null ? null : x.StorageLocation.LocationCode,
            IsLocked = x.ReservedQuantity != 0 || x.ReservedWeight != 0 || context.ProcessingOperationInputs.Any(i =>
                i.InventoryId == x.Id && i.IsActive != false && i.ProcessingOperation.IsActive != false
                && (i.ProcessingOperation.Status == "PendingOrganizationApproval"
                    || i.ProcessingOperation.Status == "PendingManagerApproval" || i.ProcessingOperation.Status == "Approved"))
        }).ToListAsync();
        return new(warehouses, organizations, rows.Select(x => new ProcessingCatalogItemDto(x.Id, x.Sku,
            x.BatchCode, x.ProcessingDirection, x.ConditionRating == 2 ? "B" : "C", x.Quantity,
            x.TotalWeight, x.LocationCode, x.IsLocked, x.IsLocked ? "Batch đang thuộc yêu cầu khác." : null)).ToList());
    }

    public async Task CancelAsync(Guid managerId, Guid operationId, ProcessingOperationDecisionDto dto)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await RequireUserAsync(managerId, "Manager");
        if (string.IsNullOrWhiteSpace(dto.RejectionReason)) throw new InvalidOperationException("Vui lòng nhập lý do hủy.");
        var operation = await context.ProcessingOperations.Include(x => x.Inputs).ThenInclude(x => x.Inventory)
            .FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu xử lý.");
        if (operation.Status is not ("PendingOrganizationApproval" or "PendingManagerApproval" or "Approved"))
            throw new InvalidOperationException("Chỉ được hủy yêu cầu chưa xuất kho.");
        if (operation.Status == "Approved")
        {
            foreach (var input in operation.Inputs.Where(x => x.IsActive != false))
            {
                if (input.Inventory.ReservedQuantity < input.RequestedQuantity || input.Inventory.ReservedWeight < input.RequestedWeight)
                    throw new InvalidOperationException("Tồn kho giữ chỗ không khớp, cần kiểm tra trước khi hủy.");
                input.Inventory.ReservedQuantity -= input.RequestedQuantity;
                input.Inventory.ReservedWeight -= input.RequestedWeight;
            }
        }
        operation.Status = "Cancelled";
        operation.ManagerRejectionReason = dto.RejectionReason.Trim();
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = managerId;
        NotificationWriter.NotifyUser(context, operation.OrganizationId, "ProcessingOperationCancelled", "Yêu cầu xử lý đã hủy",
            $"{operation.OperationCode}: {operation.ManagerRejectionReason}", $"/organization/processing-operations/{operation.Id}", managerId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task ReceiveAsync(Guid organizationId, Guid operationId)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var operation = await OrganizationOperationAsync(organizationId, operationId,
            "Issued", "ReadyForGhn", "GhnBooked", "InTransit", "Delivered", "DeliveryException");
        operation.Status = "OrganizationReceived";
        operation.OrganizationReceivedAt = DateTime.UtcNow;
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = organizationId;
        await NotifyManagersAsync(context, operation, "ProcessingOperationReceived", "Tổ chức đã nhận hàng",
            $"Tổ chức xác nhận đã nhận hàng của {operation.OperationCode}.", organizationId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task CompleteAsync(Guid organizationId, Guid operationId, CompleteProcessingOperationDto dto)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (string.IsNullOrWhiteSpace(dto.CompletionNotes)) throw new InvalidOperationException("Vui lòng nhập kết quả xử lý.");
        var operation = await OrganizationOperationAsync(organizationId, operationId, "OrganizationReceived");
        if (operation.OperationType == "Recycling")
            throw new InvalidOperationException("Đồ tái chế cần xác nhận lịch trả và gửi hàng về kho để phân loại lại.");
        operation.Status = "Completed";
        operation.ProcessingCompletedAt = DateTime.UtcNow;
        operation.CompletionNotes = dto.CompletionNotes.Trim();
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = organizationId;
        await NotifyManagersAsync(context, operation, "ProcessingOperationCompleted", "Đã hoàn tất xử lý",
            $"{operation.OperationCode}: {operation.CompletionNotes}", organizationId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task<ProcessingOperation> OrganizationOperationAsync(Guid organizationId, Guid id, params string[] expectedStatuses)
    {
        await RequireUserAsync(organizationId, "RecyclingOrganization", "DisposalOrganization");
        var operation = await context.ProcessingOperations.FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu xử lý.");
        if (operation.OrganizationId != organizationId) throw new AuthenticationException("Yêu cầu không thuộc tổ chức của bạn.");
        if (!expectedStatuses.Contains(operation.Status)) throw new InvalidOperationException("Trạng thái đã thay đổi hoặc chưa phù hợp với thao tác. Vui lòng tải lại.");
        return operation;
    }
}
