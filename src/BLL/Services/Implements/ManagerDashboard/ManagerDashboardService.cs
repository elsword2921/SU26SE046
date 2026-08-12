using BLL.Common;
using BLL.DTOs;
using BLL.Services.Interfaces.ManagerDashboard;
using DAL;
using DAL.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ManagerDashboard;

public class ManagerDashboardService(AppDbContext context) : IManagerDashboardService
{
    public async Task<ManagerDashboardDto> GetAsync(Guid? warehouseId, int? year, int? month, DateTime? date)
    {
        if (year is < 2000 or > 2100) throw new InvalidOperationException("Year must be between 2000 and 2100.");
        if (month is < 1 or > 12) throw new InvalidOperationException("Month must be between 1 and 12.");
        if (month.HasValue && !year.HasValue) throw new InvalidOperationException("Year is required when filtering by month.");

        DateTime? periodStart = null;
        DateTime? periodEnd = null;
        if (date.HasValue)
        {
            periodStart = date.Value.Date;
            periodEnd = periodStart.Value.AddDays(1);
        }
        else if (year.HasValue && month.HasValue)
        {
            periodStart = new DateTime(year.Value, month.Value, 1);
            periodEnd = periodStart.Value.AddMonths(1);
        }
        else if (year.HasValue)
        {
            periodStart = new DateTime(year.Value, 1, 1);
            periodEnd = periodStart.Value.AddYears(1);
        }

        var requests = context.DonationRequests.AsNoTracking()
            .Where(x => x.IsActive != false && (!warehouseId.HasValue || x.WarehouseId == warehouseId)
                && (!periodStart.HasValue || (x.CreateAt >= periodStart && x.CreateAt < periodEnd)));
        var intakes = context.IntakeBatches.AsNoTracking()
            .Where(x => x.IsActive != false && (!warehouseId.HasValue || x.WarehouseId == warehouseId)
                && (!periodStart.HasValue || (x.IntakeDate >= periodStart && x.IntakeDate < periodEnd)));
        var classified = context.ClassifiedBatches.AsNoTracking()
            .Where(x => x.IsActive != false && (!warehouseId.HasValue || x.WarehouseId == warehouseId)
                && (!periodStart.HasValue || (x.ClassificationDate >= periodStart && x.ClassificationDate < periodEnd)));
        var transactions = context.InventoryTransactions.AsNoTracking()
            .Where(x => x.IsActive != false && x.Status == "Posted"
                && (!warehouseId.HasValue || x.WarehouseId == warehouseId)
                && (!periodStart.HasValue || (x.PerformedAt >= periodStart && x.PerformedAt < periodEnd)));

        var donationCounts = await requests.GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var intakeCounts = await intakes.GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var classifiedCounts = await classified.GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var transactionCounts = await transactions.GroupBy(x => x.TransactionType)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var transactionWeights = await context.TransactionItems.AsNoTracking()
            .Where(x => x.IsActive != false && x.Transaction.IsActive != false && x.Transaction.Status == "Posted"
                && (!warehouseId.HasValue || x.Transaction.WarehouseId == warehouseId)
                && (!periodStart.HasValue || (x.Transaction.PerformedAt >= periodStart && x.Transaction.PerformedAt < periodEnd)))
            .GroupBy(x => x.Transaction.TransactionType)
            .Select(x => new { x.Key, Weight = x.Sum(i => i.Weight) })
            .ToDictionaryAsync(x => x.Key, x => x.Weight);

        int Donation(params DonationRequestStatus[] statuses) =>
            statuses.Sum(x => donationCounts.GetValueOrDefault(x));
        int Intake(params string[] statuses) => statuses.Sum(x => intakeCounts.GetValueOrDefault(x));
        int Classified(params string[] statuses) => statuses.Sum(x => classifiedCounts.GetValueOrDefault(x));

        var today = VietnamTime.Today;
        var trendStart = periodStart ?? today.AddDays(-6);
        var trendEnd = periodEnd ?? today.AddDays(1);
        var requestDays = await requests.Where(x => x.CreateAt >= trendStart && x.CreateAt < trendEnd)
            .GroupBy(x => x.CreateAt!.Value.Date)
            .Select(x => new { Date = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Date, x => x.Count);
        var transactionDays = await transactions.Where(x => x.PerformedAt >= trendStart && x.PerformedAt < trendEnd)
            .GroupBy(x => new { Date = x.PerformedAt.Date, x.TransactionType })
            .Select(x => new { x.Key.Date, x.Key.TransactionType, Count = x.Count() }).ToListAsync();
        var rawDaily = Enumerable.Range(0, (trendEnd - trendStart).Days).Select(offset =>
        {
            var pointDate = trendStart.AddDays(offset);
            return new DashboardDailyDto(pointDate, requestDays.GetValueOrDefault(pointDate),
                transactionDays.Where(x => x.Date == pointDate && x.TransactionType == "RECEIPT").Sum(x => x.Count),
                transactionDays.Where(x => x.Date == pointDate && x.TransactionType == "OUT").Sum(x => x.Count));
        }).ToList();
        var granularity = year.HasValue && !month.HasValue && !date.HasValue ? "month" : "day";
        var daily = granularity == "month"
            ? rawDaily.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
                .Select(x => new DashboardDailyDto(x.Key, x.Sum(i => i.DonationRequests),
                    x.Sum(i => i.InboundBatches), x.Sum(i => i.OutboundBatches))).ToList()
            : rawDaily;

        return new ManagerDashboardDto(
            await requests.CountAsync(),
            await intakes.CountAsync(),
            await classified.CountAsync(),
            new DashboardWarehouseFlowDto(
                transactionCounts.GetValueOrDefault("RECEIPT"),
                transactionCounts.GetValueOrDefault("OUT"),
                transactionWeights.GetValueOrDefault("RECEIPT"),
                transactionWeights.GetValueOrDefault("OUT")),
            [
                new("unassigned", "Chờ phân công", Donation(DonationRequestStatus.PendingStaffAssign, DonationRequestStatus.WaitingReceivingStaff)),
                new("assigned", "Đã phân công", Donation(DonationRequestStatus.ReceivingStaffAssigned)),
                new("collected", "Đã thu gom", Donation(DonationRequestStatus.Confirmed, DonationRequestStatus.SendToClassification,
                    DonationRequestStatus.Classifying, DonationRequestStatus.Classified, DonationRequestStatus.Stored)),
                new("cancelled", "Từ chối / hủy", Donation(DonationRequestStatus.Reject, DonationRequestStatus.Cancelled))
            ],
            [
                new("assigned", "Đã phân công", Intake("Planned")),
                new("collecting", "Đang thu gom", Intake("Receiving")),
                new("collected", "Đã thu gom", Intake("Completed")),
                new("classification", "Đã gửi phân loại", Intake("SentToClassification", "AwaitingClassificationCount", "ReadyForClassification")),
                new("classified", "Đã phân loại", Intake("InClassifiedArea"))
            ],
            [
                new("waiting", "Chờ kiểm đếm/phân loại", Intake("SentToClassification", "AwaitingClassificationCount", "ReadyForClassification")),
                new("classifying", "Đang phân loại", Intake("Classifying")),
                new("classified", "Trong khu đồ đã phân loại", Intake("InClassifiedArea"))
            ],
            [
                new("open", "Đang gom nhóm", Classified("Open")),
                new("inbound", "Chờ nhập kho", Classified("PendingWarehouseReceipt", "WarehouseReceived")),
                new("stored", "Đã nhập kho", Classified("Stored")),
                new("outbound", "Đã xuất kho", transactionCounts.GetValueOrDefault("OUT"))
            ],
            granularity,
            daily);
    }
}
