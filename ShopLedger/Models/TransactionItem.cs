using SQLite;

namespace ShopLedger.Models;

public class TransactionItem
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int TransactionId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";   // snapshot, survives product edits
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    [Ignore] public decimal LineTotal => Quantity * UnitPrice;
}
