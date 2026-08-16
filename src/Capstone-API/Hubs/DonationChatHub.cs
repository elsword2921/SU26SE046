using DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_API.Hubs;

[Authorize]
public class DonationChatHub(AppDbContext context) : Hub
{
    public static string GroupName(Guid requestId) => $"donation-chat:{requestId:N}";
    public static string UserGroupName(Guid userId) => $"donation-chat-user:{userId:N}";

    public override async Task OnConnectedAsync()
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
        await base.OnConnectedAsync();
    }

    public async Task JoinRequest(Guid requestId)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Context.User!.FindFirstValue(ClaimTypes.Role);
        var allowed = role == "Donor"
            ? await context.DonationRequests.AnyAsync(x => x.Id == requestId && x.DonorId == userId
                && x.IsActive != false && x.PickupAssignments.Any(a => a.IsActive != false))
            : await context.PickupAssignments.AnyAsync(x => x.DonorRequestId == requestId
                && x.IsActive != false && x.Team.Members.Any(m => m.StaffId == userId && m.IsActive != false));
        if (!allowed) throw new HubException("Bạn không thuộc cuộc trò chuyện của đơn này.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(requestId));
    }
}
