using System.Globalization;
using System.Security.Authentication;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace BLL.Services.Implements.OperatingFund;

public record FundStatementDto(Guid Id, string Month, decimal ClosingBalance, string Note, string PublishedBy, DateTimeOffset PublishedAt);
public record StatementStatus(string StartMonth, string LatestClosedMonth, List<string> MissingMonths);
public record CreateFundStatement(Guid RequestKey, string Month, decimal ClosingBalance, string? Note);

public sealed class FundStatementService(AppDbContext db, TimeProvider clock, IConfiguration config)
{
    private DateOnly CurrentMonth
    {
        get { var now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)); return new(now.Year, now.Month, 1); }
    }
    // The operating fund launched in September 2026. Configurable for another deployment.
    private DateOnly StartMonth => ParseMonth(config["OperatingFund:StatementStartMonth"] ?? "2026-09");
    private static string Month(DateOnly date) => date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    private static DateOnly ParseMonth(string? value)
    {
        if (value is null || !DateOnly.TryParseExact(value + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date.Year < 2026)
            throw new InvalidOperationException("Tháng sao kê không hợp lệ, dùng định dạng YYYY-MM từ năm 2026.");
        return date;
    }
    private async Task RequireUser(Guid userId, bool manager = false)
    {
        var roles = manager ? new[] { "Manager" } : OperatingFundService.Readers.Split(',');
        if (!await db.Users.AnyAsync(u => u.Id == userId && u.IsActive == true && u.EmailConfirmed && u.UserStatus == "Active" && roles.Contains(u.Role.RoleName)))
            throw new AuthenticationException("Tài khoản không có quyền truy cập sao kê.");
    }
    private async Task<IDbContextTransaction> Lock()
    {
        var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sp_getapplock @Resource='ReThreads:FundStatements', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51000, 'Statements busy; retry', 1;");
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
    private async Task<List<DateOnly>> Missing()
    {
        var first = StartMonth; var current = CurrentMonth;
        var published = (await db.Set<FundStatement>().Select(s => s.Period).ToListAsync()).ToHashSet();
        var missing = new List<DateOnly>();
        for (var month = first; month < current; month = month.AddMonths(1))
            if (!published.Contains(month)) missing.Add(month);
        return missing;
    }
    public async Task<StatementStatus> Status(Guid userId)
    {
        await RequireUser(userId, manager: true);
        await RemindManagers(); // Also catches up if the App Service slept at month boundary.
        return new(Month(StartMonth), Month(CurrentMonth.AddMonths(-1)), (await Missing()).Select(Month).ToList());
    }
    public async Task RemindManagers()
    {
        await using var tx = await Lock();
        var missing = await Missing();
        if (missing.Count == 0) return;
        var managers = await db.Users.Where(u => u.Role.RoleName == "Manager" && u.IsActive == true && u.EmailConfirmed && u.UserStatus == "Active").Select(u => u.Id).ToListAsync();
        var sent = (await db.Set<FundStatementReminder>().Select(r => new { r.Period, r.ManagerId }).ToListAsync()).Select(r => (r.Period, r.ManagerId)).ToHashSet();
        var now = clock.GetUtcNow();
        foreach (var manager in managers)
        {
            var unsent = missing.Where(p => !sent.Contains((p, manager))).ToList();
            if (unsent.Count == 0) continue;
            foreach (var period in unsent) db.Add(new FundStatementReminder { Period = period, ManagerId = manager, SentAt = now });
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), UserId = manager, Type = "FundStatementReminder", IsActive = true,
                Title = "Nhắc đăng sao kê quỹ ReThreads",
                Message = unsent.Count == 1 ? $"Đã đến kỳ đăng sao kê tháng {unsent[0]:MM/yyyy}. Vui lòng đăng số dư cuối tháng và file sao kê trong Quỹ vận hành."
                    : $"Còn {unsent.Count} tháng chưa đăng sao kê ({unsent[0]:MM/yyyy} đến {unsent[^1]:MM/yyyy}). Vui lòng hoàn tất trong Quỹ vận hành.",
                TargetUrl = "/manager/fund#statements", CreateAt = now.UtcDateTime
            });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task<FundPage<FundStatementDto>> List(Guid userId, int page)
    {
        await RequireUser(userId); page = Math.Clamp(page, 1, 100000);
        var query = db.Set<FundStatement>().AsNoTracking();
        var rows = await query.OrderByDescending(s => s.Period).Skip((page - 1) * 12).Take(12)
            .Select(s => new { s.Id, s.Period, s.ClosingBalance, s.Note, Name = s.Manager.FullName, s.PublishedAt }).ToListAsync();
        return new(rows.Select(s => new FundStatementDto(s.Id, Month(s.Period), s.ClosingBalance, s.Note, s.Name, s.PublishedAt)).ToList(), await query.CountAsync(), page, 12);
    }
    public async Task<Guid> Publish(Guid userId, CreateFundStatement input, byte[] document)
    {
        await RequireUser(userId, manager: true);
        var period = ParseMonth(input.Month);
        if (period < StartMonth || period >= CurrentMonth) throw new InvalidOperationException("Chỉ đăng sao kê cho tháng đã kết thúc, kể từ tháng bắt đầu quỹ.");
        if (input.RequestKey == Guid.Empty || input.ClosingBalance < 0 || input.ClosingBalance > 999999999999999m || decimal.Truncate(input.ClosingBalance) != input.ClosingBalance)
            throw new InvalidOperationException("Nhập số dư cuối tháng là số nguyên VND không âm và mã yêu cầu hợp lệ.");
        var note = input.Note?.Trim() ?? "";
        if (note.Length > 2000) throw new InvalidOperationException("Ghi chú tối đa 2.000 ký tự.");
        var type = OperatingFundService.ReceiptType(document);
        await using var tx = await Lock();
        var retry = await db.Set<FundStatement>().SingleOrDefaultAsync(s => s.ManagerId == userId && s.RequestKey == input.RequestKey);
        if (retry != null)
        {
            if (retry.Period != period || retry.ClosingBalance != input.ClosingBalance || retry.Note != note || !retry.Document.SequenceEqual(document))
                throw new InvalidOperationException("Mã yêu cầu đã dùng cho sao kê khác.");
            return retry.Id;
        }
        if (await db.Set<FundStatement>().AnyAsync(s => s.Period == period)) throw new InvalidOperationException("Tháng này đã có sao kê. Mỗi tháng chỉ đăng một lần cho toàn dự án.");
        var row = new FundStatement { Id = Guid.NewGuid(), Period = period, RequestKey = input.RequestKey, ManagerId = userId, ClosingBalance = input.ClosingBalance, Note = note, Document = document, ContentType = type, PublishedAt = clock.GetUtcNow() };
        db.Add(row); await db.SaveChangesAsync(); await tx.CommitAsync(); return row.Id;
    }
    public async Task<(byte[] Data, string ContentType)> Document(Guid userId, Guid id)
    {
        await RequireUser(userId);
        var row = await db.Set<FundStatement>().AsNoTracking().Where(s => s.Id == id).Select(s => new { s.Document, s.ContentType }).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy sao kê.");
        return (row.Document, row.ContentType);
    }
}
