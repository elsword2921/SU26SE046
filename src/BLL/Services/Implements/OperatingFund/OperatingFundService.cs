using System.Security.Authentication;
using System.Text.Json;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BLL.Services.Implements.OperatingFund;

public record ContributionDto(
    Guid Id,
    long OrderCode,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    string? CheckoutUrl,
    ContributorDto? Contributor = null
);

public record ContributorDto(
    string FullName,
    string UserName,
    string Role,
    string Email,
    string PhoneNumber
);

public record ExpenseDto(
    Guid Id,
    decimal Amount,
    string Title,
    string Description,
    DateOnly SpentOn,
    DateTimeOffset PublishedAt,
    string PublishedBy,
    DateTimeOffset? VoidedAt,
    string? VoidReason
);

public record FundSummary(
    decimal TotalReceived,
    decimal TotalSpent,
    decimal Balance,
    int Contributions,
    bool PaymentEnabled
);

public record FundPage<T>(List<T> Items, int Total, int Page, int PageSize);

public record CreateContribution(Guid RequestKey, decimal Amount);

public record CreateFundExpense(
    Guid RequestKey,
    decimal Amount,
    string Title,
    string Description,
    DateOnly SpentOn
);

public class OperatingFundService(AppDbContext db, IPayOsGateway gateway)
{
    public const string Readers =
        "Donor,CharityOrganization,RecyclingOrganization,DisposalOrganization,Manager";
    public const string Contributors =
        "Donor,CharityOrganization,RecyclingOrganization,DisposalOrganization";
    private IQueryable<FundContribution> Contributions => db.Set<FundContribution>();
    private IQueryable<FundExpense> Expenses => db.Set<FundExpense>();

