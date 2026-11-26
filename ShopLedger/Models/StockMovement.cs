using SQLite;

namespace ShopLedger.Models;

/// <summary>Audit log of every stock change. Quantity is signed (+in / -out).</summary>
public class StockMovement
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public StockReason Reason { get; set; }
    public int? TransactionId { get; set; }
    public string? Note { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

    [Ignore] public string ReasonText => Services.Loc.T("Stock_" + Reason);
}
