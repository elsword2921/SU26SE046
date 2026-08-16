using BLL.Common;
using BLL.DTOs;
using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_API.Controllers;

[ApiController]
[Authorize(Roles = "Donor,ReceivingStaff")]
[Route("api/donation-chat")]
public class DonationChatController(AppDbContext context) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> Get(Guid requestId)
    {
        await EnsureParticipantAsync(requestId, CurrentUserId);
        var messages = await context.DonationChatMessages.AsNoTracking()
            .Where(x => x.DonationRequestId == requestId && x.IsActive != false)
            .OrderBy(x => x.SentAt)
            .Select(x => new DonationChatMessageDto(x.Id, x.SenderId, x.Sender.FullName,
                x.Sender.Role.RoleName, x.Message, x.SentAt, x.SenderId == CurrentUserId))
            .ToListAsync();
        return Ok(messages);
    }

    [HttpPost("{requestId:guid}")]
    public async Task<IActionResult> Send(Guid requestId, SendDonationChatMessageDto dto)
    {
        await EnsureParticipantAsync(requestId, CurrentUserId);
        var message = dto.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message)) return BadRequest(new { message = "Tin nhắn không được để trống." });
        if (message.Length > 2000) return BadRequest(new { message = "Tin nhắn không được vượt quá 2000 ký tự." });
        var entity = new DonationChatMessage
        {
            Id = Guid.NewGuid(), DonationRequestId = requestId, SenderId = CurrentUserId,
            Message = message, SentAt = VietnamTime.Now, CreateAt = VietnamTime.Now, IsActive = true
        };
        context.DonationChatMessages.Add(entity);
        await context.SaveChangesAsync();
        return Ok(new { entity.Id });
    }

    private async Task EnsureParticipantAsync(Guid requestId, Guid userId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var allowed = role == "Donor"
            ? await context.DonationRequests.AnyAsync(x => x.Id == requestId && x.DonorId == userId && x.IsActive != false
                && x.PickupAssignments.Any(a => a.IsActive != false))
            : await context.PickupAssignments.AnyAsync(x => x.DonorRequestId == requestId && x.IsActive != false
                && x.Team.Members.Any(m => m.StaffId == userId && m.IsActive != false));
        if (!allowed) throw new UnauthorizedAccessException("Bạn không thuộc cuộc trò chuyện của đơn này.");
    }
}
