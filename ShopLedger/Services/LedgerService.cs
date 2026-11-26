using ShopLedger.Data;
using ShopLedger.Models;

namespace ShopLedger.Services;

public interface ILedgerService
{
    Task<LedgerTransaction> RecordSaleAsync(SaleRequest request);
    Task<LedgerTransaction> RecordPurchaseAsync(List<LineRequest> items, string? note = null, DateTime? date = null);
    Task<LedgerTransaction> RecordPaymentAsync(int customerId, decimal amount, string? note = null, DateTime? date = null);
    /// <summary>signedAmount: + customer owes more, - customer owes less.</summary>
    Task<LedgerTransaction> RecordAdjustmentAsync(int customerId, decimal signedAmount, string? note = null, DateTime? date = null);
}

/// <summary>Every operation runs in ONE database transaction: transaction row + items +
/// stock + stock log + customer balance. Any failure rolls everything back.</summary>
public class LedgerService : ILedgerService
{
    private readonly AppDatabase _db;
    public LedgerService(AppDatabase db) => _db = db;

    public async Task<LedgerTransaction> RecordSaleAsync(SaleRequest r)
    {
        if (r.Type is not (TransactionType.CreditSale or TransactionType.CashSale))
            throw new ArgumentException(Loc.T("Err_SaleType"));
        ValidateLines(r.Items);
        if (r.Type == TransactionType.CreditSale && r.CustomerId is null)
            throw new InvalidOperationException(Loc.T("Err_CreditNeedsCustomer"));

        var total = r.Items.Sum(i => LedgerCalculator.LineTotal(i.Quantity, i.UnitPrice));
        var paid = r.Type == TransactionType.CashSale ? total : r.PaidAmount;
        if (paid < 0 || paid > total)
            throw new InvalidOperationException(Loc.T("Err_PaidRange"));

        LedgerTransaction? result = null;
        await _db.InTransactionAsync(c =>
        {
            Customer? cust = null;
            if (r.CustomerId is int cid)
                cust = c.Find<Customer>(cid) ?? throw new InvalidOperationException(Loc.T("Err_CustNotFound"));

            var delta = LedgerCalculator.BalanceDelta(r.Type, total, paid);
            var newBalance = (cust?.CurrentBalance ?? 0m) + delta;
            if (cust is not null && delta > 0 && !r.AllowOverLimit &&
                LedgerCalculator.ExceedsLimit(cust.CreditLimit, newBalance))
                throw new CreditLimitExceededException(cust.Name, cust.CreditLimit, newBalance);

            // stock check + deduction (grouped, in case a product appears on two lines)
            var touched = new List<(Product p, decimal qty)>();
            foreach (var g in r.Items.GroupBy(i => i.ProductId))
            {
                var p = c.Find<Product>(g.Key) ?? throw new InvalidOperationException(Loc.T("Err_ProdNotFound"));
                var qty = g.Sum(x => x.Quantity);
                if (p.Stock < qty) throw new InsufficientStockException(p.Name, p.Stock);
                p.Stock += LedgerCalculator.StockDelta(r.Type, qty);
                touched.Add((p, qty));
            }

            var t = new LedgerTransaction
            {
                CustomerId = r.CustomerId, Type = r.Type, TotalAmount = total, PaidAmount = paid,
                BalanceAfter = newBalance, Date = r.Date ?? DateTime.Now, Note = r.Note
            };
            c.Insert(t);
            InsertItems(c, t, r.Items, touched.Select(x => x.p));
            foreach (var (p, qty) in touched)
            {
                c.Update(p);
                c.Insert(new StockMovement { ProductId = p.Id, Quantity = -qty, Reason = StockReason.Sale,
                                             TransactionId = t.Id, Date = t.Date });
            }
            if (cust is not null && delta != 0) { cust.CurrentBalance = newBalance; c.Update(cust); }
            result = t;
        });
        return result!;
    }

