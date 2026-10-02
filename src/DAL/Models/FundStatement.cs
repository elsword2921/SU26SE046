namespace DAL.Models;

public class FundStatement
{
    public Guid Id { get; set; }
    public DateOnly Period { get; set; }
    public Guid RequestKey { get; set; }
    public Guid ManagerId { get; set; }
    public User Manager { get; set; } = null!;
    public decimal ClosingBalance { get; set; }
    public string Note { get; set; } = "";
    public byte[] Document { get; set; } = [];
    public string ContentType { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; }
}

// Independent of disposable notifications so Clear All cannot cause reminder spam.
public class FundStatementReminder
{
    public DateOnly Period { get; set; }
    public Guid ManagerId { get; set; }
    public User Manager { get; set; } = null!;
    public DateTimeOffset SentAt { get; set; }
}
