using DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BLL.Common;

namespace Capstone_API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController(AppDbContext context) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int take = 30)
    {
        take = Math.Clamp(take, 1, 100);
        var candidates = await context.Notifications.AsNoTracking()
            .Where(x => x.UserId == UserId && x.IsActive != false)
            .OrderByDescending(x => x.CreateAt).Take(Math.Min(take * 3, 300))
            .Select(x => new { x.Id, x.Type, x.Title, x.Message, x.TargetUrl,
                x.DonationRequestId, x.IsRead, CreatedAt = x.CreateAt })
            .ToListAsync();
        // Hide legacy duplicates produced by concurrent workflow requests. New writes are
        // protected by atomic status transitions in their corresponding operations.
        var items = candidates
            .GroupBy(x => new { x.Type, x.DonationRequestId, x.Title, x.Message, x.TargetUrl })
            .Select(group => group.OrderByDescending(x => x.CreatedAt).First())
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToList();
        return Ok(new { unreadCount = items.Count(x => !x.IsRead), items });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var item = await context.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId && x.IsActive != false);
        if (item is null) return NotFound();
        item.IsRead = true; item.ReadAt = VietnamTime.Now; item.UpdateAt = VietnamTime.Now;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await context.Notifications.Where(x => x.UserId == UserId && !x.IsRead && x.IsActive != false)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.ReadAt, VietnamTime.Now).SetProperty(x => x.UpdateAt, VietnamTime.Now));
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearAll()
    {
        await context.Notifications.Where(x => x.UserId == UserId).ExecuteDeleteAsync();
        return NoContent();
    }
}
