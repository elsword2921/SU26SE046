using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.Voucher;

public static class DonationPointWriter
{
    public static async Task<int> GetPointsPerKgAsync(AppDbContext context)
    {
        return await context.DonationPointRules.AsNoTracking()
            .Where(x => x.IsActive != false)
            .OrderByDescending(x => x.UpdateAt ?? x.CreateAt)
            .Select(x => x.PointsPerKg)
            .FirstAsync();
    }

    public static async Task<int> AwardDonationAsync(AppDbContext context, DonationRequest request,
        decimal actualWeightKg, Guid actorId)
    {
        const string type = "DonationReceived";
        var alreadyAwarded = context.DonationPointTransactions.Local.Any(x =>
                                 x.DonationRequestId == request.Id && x.Type == type)
                             || await context.DonationPointTransactions.AsNoTracking().AnyAsync(x =>
                                 x.DonationRequestId == request.Id && x.Type == type);
        if (alreadyAwarded) return 0;

        var pointsPerKg = await GetPointsPerKgAsync(context);
        var points = (int)decimal.Round(actualWeightKg * pointsPerKg, 0, MidpointRounding.AwayFromZero);
        if (points <= 0) return 0;
        var donor = await context.Users.FirstAsync(x => x.Id == request.DonorId && x.IsActive != false);
        donor.DonationPoint += points;
        donor.UpdateAt = DateTime.UtcNow;
        context.DonationPointTransactions.Add(new DonationPointTransaction
        {
            Id = Guid.NewGuid(), UserId = donor.Id, DonationRequestId = request.Id,
            Points = points, BalanceAfter = donor.DonationPoint, WeightKg = actualWeightKg,
            Type = type, Description = $"Cộng điểm từ đơn {request.RequestCode}: {actualWeightKg:0.##} kg × {pointsPerKg} điểm/kg",
            OccurredAt = DateTime.UtcNow, CreateAt = DateTime.UtcNow, CreatedBy = actorId, IsActive = true
        });
        return points;
    }
}