    public async Task<LedgerTransaction> RecordPurchaseAsync(List<LineRequest> items, string? note = null, DateTime? date = null)
    {
        ValidateLines(items);
        var total = items.Sum(i => LedgerCalculator.LineTotal(i.Quantity, i.UnitPrice));

        LedgerTransaction? result = null;
        await _db.InTransactionAsync(c =>
        {
            var touched = new List<(Product p, decimal qty)>();
            foreach (var g in items.GroupBy(i => i.ProductId))
            {
                var p = c.Find<Product>(g.Key) ?? throw new InvalidOperationException(Loc.T("Err_ProdNotFound"));
                var qty = g.Sum(x => x.Quantity);
                p.Stock += LedgerCalculator.StockDelta(TransactionType.Purchase, qty);
                p.PurchasePrice = g.Last().UnitPrice;      // latest cost price
                touched.Add((p, qty));
            }
            var t = new LedgerTransaction
            {
                Type = TransactionType.Purchase, TotalAmount = total, PaidAmount = total,
                Date = date ?? DateTime.Now, Note = note
            };
            c.Insert(t);
            InsertItems(c, t, items, touched.Select(x => x.p));
            foreach (var (p, qty) in touched)
            {
                c.Update(p);
                c.Insert(new StockMovement { ProductId = p.Id, Quantity = qty, Reason = StockReason.Purchase,
                                             TransactionId = t.Id, Date = t.Date });
            }
            result = t;
        });
        return result!;
    }

    public async Task<LedgerTransaction> RecordPaymentAsync(int customerId, decimal amount, string? note = null, DateTime? date = null)
    {
        if (amount <= 0) throw new InvalidOperationException(Loc.T("Err_PaymentZero"));
        LedgerTransaction? result = null;
        await _db.InTransactionAsync(c =>
        {
            var cust = c.Find<Customer>(customerId) ?? throw new InvalidOperationException(Loc.T("Err_CustNotFound"));
            var newBalance = cust.CurrentBalance + LedgerCalculator.BalanceDelta(TransactionType.Payment, amount, amount);
            var t = new LedgerTransaction
            {
                CustomerId = customerId, Type = TransactionType.Payment, TotalAmount = amount, PaidAmount = amount,
                BalanceAfter = newBalance, Date = date ?? DateTime.Now, Note = note
            };
            c.Insert(t);
            cust.CurrentBalance = newBalance; c.Update(cust);
            result = t;
        });
        return result!;
    }

    public async Task<LedgerTransaction> RecordAdjustmentAsync(int customerId, decimal signedAmount, string? note = null, DateTime? date = null)
    {
        if (signedAmount == 0) throw new InvalidOperationException(Loc.T("Err_AmountNotZero"));
        LedgerTransaction? result = null;
        await _db.InTransactionAsync(c =>
        {
            var cust = c.Find<Customer>(customerId) ?? throw new InvalidOperationException(Loc.T("Err_CustNotFound"));
            var newBalance = cust.CurrentBalance + LedgerCalculator.BalanceDelta(TransactionType.Adjustment, signedAmount, 0);
            var t = new LedgerTransaction
            {
                CustomerId = customerId, Type = TransactionType.Adjustment, TotalAmount = signedAmount,
                BalanceAfter = newBalance, Date = date ?? DateTime.Now, Note = note
            };
            c.Insert(t);
            cust.CurrentBalance = newBalance; c.Update(cust);
            result = t;
        });
        return result!;
    }

    private static void ValidateLines(List<LineRequest> items)
    {
        if (items.Count == 0) throw new InvalidOperationException(Loc.T("Err_AddProduct"));
        if (items.Any(i => i.Quantity <= 0)) throw new InvalidOperationException(Loc.T("Err_QtyPositive"));
        if (items.Any(i => i.UnitPrice < 0)) throw new InvalidOperationException(Loc.T("Err_PriceNeg"));
    }

    private static void InsertItems(SQLite.SQLiteConnection c, LedgerTransaction t,
                                    List<LineRequest> items, IEnumerable<Product> products)
    {
        var names = products.ToDictionary(p => p.Id, p => p.Name);
        foreach (var i in items)
        {
            var row = new TransactionItem
            {
                TransactionId = t.Id, ProductId = i.ProductId, ProductName = names[i.ProductId],
                Quantity = i.Quantity, UnitPrice = i.UnitPrice
            };
            c.Insert(row);
            t.Items.Add(row);
        }
    }
}
