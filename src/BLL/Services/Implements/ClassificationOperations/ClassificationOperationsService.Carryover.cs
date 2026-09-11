using System.Data;
using BLL.Common;
using BLL.DTOs;
using BLL.Services.Implements.Notifications;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ClassificationOperations;

public partial class ClassificationOperationsService
{
    public async Task<IReadOnlyList<CurrentClassificationTeamDto>> GetCurrentTeamsAsync(Guid staffId)
    {
        var warehouseId = await RequireStaffWarehouseIdAsync(staffId);
        await ShiftLifecycle.CompleteEndedShiftsAsync(context);
        var today = VietnamTime.Today;
        return await context.OperationalTeams.AsNoTracking().Where(t => t.IsActive != false
                && t.TeamType == "Classification" && t.Shift.WarehouseId == warehouseId
                && t.Shift.IsActive != false && t.Shift.ShiftDate.Date == today
                && t.Members.Any(m => m.StaffId == staffId && m.IsActive != false))
            .OrderBy(t => t.Shift.StartTime).Select(t => new CurrentClassificationTeamDto(t.Id,
                t.TeamName, t.Status, t.Shift.ShiftDate, t.Shift.StartTime, t.Shift.EndTime)).ToListAsync();
    }

    public async Task ResumeBatchAsync(Guid staffId, Guid batchId, Guid teamId)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var warehouseId = await RequireStaffWarehouseIdAsync(staffId);
        var batch = await RequireBatch(batchId);
        if (batch.WarehouseId != warehouseId || !batch.ClassificationTeamId.HasValue)
            throw new InvalidOperationException("Lô hàng không thuộc kho hoặc chưa được phân công.");
        if (batch.Status is not ("AssignedToClassification" or "AwaitingClassificationCount" or "ReadyForClassification" or "Classifying"))
            throw new InvalidOperationException("Chỉ chuyển ca cho lô chưa hoàn tất phân loại.");
        var source = await RequireMyClassificationTeamAsync(staffId, batch.ClassificationTeamId.Value);
        var target = await RequireMyClassificationTeamAsync(staffId, teamId);
        var now = VietnamTime.Now;
        if (source.Id == target.Id || now < source.Shift.ShiftDate.Date.Add(source.Shift.EndTime))
            throw new InvalidOperationException("Lô vẫn thuộc ca hiện tại; chỉ tiếp tục sang ca mới sau khi ca trước đã hết giờ.");
        if (target.Shift.WarehouseId != warehouseId || target.Shift.IsActive == false
            || target.Shift.Status == "Completed" || target.Status != "InProgress"
            || target.Shift.ShiftDate.Date != VietnamTime.Today
            || now < target.Shift.ShiftDate.Date.Add(target.Shift.StartTime)
            || now >= target.Shift.ShiftDate.Date.Add(target.Shift.EndTime))
            throw new InvalidOperationException("Chọn team của bạn đang làm việc trong ca hôm nay, cùng kho.");
        context.ClassificationBatchTransfers.Add(new ClassificationBatchTransfer
        {
            Id = Guid.NewGuid(), IntakeBatchId = batch.Id, FromTeamId = source.Id,
            ToTeamId = target.Id, StaffId = staffId, TransferredAt = DateTime.UtcNow,
            CreateAt = DateTime.UtcNow, CreatedBy = staffId, IsActive = true
        });
        batch.ClassificationTeamId = target.Id;
        batch.UpdateAt = DateTime.UtcNow;
        batch.UpdatedBy = staffId;
        // Preserve count, items, original assignment audit and physical placement.
        foreach (var member in target.Members.Where(m => m.IsActive != false))
            NotificationWriter.NotifyUser(context, member.StaffId, "ClassificationBatchResumed",
                "Tiếp tục lô phân loại từ ca trước", $"Lô {batch.BatchCode} được chuyển sang {target.TeamName}, giữ nguyên tiến độ.",
                "/classification?tab=pending", staffId);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
