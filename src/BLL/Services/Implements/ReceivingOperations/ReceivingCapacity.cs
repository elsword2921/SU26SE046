using System.Data;
using BLL.Common;
using BLL.DTOs;
using DAL.Models;
using DAL.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BLL.Services.Implements.ReceivingOperations;

public partial class ReceivingOperationsService
{
    // All dispatch writers take the same transaction lock before reading capacity.
    // Serializable also keeps team/shift state stable until assignment commits.
    private async Task<IDbContextTransaction> BeginDispatchAsync()
    {
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await context.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource = N'ReThreads:ReceivingDispatch', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @r < 0 THROW 51000, 'Dispatch is busy. Please retry.', 1;");
            return transaction;
        }
        catch { await transaction.DisposeAsync(); throw; }
    }

    private static void ValidateLimits(ReceivingLimitsDto dto)
    {
        if (dto.MaxRequests < 1 || dto.MaxRequests > 1000 || dto.MaxWeightKg <= 0
            || dto.MaxWeightKg > 100000 || decimal.Round(dto.MaxWeightKg, 2) != dto.MaxWeightKg)
            throw new InvalidOperationException("Giới hạn phải từ 1–1000 đơn, kg lớn hơn 0, tối đa 100000 và tối đa 2 số lẻ.");
    }

    private static bool CountsTowardLoad(PickupAssignment a) => a.IsActive != false
        && a.Status is not ("Cancelled" or "Canceled" or "Rejected" or "Rescheduled");

    private async Task<List<ReceivingTeamLoadDto>> TeamLoadsAsync(Guid? warehouseId = null, DateTime? date = null)
    {
        var teams = await context.OperationalTeams.AsNoTracking().Include(t => t.Shift).ThenInclude(s => s.Warehouse)
            .Where(t => t.IsActive != false && t.Shift.IsActive != false
                && (t.TeamType == "Receiving" || t.TeamType == "ReceivingPickup" || t.TeamType == "ReceivingWarehouse")
                && (!warehouseId.HasValue || t.Shift.WarehouseId == warehouseId)
                && (!date.HasValue || t.Shift.ShiftDate.Date == date.Value.Date))
            .OrderBy(t => t.Shift.ShiftDate).ThenBy(t => t.Shift.StartTime).ThenBy(t => t.TeamName).ToListAsync();
        var ids = teams.Select(t => t.Id).ToList();
        var assignments = await context.PickupAssignments.AsNoTracking().Include(a => a.DonorRequest)
            .Where(a => ids.Contains(a.TeamId) && a.IsActive != false).ToListAsync();
        return teams.Select(t =>
        {
            var rows = assignments.Where(a => a.TeamId == t.Id && CountsTowardLoad(a)).ToList();
            return new ReceivingTeamLoadDto(t.Id, t.TeamName, t.Shift.WarehouseId, t.ShiftId,
                t.Shift.ShiftName, t.Shift.ShiftDate, t.Shift.StartTime, t.Shift.EndTime, t.Status, t.TeamType,
                t.MaxReceivingRequests ?? t.Shift.Warehouse.MaxReceivingRequests,
                t.MaxReceivingWeightKg ?? t.Shift.Warehouse.MaxReceivingWeightKg,
                rows.Count, rows.Sum(a => a.DonorRequest.EstimateWeight),
                rows.Where(a => a.Status == "Received").Sum(a => a.DonorRequest.ActualWeight ?? 0),
                t.MaxReceivingRequests == null && t.MaxReceivingWeightKg == null);
        }).ToList();
    }

    public async Task<ReceivingCapacityBoardDto> GetCapacityBoardAsync(Guid? warehouseId, DateTime? date)
    {
        var warehouses = await context.Warehouses.AsNoTracking().Where(w => w.IsActive != false
                && (!warehouseId.HasValue || w.Id == warehouseId))
            .Select(w => new ReceivingWarehouseLimitsDto(w.Id, w.WarehouseName, w.MaxReceivingRequests, w.MaxReceivingWeightKg)).ToListAsync();
        return new(warehouses, await TeamLoadsAsync(warehouseId, date ?? VietnamTime.Now.Date));
    }

    public async Task SetWarehouseLimitsAsync(Guid id, ReceivingLimitsDto dto)
    {
        ValidateLimits(dto);
        await using var transaction = await BeginDispatchAsync();
        var warehouse = await context.Warehouses.SingleOrDefaultAsync(w => w.Id == id && w.IsActive != false)
            ?? throw new InvalidOperationException("Không tìm thấy kho.");
        var loads = await TeamLoadsAsync(id);
        if (loads.Any(t => t.UsesWarehouseDefaults && t.Status != "Completed"
                && t.ShiftDate.Date.Add(t.EndTime) > VietnamTime.Now
                && (t.AssignedRequests > dto.MaxRequests || t.EstimatedWeightKg > dto.MaxWeightKg)))
            throw new InvalidOperationException("Giới hạn mới thấp hơn tải đã phân công. Hãy chuyển bớt đơn hoặc chỉnh giới hạn riêng của team trước.");
        warehouse.MaxReceivingRequests = dto.MaxRequests;
        warehouse.MaxReceivingWeightKg = dto.MaxWeightKg;
        await context.SaveChangesAsync(); await transaction.CommitAsync();
    }

    public async Task SetTeamLimitsAsync(Guid id, ReceivingLimitsDto? dto)
    {
        if (dto != null) ValidateLimits(dto);
        await using var transaction = await BeginDispatchAsync();
        var team = await context.OperationalTeams.Include(t => t.Shift).ThenInclude(s => s.Warehouse)
            .SingleOrDefaultAsync(t => t.Id == id && t.IsActive != false)
            ?? throw new InvalidOperationException("Không tìm thấy team.");
        if (team.TeamType is not ("Receiving" or "ReceivingPickup" or "ReceivingWarehouse") || IsShiftEnded(team.Shift)
            || team.Status == "Completed" || team.Shift.Status == "Completed")
            throw new InvalidOperationException("Chỉ cấu hình tải cho receiving team trong ca chưa kết thúc.");
        var load = (await TeamLoadsAsync(team.Shift.WarehouseId, team.Shift.ShiftDate)).Single(t => t.Id == id);
        var maxRequests = dto?.MaxRequests ?? team.Shift.Warehouse.MaxReceivingRequests;
        var maxWeight = dto?.MaxWeightKg ?? team.Shift.Warehouse.MaxReceivingWeightKg;
        if (load.AssignedRequests > maxRequests || load.EstimatedWeightKg > maxWeight)
            throw new InvalidOperationException("Giới hạn mới thấp hơn tải đã phân công cho team.");
        team.MaxReceivingRequests = dto?.MaxRequests; team.MaxReceivingWeightKg = dto?.MaxWeightKg;
        await context.SaveChangesAsync(); await transaction.CommitAsync();
    }

    private async Task EnsureTeamCapacityAsync(OperationalTeam team, DonationRequest request)
    {
        var warehouse = await context.Warehouses.SingleAsync(w => w.Id == team.Shift.WarehouseId);
        var rows = await context.PickupAssignments.Include(a => a.DonorRequest)
            .Where(a => a.TeamId == team.Id && a.DonorRequestId != request.Id && a.IsActive != false).ToListAsync();
        var active = rows.Where(CountsTowardLoad).ToList();
        var maxCount = team.MaxReceivingRequests ?? warehouse.MaxReceivingRequests;
        var maxKg = team.MaxReceivingWeightKg ?? warehouse.MaxReceivingWeightKg;
        if (request.EstimateWeight <= 0 || active.Count + 1 > maxCount
            || active.Sum(a => a.DonorRequest.EstimateWeight) + request.EstimateWeight > maxKg)
            throw new InvalidOperationException($"Team {team.TeamName} vượt giới hạn {maxCount} đơn / {maxKg:0.##} kg dự kiến trong ca. Chọn team khác hoặc điều chỉnh giới hạn.");
    }

    public async Task<ReceivingPlanPreviewDto> PreviewPlanAsync(Guid shiftId)
        => await PreviewPlanCoreAsync(shiftId);

    private async Task<ReceivingPlanPreviewDto> PreviewPlanCoreAsync(Guid shiftId, Guid? onlyTeam = null)
    {
        var shift = await context.Shifts.AsNoTracking().SingleOrDefaultAsync(s => s.Id == shiftId && s.IsActive != false)
            ?? throw new InvalidOperationException("Không tìm thấy ca.");
        if (IsShiftEnded(shift) || shift.Status == "Completed") throw new InvalidOperationException("Ca đã kết thúc.");
        var eligibleIds = await context.OperationalTeams.Where(t => t.ShiftId == shiftId && t.IsActive != false
            && t.Status == "Scheduled" && t.Members.Count(m => m.IsActive != false) >= 1
            && t.Members.Count(m => m.IsActive != false) <= 2).Select(t => t.Id).ToListAsync();
        var loads = (await TeamLoadsAsync(shift.WarehouseId, shift.ShiftDate)).Where(t => t.ShiftId == shiftId
            && eligibleIds.Contains(t.Id) && (!onlyTeam.HasValue || t.Id == onlyTeam)).ToList();
        var requests = await context.DonationRequests.AsNoTracking().Where(r => r.IsActive != false
            && r.WarehouseId == shift.WarehouseId && r.PickupDate.HasValue && r.PickupDate.Value.Date == shift.ShiftDate.Date
            && r.PickupDate.Value.TimeOfDay >= shift.StartTime && r.PickupDate.Value.TimeOfDay < shift.EndTime
            && (r.Status == DonationRequestStatus.WaitingReceivingStaff || r.Status == DonationRequestStatus.PendingStaffAssign)
            && (r.DeliveryMethod == "StaffPickup" || r.DeliveryMethod == "DonorDropOff")
            && !context.PickupAssignments.Any(a => a.DonorRequestId == r.Id && a.IsActive != false))
            .ToListAsync();
        var counts = loads.ToDictionary(t => t.Id, t => t.AssignedRequests);
        var weights = loads.ToDictionary(t => t.Id, t => t.EstimatedWeightKg);
        var areas = loads.ToDictionary(t => t.Id, _ => new HashSet<string>());
        static string AddressKey(string address) => System.Text.RegularExpressions.Regex.Replace(address.Trim(), @"\s+", " ").ToLowerInvariant();
        var addresses = loads.ToDictionary(t => t.Id, _ => new HashSet<string>());
        var existing = await context.PickupAssignments.AsNoTracking().Include(a => a.DonorRequest)
            .Where(a => a.ShiftId == shiftId && a.IsActive != false).ToListAsync();
        foreach (var a in existing.Where(CountsTowardLoad))
            if (areas.TryGetValue(a.TeamId, out var set))
            { set.Add(ExtractArea(a.DonorRequest.PickupAddress)); addresses[a.TeamId].Add(AddressKey(a.DonorRequest.PickupAddress)); }
        var assignments = new List<ReceivingSuggestedAssignmentDto>();
        var unassigned = new List<ReceivingUnassignedDto>();
        // Large loads first reduce unusable remaining capacity; address/area breaks load ties.
        foreach (var r in requests.OrderByDescending(r => r.EstimateWeight).ThenBy(r => r.PickupDate).ThenBy(r => r.Id))
        {
            if (r.EstimateWeight <= 0 || (r.DropOffMethod == "ThirdPartyDelivery"
                && (string.IsNullOrWhiteSpace(r.CarrierName) || string.IsNullOrWhiteSpace(r.TrackingCode))))
            { unassigned.Add(new(r.Id, r.RequestCode, r.EstimateWeight, "Thiếu kg dự kiến hoặc thông tin vận chuyển.")); continue; }
            var candidates = loads.Where(t => (t.TeamType == "ReceivingWarehouse") == (r.DeliveryMethod == "DonorDropOff")).ToList();
            var area = ExtractArea(r.PickupAddress);
            var team = candidates.Where(t => counts[t.Id] < t.MaxRequests && weights[t.Id] + r.EstimateWeight <= t.MaxWeightKg)
                .OrderBy(t => 0.5m * (counts[t.Id] + 1) / t.MaxRequests + 0.5m * (weights[t.Id] + r.EstimateWeight) / t.MaxWeightKg)
                .ThenByDescending(t => addresses[t.Id].Contains(AddressKey(r.PickupAddress)))
                .ThenByDescending(t => areas[t.Id].Contains(area)).ThenBy(t => t.Id).FirstOrDefault();
            if (team == null)
            { unassigned.Add(new(r.Id, r.RequestCode, r.EstimateWeight, candidates.Count == 0 ? "Chưa có team phù hợp trong ca." : "Hết khả năng tiếp nhận trong ca (giới hạn đơn/kg).")); continue; }
            counts[team.Id]++; weights[team.Id] += r.EstimateWeight; areas[team.Id].Add(area); addresses[team.Id].Add(AddressKey(r.PickupAddress));
            assignments.Add(new(r.Id, r.RequestCode, r.PickupAddress, r.EstimateWeight, team.Id, candidates.Select(t => t.Id).ToList()));
        }
        return new(shiftId, loads, assignments, unassigned);
    }

    public async Task ApplyPlanAsync(ApplyReceivingPlanDto dto)
    {
        if (dto.Assignments == null || dto.Assignments.Count == 0 || dto.Assignments.Count > 1000
            || dto.Assignments.Select(a => a.RequestId).Distinct().Count() != dto.Assignments.Count)
            throw new InvalidOperationException("Danh sách phân công rỗng, trùng đơn hoặc quá lớn.");
        await using var transaction = await BeginDispatchAsync();
        foreach (var assignment in dto.Assignments)
        {
            if (!await context.OperationalTeams.AnyAsync(t => t.Id == assignment.TeamId && t.ShiftId == dto.ShiftId)
                || await context.PickupAssignments.AnyAsync(a => a.DonorRequestId == assignment.RequestId && a.IsActive != false))
                throw new InvalidOperationException("Dữ liệu phân công đã thay đổi. Hãy tải lại gợi ý.");
            await AssignRequestCoreAsync(assignment);
        }
        await transaction.CommitAsync();
    }
}
