namespace DAL.Models;

public class FundContribution
{
    public Guid Id { get; set; }
    public long OrderCode { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RequestKey { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? PaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

public class FundExpense
{
    public Guid Id { get; set; }
    public Guid RequestKey { get; set; }
    public Guid ManagerId { get; set; }
    public User Manager { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly SpentOn { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public byte[] Receipt { get; set; } = [];
    public string ReceiptContentType { get; set; } = "";
    public DateTimeOffset? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
}
