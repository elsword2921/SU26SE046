using DAL;
using Microsoft.EntityFrameworkCore;

namespace BLL.Common;

public static class ShiftLifecycle
{
    public static async Task CompleteEndedShiftsAsync(AppDbContext context)
    {
        var now = VietnamTime.Now;
        var candidates = await context.Shifts
            .Include(x => x.Teams.Where(team => team.IsActive != false))
            .Where(x => x.IsActive != false
                && (x.Status != "Completed" || x.Teams.Any(team =>
                    team.IsActive != false && team.Status != "Completed")))
            .ToListAsync();

        var changed = false;
        foreach (var shift in candidates)
        {
            var scheduledEnd = shift.ShiftDate.Date.Add(shift.EndTime);
            if (now < scheduledEnd) continue;

            if (shift.Status != "Completed")
            {
                shift.Status = "Completed";
                shift.CompletedAt ??= scheduledEnd;
                shift.UpdateAt = now;
                changed = true;
            }

            foreach (var team in shift.Teams.Where(x => x.Status != "Completed"))
            {
                team.Status = "Completed";
                team.CompletedAt ??= scheduledEnd;
                team.CompletedByStaffId = null;
                team.UpdateAt = now;
                changed = true;
            }
        }

        if (changed) await context.SaveChangesAsync();
    }
}
