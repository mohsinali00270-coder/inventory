using SQLite;

namespace ShopLedger.Models;

/// <summary>
/// Balance rules (applied by the ledger service in Phase 3):
///  CreditSale : balance += TotalAmount - PaidAmount
///  CashSale   : no balance change
///  Payment    : balance -= PaidAmount
///  Adjustment : balance += signed TotalAmount (+ = customer owes more, - = less)
/// </summary>
public class LedgerTransaction
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int? CustomerId { get; set; }
    [Indexed] public TransactionType Type { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    /// <summary>Customer balance right after this transaction (for fast statements).</summary>
    public decimal BalanceAfter { get; set; }
    [Indexed] public DateTime Date { get; set; } = DateTime.Now;
    public string? Note { get; set; }

    [Ignore] public List<TransactionItem> Items { get; set; } = new();
    [Ignore] public string TypeText => Services.Loc.T("Type_" + Type);
}
