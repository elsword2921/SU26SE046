using System.Text.RegularExpressions;
using BLL.DTOs;
using BLL.Common;
using BLL.Services.Interfaces.ReceivingOperations;
using BLL.Services.Implements.Notifications;
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

        var template = await context.WorkScheduleTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId
                && x.Year == date.Year && x.IsActive != false);
        var definitions = template is null ? new[]
        {
            ("Ca sáng", new TimeSpan(8, 0, 0), new TimeSpan(11, 0, 0)),
            ("Ca chiều", new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0))
        } : new[]
        {
            ("Ca sáng", template.MorningStartTime, template.MorningEndTime),
            ("Ca chiều", template.AfternoonStartTime, template.AfternoonEndTime)
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

    public async Task<GenerateMonthShiftsResultDto> GenerateMonthShiftsAsync(
    GenerateMonthShiftsDto dto)
    {
        if (dto.Year is < 2020 or > 2100)
            throw new InvalidOperationException("Year must be between 2020 and 2100.");
        if (dto.Month is < 1 or > 12)
            throw new InvalidOperationException("Month must be between 1 and 12.");
        if (!await context.Warehouses.AnyAsync(x => x.Id == dto.WarehouseId && x.IsActive != false))
            throw new InvalidOperationException("Warehouse not found.");
        var template = await context.WorkScheduleTemplates
            .FirstOrDefaultAsync(x =>
                x.WarehouseId == dto.WarehouseId &&
                x.Year == dto.Year &&
                x.IsActive != false);
        HashSet<DayOfWeek> workingDaySet;
        if (dto.WorkingDays is { Count: > 0 })
        {
            workingDaySet = dto.WorkingDays
                .Distinct()
                .ToHashSet();
        }
        else if (template is not null && !string.IsNullOrWhiteSpace(template.WorkingDays))
        {
            workingDaySet = template.WorkingDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value =>
                {
                    if (int.TryParse(value, out var number) && number is >= 0 and <= 6)
                    {
                        return (DayOfWeek?)number;
                    }
                    return Enum.TryParse<DayOfWeek>(
                        value,
                        true,
                        out var day)
                        ? day
                        : null;
                })
                .Where(day => day.HasValue)
                .Select(day => day!.Value)
                .ToHashSet();
            if (workingDaySet.Count == 0)
            {
                workingDaySet =
                [
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                ];
            }
        }
        else
        {
            workingDaySet =
            [
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            ];
        }
        if (workingDaySet.Any(day => !Enum.IsDefined(day)))
            throw new InvalidOperationException(
                "Every working day must be a valid day of week.");
        var morningStart =
            dto.MorningStartTime
            ?? template?.MorningStartTime
            ?? new TimeSpan(8, 0, 0);
        var morningEnd =
            dto.MorningEndTime
            ?? template?.MorningEndTime
            ?? new TimeSpan(11, 0, 0);
        var afternoonStart =
            dto.AfternoonStartTime
            ?? template?.AfternoonStartTime
            ?? new TimeSpan(13, 0, 0);
        var afternoonEnd =
            dto.AfternoonEndTime
            ?? template?.AfternoonEndTime
            ?? new TimeSpan(17, 0, 0);
        if (morningStart >= morningEnd)
            throw new InvalidOperationException("Morning end time must be after morning start time.");
        if (afternoonStart >= afternoonEnd)
            throw new InvalidOperationException("Afternoon end time must be after afternoon start time.");
        if (morningEnd > afternoonStart)
            throw new InvalidOperationException("Morning shift must end before the afternoon shift starts.");

        var monthStart = new DateTime(dto.Year, dto.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var excludedDates = new HashSet<DateTime>();
        var fixedHolidays = new[]
        {
            new DateTime(dto.Year, 1, 1),
            new DateTime(dto.Year, 4, 30),
            new DateTime(dto.Year, 5, 1),
            new DateTime(dto.Year, 9, 2)
        };
        foreach (var holiday in fixedHolidays)
        {
            if (holiday >= monthStart && holiday < monthEnd)
                excludedDates.Add(holiday.Date);
        }
        foreach (var holiday in dto.HolidayDates ?? [])
        {
            if (holiday.Year != dto.Year)
                throw new InvalidOperationException(
                    "Every additional holiday must belong to the selected year.");
            if (holiday.Date >= monthStart && holiday.Date < monthEnd)
                excludedDates.Add(holiday.Date);
        }
        if (template is null)
        {
            template = new WorkScheduleTemplate
            {
                Id = Guid.NewGuid(),
                WarehouseId = dto.WarehouseId,
                Year = dto.Year,
                CreateAt = DateTime.UtcNow
            };
            context.WorkScheduleTemplates.Add(template);
        }
        template.WorkingDays = string.Join(',', workingDaySet.OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day));
        template.MorningStartTime = morningStart;
        template.MorningEndTime = morningEnd;
        template.AfternoonStartTime = afternoonStart;
        template.AfternoonEndTime = afternoonEnd;
        template.UpdateAt = DateTime.UtcNow;
        template.IsActive = true;

        var existing = await context.Shifts
            .AsNoTracking()
            .Where(x =>
                x.WarehouseId == dto.WarehouseId &&
                x.ShiftDate >= monthStart &&
                x.ShiftDate < monthEnd &&
                x.IsActive != false)
            .Select(x => new
            {
                Date = x.ShiftDate.Date,
                x.StartTime
            })
            .ToListAsync();

        var existingKeys = existing
            .Select(x => (x.Date, x.StartTime))
            .ToHashSet();
        var definitions = new[]
        {
            ("Ca sáng", morningStart, morningEnd),
            ("Ca chiều", afternoonStart, afternoonEnd)
        };
        var workingDays = 0;
        var created = 0;
        var skipped = 0;
        for (var date = monthStart; date < monthEnd; date = date.AddDays(1))
        {
            if (!workingDaySet.Contains(date.DayOfWeek) ||
                excludedDates.Contains(date.Date))
            {
                continue;
            }
            workingDays++;
            foreach (var definition in definitions)
            {
                if (existingKeys.Contains(
                        (date.Date, definition.Item2)))
                {
                    skipped++;
                    continue;
                }
                context.Shifts.Add(new Shift
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = dto.WarehouseId,
                    ShiftDate = date.Date,
                    ShiftName = definition.Item1,
                    StartTime = definition.Item2,
                    EndTime = definition.Item3,
                    Status = "Scheduled",
                    CreateAt = DateTime.UtcNow
                });
                existingKeys.Add((date.Date, definition.Item2));
                created++;
            }
    }

    await context.SaveChangesAsync();

    return new GenerateMonthShiftsResultDto(
        workingDays,
        created,
        skipped);
}

    public async Task<GenerateYearShiftsResultDto> GenerateYearShiftsAsync(GenerateYearShiftsDto dto)
    {
        if (dto.Year is < 2020 or > 2100)
            throw new InvalidOperationException("Year must be between 2020 and 2100.");
        if (!await context.Warehouses.AnyAsync(x => x.Id == dto.WarehouseId && x.IsActive != false))
            throw new InvalidOperationException("Warehouse not found.");

        // Fixed-date Vietnamese public holidays. Lunar holidays such as Tet and Hung Kings
        // are supplied by Manager for the selected year because their solar dates change.
        var excludedDates = new HashSet<DateTime>
        {
            new(dto.Year, 1, 1),
            new(dto.Year, 4, 30),
            new(dto.Year, 5, 1),
            new(dto.Year, 9, 2)
        };
        foreach (var holiday in dto.HolidayDates ?? [])
        {
            if (holiday.Year != dto.Year)
                throw new InvalidOperationException("Every additional holiday must belong to the selected year.");
            excludedDates.Add(holiday.Date);
        }

        // Preserve the previous Monday-Friday behavior for older clients that do not
        // send WorkingDays yet, while allowing managers to define the company schedule.
        var workingDaySet = (dto.WorkingDays is { Count: > 0 }
                ? dto.WorkingDays
                : [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                    DayOfWeek.Thursday, DayOfWeek.Friday])
            .Distinct()
            .ToHashSet();
        if (workingDaySet.Any(day => !Enum.IsDefined(day)))
            throw new InvalidOperationException("Every working day must be a valid day of week.");

        var morningStart = dto.MorningStartTime ?? new TimeSpan(8, 0, 0);
        var morningEnd = dto.MorningEndTime ?? new TimeSpan(11, 0, 0);
        var afternoonStart = dto.AfternoonStartTime ?? new TimeSpan(13, 0, 0);
        var afternoonEnd = dto.AfternoonEndTime ?? new TimeSpan(17, 0, 0);
        if (morningStart >= morningEnd)
            throw new InvalidOperationException("Morning end time must be after morning start time.");
        if (afternoonStart >= afternoonEnd)
            throw new InvalidOperationException("Afternoon end time must be after afternoon start time.");
        if (morningEnd > afternoonStart)
            throw new InvalidOperationException("Morning shift must end before the afternoon shift starts.");

        var template = await context.WorkScheduleTemplates
            .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId && x.Year == dto.Year);
        if (template is null)
        {
            template = new WorkScheduleTemplate
            {
                Id = Guid.NewGuid(), WarehouseId = dto.WarehouseId, Year = dto.Year,
                CreateAt = DateTime.UtcNow
            };
            context.WorkScheduleTemplates.Add(template);
        }
        template.WorkingDays = string.Join(',', workingDaySet.OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day));
        template.MorningStartTime = morningStart;
        template.MorningEndTime = morningEnd;
        template.AfternoonStartTime = afternoonStart;
        template.AfternoonEndTime = afternoonEnd;
        template.UpdateAt = DateTime.UtcNow;
        template.IsActive = true;

        var yearStart = new DateTime(dto.Year, 1, 1);
        var yearEnd = new DateTime(dto.Year + 1, 1, 1);
        var existing = await context.Shifts.AsNoTracking()
            .Where(x => x.WarehouseId == dto.WarehouseId && x.ShiftDate >= yearStart
                && x.ShiftDate < yearEnd && x.IsActive != false)
            .Select(x => new { Date = x.ShiftDate.Date, x.StartTime })
            .ToListAsync();
        var existingKeys = existing.Select(x => (x.Date, x.StartTime)).ToHashSet();
        var definitions = new[]
        {
            ("Ca sáng", morningStart, morningEnd),
            ("Ca chiều", afternoonStart, afternoonEnd)
        };
        var workingDays = 0;
        var created = 0;
        var skipped = 0;
        for (var date = yearStart; date < yearEnd; date = date.AddDays(1))
        {
            if (!workingDaySet.Contains(date.DayOfWeek) || excludedDates.Contains(date.Date))
                continue;
            workingDays++;
            foreach (var definition in definitions)
            {
                if (existingKeys.Contains((date.Date, definition.Item2)))
                {
                    skipped++;
                    continue;
                }
                context.Shifts.Add(new Shift
                {
                    Id = Guid.NewGuid(), WarehouseId = dto.WarehouseId, ShiftDate = date.Date,
                    ShiftName = definition.Item1, StartTime = definition.Item2,
                    EndTime = definition.Item3, Status = "Scheduled", CreateAt = DateTime.UtcNow
                });
                created++;
            }
        }
        await context.SaveChangesAsync();
        return new GenerateYearShiftsResultDto(workingDays, created, skipped);
    }

    public async Task<GenerateShiftsResultDto> GenerateShiftsAsync(GenerateShiftsV2Dto dto)
    {
        if (!await context.Warehouses
            .AnyAsync(x => x.Id == dto.WarehouseId && x.IsActive != false))
        {
            throw new InvalidOperationException("Warehouse not found.");
        }
        if (dto.StartDate == default)
            throw new InvalidOperationException("Start date is required.");
        if (dto.PeriodUnit != ShiftGenerationPeriodUnit.Custom &&
            dto.PeriodValue <= 0)
        {
            throw new InvalidOperationException(
                "Period value must be greater than zero.");
        }
        var startDate = dto.StartDate.Date;
        var endDate = ResolveEndDate(dto, startDate);
        if (endDate <= startDate)
        {
            throw new InvalidOperationException(
                "The generation end date must be after the start date.");
        }
        var workingDaySet = (dto.WorkingDays is { Count: > 0 }
                ? dto.WorkingDays
                : new List<DayOfWeek>
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                })
            .Distinct()
            .ToHashSet();
        if (workingDaySet.Any(day => !Enum.IsDefined(day)))
        {
            throw new InvalidOperationException(
                "Every working day must be a valid day of week.");
        }
        var excludedDates = BuildExcludedDates(
            startDate.Year,
            dto.ExcludedDates);
        var definitions = await ResolveShiftDefinitionsAsync(dto);
        ValidateShiftDefinitions(definitions);
        var existing = await context.Shifts
            .AsNoTracking()
            .Where(x =>
                x.WarehouseId == dto.WarehouseId &&
                x.ShiftDate >= startDate &&
                x.ShiftDate < endDate &&
                x.IsActive != false)
            .Select(x => new
            {
                x.ShiftDate,
                x.StartTime,
                x.EndTime
            })
            .ToListAsync();
        var existingKeys = existing
            .Select(x => (
                Date: x.ShiftDate.Date,
                x.StartTime,
                x.EndTime))
            .ToHashSet();
        var workingDays = 0;
        var created = 0;
        var skipped = 0;
        for (var date = startDate;
            date < endDate;
            date = date.AddDays(1))
        {
            if (!workingDaySet.Contains(date.DayOfWeek))
                continue;
            if (excludedDates.Contains(date.Date))
                continue;
            workingDays++;
            foreach (var definition in definitions)
            {
                var key = (
                    Date: date.Date,
                    definition.StartTime,
                    definition.EndTime);
                if (existingKeys.Contains(key))
                {
                    skipped++;
                    continue;
                }
                context.Shifts.Add(new Shift
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = dto.WarehouseId,
                    ShiftDate = date.Date,
                    ShiftName = definition.Name,
                    StartTime = definition.StartTime,
                    EndTime = definition.EndTime,
                    Status = "Scheduled",
                    CreateAt = DateTime.UtcNow
                });
                existingKeys.Add(key);
                created++;
            }
        }
        await context.SaveChangesAsync();
        return new GenerateShiftsResultDto(
            startDate,
            endDate.AddDays(-1),
            workingDays,
            created,
            skipped);
    }

    public async Task UpdateShiftAsync(Guid shiftId, UpdateManagerShiftDto dto)
    {
        var shift = await context.Shifts.FirstOrDefaultAsync(x => x.Id == shiftId && x.IsActive != false)
            ?? throw new InvalidOperationException("Shift not found.");
        if (shift.Status != "Scheduled")
            throw new InvalidOperationException("Only a scheduled shift can be edited.");
        if (string.IsNullOrWhiteSpace(dto.ShiftName))
            throw new InvalidOperationException("Shift name is required.");
        if (dto.StartTime >= dto.EndTime)
            throw new InvalidOperationException("Shift end time must be later than its start time.");
        if (!await context.Warehouses.AnyAsync(x => x.Id == dto.WarehouseId && x.IsActive != false))
            throw new InvalidOperationException("Warehouse not found.");

        var shiftDate = dto.ShiftDate.Date;
        var overlaps = await context.Shifts.AnyAsync(x => x.Id != shiftId
            && x.WarehouseId == dto.WarehouseId && x.ShiftDate == shiftDate
            && x.IsActive != false && dto.StartTime < x.EndTime && dto.EndTime > x.StartTime);
        if (overlaps)
            throw new InvalidOperationException("This time overlaps another shift at the selected warehouse.");

        shift.WarehouseId = dto.WarehouseId;
        shift.ShiftName = dto.ShiftName.Trim();
        shift.ShiftDate = shiftDate;
        shift.StartTime = dto.StartTime;
        shift.EndTime = dto.EndTime;
        shift.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task DeleteShiftAsync(Guid shiftId)
    {
        var shift = await context.Shifts.FirstOrDefaultAsync(x => x.Id == shiftId && x.IsActive != false)
            ?? throw new InvalidOperationException("Shift not found.");
        if (shift.Status != "Scheduled")
            throw new InvalidOperationException("Only a scheduled shift can be deleted.");
        var hasOperations = await context.OperationalTeams.AnyAsync(x => x.ShiftId == shiftId && x.IsActive != false)
            || await context.PickupAssignments.AnyAsync(x => x.ShiftId == shiftId && x.IsActive != false)
            || await context.IntakeBatches.AnyAsync(x => x.ShiftId == shiftId && x.IsActive != false);
        if (hasOperations)
            throw new InvalidOperationException("Cannot delete a shift that already has a team, assignments, or an intake batch.");

        shift.IsActive = false;
        shift.DeleteAt = DateTime.UtcNow;
        shift.UpdateAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<DeleteYearShiftsResultDto> DeleteYearShiftsAsync(DeleteYearShiftsDto dto)
    {
        if (dto.Year is < 2020 or > 2100)
            throw new InvalidOperationException("Year must be between 2020 and 2100.");
        if (!await context.Warehouses.AnyAsync(x => x.Id == dto.WarehouseId && x.IsActive != false))
            throw new InvalidOperationException("Warehouse not found.");

        var start = new DateTime(dto.Year, 1, 1);
        var end = start.AddYears(1);
        var shifts = await context.Shifts.Where(x => x.WarehouseId == dto.WarehouseId
            && x.ShiftDate >= start && x.ShiftDate < end && x.IsActive != false).ToListAsync();
        var shiftIds = shifts.Select(x => x.Id).ToList();
        var protectedIds = new HashSet<Guid>();
        if (shiftIds.Count > 0)
        {
            protectedIds.UnionWith(await context.OperationalTeams.Where(x => shiftIds.Contains(x.ShiftId)
                && x.IsActive != false).Select(x => x.ShiftId).Distinct().ToListAsync());
            protectedIds.UnionWith(await context.PickupAssignments.Where(x => shiftIds.Contains(x.ShiftId)
                && x.IsActive != false).Select(x => x.ShiftId).Distinct().ToListAsync());
            protectedIds.UnionWith(await context.IntakeBatches.Where(x => shiftIds.Contains(x.ShiftId)
                && x.IsActive != false).Select(x => x.ShiftId).Distinct().ToListAsync());
        }

        var deletable = shifts.Where(x => x.Status == "Scheduled" && !protectedIds.Contains(x.Id)).ToList();
        var now = DateTime.UtcNow;
        foreach (var shift in deletable)
        {
            shift.IsActive = false;
            shift.DeleteAt = now;
            shift.UpdateAt = now;
        }
        var templates = await context.WorkScheduleTemplates.Where(x => x.WarehouseId == dto.WarehouseId
            && x.Year == dto.Year && x.IsActive != false).ToListAsync();
        foreach (var template in templates)
        {
            template.IsActive = false;
            template.DeleteAt = now;
            template.UpdateAt = now;
        }
        await context.SaveChangesAsync();
        return new DeleteYearShiftsResultDto(deletable.Count, shifts.Count - deletable.Count);
    }

    public async Task<Guid> CreateTeamAsync(CreateReceivingTeamDto dto)
    {
        if (dto.StaffIds.Distinct().Count() != 2)
            throw new InvalidOperationException("A receiving team must have exactly two different staff members.");
        var teamType = dto.TeamType switch
        {
            "ReceivingWarehouse" => "ReceivingWarehouse",
            "ReceivingPickup" or "Receiving" => "ReceivingPickup",
            _ => throw new InvalidOperationException("Team type must be ReceivingPickup or ReceivingWarehouse.")
        };
        var shift = await context.Shifts.FirstOrDefaultAsync(x => x.Id == dto.ShiftId && x.IsActive != false)
            ?? throw new InvalidOperationException("Shift not found.");
        if (shift.Status != "Scheduled")
            throw new InvalidOperationException("A team can only be created for a scheduled shift.");
        var validStaff = await context.Users.Include(x => x.Role).CountAsync(x => dto.StaffIds.Contains(x.Id)
            && x.Role.RoleName == "ReceivingStaff" && x.WarehouseId == shift.WarehouseId
            && x.IsActive != false);
        if (validStaff != 2)
            throw new InvalidOperationException("Both members must be active ReceivingStaff users working at the shift warehouse.");
        var overlappingStaff = await context.TeamMembers
            .Where(x => dto.StaffIds.Contains(x.StaffId) && x.IsActive != false
                && x.Team.IsActive != false && x.Team.Shift.IsActive != false
                && x.Team.Shift.ShiftDate == shift.ShiftDate
                && x.Team.Shift.Status != "Completed"
                && x.Team.Shift.StartTime < shift.EndTime
                && shift.StartTime < x.Team.Shift.EndTime)
            .Select(x => x.Staff.FullName).Distinct().ToListAsync();
        if (overlappingStaff.Count != 0)
            throw new InvalidOperationException(
                $"Staff already assigned to an overlapping shift: {string.Join(", ", overlappingStaff)}.");
        if (teamType == "ReceivingWarehouse" && await context.OperationalTeams.AnyAsync(x =>
                x.ShiftId == shift.Id && x.IsActive != false && x.TeamType == "ReceivingWarehouse"))
            throw new InvalidOperationException("This shift already has a warehouse receiving team.");

        var team = new OperationalTeam
        {
            Id = Guid.NewGuid(), ShiftId = shift.Id, TeamName = dto.TeamName,
            TeamType = teamType, CreateAt = DateTime.UtcNow
        };
        context.OperationalTeams.Add(team);
        context.TeamMembers.AddRange(dto.StaffIds.Select(id => new TeamMember
        {
            Id = Guid.NewGuid(), TeamId = team.Id, StaffId = id, CreateAt = DateTime.UtcNow
        }));
        await context.SaveChangesAsync();
        return team.Id;
    }

    public async Task UpdateTeamAsync(Guid teamId, UpdateReceivingTeamDto dto)
    {
        if (dto.StaffIds.Distinct().Count() != 2)
            throw new InvalidOperationException("A receiving team must have exactly two different staff members.");
        if (string.IsNullOrWhiteSpace(dto.TeamName))
            throw new InvalidOperationException("Team name is required.");

        var team = await context.OperationalTeams.Include(x => x.Shift).Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == teamId && x.IsActive != false)
            ?? throw new InvalidOperationException("Receiving team not found.");
        if (team.Shift.Status != "Scheduled")
            throw new InvalidOperationException("Members can only be changed before the shift starts.");

        var validStaff = await context.Users.Include(x => x.Role).CountAsync(x => dto.StaffIds.Contains(x.Id)
            && x.Role.RoleName == "ReceivingStaff" && x.WarehouseId == team.Shift.WarehouseId
            && x.IsActive != false);
        if (validStaff != 2)
            throw new InvalidOperationException("Both members must be active ReceivingStaff users working at the shift warehouse.");

        var conflicts = await context.TeamMembers.AnyAsync(x => dto.StaffIds.Contains(x.StaffId)
            && x.TeamId != teamId && x.IsActive != false && x.Team.IsActive != false
            && x.Team.Shift.IsActive != false && x.Team.Shift.ShiftDate == team.Shift.ShiftDate
            && team.Shift.StartTime < x.Team.Shift.EndTime && team.Shift.EndTime > x.Team.Shift.StartTime);
        if (conflicts)
            throw new InvalidOperationException("A selected staff member is already assigned to an overlapping shift.");

        team.TeamName = dto.TeamName.Trim();
        team.UpdateAt = DateTime.UtcNow;
        foreach (var member in team.Members)
        {
            member.IsActive = dto.StaffIds.Contains(member.StaffId);
            member.UpdateAt = DateTime.UtcNow;
            member.DeleteAt = member.IsActive == false ? DateTime.UtcNow : null;
        }
        foreach (var staffId in dto.StaffIds.Where(id => team.Members.All(x => x.StaffId != id)))
            context.TeamMembers.Add(new TeamMember
            {
                Id = Guid.NewGuid(), TeamId = team.Id, StaffId = staffId,
                CreateAt = DateTime.UtcNow, IsActive = true
            });
        await context.SaveChangesAsync();
    }

    public async Task DeleteTeamAsync(Guid teamId)
    {
        var team = await context.OperationalTeams.Include(x => x.Shift)
            .FirstOrDefaultAsync(x => x.Id == teamId && x.IsActive != false)
            ?? throw new InvalidOperationException("Receiving team not found.");
        if (team.Shift.Status != "Scheduled")
            throw new InvalidOperationException("A team can only be deleted before the shift starts.");
        if (await context.PickupAssignments.AnyAsync(x => x.TeamId == teamId && x.IsActive != false)
            || await context.IntakeBatches.AnyAsync(x => x.ReceivingTeamId == teamId && x.IsActive != false))
            throw new InvalidOperationException("Move all requests out of this team before deleting it.");

        team.IsActive = false;
        team.DeleteAt = DateTime.UtcNow;
        var members = await context.TeamMembers.Where(x => x.TeamId == teamId && x.IsActive != false).ToListAsync();
        foreach (var member in members)
        {
            member.IsActive = false;
            member.DeleteAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
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
                && x.DeliveryMethod == "StaffPickup"
                && x.PickupDate.HasValue && x.PickupDate.Value.Date == shift.ShiftDate.Date
                && !alreadyPlanned.Contains(x.Id))
            .OrderBy(x => x.PickupDate)
            .ThenBy(x => x.PickupAddress)
            .ToListAsync();

        if (shift.StartTime < TimeSpan.FromHours(12))
            candidates = candidates.Where(x => !WasCreatedOnScheduledDate(x, shift.ShiftDate)).ToList();

        if (candidates.Count == 0) return 0;

        var batch = await context.IntakeBatches
            .FirstOrDefaultAsync(x => x.ShiftId == shift.Id && x.ReceivingTeamId == team.Id
                && x.IsActive != false);
        if (batch is null)
        {
            var areas = candidates.Select(x => ExtractArea(x.PickupAddress)).Distinct().ToList();
            batch = new IntakeBatch
            {
                Id = Guid.NewGuid(), WarehouseId = shift.WarehouseId, ShiftId = shift.Id, ReceivingTeamId = team.Id,
                IntakeDate = shift.ShiftDate.Date.Add(shift.StartTime), BatchCode = $"INT-{shift.ShiftDate:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                RouteName = string.Join(" → ", areas), Status = "Planned", CreateAt = DateTime.UtcNow,
                IsActive = true
            };
            context.IntakeBatches.Add(batch);
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
            NotificationWriter.NotifyDonor(context, request, "ReceivingStaffAssigned", "Đã phân công nhân viên tiếp nhận",
                $"được phân công vào team {team.TeamName}, ca {shift.ShiftName} ngày {shift.ShiftDate:dd/MM/yyyy}.");
            planned++;
        }
        await context.SaveChangesAsync();
        return planned;
    }

    public async Task<AutoBalanceResultDto> AutoBalanceShiftAsync(Guid shiftId)
    {
        var selectedShift = await context.Shifts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == shiftId && x.IsActive != false)
            ?? throw new InvalidOperationException("Shift not found.");
        if (selectedShift.Status != "Scheduled")
            throw new InvalidOperationException("Requests can only be balanced before the shift starts.");

        // Planning is performed for the whole warehouse working day. This prevents
        // the morning shift from consuming every request before the afternoon shift
        // is planned and lets geographic clusters be shared fairly across all teams.
        var dayShifts = await context.Shifts
            .Include(x => x.Teams.Where(t => t.IsActive != false))
                .ThenInclude(x => x.Members.Where(m => m.IsActive != false))
            .Where(x => x.IsActive != false
                && x.WarehouseId == selectedShift.WarehouseId
                && x.ShiftDate.Date == selectedShift.ShiftDate.Date
                && x.Status == "Scheduled")
            .OrderBy(x => x.StartTime)
            .ToListAsync();
        var allTeams = dayShifts.SelectMany(x => x.Teams)
            .Where(x => x.Members.Count(m => m.IsActive != false) == 2)
            .OrderBy(x => x.Shift.StartTime)
            .ThenBy(x => x.TeamName)
            .ThenBy(x => x.Id)
            .ToList();
        var pickupTeams = allTeams.Where(x => x.TeamType != "ReceivingWarehouse").ToList();
        var warehouseTeams = allTeams.Where(x => x.TeamType == "ReceivingWarehouse").ToList();
        if (allTeams.Count == 0)
            throw new InvalidOperationException(
                "Create at least one complete receiving team for this warehouse and working day before auto-assigning requests.");

        var dayShiftIds = dayShifts.Select(x => x.Id).ToList();
        var pendingAssignments = await context.PickupAssignments
            .Include(x => x.DonorRequest)
            .Where(x => dayShiftIds.Contains(x.ShiftId)
                && x.IsActive != false && x.Status == "Pending")
            .ToListAsync();
        var assignedElsewhere = context.PickupAssignments
            .Where(x => x.IsActive != false && !dayShiftIds.Contains(x.ShiftId))
            .Select(x => x.DonorRequestId);
        var unassigned = await context.DonationRequests
            .Where(x => x.WarehouseId == selectedShift.WarehouseId && x.IsActive != false
                && (x.Status == DonationRequestStatus.WaitingReceivingStaff
                    || x.Status == DonationRequestStatus.PendingStaffAssign)
                && (x.DeliveryMethod == "StaffPickup" || x.DeliveryMethod == "DonorDropOff")
                && x.PickupDate.HasValue && x.PickupDate.Value.Date == selectedShift.ShiftDate.Date
                && !assignedElsewhere.Contains(x.Id))
            .ToListAsync();

        var requests = pendingAssignments.Select(x => x.DonorRequest).Concat(unassigned)
            .GroupBy(x => x.Id).Select(x => x.First())
            .OrderBy(x => ExtractArea(x.PickupAddress))
            .ThenBy(x => x.PickupAddress).ThenBy(x => x.Id).ToList();
        if (requests.Count == 0)
            return new AutoBalanceResultDto(allTeams.Count, 0,
                allTeams.ToDictionary(x => x.Id, _ => 0));
        var pickupRequests = requests.Where(x => x.DeliveryMethod == "StaffPickup").ToList();
        var dropOffRequests = requests.Where(x => x.DeliveryMethod == "DonorDropOff").ToList();
        var afternoonPickupTeams = pickupTeams.Where(x => x.Shift.StartTime >= TimeSpan.FromHours(12)).ToList();
        var afternoonWarehouseTeams = warehouseTeams.Where(x => x.Shift.StartTime >= TimeSpan.FromHours(12)).ToList();
        if (pickupRequests.Count != 0 && pickupTeams.Count == 0)
            throw new InvalidOperationException("Create at least one pickup team before assigning staff-pickup requests.");
        if (dropOffRequests.Count != 0 && warehouseTeams.Count == 0)
            throw new InvalidOperationException(
                "There are warehouse drop-off requests for this day. Create a warehouse receiving team before dispatching.");
        if (dropOffRequests.Count != 0)
        {
            var shiftsWithoutWarehouseDuty = dayShifts.Where(shift =>
                !warehouseTeams.Any(team => team.ShiftId == shift.Id)).Select(x => x.ShiftName).ToList();
            if (shiftsWithoutWarehouseDuty.Count != 0)
                throw new InvalidOperationException(
                    $"Create a warehouse receiving team for every shift before dispatching: {string.Join(", ", shiftsWithoutWarehouseDuty)}.");
        }

        var batches = await context.IntakeBatches
            .Where(x => dayShiftIds.Contains(x.ShiftId) && x.IsActive != false)
            .ToListAsync();
        var batchByTeam = new Dictionary<Guid, IntakeBatch>();
        foreach (var team in allTeams)
        {
            var batch = batches.FirstOrDefault(x => x.ReceivingTeamId == team.Id);
            if (batch is null)
            {
                batch = new IntakeBatch
                {
                    Id = Guid.NewGuid(), WarehouseId = selectedShift.WarehouseId, ShiftId = team.ShiftId,
                    ReceivingTeamId = team.Id,
                    IntakeDate = team.Shift.ShiftDate.Date.Add(team.Shift.StartTime),
                    BatchCode = $"INT-{team.Shift.ShiftDate:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                    RouteName = string.Empty, Status = "Planned", CreateAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.IntakeBatches.Add(batch);
            }
            batchByTeam[team.Id] = batch;
        }

        var assignmentByRequest = pendingAssignments.ToDictionary(x => x.DonorRequestId);
        var counts = allTeams.ToDictionary(x => x.Id, _ => 0);

        void AssignBalanced(List<DonationRequest> sourceRequests,
            List<OperationalTeam> targetTeams, bool warehouseDropOff)
        {
            if (sourceRequests.Count == 0) return;
            var baseSize = sourceRequests.Count / targetTeams.Count;
            var remainder = sourceRequests.Count % targetTeams.Count;
            var offset = 0;
            for (var teamIndex = 0; teamIndex < targetTeams.Count; teamIndex++)
            {
                var team = targetTeams[teamIndex];
                var quota = baseSize + (teamIndex < remainder ? 1 : 0);
                var teamRequests = sourceRequests.Skip(offset).Take(quota).ToList();
                offset += quota;
                var existingCount = counts[team.Id];
                counts[team.Id] += teamRequests.Count;
                var batch = batchByTeam[team.Id];
                batch.RouteName = warehouseDropOff
                    ? "Nhận trực tiếp tại kho"
                    : string.Join(" → ", teamRequests.Select(x => ExtractArea(x.PickupAddress)).Distinct());
                batch.UpdateAt = DateTime.UtcNow;

                for (var index = 0; index < teamRequests.Count; index++)
                {
                    var request = teamRequests[index];
                    if (!assignmentByRequest.TryGetValue(request.Id, out var assignment))
                    {
                        assignment = new PickupAssignment
                        {
                            Id = Guid.NewGuid(), DonorRequestId = request.Id,
                            CreateAt = DateTime.UtcNow, IsActive = true, Status = "Pending"
                        };
                        context.PickupAssignments.Add(assignment);
                        assignmentByRequest[request.Id] = assignment;
                    }
                    assignment.ShiftId = team.ShiftId;
                    assignment.TeamId = team.Id;
                    assignment.IntakeBatchId = batch.Id;
                    assignment.RouteOrder = existingCount + index + 1;
                    assignment.AreaKey = warehouseDropOff ? "Tại kho" : ExtractArea(request.PickupAddress);
                    assignment.UpdateAt = DateTime.UtcNow;
                    request.Status = DonationRequestStatus.ReceivingStaffAssigned;
                    request.UpdateAt = DateTime.UtcNow;
                }
            }
        }
        var pickupCreatedToday = pickupRequests.Where(x => WasCreatedOnScheduledDate(x, selectedShift.ShiftDate)).ToList();
        var pickupPlannedEarlier = pickupRequests.Except(pickupCreatedToday).ToList();
        var dropOffCreatedToday = dropOffRequests.Where(x => WasCreatedOnScheduledDate(x, selectedShift.ShiftDate)).ToList();
        var dropOffPlannedEarlier = dropOffRequests.Except(dropOffCreatedToday).ToList();
        if (pickupCreatedToday.Count > 0 && afternoonPickupTeams.Count == 0)
            throw new InvalidOperationException("Create a complete afternoon pickup team for requests created this morning.");
        if (dropOffCreatedToday.Count > 0 && afternoonWarehouseTeams.Count == 0)
            throw new InvalidOperationException("Create a complete afternoon warehouse team for drop-offs created this morning.");
        AssignBalanced(pickupPlannedEarlier, pickupTeams, false);
        AssignBalanced(pickupCreatedToday, afternoonPickupTeams, false);
        AssignBalanced(dropOffPlannedEarlier, warehouseTeams, true);
        AssignBalanced(dropOffCreatedToday, afternoonWarehouseTeams, true);
        foreach (var request in requests)
            NotificationWriter.NotifyDonor(context, request, "ReceivingStaffAssigned", "Đã phân công nhân viên tiếp nhận",
                $"được hệ thống điều phối vào team tiếp nhận ngày {selectedShift.ShiftDate:dd/MM/yyyy}.");
        await context.SaveChangesAsync();
        return new AutoBalanceResultDto(allTeams.Count, pickupRequests.Count, counts);
    }

    public async Task<ReceivingDispatchBoardDto> GetDispatchBoardAsync()
    {
        var assignedIds = context.PickupAssignments.Where(x => x.IsActive != false)
            .Select(x => x.DonorRequestId);
        var requests = await context.DonationRequests.AsNoTracking()
            .Include(x => x.Warehouse)
            .Where(x => x.IsActive != false
                && (x.Status == DonationRequestStatus.WaitingReceivingStaff
                    || x.Status == DonationRequestStatus.PendingStaffAssign)
                && !assignedIds.Contains(x.Id))
            .OrderBy(x => x.PickupDate).ThenBy(x => x.CreateAt)
            .Select(x => new DispatchRequestDto(
                x.Id, x.RequestCode,
                x.ContactName, x.ContactPhoneNumber, x.DeliveryMethod, x.PickupAddress,
                x.PickupDate, x.WarehouseId, x.Warehouse.WarehouseName, x.CreateAt))
            .ToListAsync();

        var teams = await context.OperationalTeams.AsNoTracking()
            .Include(x => x.Shift)
            .Include(x => x.Members).ThenInclude(x => x.Staff)
            .Where(x => x.IsActive != false
                && (x.TeamType == "Receiving" || x.TeamType == "ReceivingPickup"
                    || x.TeamType == "ReceivingWarehouse")
                && x.Shift.IsActive != false && x.Shift.Status != "Completed")
            .OrderBy(x => x.Shift.ShiftDate).ThenBy(x => x.Shift.StartTime)
            .Select(x => new DispatchTeamDto(
                x.Id, x.TeamName, x.TeamType, x.ShiftId, x.Shift.ShiftName, x.Shift.ShiftDate,
                $"{x.Shift.StartTime:hh\\:mm} - {x.Shift.EndTime:hh\\:mm}", x.Shift.WarehouseId,
                x.Members.Where(m => m.IsActive != false)
                    .Select(m => new ReceivingTeamMemberDto(m.StaffId, m.Staff.FullName, m.Staff.PhoneNumber)).ToList()))
            .ToListAsync();
        return new ReceivingDispatchBoardDto(requests, teams);
    }

    public async Task<ManagerReceivingSetupDto> GetManagerSetupAsync()
    {
        var warehouses = await context.Warehouses.AsNoTracking()
            .Where(x => x.IsActive != false).OrderBy(x => x.WarehouseName)
            .Select(x => new ManagerWarehouseOptionDto(x.Id, x.WarehouseName, x.Address)).ToListAsync();
        var staff = await context.Users.AsNoTracking()
            .Where(x => x.IsActive != false && x.Role.RoleName == "ReceivingStaff")
            .OrderBy(x => x.FullName)
            .Select(x => new ManagerStaffOptionDto(x.Id, x.FullName, x.UserName, x.PhoneNumber,
                x.WarehouseId)).ToListAsync();
        var shifts = await context.Shifts.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Teams.Where(t => t.IsActive != false))
                .ThenInclude(x => x.Members.Where(m => m.IsActive != false)).ThenInclude(x => x.Staff)
            .Include(x => x.IntakeBatches.Where(b => b.IsActive != false))
                .ThenInclude(x => x.PickupAssignments.Where(a => a.IsActive != false))
                    .ThenInclude(x => x.DonorRequest)
            .Where(x => x.IsActive != false)
            .OrderByDescending(x => x.ShiftDate).ThenBy(x => x.StartTime)
            .ToListAsync();
        var dropOffDemand = await context.DonationRequests.AsNoTracking()
            .Where(request => request.IsActive != false && request.DeliveryMethod == "DonorDropOff"
                && request.PickupDate.HasValue
                && (request.Status == DonationRequestStatus.PendingStaffAssign
                    || request.Status == DonationRequestStatus.WaitingReceivingStaff))
            .GroupBy(request => new { request.WarehouseId, Date = request.PickupDate!.Value.Date })
            .Select(group => new { group.Key.WarehouseId, group.Key.Date, Count = group.Count() })
            .ToDictionaryAsync(x => (x.WarehouseId, x.Date), x => x.Count);
        var shiftDtos = shifts.Select(x =>
        {
            var teamDtos = x.Teams.OrderBy(t => t.TeamName).Select(team =>
            {
                var batch = x.IntakeBatches.FirstOrDefault(b => b.ReceivingTeamId == team.Id);
                var requests = batch?.PickupAssignments.OrderBy(a => a.RouteOrder).Select(a =>
                    new ManagerAssignedRequestDto(a.DonorRequestId,
                        a.DonorRequest.RequestCode,
                        a.DonorRequest.ContactName, a.DonorRequest.ContactPhoneNumber,
                        a.DonorRequest.PickupAddress, a.DonorRequest.PickupDate,
                        a.DonorRequest.DeliveryMethod, a.Status, a.RouteOrder)).ToList() ?? [];
                return new ManagerTeamOverviewDto(team.Id, team.TeamName, team.TeamType,
                    team.Members.Select(m => new ReceivingTeamMemberDto(
                        m.StaffId, m.Staff.FullName, m.Staff.PhoneNumber)).ToList(),
                    batch?.Id, batch?.BatchCode, batch?.Status, batch?.RouteName,
                    batch?.TotalWeight ?? 0, requests);
            }).ToList();
            dropOffDemand.TryGetValue((x.WarehouseId, x.ShiftDate.Date), out var pendingDropOffRequests);
            return new ManagerShiftOverviewDto(x.Id, x.WarehouseId, x.Warehouse.WarehouseName,
                x.ShiftName, x.ShiftDate, x.StartTime, x.EndTime, x.Status,
                teamDtos, teamDtos.Sum(t => t.Requests.Count), pendingDropOffRequests);
        }).ToList();
        return new ManagerReceivingSetupDto(warehouses, staff, shiftDtos);
    }

    public async Task AssignRequestAsync(AssignDonationRequestDto dto)
    {
        var request = await context.DonationRequests.Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == dto.RequestId && x.IsActive != false)
            ?? throw new InvalidOperationException("Donation request not found.");
        var existingAssignment = await context.PickupAssignments
            .FirstOrDefaultAsync(x => x.DonorRequestId == dto.RequestId && x.IsActive != false);
        if (existingAssignment is not null && existingAssignment.Status != "Pending")
            throw new InvalidOperationException("Only a pending assignment can be moved to another team.");
        var team = await context.OperationalTeams.Include(x => x.Shift).Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == dto.TeamId && x.IsActive != false
                && (x.TeamType == "Receiving" || x.TeamType == "ReceivingPickup"
                    || x.TeamType == "ReceivingWarehouse"))
            ?? throw new InvalidOperationException("Receiving team not found.");
        if (team.Members.Count(x => x.IsActive != false) != 2)
            throw new InvalidOperationException("Receiving team must contain exactly two members.");
        if (team.Shift.WarehouseId != request.WarehouseId)
            throw new InvalidOperationException("The team and donation request must belong to the same warehouse.");
        if (!request.PickupDate.HasValue || request.PickupDate.Value.Date != team.Shift.ShiftDate.Date)
            throw new InvalidOperationException("The team shift date must match the donation pickup appointment date.");
        if (WasCreatedOnScheduledDate(request, team.Shift.ShiftDate)
            && team.Shift.StartTime < TimeSpan.FromHours(12))
            throw new InvalidOperationException(
                "A request created this morning can only be assigned to an afternoon shift.");
        var warehouseTeam = team.TeamType == "ReceivingWarehouse";
        if (request.DeliveryMethod == "DonorDropOff" && !warehouseTeam)
            throw new InvalidOperationException("A warehouse drop-off request can only be assigned to a warehouse receiving team.");
        if (request.DeliveryMethod == "StaffPickup" && warehouseTeam)
            throw new InvalidOperationException("A staff-pickup request can only be assigned to a pickup team.");

        var batch = await context.IntakeBatches.FirstOrDefaultAsync(x => x.ShiftId == team.ShiftId
            && x.ReceivingTeamId == team.Id && x.IsActive != false);
        if (batch is null)
        {
            batch = new IntakeBatch
            {
                Id = Guid.NewGuid(), WarehouseId = request.WarehouseId, ShiftId = team.ShiftId,
                ReceivingTeamId = team.Id, IntakeDate = team.Shift.ShiftDate.Date.Add(team.Shift.StartTime),
                BatchCode = $"INT-{team.Shift.ShiftDate:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                RouteName = request.DeliveryMethod == "DonorDropOff" ? "Nhận trực tiếp tại kho" : ExtractArea(request.PickupAddress),
                Status = "Planned", CreateAt = DateTime.UtcNow, IsActive = true
            };
            context.IntakeBatches.Add(batch);
        }
        var order = await context.PickupAssignments.Where(x => x.IntakeBatchId == batch.Id && x.IsActive != false)
            .Select(x => (int?)x.RouteOrder).MaxAsync() ?? 0;
        var assignment = existingAssignment ?? new PickupAssignment
        {
            Id = Guid.NewGuid(), DonorRequestId = request.Id,
            Status = "Pending", CreateAt = DateTime.UtcNow, IsActive = true
        };
        if (existingAssignment is null) context.PickupAssignments.Add(assignment);
        assignment.ShiftId = team.ShiftId;
        assignment.TeamId = team.Id;
        assignment.IntakeBatchId = batch.Id;
        assignment.RouteOrder = order + 1;
        assignment.AreaKey = request.DeliveryMethod == "DonorDropOff" ? "Tại kho" : ExtractArea(request.PickupAddress);
        assignment.UpdateAt = DateTime.UtcNow;
        request.Status = DonationRequestStatus.ReceivingStaffAssigned;
        request.UpdateAt = DateTime.UtcNow;
        NotificationWriter.NotifyDonor(context, request, "ReceivingStaffAssigned", "Đã phân công nhân viên tiếp nhận",
            $"được phân công vào team {team.TeamName}, ca ngày {team.Shift.ShiftDate:dd/MM/yyyy}.");
        await context.SaveChangesAsync();
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
        var actor = await NotificationWriter.ActorNameAsync(context, staffId);
        NotificationWriter.NotifyDonor(context, assignment.DonorRequest, "DonationReceived", "Đã tiếp nhận đồ quyên góp",
            $"được {actor} tiếp nhận lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}, khối lượng {dto.ActualWeight:0.##} kg.", staffId);
        await context.SaveChangesAsync();
    }

    public async Task<WarehouseDropOffBoardDto> GetMyWarehouseDropOffsAsync(Guid staffId)
    {
        var fromDate = DateTime.Today.AddDays(-1);
        var dutyContexts = await context.OperationalTeams.AsNoTracking()
            .Where(team => team.IsActive != false && team.TeamType == "ReceivingWarehouse"
                && team.Members.Any(member => member.StaffId == staffId && member.IsActive != false)
                && team.Shift.IsActive != false && team.Shift.ShiftDate >= fromDate)
            .Include(team => team.Shift).ThenInclude(shift => shift.Warehouse)
            .Include(team => team.IntakeBatches.Where(batch => batch.IsActive != false))
            .OrderBy(team => team.Shift.ShiftDate).ThenBy(team => team.Shift.StartTime)
            .ToListAsync();

        var contexts = dutyContexts.Select(team => new WarehouseDutyContextDto(
            team.Id, team.TeamName, team.ShiftId, team.Shift.ShiftName, team.Shift.ShiftDate,
            team.Shift.StartTime, team.Shift.EndTime, team.Shift.Status,
            team.Shift.WarehouseId, team.Shift.Warehouse.WarehouseName, team.Shift.Warehouse.Address,
            team.IntakeBatches.FirstOrDefault()?.Id)).ToList();
        if (contexts.Count == 0) return new WarehouseDropOffBoardDto([], []);

        var warehouseIds = contexts.Select(x => x.WarehouseId).Distinct().ToList();
        var dates = contexts.Select(x => x.ShiftDate.Date).Distinct().ToList();
        var requests = await context.DonationRequests.AsNoTracking()
            .Where(request => request.IsActive != false && request.DeliveryMethod == "DonorDropOff"
                && request.PickupDate.HasValue && warehouseIds.Contains(request.WarehouseId)
                && dates.Contains(request.PickupDate.Value.Date)
                && (request.Status == DonationRequestStatus.PendingStaffAssign
                    || request.Status == DonationRequestStatus.WaitingReceivingStaff))
            .OrderBy(request => request.PickupDate).ThenBy(request => request.CreateAt)
            .Select(request => new WarehouseDropOffItemDto(
                request.Id, request.WarehouseId,
                request.RequestCode,
                request.ContactName, request.ContactPhoneNumber, request.PickupAddress,
                request.PickupDate!.Value, request.Description ?? string.Empty,
                request.EstimateWeight, request.Status.ToString(), request.ImageUrls))
            .ToListAsync();
        return new WarehouseDropOffBoardDto(contexts, requests);
    }

    public async Task ConfirmWarehouseDropOffAsync(Guid staffId, Guid requestId, ConfirmPickupDto dto)
    {
        if (dto.ActualWeight <= 0)
            throw new InvalidOperationException("Actual weight must be greater than zero.");
        var request = await context.DonationRequests
            .FirstOrDefaultAsync(x => x.Id == requestId && x.IsActive != false
                && x.DeliveryMethod == "DonorDropOff")
            ?? throw new InvalidOperationException("Warehouse drop-off request not found.");
        if (!request.PickupDate.HasValue)
            throw new InvalidOperationException("The request does not have an expected warehouse delivery date.");
        if (request.Status != DonationRequestStatus.PendingStaffAssign
            && request.Status != DonationRequestStatus.WaitingReceivingStaff)
            throw new InvalidOperationException("This warehouse drop-off request has already been processed.");
        if (await context.PickupAssignments.AnyAsync(x =>
                x.DonorRequestId == request.Id && x.IsActive != false))
            throw new InvalidOperationException("This request is already assigned or received.");

        var team = await context.OperationalTeams
            .Include(x => x.Shift)
            .Include(x => x.Members)
            .Include(x => x.IntakeBatches)
            .Where(x => x.IsActive != false && x.TeamType == "ReceivingWarehouse"
                && x.Shift.IsActive != false && x.Shift.Status == "InProgress"
                && x.Shift.WarehouseId == request.WarehouseId
                && x.Shift.ShiftDate.Date == request.PickupDate.Value.Date
                && x.Members.Any(member => member.StaffId == staffId && member.IsActive != false))
            .OrderBy(x => x.Shift.StartTime)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException(
                "Start your warehouse receiving shift before confirming a donor drop-off.");

        var batch = team.IntakeBatches.FirstOrDefault(x => x.IsActive != false);
        if (batch is null)
        {
            batch = new IntakeBatch
            {
                Id = Guid.NewGuid(), WarehouseId = request.WarehouseId, ShiftId = team.ShiftId,
                ReceivingTeamId = team.Id, IntakeDate = team.Shift.ShiftDate.Date.Add(team.Shift.StartTime),
                BatchCode = $"INT-{team.Shift.ShiftDate:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                RouteName = "Nhận trực tiếp tại kho", Status = "Receiving",
                StartedAt = DateTime.UtcNow, CreateAt = DateTime.UtcNow, IsActive = true
            };
            context.IntakeBatches.Add(batch);
        }
        if (batch.Status != "Receiving")
            throw new InvalidOperationException("The warehouse receiving intake batch is not active.");

        var routeOrder = await context.PickupAssignments
            .Where(x => x.IntakeBatchId == batch.Id && x.IsActive != false)
            .Select(x => (int?)x.RouteOrder).MaxAsync() ?? 0;
        context.PickupAssignments.Add(new PickupAssignment
        {
            Id = Guid.NewGuid(), DonorRequestId = request.Id, ShiftId = team.ShiftId,
            TeamId = team.Id, IntakeBatchId = batch.Id, RouteOrder = routeOrder + 1,
            AreaKey = "Tại kho", Status = "Received", ProcessedAt = DateTime.UtcNow,
            Notes = dto.Notes, CreateAt = DateTime.UtcNow, IsActive = true
        });
        context.IntakeBatchDonationRequests.Add(new IntakeBatchDonationRequest
        {
            Id = Guid.NewGuid(), IntakeBatchId = batch.Id, DonationRequestId = request.Id,
            AddedAt = DateTime.UtcNow, AddedByStaffId = staffId,
            CreateAt = DateTime.UtcNow, IsActive = true
        });
        request.ActualWeight = dto.ActualWeight;
        request.ImageUrls = dto.ImageUrls ?? request.ImageUrls;
        request.Status = DonationRequestStatus.Confirmed;
        request.UpdateAt = DateTime.UtcNow;
        batch.TotalWeight += dto.ActualWeight;
        batch.UpdateAt = DateTime.UtcNow;
        var actor = await NotificationWriter.ActorNameAsync(context, staffId);
        NotificationWriter.NotifyDonor(context, request, "DonationReceived", "Đã tiếp nhận tại kho",
            $"được {actor} tiếp nhận lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}, khối lượng {dto.ActualWeight:0.##} kg.", staffId);
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

    public async Task SendToClassificationAsync(Guid staffId, Guid batchId)
    {
        var batch = await RequireMyBatch(staffId, batchId);
        if (batch.Status != "Completed")
            throw new InvalidOperationException("Only a completed intake batch can be sent to classification.");
        if (!batch.IntakeBatchDonationRequests.Any())
            throw new InvalidOperationException("The intake batch does not contain any received donation request.");
        batch.Status = "SentToClassification";
        batch.SentToClassificationAt = DateTime.UtcNow;
        batch.UpdateAt = DateTime.UtcNow;
        batch.UpdatedBy = staffId;
        await NotificationWriter.NotifyDonorsAsync(context,
            batch.IntakeBatchDonationRequests.Select(x => x.DonationRequestId),
            "SentToClassification", "Đã chuyển sang phân loại",
            _ => $"đã được chuyển trong lô {batch.BatchCode} lúc {NotificationWriter.FormatTime(DateTime.UtcNow)}.", staffId);
        await context.SaveChangesAsync();
    }

    private IQueryable<IntakeBatch> MyBatchQuery(Guid staffId) => context.IntakeBatches.AsNoTracking()
        .Include(x => x.Warehouse)
        .Include(x => x.ReceivingTeam!).ThenInclude(x => x.Shift)
        .Include(x => x.ReceivingTeam!).ThenInclude(x => x.Members).ThenInclude(x => x.Staff)
        .Include(x => x.PickupAssignments.Where(a => a.IsActive != false)).ThenInclude(x => x.DonorRequest).ThenInclude(x => x.Donor)
        .Where(x => x.IsActive != false && x.ReceivingTeam!.Members.Any(m => m.StaffId == staffId && m.IsActive != false));

    private async Task<IntakeBatch> RequireMyBatch(Guid staffId, Guid batchId) =>
        await context.IntakeBatches.Include(x => x.Warehouse)
            .Include(x => x.ReceivingTeam!).ThenInclude(x => x.Members).ThenInclude(x => x.Staff)
            .Include(x => x.ReceivingTeam!).ThenInclude(x => x.Shift)
            .Include(x => x.PickupAssignments).ThenInclude(x => x.DonorRequest).ThenInclude(x => x.Donor)
            .Include(x => x.IntakeBatchDonationRequests)
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
        TeamName = batch.ReceivingTeam?.TeamName ?? string.Empty,
        WarehouseAddress = batch.Warehouse?.Address ?? string.Empty,
        TeamMembers = batch.ReceivingTeam?.Members.Where(x => x.IsActive != false)
            .Select(x => new ReceivingTeamMemberDto(x.StaffId, x.Staff.FullName, x.Staff.PhoneNumber)).ToList() ?? [],
        Requests = batch.PickupAssignments.OrderBy(x => x.RouteOrder).Select(x => new ReceivingRequestDto
        {
            Id = x.DonorRequestId, BatchId = batch.Id,
            Code = x.DonorRequest.RequestCode,
            DonorName = x.DonorRequest.ContactName, PhoneNumber = x.DonorRequest.ContactPhoneNumber,
            PickupAddress = x.DonorRequest.PickupAddress, Description = x.DonorRequest.Description ?? string.Empty,
            EstimateWeight = x.DonorRequest.EstimateWeight, ActualWeight = x.DonorRequest.ActualWeight,
            PickupDate = x.DonorRequest.PickupDate, Status = x.Status, Notes = x.Notes,
            DeliveryMethod = x.DonorRequest.DeliveryMethod,
            ImageUrls = x.DonorRequest.ImageUrls
        }).ToList()
    };

    private static bool WasCreatedOnScheduledDate(DonationRequest request, DateTime scheduledDate)
    {
        if (!request.CreateAt.HasValue) return false;
        return VietnamTime.IsSameLocalDate(request.CreateAt.Value, scheduledDate);
    }

    private static string ExtractArea(string address)
    {
        var match = Regex.Match(address, @"(?i)(quận|q\.?|huyện|thành phố|tp\.?|thủ đức)\s*[^,]+", RegexOptions.CultureInvariant);
        return match.Success ? match.Value.Trim() : address.Split(',', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? "Khu vực khác";
    }

    private static DateTime ResolveEndDate(GenerateShiftsV2Dto dto, DateTime startDate)
    {
        if (dto.PeriodUnit == ShiftGenerationPeriodUnit.Custom)
        {
            if (dto.CustomEndDate is null)
            {
                throw new InvalidOperationException("Custom end date is required when using a custom period.");
            }
            var customEndDate = dto.CustomEndDate.Value.Date;
            return customEndDate.AddDays(1);
        }
        return dto.PeriodUnit switch
        {
            ShiftGenerationPeriodUnit.Day => startDate.AddDays(dto.PeriodValue),
            ShiftGenerationPeriodUnit.Week => startDate.AddDays(dto.PeriodValue * 7),
            ShiftGenerationPeriodUnit.Month => startDate.AddMonths(dto.PeriodValue),
            ShiftGenerationPeriodUnit.Quarter => startDate.AddMonths(dto.PeriodValue * 3),
            ShiftGenerationPeriodUnit.Year => startDate.AddYears(dto.PeriodValue),
            _ => throw new InvalidOperationException("Unsupported shift generation period.")
        };
    }

    private static HashSet<DateTime> BuildExcludedDates(int year, List<DateTime>? additionalDates)
    {
        var excludedDates = new HashSet<DateTime>
        {
            new DateTime(year, 1, 1), new DateTime(year, 4, 30), new DateTime(year, 5, 1), new DateTime(year, 9, 2)
        };
        foreach (var date in additionalDates ?? [])
        {
            excludedDates.Add(date.Date);
        }
        return excludedDates;
    }

    private async Task<List<ShiftDefinitionDto>> ResolveShiftDefinitionsAsync(GenerateShiftsV2Dto dto)
    {
        if (dto.ShiftDefinitions is { Count: > 0 })
        {
            return dto.ShiftDefinitions;
        }
        var template = await context.WorkScheduleTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.WarehouseId == dto.WarehouseId &&
                x.Year == dto.StartDate.Year &&
                x.IsActive != false);
        if (template is not null)
        {
            return
            [
                new ShiftDefinitionDto(
                    "Ca sáng",
                    template.MorningStartTime,
                    template.MorningEndTime),
                new ShiftDefinitionDto(
                    "Ca chiều",
                    template.AfternoonStartTime,
                    template.AfternoonEndTime)
            ];
        }
        // Default values for the creation form / backward-compatible behavior.
        return
        [
            new ShiftDefinitionDto(
                "Ca sáng",
                new TimeSpan(8, 0, 0),
                new TimeSpan(11, 0, 0)),
            new ShiftDefinitionDto(
                "Ca chiều",
                new TimeSpan(13, 0, 0),
                new TimeSpan(17, 0, 0))
        ];
    }

    private static void ValidateShiftDefinitions(IEnumerable<ShiftDefinitionDto> definitions)
    {
        var definitionList = definitions.ToList();
        if (definitionList.Count == 0)
        {
            throw new InvalidOperationException("At least one shift definition is required.");
        }
        foreach (var definition in definitionList)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                throw new InvalidOperationException("Shift name is required.");
            }
            if (definition.StartTime >= definition.EndTime)
            {
                throw new InvalidOperationException($"Shift '{definition.Name}' must end after it starts.");
            }
        }

        for (var i = 0; i < definitionList.Count; i++)
        {
            for (var j = i + 1; j < definitionList.Count; j++)
            {
                var first = definitionList[i]; var second = definitionList[j];
                var overlaps =
                    first.StartTime < second.EndTime &&
                    first.EndTime > second.StartTime;
                if (overlaps)
                {
                    throw new InvalidOperationException(
                        $"Shift definitions '{first.Name}' and '{second.Name}' overlap.");
                }
            }
        }
    }
}
