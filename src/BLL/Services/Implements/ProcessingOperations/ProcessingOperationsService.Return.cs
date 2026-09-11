using System.Data;
using System.Security.Authentication;
using BLL.Common;
using BLL.DTOs;
using BLL.Services.Implements.Notifications;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ProcessingOperations;

public partial class ProcessingOperationsService
{
    private async Task<RecyclingReturnDetailDto> GetReturnDetailAsync(ProcessingOperation operation)
    {
        var batches = await context.IntakeBatches.AsNoTracking().Include(x => x.CurrentArea)
            .Include(x => x.CurrentStorageLocation).Include(x => x.ClassificationTeam)
            .Where(x => x.ProcessingOperationOutput != null && x.ProcessingOperationOutput.ProcessingOperationId == operation.Id)
            .ToListAsync();
        return new(operation.ExpectedReturnDate, operation.ReturnDispatchedAt, operation.OutputReturnedAt,
            operation.ReturnCarrierName, operation.ReturnTrackingCode, operation.ReturnNotes,
            operation.Outputs.Where(x => x.IsActive != false).Select(o =>
            {
                var batch = batches.SingleOrDefault(x => x.ProcessingOperationOutputId == o.Id);
                return new RecyclingReturnBatchDto(o.Id, o.Notes ?? "Đồ tái chế", o.Quantity, o.Weight,
                    o.ReturnedQuantity, o.ReturnedWeight, batch?.Note, batch?.Id, batch?.BatchCode,
                    batch?.Status, batch?.CurrentArea?.AreaName, batch?.CurrentStorageLocation?.LocationCode,
                    batch?.ClassificationTeam?.TeamName);
            }).ToList());
    }
    public async Task ScheduleReturnAsync(Guid organizationId, Guid operationId, ScheduleRecyclingReturnDto dto)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var operation = await OrganizationOperationAsync(organizationId, operationId, "OrganizationReceived", "ReturnScheduled");
        RequireRecycling(operation);
        if (dto.ExpectedReturnDate.Date < VietnamTime.Today)
            throw new InvalidOperationException("Ngày trả dự kiến không được trước hôm nay.");
        operation.ExpectedReturnDate = dto.ExpectedReturnDate.Date;
        operation.ReturnNotes = dto.Notes?.Trim();
        operation.Status = "ReturnScheduled";
        operation.ProcessingStartedAt ??= DateTime.UtcNow;
        TouchReturn(operation, organizationId);
        await NotifyReturnAsync(operation, "Đã hẹn ngày trả đồ tái chế",
            $"{operation.OperationCode}: dự kiến trả ngày {dto.ExpectedReturnDate:dd/MM/yyyy}.", organizationId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task DispatchReturnAsync(Guid organizationId, Guid operationId, DispatchRecyclingReturnDto dto)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var operation = await OrganizationOperationAsync(organizationId, operationId, "ReturnScheduled");
        RequireRecycling(operation);
        if (string.IsNullOrWhiteSpace(dto.CarrierName) || dto.CarrierName.Length > 100
            || string.IsNullOrWhiteSpace(dto.TrackingCode) || dto.TrackingCode.Length > 100
            || string.IsNullOrWhiteSpace(dto.CompletionNotes))
            throw new InvalidOperationException("Nhập đơn vị vận chuyển, mã vận đơn/biên bản bàn giao và kết quả tái chế.");
        if (dto.Batches is null || dto.Batches.Count is < 1 or > 100
            || dto.Batches.Any(x => x is null || string.IsNullOrWhiteSpace(x.Description)
                || x.Description.Length > 500 || x.Quantity <= 0 || !ValidReturnWeight(x.Weight)))
            throw new InvalidOperationException("Mỗi batch trả về phải có mô tả, số lượng dương và khối lượng hợp lệ (tối đa 2 chữ số thập phân).");
        if (await context.ProcessingOperationOutputs.AnyAsync(x => x.ProcessingOperationId == operationId))
            throw new InvalidOperationException("Đợt tái chế đã có danh sách hàng trả về.");
        foreach (var batch in dto.Batches)
            context.ProcessingOperationOutputs.Add(new ProcessingOperationOutput
            {
                Id = Guid.NewGuid(), ProcessingOperationId = operationId, OutputType = "RecycledClothing",
                Quantity = batch.Quantity, Weight = batch.Weight, Notes = batch.Description.Trim(),
                CreateAt = DateTime.UtcNow, CreatedBy = organizationId, IsActive = true
            });
        operation.Status = "ReturnInTransit";
        operation.ReturnCarrierName = dto.CarrierName.Trim();
        operation.ReturnTrackingCode = dto.TrackingCode.Trim();
        operation.CompletionNotes = dto.CompletionNotes.Trim();
        operation.ProcessingCompletedAt = DateTime.UtcNow;
        operation.ReturnDispatchedAt = DateTime.UtcNow;
        TouchReturn(operation, organizationId);
        await NotifyReturnAsync(operation, "Đồ tái chế đang được gửi về kho",
            $"{operation.OperationCode}: {operation.ReturnCarrierName} · {operation.ReturnTrackingCode}.", organizationId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<RecyclingReceiptOptionsDto> ReturnReceiptOptionsAsync(Guid staffId, Guid operationId)
    {
        var operation = await WarehouseReturnAsync(staffId, operationId);
        var today = VietnamTime.Today;
        var shifts = await context.Shifts.AsNoTracking().Where(x => x.WarehouseId == operation.WarehouseId
                && x.IsActive != false && x.ShiftDate.Date == today && (x.Status == "Scheduled" || x.Status == "InProgress"))
            .OrderBy(x => x.StartTime).Select(x => new RecyclingReceiptShiftDto(x.Id, x.ShiftName, x.ShiftDate)).ToListAsync();
        var locations = await context.StorageLocations.AsNoTracking().Where(x => x.WarehouseId == operation.WarehouseId
                && x.IsActive != false && x.Status == "Available" && x.Area.IsActive != false
                && x.Area.AreaType == "Recycled" && x.AreaGroup != null && x.AreaGroup.IsActive != false)
            .OrderBy(x => x.LocationCode).Select(x => new RecyclingReceiptLocationDto(x.Id, x.LocationCode,
                x.Area.AreaName, Math.Min(x.CapacityKg - x.CurrentWeightKg,
                    Math.Min(x.Area.CapacityKg - x.Area.CurrentKg, x.AreaGroup!.CapacityKg - x.AreaGroup.CurrentKg))))
            .ToListAsync();
        return new(shifts, locations);
    }

    public async Task ReceiveReturnAsync(Guid staffId, Guid operationId, ReceiveRecyclingReturnDto dto)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var operation = await WarehouseReturnAsync(staffId, operationId);
        if (operation.Status != "ReturnInTransit")
            throw new InvalidOperationException("Chỉ nhận hàng sau khi tổ chức xác nhận gửi trả; không thể nhập trùng.");
        var today = VietnamTime.Today;
        var shift = await context.Shifts.FirstOrDefaultAsync(x => x.Id == dto.ShiftId
            && x.WarehouseId == operation.WarehouseId && x.IsActive != false && x.ShiftDate.Date == today
            && (x.Status == "Scheduled" || x.Status == "InProgress"))
            ?? throw new InvalidOperationException("Chọn ca nhận hàng hôm nay thuộc kho của bạn.");
        var outputs = await context.ProcessingOperationOutputs.Where(x => x.ProcessingOperationId == operationId
            && x.IsActive != false).ToListAsync();
        if (dto.Batches is null || dto.Batches.Any(x => x is null) || dto.Batches.Count != outputs.Count || outputs.Count == 0
            || dto.Batches.Select(x => x.OutputId).Distinct().Count() != outputs.Count
            || dto.Batches.Any(x => !outputs.Any(o => o.Id == x.OutputId)))
            throw new InvalidOperationException("Xác nhận đầy đủ các batch của chuyến trả hàng, mỗi batch một lần.");
        if (await context.IntakeBatches.AnyAsync(x => x.ProcessingOperationOutputId.HasValue
            && outputs.Select(o => o.Id).Contains(x.ProcessingOperationOutputId.Value)))
            throw new InvalidOperationException("Hàng trả đã được nhập kho.");
        foreach (var receipt in dto.Batches)
        {
            var output = outputs.Single(x => x.Id == receipt.OutputId);
            if (receipt.Quantity <= 0 || !ValidReturnWeight(receipt.Weight))
                throw new InvalidOperationException("Số lượng và khối lượng thực nhận phải lớn hơn 0; khối lượng tối đa 2 chữ số thập phân.");
            if ((receipt.Quantity != output.Quantity || receipt.Weight != output.Weight) && string.IsNullOrWhiteSpace(receipt.Notes))
                throw new InvalidOperationException("Ghi rõ lý do khi số lượng/khối lượng thực nhận khác số tổ chức gửi.");
            var location = await context.StorageLocations.Include(x => x.Area).Include(x => x.AreaGroup)
                .FirstOrDefaultAsync(x => x.Id == receipt.StorageLocationId && x.WarehouseId == operation.WarehouseId
                    && x.IsActive != false && x.Status == "Available")
                ?? throw new InvalidOperationException("Vị trí nhận hàng không hợp lệ.");
            if (location.Area.AreaType != "Recycled" || location.Area.IsActive == false
                || location.AreaGroup is null || location.AreaGroup.IsActive == false
                || location.AreaGroup.AreaId != location.AreaId)
                throw new InvalidOperationException("Chọn vị trí trong khu đồ đã tái chế, chờ phân loại lại.");
            if (location.CurrentWeightKg + receipt.Weight > location.CapacityKg
                || location.Area.CurrentKg + receipt.Weight > location.Area.CapacityKg
                || location.AreaGroup.CurrentKg + receipt.Weight > location.AreaGroup.CapacityKg)
                throw new InvalidOperationException("Vị trí, dãy hoặc khu tái chế không đủ sức chứa.");
            location.CurrentWeightKg += receipt.Weight;
            location.Area.CurrentKg += receipt.Weight;
            location.AreaGroup.CurrentKg += receipt.Weight;
            location.UpdateAt = location.Area.UpdateAt = location.AreaGroup.UpdateAt = DateTime.UtcNow;
            output.ReturnedQuantity = receipt.Quantity;
            output.ReturnedWeight = receipt.Weight;
            output.RecordedAt = DateTime.UtcNow;
            output.RecordedByStaffId = staffId;
            output.UpdateAt = DateTime.UtcNow;
            output.UpdatedBy = staffId;
            context.IntakeBatches.Add(new IntakeBatch
            {
                Id = Guid.NewGuid(), WarehouseId = operation.WarehouseId, ShiftId = shift.Id,
                ProcessingOperationOutputId = output.Id,
                BatchCode = $"RC-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                RouteName = $"Tái chế về · {operation.OperationCode}",
                IntakeDate = VietnamTime.Today, TotalWeight = receipt.Weight,
                Status = "AwaitingClassificationAssignment", CurrentAreaId = location.AreaId,
                CurrentAreaGroupId = location.AreaGroupId, CurrentStorageLocationId = location.Id,
                WarehouseReceivedAt = DateTime.UtcNow, WarehouseReceivedByStaffId = staffId,
                SentToClassificationAt = DateTime.UtcNow, Note = receipt.Notes?.Trim(),
                CreateAt = DateTime.UtcNow, CreatedBy = staffId, IsActive = true
            });
        }
        operation.Status = "ReturnReceived";
        operation.OutputReturnedAt = DateTime.UtcNow;
        TouchReturn(operation, staffId);
        await NotifyManagersAsync(context, operation, "RecyclingReturnReceived", "Đồ tái chế về đang chờ phân công",
            $"{operation.OperationCode}: {outputs.Count} batch đã nhận, chờ phân công đội phân loại.", staffId);
        NotificationWriter.NotifyUser(context, operation.OrganizationId, "RecyclingReturnReceived", "Kho đã nhận đồ tái chế",
            $"Kho đã nhận hàng trả của {operation.OperationCode}.", $"/organization/processing-operations/{operation.Id}", staffId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task<ProcessingOperation> WarehouseReturnAsync(Guid staffId, Guid operationId)
    {
        var staff = await RequireUserAsync(staffId, "WarehouseStaff");
        var operation = await context.ProcessingOperations.FirstOrDefaultAsync(x => x.Id == operationId && x.IsActive != false)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        if (staff.WarehouseId != operation.WarehouseId) throw new AuthenticationException("Yêu cầu thuộc kho khác.");
        RequireRecycling(operation);
        return operation;
    }

    private static void RequireRecycling(ProcessingOperation operation)
    {
        if (operation.OperationType != "Recycling") throw new InvalidOperationException("Chỉ đồ tái chế có luồng gửi trả về phân loại lại.");
    }
    private static bool ValidReturnWeight(decimal weight) => weight > 0 && weight <= 1000000 && decimal.Round(weight, 2) == weight;
    private static void TouchReturn(ProcessingOperation operation, Guid actor)
    {
        operation.UpdateAt = DateTime.UtcNow;
        operation.UpdatedBy = actor;
    }
    private async Task NotifyReturnAsync(ProcessingOperation operation, string title, string body, Guid actor)
    {
        await NotifyManagersAsync(context, operation, "RecyclingReturn", title, body, actor);
        var staff = await context.Users.Where(x => x.WarehouseId == operation.WarehouseId && x.IsActive != false
            && x.Role.RoleName == "WarehouseStaff").Select(x => x.Id).ToListAsync();
        foreach (var id in staff)
            NotificationWriter.NotifyUser(context, id, "RecyclingReturn", title, body,
                $"/warehouse/processing-operations/{operation.Id}", actor);
    }
}