    private async Task RequireUser(Guid id, bool manager = false, bool contributor = false)
    {
        var user = await db
            .Users.Include(u => u.Role)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id);
        var roles =
            manager ? "Manager"
            : contributor ? Contributors
            : Readers;
        if (
            user is null
            || user.IsActive != true
            || !user.EmailConfirmed
            || user.UserStatus != "Active"
            || !roles.Split(',').Contains(user.Role.RoleName)
        )
            throw new AuthenticationException("Tài khoản chưa được phép sử dụng quỹ vận hành.");
    }

    private async Task<IDbContextTransaction> Lock()
    {
        var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "DECLARE @r int; EXEC @r=sp_getapplock @Resource='ReThreads:OperatingFund', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51000, 'Fund busy; retry', 1;"
            );
            return tx;
        }
        catch
        {
            await tx.DisposeAsync();
            throw;
        }
    }

    private static void Money(decimal amount, decimal minimum = 1000)
    {
        if (amount < minimum || amount > 500000000 || decimal.Truncate(amount) != amount)
            throw new InvalidOperationException(
                $"Số tiền phải là số nguyên VND, từ {minimum:N0} đến 500.000.000 đồng."
            );
    }

    private static ContributionDto View(FundContribution c) =>
        new(
            c.Id,
            c.OrderCode,
            c.Amount,
            c.Status,
            c.CreatedAt,
            c.ConfirmedAt,
            c.Status == "Pending" ? c.CheckoutUrl : null
        );

    public async Task<FundSummary> Summary(Guid userId)
    {
        await RequireUser(userId);
        await using var tx = await Lock();
        return await db
            .Users.Where(u => u.Id == userId)
            .Select(_ => new FundSummary(
                db.Set<FundContribution>()
                    .Where(c => c.Status == "Paid")
                    .Sum(c => (decimal?)c.Amount)
                ?? 0,
                db.Set<FundExpense>().Where(e => e.VoidedAt == null).Sum(e => (decimal?)e.Amount)
                ?? 0,
                (
                    db.Set<FundContribution>()
                        .Where(c => c.Status == "Paid")
                        .Sum(c => (decimal?)c.Amount)
                    ?? 0
                )
                    - (
                        db.Set<FundExpense>()
                            .Where(e => e.VoidedAt == null)
                            .Sum(e => (decimal?)e.Amount)
                        ?? 0
                    ),
                db.Set<FundContribution>().Count(c => c.Status == "Paid"),
                gateway.Enabled
            ))
            .SingleAsync();
    }

    public async Task<ContributionDto> Checkout(Guid userId, CreateContribution input)
    {
        await RequireUser(userId, contributor: true);
        Money(input.Amount);
        if (input.RequestKey == Guid.Empty)
            throw new InvalidOperationException("Thiếu mã chống gửi trùng.");
        if (!gateway.Enabled)
            throw new InvalidOperationException(
                "Chưa cấu hình kênh PayOS. Tính năng thanh toán chưa được bật."
            );
        Guid id;
        await using (var tx = await Lock())
        {
            var c = await Contributions.SingleOrDefaultAsync(x =>
                x.UserId == userId && x.RequestKey == input.RequestKey
            );
            if (c != null && c.Amount != input.Amount)
                throw new InvalidOperationException("Mã yêu cầu đã dùng cho số tiền khác.");
            if (c == null)
            {
                if (
                    await Contributions.CountAsync(x =>
                        x.UserId == userId
                        && x.Status == "Pending"
                        && x.ExpiresAt > DateTimeOffset.UtcNow
                    ) >= 5
                )
                    throw new InvalidOperationException(
                        "Bạn đang có nhiều giao dịch chờ. Vui lòng kiểm tra lịch sử đóng góp."
                    );
                c = new FundContribution
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RequestKey = input.RequestKey,
                    Amount = input.Amount,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
                };
                db.Add(c);
                await db.SaveChangesAsync();
            }
            id = c.Id;
            await tx.CommitAsync();
        }
        await using (var tx = await Lock())
        {
            var c = await Contributions.SingleAsync(x => x.Id == id);
            await db.Entry(c).ReloadAsync();
            if (c.Status == "Pending" && c.CheckoutUrl == null)
            {
                if (c.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    c.Status = "Expired";
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                    return View(c);
                }
                PayOsLink link;
                try
                {
                    link = await gateway.CreateAsync(c);
                }
                catch
                {
                    link =
                        await gateway.GetAsync(c.OrderCode)
                        ?? throw new InvalidOperationException(
                            "Chưa tạo được link. Hãy thử lại với cùng giao dịch."
                        );
                }
                ValidateLink(c, link);
                c.PaymentLinkId = link.Id;
                var url =
                    link.CheckoutUrl ?? $"https://pay.payos.vn/web/{Uri.EscapeDataString(link.Id)}";
                if (
                    !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || uri.Scheme != "https"
                    || uri.Host != "pay.payos.vn"
                    || !string.IsNullOrEmpty(uri.UserInfo)
                )
                    throw new InvalidOperationException("Địa chỉ thanh toán PayOS không hợp lệ.");
                c.CheckoutUrl = url;
                ApplyStatus(c, link);
                await db.SaveChangesAsync();
            }
            await tx.CommitAsync();
            return View(c);
        }
    }

    private static void ValidateLink(FundContribution c, PayOsLink link)
    {
        if (
            link.OrderCode != c.OrderCode
            || link.Amount != c.Amount
            || string.IsNullOrWhiteSpace(link.Id)
            || link.Id.Length > 100
            || (c.PaymentLinkId != null && c.PaymentLinkId != link.Id)
        )
            throw new InvalidOperationException("Thông tin thanh toán không khớp giao dịch.");
    }

    private static void ApplyStatus(FundContribution c, PayOsLink link)
    {
        if (c.Status == "Paid")
            return;
        if (link.Status == "PAID")
        {
            if (link.Paid != c.Amount)
                throw new InvalidOperationException(
                    "Số tiền thực nhận chưa khớp. Cần đối soát với PayOS."
                );
            c.Status = "Paid";
            c.ConfirmedAt = DateTimeOffset.UtcNow;
        }
        else if (link.Status is "CANCELLED" or "EXPIRED")
            c.Status = link.Status == "CANCELLED" ? "Cancelled" : "Expired";
    }

    private async Task Reconcile(Guid id)
    {
        var order = await Contributions.AsNoTracking().SingleAsync(c => c.Id == id);
        if (order.Status == "Paid")
            return;
        var link = await gateway.GetAsync(order.OrderCode);
        if (link == null)
            return;
        await using var tx = await Lock();
        var c = await Contributions.SingleAsync(c => c.Id == id);
        await db.Entry(c).ReloadAsync();
        ValidateLink(c, link);
        c.PaymentLinkId ??= link.Id;
        ApplyStatus(c, link);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<ContributionDto> Refresh(Guid userId, Guid id)
    {
        await RequireUser(userId);
        var manager = await db.Users.AnyAsync(u => u.Id == userId && u.Role.RoleName == "Manager");
        if (!await Contributions.AnyAsync(c => c.Id == id && (manager || c.UserId == userId)))
            throw new AuthenticationException("Không có quyền xem giao dịch này.");
        await Reconcile(id);
        return View(await Contributions.AsNoTracking().SingleAsync(c => c.Id == id));
    }

    public async Task Webhook(JsonElement body)
    {
        if (
            !body.TryGetProperty("data", out var data)
            || !body.TryGetProperty("signature", out var signature)
            || !gateway.VerifyWebhook(data, signature.GetString() ?? "")
        )
            throw new AuthenticationException("Chữ ký PayOS không hợp lệ.");
        if (!data.TryGetProperty("orderCode", out var code) || !code.TryGetInt64(out var orderCode))
            throw new InvalidOperationException("Thiếu mã thanh toán.");
        var c = await Contributions
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.OrderCode == orderCode);
        // PayOS sends a signed sample transaction during webhook registration.
        if (c == null)
            return;
        if (
            data.GetProperty("currency").GetString() != "VND"
            || data.GetProperty("paymentLinkId").GetString() != c.PaymentLinkId
                && c.PaymentLinkId != null
        )
            throw new InvalidOperationException("Webhook không khớp link thanh toán.");
        // Query the authenticated payment API for the complete amount, including partial payments.
        await Reconcile(c.Id);
    }

    public async Task<FundPage<ContributionDto>> Mine(Guid userId, int page)
    {
        await RequireUser(userId);
        page = Math.Clamp(page, 1, 100000);
        var query = Contributions.AsNoTracking().Where(c => c.UserId == userId);
        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * 20)
            .Take(20)
            .ToListAsync();
        return new(rows.Select(View).ToList(), await query.CountAsync(), page, 20);
    }

    public async Task<FundPage<ContributionDto>> AllContributions(Guid userId, int page)
    {
        await RequireUser(userId, manager: true);
        page = Math.Clamp(page, 1, 100000);
        var query = Contributions.AsNoTracking().Include(c => c.User).ThenInclude(u => u.Role);
        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * 20)
            .Take(20)
            .ToListAsync();
        return new(
            rows.Select(c =>
                    View(c) with
                    {
                        Contributor = new ContributorDto(
                            c.User.FullName,
                            c.User.UserName,
                            c.User.Role.RoleName,
                            c.User.Email,
                            c.User.PhoneNumber
                        ),
                    }
                )
                .ToList(),
            await query.CountAsync(),
            page,
            20
        );
    }

    public async Task<FundPage<ExpenseDto>> ExpenseList(Guid userId, int page)
    {
        await RequireUser(userId);
        page = Math.Clamp(page, 1, 100000);
        var query = Expenses.AsNoTracking();
        return new(
            await query
                .OrderByDescending(e => e.PublishedAt)
                .ThenBy(e => e.Id)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(e => new ExpenseDto(
                    e.Id,
                    e.Amount,
                    e.Title,
                    e.Description,
                    e.SpentOn,
                    e.PublishedAt,
                    e.Manager.FullName,
                    e.VoidedAt,
                    e.VoidReason
                ))
                .ToListAsync(),
            await query.CountAsync(),
            page,
            20
        );
    }

    public async Task<Guid> Publish(Guid managerId, CreateFundExpense input, byte[] receipt)
    {
        await RequireUser(managerId, manager: true);
        Money(input.Amount, 1);
        if (
            input.RequestKey == Guid.Empty
            || string.IsNullOrWhiteSpace(input.Title)
            || input.Title.Trim().Length > 160
            || string.IsNullOrWhiteSpace(input.Description)
            || input.Description.Trim().Length > 2000
        )
            throw new InvalidOperationException(
                "Nhập tiêu đề và mục đích chi đầy đủ (tối đa 160/2000 ký tự)."
            );
        if (
            input.SpentOn > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))
            || input.SpentOn < new DateOnly(2026, 1, 1)
        )
            throw new InvalidOperationException("Ngày chi không hợp lệ.");
        var mime = ReceiptType(receipt);
        await using var tx = await Lock();
        var existing = await Expenses.SingleOrDefaultAsync(e =>
            e.ManagerId == managerId && e.RequestKey == input.RequestKey
        );
        if (existing != null)
        {
            if (
                existing.Amount != input.Amount
                || existing.Title != input.Title.Trim()
                || existing.Description != input.Description.Trim()
                || existing.SpentOn != input.SpentOn
                || !existing.Receipt.SequenceEqual(receipt)
            )
                throw new InvalidOperationException("Mã yêu cầu đã dùng cho khoản chi khác.");
            return existing.Id;
        }
        var received =
            await Contributions.Where(c => c.Status == "Paid").SumAsync(c => (decimal?)c.Amount)
            ?? 0;
        var spent =
            await Expenses.Where(e => e.VoidedAt == null).SumAsync(e => (decimal?)e.Amount) ?? 0;
        if (input.Amount > received - spent)
            throw new InvalidOperationException("Khoản chi vượt số dư quỹ đã được xác nhận.");
        var row = new FundExpense
        {
            Id = Guid.NewGuid(),
            RequestKey = input.RequestKey,
            ManagerId = managerId,
            Amount = input.Amount,
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            SpentOn = input.SpentOn,
            PublishedAt = DateTimeOffset.UtcNow,
            Receipt = receipt,
            ReceiptContentType = mime,
        };
        db.Add(row);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return row.Id;
    }

    public static string ReceiptType(byte[] bytes)
    {
        if (bytes.Length < 12 || bytes.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Chứng từ bắt buộc, tối đa 5 MB.");
        if (bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255)
            return "image/jpeg";
        if (bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            return "application/pdf";
        throw new InvalidOperationException("Chứng từ phải là JPG, PNG hoặc PDF hợp lệ.");
    }

    public async Task Void(Guid managerId, Guid id, string reason)
    {
        await RequireUser(managerId, manager: true);
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            throw new InvalidOperationException("Nhập lý do hủy khoản chi, tối đa 1000 ký tự.");
        await using var tx = await Lock();
        var row =
            await db.Set<FundExpense>().SingleOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException("Không tìm thấy khoản chi.");
        if (row.VoidedAt == null)
        {
            row.VoidedAt = DateTimeOffset.UtcNow;
            row.VoidedBy = managerId;
            row.VoidReason = reason.Trim();
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }

    public async Task<(byte[] Data, string ContentType)> Receipt(Guid userId, Guid id)
    {
        await RequireUser(userId);
        var row =
            await Expenses
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new { e.Receipt, e.ReceiptContentType })
                .SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy chứng từ.");
        return (row.Receipt, row.ReceiptContentType);
    }
}
