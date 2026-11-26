using ShopLedger.Models;

namespace ShopLedger.Services;

public record LineRequest(int ProductId, decimal Quantity, decimal UnitPrice);

public class SaleRequest
{
    public TransactionType Type { get; set; } = TransactionType.CashSale;   // CreditSale or CashSale
    public int? CustomerId { get; set; }
    public List<LineRequest> Items { get; set; } = new();
    /// <summary>Credit sale: amount paid now (can be 0). Ignored for cash sale (paid in full).</summary>
    public decimal PaidAmount { get; set; }
    public string? Note { get; set; }
    public DateTime? Date { get; set; }
    public bool AllowOverLimit { get; set; }
}

public class CreditLimitExceededException : Exception
{
    public string CustomerName { get; }
    public decimal Limit { get; }
    public decimal NewBalance { get; }
    public CreditLimitExceededException(string name, decimal limit, decimal newBalance)
        : base(Loc.F("Err_CreditLimit", name, newBalance, limit))
    { CustomerName = name; Limit = limit; NewBalance = newBalance; }
}

public class InsufficientStockException : Exception
{
    public InsufficientStockException(string product, decimal available)
        : base(Loc.F("Err_NoStock", product, available)) { }
}
