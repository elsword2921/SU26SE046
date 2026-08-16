using BLL.Common;
using BLL.DTOs;
using Capstone_API.Hubs;
using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_API.Controllers;

[ApiController, Authorize, Route("api/direct-chat")]
public class DirectChatController(AppDbContext context, IHubContext<DonationChatHub> hub) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role => User.FindFirstValue(ClaimTypes.Role)!;
    private static readonly string[] StaffRoles = ["ReceivingStaff", "ClassificationStaff", "WarehouseStaff"];
    private static readonly string[] OrganizationRoles = ["CharityOrganization", "RecyclingOrganization"];

    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts()
    {
        var me = await context.Users.AsNoTracking().FirstAsync(x => x.Id == UserId);
        var users = await context.Users.AsNoTracking().Include(x => x.Role)
            .Where(x => x.Id != UserId && x.IsActive != false).ToListAsync();
        var allowed = users.Where(x => CanChat(Role, me.WarehouseId, x.Role.RoleName, x.WarehouseId)).ToList();
        var ids = allowed.Select(x => x.Id).ToList();
        var latest = await context.DirectChatMessages.AsNoTracking()
            .Where(x => (x.SenderId == UserId && ids.Contains(x.RecipientId)) ||
                        (x.RecipientId == UserId && ids.Contains(x.SenderId)))
            .OrderByDescending(x => x.SentAt).ToListAsync();
        return Ok(allowed.Select(x =>
        {
            var last = latest.FirstOrDefault(m => m.SenderId == x.Id || m.RecipientId == x.Id);
            return new { UserId = x.Id, x.FullName, Role = x.Role.RoleName, x.AvatarUrl,
                LastMessage = last?.Message, LastMessageAt = last?.SentAt };
        }).OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue).ThenBy(x => x.FullName));
    }

    [HttpGet("{otherUserId:guid}")]
    public async Task<IActionResult> Messages(Guid otherUserId)
    {
        await EnsureAllowed(otherUserId);
        var messages = await context.DirectChatMessages.AsNoTracking()
            .Where(x => x.IsActive != false && ((x.SenderId == UserId && x.RecipientId == otherUserId) ||
                (x.SenderId == otherUserId && x.RecipientId == UserId)))
            .OrderBy(x => x.SentAt)
            .Select(x => new DonationChatMessageDto(x.Id, x.SenderId, x.Sender.FullName,
                x.Sender.Role.RoleName, x.Message, x.SentAt, x.SenderId == UserId)).ToListAsync();
        return Ok(messages);
    }

    [HttpPost("{otherUserId:guid}")]
    public async Task<IActionResult> Send(Guid otherUserId, SendDonationChatMessageDto dto)
    {
        var recipient = await EnsureAllowed(otherUserId);
        var text = dto.Message?.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
            return BadRequest(new { message = "Tin nhắn phải có từ 1 đến 2000 ký tự." });
        var sender = await context.Users.AsNoTracking().Include(x => x.Role).FirstAsync(x => x.Id == UserId);
        var entity = new DirectChatMessage { Id = Guid.NewGuid(), SenderId = UserId, RecipientId = otherUserId,
            Message = text, SentAt = VietnamTime.Now, CreateAt = VietnamTime.Now, IsActive = true };
        context.DirectChatMessages.Add(entity); await context.SaveChangesAsync();
        var result = new DonationChatMessageDto(entity.Id, UserId, sender.FullName, sender.Role.RoleName,
            entity.Message, entity.SentAt, true);
        await hub.Clients.Group(DonationChatHub.UserGroupName(otherUserId)).SendAsync("ChatNotification", new
        {
            ConversationType = "direct", ParticipantId = UserId, ParticipantLabel = sender.FullName,
            SenderId = UserId, SenderName = sender.FullName, Message = entity.Message, SentAt = entity.SentAt
        });
        return Ok(result);
    }

    private async Task<User> EnsureAllowed(Guid otherId)
    {
        var me = await context.Users.AsNoTracking().FirstAsync(x => x.Id == UserId);
        var other = await context.Users.AsNoTracking().Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == otherId && x.IsActive != false)
            ?? throw new InvalidOperationException("Người dùng không tồn tại.");
        if (!CanChat(Role, me.WarehouseId, other.Role.RoleName, other.WarehouseId))
            throw new UnauthorizedAccessException("Bạn không có quyền nhắn tin cho tài khoản này.");
        return other;
    }

    private static bool CanChat(string role, Guid? warehouseId, string otherRole, Guid? otherWarehouseId)
    {
        if (role == "Manager" || otherRole == "Manager") return true;
        if (role == "Donor" || otherRole == "Donor") return false;
        var staff = StaffRoles.Contains(role); var otherStaff = StaffRoles.Contains(otherRole);
        if (staff && otherStaff) return warehouseId.HasValue && warehouseId == otherWarehouseId;
        if (role == "WarehouseStaff" && OrganizationRoles.Contains(otherRole)) return true;
        if (otherRole == "WarehouseStaff" && OrganizationRoles.Contains(role)) return true;
        return false;
    }
}
