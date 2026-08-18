using BLL.Common;
using BLL.Services.Implements.Notifications;
using BLL.DTOs;
using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Capstone_API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Capstone_API.Controllers;

[ApiController]
[Authorize(Roles = "Donor,ReceivingStaff")]
[Route("api/donation-chat")]
public class DonationChatController(AppDbContext context, IHubContext<DonationChatHub> hub) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var query = context.DonationRequests.AsNoTracking()
            .Where(x => x.IsActive != false && x.PickupAssignments.Any(a => a.IsActive != false));
        query = role == "Donor"
            ? query.Where(x => x.DonorId == CurrentUserId)
            : query.Where(x => x.PickupAssignments.Any(a => a.IsActive != false &&
                a.Team.Members.Any(m => m.StaffId == CurrentUserId && m.IsActive != false)));

        var rows = await query.Select(x => new
        {
            x.Id,
            x.RequestCode,
            DonorName = x.Donor.FullName,
            DonorAvatar = x.Donor.AvatarUrl,
            Staff = x.PickupAssignments.Where(a => a.IsActive != false)
                .SelectMany(a => a.Team.Members).Where(m => m.IsActive != false)
                .Select(m => new { m.Staff.FullName, m.Staff.AvatarUrl }).Distinct().ToList(),
            Last = context.DonationChatMessages.Where(m => m.DonationRequestId == x.Id && m.IsActive != false)
                .OrderByDescending(m => m.SentAt)
                .Select(m => new { m.Message, m.SentAt }).FirstOrDefault()
        }).ToListAsync();

        var result = rows.Select(x => new DonationChatConversationDto(x.Id, x.RequestCode,
            role == "Donor"
                ? (x.Staff.Count > 0 ? string.Join(", ", x.Staff.Select(s => s.FullName)) : "Đội tiếp nhận")
                : x.DonorName,
            role == "Donor" ? x.Staff.Select(s => s.AvatarUrl).FirstOrDefault(a => a != null) : x.DonorAvatar,
            x.Last?.Message, x.Last?.SentAt))
            .OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.RequestCode)
            .ToList();
        return Ok(result);
    }

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
        var sender = await context.Users.AsNoTracking().Include(x => x.Role)
            .FirstAsync(x => x.Id == CurrentUserId);
        var result = new DonationChatMessageDto(entity.Id, entity.SenderId, sender.FullName,
            sender.Role.RoleName, entity.Message, entity.SentAt, true);
        await hub.Clients.Group(DonationChatHub.GroupName(requestId)).SendAsync("MessageReceived", result);

        var request = await context.DonationRequests.AsNoTracking()
            .Where(x => x.Id == requestId)
            .Select(x => new
            {
                Code = x.RequestCode,
                DonorName = x.Donor.FullName,
                x.DonorId,
                ReceivingStaffIds = x.PickupAssignments
                    .Where(a => a.IsActive != false)
                    .SelectMany(a => a.Team.Members)
                    .Where(m => m.IsActive != false)
                    .Select(m => m.StaffId)
                    .Distinct()
                    .ToList()
            })
            .FirstAsync();
        var recipientIds = sender.Role.RoleName == "Donor"
            ? request.ReceivingStaffIds
            : [request.DonorId];
        foreach (var recipientId in recipientIds)
            NotificationWriter.NotifyUser(context, recipientId, "DonationChatMessage",
                $"Tin nhắn mới từ {sender.FullName}", $"Đơn {request.Code}: {entity.Message}",
                $"/my-orders?requestId={requestId}", CurrentUserId);
        await context.SaveChangesAsync();
        var participantLabel = sender.Role.RoleName == "Donor" ? request.DonorName : sender.FullName;
        var notification = new
        {
            RequestId = requestId,
            RequestCode = request.Code,
            ParticipantLabel = participantLabel,
            SenderId = CurrentUserId,
            SenderName = sender.FullName,
            Message = entity.Message,
            SentAt = entity.SentAt
        };
        await Task.WhenAll(recipientIds.Select(id => hub.Clients
            .Group(DonationChatHub.UserGroupName(id))
            .SendAsync("ChatNotification", notification)));
        return Ok(result);
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
