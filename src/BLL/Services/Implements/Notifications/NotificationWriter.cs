using BLL.Common;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.Notifications;

public static class NotificationWriter
{
    public static void NotifyUser(AppDbContext context, Guid userId, string type, string title,
        string message, string? targetUrl = null, Guid? actorId = null) => context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(), UserId = userId, Type = type, Title = title, Message = message,
            TargetUrl = targetUrl, IsRead = false, CreateAt = VietnamTime.Now,
            CreatedBy = actorId, IsActive = true
        });
    public static async Task NotifyManagersNewRequestAsync(AppDbContext context, DonationRequest request)
    {
        var managerIds = await context.Users.AsNoTracking()
            .Where(x => x.IsActive != false && x.Role.RoleName == "Manager")
            .Select(x => x.Id).ToListAsync();
        foreach (var managerId in managerIds)
            Add(context, managerId, request, "DonationRequestCreated", "Có đơn quyên góp mới",
                $"Donor {request.ContactName} vừa tạo đơn {request.RequestCode}, yêu cầu tiếp nhận lúc {FormatAppointment(request.PickupDate)}.",
                $"/manager/dispatch?requestId={request.Id}");
    }

    public static void NotifyDonor(AppDbContext context, DonationRequest request, string type,
        string title, string message, Guid? actorId = null) =>
        Add(context, request.DonorId, request, type, title, $"Đơn {request.RequestCode}: {message}",
            $"/my-orders?requestId={request.Id}", actorId);

    public static async Task NotifyDonorsAsync(AppDbContext context, IEnumerable<Guid> requestIds,
        string type, string title, Func<DonationRequest, string> message, Guid? actorId = null)
    {
        var ids = requestIds.Distinct().ToList();
        if (ids.Count == 0) return;
        var requests = await context.DonationRequests.Where(x => ids.Contains(x.Id)).ToListAsync();
        foreach (var request in requests) NotifyDonor(context, request, type, title, message(request), actorId);
    }

    public static async Task<string> ActorNameAsync(AppDbContext context, Guid actorId) =>
        await context.Users.Where(x => x.Id == actorId).Select(x => x.FullName).FirstOrDefaultAsync()
        ?? "Nhân viên hệ thống";

    public static string FormatTime(DateTime utc)
    {
        return VietnamTime.FromUtc(utc).ToString("HH:mm dd/MM/yyyy");
    }

    private static string FormatDate(DateTime? date) =>
        date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "chưa xác định";

    private static string FormatAppointment(DateTime? date) =>
        date.HasValue ? date.Value.ToString("HH:mm dd/MM/yyyy") : "chưa xác định";

    private static void Add(AppDbContext context, Guid userId, DonationRequest request, string type,
        string title, string message, string targetUrl, Guid? actorId = null) => context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(), UserId = userId, DonationRequestId = request.Id, Type = type,
            Title = title, Message = message, TargetUrl = targetUrl, IsRead = false,
            CreateAt = VietnamTime.Now, CreatedBy = actorId, IsActive = true
        });
}
