using System.Text.RegularExpressions;
using BLL.DTOs;
using BLL.Services.Interfaces.ReceivingOperations;
using DAL;
using DAL.Models;
using DAL.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ReceivingOperations;

public class ReceivingOperationsService(AppDbContext context) : IReceivingOperationsService
{
    public async Task GenerateStandardShiftsAsync(GenerateShiftsDto dto)
    {
        var date = dto.Date.Date;
        if (!await context.Warehouses.AnyAsync(x => x.Id == dto.WarehouseId && x.IsActive != false))
            throw new InvalidOperationException("Warehouse not found.");

        var definitions = new[]
        {
            ("Ca sáng", new TimeSpan(8, 0, 0), new TimeSpan(11, 0, 0)),
            ("Ca chiều", new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0))
        };

        foreach (var definition in definitions)
        {
            var exists = await context.Shifts.AnyAsync(x => x.WarehouseId == dto.WarehouseId
                && x.ShiftDate == date && x.StartTime == definition.Item2 && x.IsActive != false);
            if (exists) continue;
            context.Shifts.Add(new Shift
            {
                Id = Guid.NewGuid(), WarehouseId = dto.WarehouseId, ShiftDate = date,
                ShiftName = definition.Item1, StartTime = definition.Item2, EndTime = definition.Item3,
                Status = "Scheduled", CreateAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    public async Task<Guid> CreateTeamAsync(CreateReceivingTeamDto dto)
    {
        if (dto.StaffIds.Distinct().Count() != 2)
            throw new InvalidOperationException("A receiving team must have exactly two different staff members.");
        var shift = await context.Shifts.FirstOrDefaultAsync(x => x.Id == dto.ShiftId && x.IsActive != false)
            ?? throw new InvalidOperationException("Shift not found.");
        var validStaff = await context.Users.Include(x => x.Role).CountAsync(x => dto.StaffIds.Contains(x.Id)
            && x.Role.RoleName == "ReceivingStaff" && x.IsActive != false);
        if (validStaff != 2) throw new InvalidOperationException("Both members must be active ReceivingStaff users.");

        var team = new OperationalTeam
        {
            Id = Guid.NewGuid(), ShiftId = shift.Id, TeamName = dto.TeamName,
            TeamType = "Receiving", CreateAt = DateTime.UtcNow
        };
        context.OperationalTeams.Add(team);
        context.TeamMembers.AddRange(dto.StaffIds.Select(id => new TeamMember
        {
            Id = Guid.NewGuid(), TeamId = team.Id, StaffId = id, CreateAt = DateTime.UtcNow
        }));
        await context.SaveChangesAsync();
        return team.Id;
    }

    public async Task<int> PlanShiftAsync(PlanReceivingShiftDto dto)
    {
        var shift = await context.Shifts.FirstOrDefaultAsync(x => x.Id == dto.ShiftId && x.IsActive != false)
            ?? throw new InvalidOperationException("Shift not found.");
        var team = await context.OperationalTeams.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == dto.TeamId && x.ShiftId == shift.Id && x.IsActive != false)
            ?? throw new InvalidOperationException("Receiving team does not belong to this shift.");
        if (team.Members.Count(x => x.IsActive != false) != 2)
            throw new InvalidOperationException("Receiving team must contain exactly two members.");

        var alreadyPlanned = context.PickupAssignments.Where(x => x.IsActive != false).Select(x => x.DonorRequestId);
        var candidates = await context.DonationRequests.Include(x => x.Donor)
            .Where(x => x.WarehouseId == shift.WarehouseId && x.IsActive != false
                && x.Status == DonationRequestStatus.WaitingReceivingStaff
                && x.PickupDate.HasValue && x.PickupDate.Value.Date <= shift.ShiftDate.Date
                && (x.PickupDate.Value.Date < shift.ShiftDate.Date
                    || (x.PickupDate.Value.TimeOfDay >= shift.StartTime
                        && x.PickupDate.Value.TimeOfDay <= shift.EndTime))
                && !alreadyPlanned.Contains(x.Id))
            .OrderBy(x => x.PickupDate)
            .ThenBy(x => x.PickupAddress)
            .ToListAsync();

        if (candidates.Count == 0) return 0;

        var batch = await context.IntakeBatches
            .FirstOrDefaultAsync(x => x.ShiftId == shift.Id && x.IsActive != false);
        if (batch is null)
        {
            var areas = candidates.Select(x => ExtractArea(x.PickupAddress)).Distinct().ToList();
            batch = new IntakeBatch
            {
                Id = Guid.NewGuid(), WarehouseId = shift.WarehouseId, ShiftId = shift.Id, ReceivingTeamId = team.Id,
                IntakeDate = shift.ShiftDate.Date.Add(shift.StartTime), BatchCode = $"INT-{shift.ShiftDate:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                RouteName = string.Join(" → ", areas), Status = "Planned", CreateAt = DateTime.UtcNow
            };
            context.IntakeBatches.Add(batch);
        }
        else if (batch.ReceivingTeamId != team.Id)
        {
            throw new InvalidOperationException("This shift already has an intake batch assigned to another team.");
        }

        var planned = 0;
        var order = await context.PickupAssignments
            .Where(x => x.IntakeBatchId == batch.Id && x.IsActive != false)
            .Select(x => (int?)x.RouteOrder).MaxAsync() ?? 0;
        foreach (var request in candidates.OrderBy(x => ExtractArea(x.PickupAddress)).ThenBy(x => x.PickupAddress))
        {
            var area = ExtractArea(request.PickupAddress);
            context.PickupAssignments.Add(new PickupAssignment
            {
                Id = Guid.NewGuid(), DonorRequestId = request.Id, ShiftId = shift.Id, TeamId = team.Id,
                IntakeBatchId = batch.Id, RouteOrder = ++order, AreaKey = area,
                Status = "Pending", CreateAt = DateTime.UtcNow
            });
            request.Status = DonationRequestStatus.ReceivingStaffAssigned;
            request.UpdateAt = DateTime.UtcNow;
            planned++;
        }
        await context.SaveChangesAsync();
        return planned;
    }

    public async Task<List<ReceivingBatchDto>> GetMyBatchesAsync(Guid staffId)
    {
        var batches = await MyBatchQuery(staffId).OrderByDescending(x => x.IntakeDate).ToListAsync();
        return batches.Select(MapBatch).ToList();
    }

    public async Task<ReceivingBatchDto?> GetMyBatchAsync(Guid staffId, Guid batchId)
    {
        var batch = await MyBatchQuery(staffId).FirstOrDefaultAsync(x => x.Id == batchId);
        return batch is null ? null : MapBatch(batch);
    }

    public async Task StartBatchAsync(Guid staffId, Guid batchId)
    {
        var batch = await RequireMyBatch(staffId, batchId);
        var shift = batch.ReceivingTeam!.Shift;
        if (shift.Status == "Completed") throw new InvalidOperationException("Completed shift cannot be started again.");
        if (shift.Status == "Scheduled")
        {
            shift.Status = "InProgress";
            shift.StartedAt = DateTime.UtcNow;
            shift.UpdateAt = DateTime.UtcNow;
        }
        if (batch.Status == "Planned")
        {
            batch.Status = "Receiving";
            batch.StartedAt = DateTime.UtcNow;
            batch.UpdateAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
    }

    public async Task CompleteShiftAsync(Guid staffId, Guid shiftId)
    {
        var shift = await context.Shifts
            .Include(x => x.Teams).ThenInclude(x => x.Members)
            .Include(x => x.Teams).ThenInclude(x => x.IntakeBatches)
                .ThenInclude(x => x.PickupAssignments)
            .FirstOrDefaultAsync(x => x.Id == shiftId && x.IsActive != false
                && x.Teams.Any(t => t.Members.Any(m => m.StaffId == staffId && m.IsActive != false)))
            ?? throw new InvalidOperationException("Shift not found or is not assigned to this staff member.");

        if (shift.Status != "InProgress")
            throw new InvalidOperationException("Only an in-progress shift can be completed.");

        var batches = shift.Teams
            .Where(t => t.Members.Any(m => m.StaffId == staffId && m.IsActive != false))
            .SelectMany(t => t.IntakeBatches)
            .Where(b => b.IsActive != false)
            .ToList();
        if (batches.SelectMany(b => b.PickupAssignments)
            .Any(a => a.IsActive != false && a.Status == "Pending"))
            throw new InvalidOperationException("All assigned requests must be processed before ending the shift.");

        foreach (var batch in batches.Where(b => b.Status is "Planned" or "Receiving"))
        {
            batch.Status = "Completed";
            batch.CompletedAt = DateTime.UtcNow;
            batch.UpdateAt = DateTime.UtcNow;
        }
        shift.Status = "Completed";
        shift.CompletedAt = DateTime.UtcNow;
        shift.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task ConfirmPickupAsync(Guid staffId, Guid batchId, Guid requestId, ConfirmPickupDto dto)
    {
        if (dto.ActualWeight <= 0) throw new InvalidOperationException("Actual weight must be greater than zero.");
        var batch = await RequireMyBatch(staffId, batchId);
        if (batch.Status != "Receiving" || batch.ReceivingTeam?.Shift.Status != "InProgress")
            throw new InvalidOperationException("The assigned shift must be started before receiving donations.");
        var assignment = batch.PickupAssignments.FirstOrDefault(x => x.DonorRequestId == requestId && x.IsActive != false)
            ?? throw new InvalidOperationException("Request is not assigned to this route.");
        if (assignment.Status != "Pending") throw new InvalidOperationException("Request has already been processed.");
        assignment.Status = "Received"; assignment.ProcessedAt = DateTime.UtcNow; assignment.Notes = dto.Notes;
        var alreadyInBatch = await context.IntakeBatchDonationRequests.AnyAsync(x =>
            x.IntakeBatchId == batch.Id && x.DonationRequestId == requestId);
        if (alreadyInBatch) throw new InvalidOperationException("Donation request is already included in this intake batch.");
        context.IntakeBatchDonationRequests.Add(new IntakeBatchDonationRequest
        {
            Id = Guid.NewGuid(), IntakeBatchId = batch.Id, DonationRequestId = requestId,
            AddedAt = DateTime.UtcNow, AddedByStaffId = staffId, CreateAt = DateTime.UtcNow
        });
        assignment.DonorRequest.ActualWeight = dto.ActualWeight;
        assignment.DonorRequest.ImageUrls = dto.ImageUrls ?? assignment.DonorRequest.ImageUrls;
        assignment.DonorRequest.Status = DonationRequestStatus.Confirmed; assignment.DonorRequest.UpdateAt = DateTime.UtcNow;
        batch.TotalWeight += dto.ActualWeight; batch.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task RescheduleAsync(Guid staffId, Guid batchId, Guid requestId, ReschedulePickupDto dto)
    {
        var batch = await RequireMyBatch(staffId, batchId);
        EnsureShiftIsInProgress(batch);
        var assignment = RequirePendingAssignment(batch, requestId);
        assignment.Status = "Rescheduled"; assignment.ProcessedAt = DateTime.UtcNow; assignment.Notes = dto.Reason; assignment.IsActive = false;
        assignment.DonorRequest.PickupDate = dto.PickupDate; assignment.DonorRequest.Status = DonationRequestStatus.WaitingReceivingStaff;
        assignment.DonorRequest.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task RejectAsync(Guid staffId, Guid batchId, Guid requestId, RejectPickupDto dto)
    {
        var batch = await RequireMyBatch(staffId, batchId);
        EnsureShiftIsInProgress(batch);
        var assignment = RequirePendingAssignment(batch, requestId);
        assignment.Status = "Cancelled"; assignment.ProcessedAt = DateTime.UtcNow; assignment.Notes = dto.Reason;
        assignment.DonorRequest.Status = DonationRequestStatus.Reject; assignment.DonorRequest.RejectReason = dto.Reason;
        assignment.DonorRequest.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task CompleteBatchAsync(Guid staffId, Guid batchId)
    {
        var batch = await RequireMyBatch(staffId, batchId);
        if (batch.PickupAssignments.Any(x => x.IsActive != false && x.Status == "Pending"))
            throw new InvalidOperationException("All requests must be processed before completing the batch.");
        batch.Status = "Completed"; batch.CompletedAt = DateTime.UtcNow; batch.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private IQueryable<IntakeBatch> MyBatchQuery(Guid staffId) => context.IntakeBatches.AsNoTracking()
        .Include(x => x.ReceivingTeam!).ThenInclude(x => x.Shift)
        .Include(x => x.PickupAssignments.Where(a => a.IsActive != false)).ThenInclude(x => x.DonorRequest).ThenInclude(x => x.Donor)
        .Where(x => x.IsActive != false && x.ReceivingTeam!.Members.Any(m => m.StaffId == staffId && m.IsActive != false));

    private async Task<IntakeBatch> RequireMyBatch(Guid staffId, Guid batchId) =>
        await context.IntakeBatches.Include(x => x.ReceivingTeam!).ThenInclude(x => x.Members)
            .Include(x => x.ReceivingTeam!).ThenInclude(x => x.Shift)
            .Include(x => x.PickupAssignments).ThenInclude(x => x.DonorRequest).ThenInclude(x => x.Donor)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.IsActive != false
                && x.ReceivingTeam!.Members.Any(m => m.StaffId == staffId && m.IsActive != false))
        ?? throw new InvalidOperationException("Batch not found or is not assigned to this staff member.");

    private static PickupAssignment RequirePendingAssignment(IntakeBatch batch, Guid requestId)
    {
        var assignment = batch.PickupAssignments.FirstOrDefault(x => x.DonorRequestId == requestId && x.IsActive != false)
            ?? throw new InvalidOperationException("Request is not assigned to this route.");
        if (assignment.Status != "Pending") throw new InvalidOperationException("Request has already been processed.");
        return assignment;
    }

    private static void EnsureShiftIsInProgress(IntakeBatch batch)
    {
        if (batch.Status != "Receiving" || batch.ReceivingTeam?.Shift.Status != "InProgress")
            throw new InvalidOperationException("The assigned shift must be started before processing donations.");
    }

    private static ReceivingBatchDto MapBatch(IntakeBatch batch) => new()
    {
        Id = batch.Id, Code = batch.BatchCode, Route = batch.RouteName, Date = batch.IntakeDate,
        ShiftId = batch.ReceivingTeam?.ShiftId ?? Guid.Empty,
        ShiftStatus = batch.ReceivingTeam?.Shift.Status ?? string.Empty,
        ShiftName = batch.ReceivingTeam?.Shift.ShiftName ?? string.Empty,
        StartTime = batch.ReceivingTeam?.Shift.StartTime ?? default, EndTime = batch.ReceivingTeam?.Shift.EndTime ?? default,
        Status = batch.Status,
        Requests = batch.PickupAssignments.OrderBy(x => x.RouteOrder).Select(x => new ReceivingRequestDto
        {
            Id = x.DonorRequestId, BatchId = batch.Id,
            Code = $"DR-{x.DonorRequest.CreateAt?.Year}-{x.DonorRequestId.ToString()[..8].ToUpperInvariant()}",
            DonorName = x.DonorRequest.Donor.FullName, PhoneNumber = x.DonorRequest.Donor.PhoneNumber,
            PickupAddress = x.DonorRequest.PickupAddress, Description = x.DonorRequest.Description ?? string.Empty,
            EstimateWeight = x.DonorRequest.EstimateWeight, ActualWeight = x.DonorRequest.ActualWeight,
            PickupDate = x.DonorRequest.PickupDate, Status = x.Status, Notes = x.Notes,
            ImageUrls = x.DonorRequest.ImageUrls
        }).ToList()
    };

    private static string ExtractArea(string address)
    {
        var match = Regex.Match(address, @"(?i)(quận|q\.?|huyện|thành phố|tp\.?|thủ đức)\s*[^,]+", RegexOptions.CultureInvariant);
        return match.Success ? match.Value.Trim() : address.Split(',', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? "Khu vực khác";
    }
}
