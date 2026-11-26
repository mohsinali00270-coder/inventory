using ShopLedger.Models;

namespace ShopLedger.ViewModels;

public class TransactionRow
{
    public LedgerTransaction Transaction { get; init; } = new();
    public string CustomerName { get; init; } = "";

    public string TypeLabel => Label(Transaction.Type);
    public string Title => string.IsNullOrEmpty(CustomerName) ? TypeLabel : $"{TypeLabel} - {CustomerName}";
    public string Subtitle => $"{Transaction.Date:dd MMM yyyy, HH:mm}" +
                              (string.IsNullOrWhiteSpace(Transaction.Note) ? "" : $"  |  {Transaction.Note}");
    public decimal Amount => Transaction.Type == TransactionType.Payment ? Transaction.PaidAmount : Transaction.TotalAmount;
    public bool IsMoneyIn => Transaction.Type is TransactionType.Payment or TransactionType.CashSale;

    public static string Label(TransactionType t) => Services.Loc.T("Type_" + t);
}
