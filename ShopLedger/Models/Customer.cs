using SQLite;

namespace ShopLedger.Models;

public class Customer
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed, MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(25)] public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    /// <summary>0 = no limit.</summary>
    public decimal CreditLimit { get; set; }
    /// <summary>Positive = customer owes the shop. Negative = shop owes customer (advance).</summary>
    public decimal CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore] public bool HasDue => CurrentBalance > 0;
    [Ignore] public bool IsOverLimit => CreditLimit > 0 && CurrentBalance > CreditLimit;
    /// <summary>Owes money but within the credit limit (amber "Due" badge).</summary>
    [Ignore] public bool IsDueWithinLimit => HasDue && !IsOverLimit;
    [Ignore] public bool IsSettled => CurrentBalance == 0;
    [Ignore] public bool HasAdvance => CurrentBalance < 0;
}
