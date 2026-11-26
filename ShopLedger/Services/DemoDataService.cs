using ShopLedger.Data;
using ShopLedger.Models;

namespace ShopLedger.Services;

public interface IDemoDataService
{
    /// <summary>True when there are no products, customers or transactions at all.</summary>
    Task<bool> IsEmptyAsync();
    /// <summary>Fills an EMPTY database with sample products, customers and a realistic 3-week history.</summary>
    Task SeedAsync();
    /// <summary>Permanently deletes every product, customer, transaction and stock record.</summary>
    Task ClearAllAsync();
}

public class DemoDataService : IDemoDataService
{
    private readonly AppDatabase _db;
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;
    private readonly ILedgerService _ledger;
    private readonly ISettingsService _settings;

    public DemoDataService(AppDatabase db, IProductRepository products, ICustomerRepository customers,
                           ILedgerService ledger, ISettingsService settings)
    { _db = db; _products = products; _customers = customers; _ledger = ledger; _settings = settings; }

    public async Task<bool> IsEmptyAsync()
    {
        await _db.InitAsync();
        return await _db.Connection.Table<Product>().CountAsync() == 0
            && await _db.Connection.Table<Customer>().CountAsync() == 0
            && await _db.Connection.Table<LedgerTransaction>().CountAsync() == 0;
    }

    public async Task SeedAsync()
    {
        if (!await IsEmptyAsync()) throw new InvalidOperationException(Loc.T("Demo_NotEmpty"));

        var today = DateTime.Today;
        DateTime At(int daysAgo, int hour) => today.AddDays(-daysAgo).AddHours(hour);

        async Task<Product> P(string name, string category, string unit, decimal cost, decimal sale, decimal stock, decimal low)
        {
            var p = new Product { Name = name, Category = category, Unit = unit, PurchasePrice = cost,
                                  SalePrice = sale, Stock = stock, LowStockLevel = low };
            await _products.SaveAsync(p);
            return p;
        }
        async Task<Customer> C(string name, string phone, decimal limit)
        {
            var c = new Customer { Name = name, Phone = phone, CreditLimit = limit, Notes = "Demo" };
            await _customers.SaveAsync(c);
            return c;
        }

        var sugar = await P("Sugar", "Grocery", "kg", 140, 150, 60, 10);
        var rice = await P("Rice", "Grocery", "kg", 200, 230, 40, 10);
        var flour = await P("Flour (Atta)", "Grocery", "kg", 90, 100, 100, 20);
        var oil = await P("Cooking Oil", "Grocery", "litre", 450, 480, 20, 15);
        var tea = await P("Tea", "Grocery", "pack", 380, 420, 25, 5);
        var milk = await P("Milk", "Dairy", "litre", 190, 210, 8, 5);
        var soap = await P("Soap", "Household", "pcs", 80, 100, 30, 5);
        var biscuits = await P("Biscuits", "Grocery", "pack", 40, 50, 40, 10);

        var ali = await C("Ali Khan", "03001234567", 20000);
        var sara = await C("Sara Bibi", "03111234567", 5000);
        var bilal = await C("Bilal Ahmed", "03211234567", 10000);
        var usman = await C("Usman Traders", "03331234567", 50000);
        var fatima = await C("Fatima", "03451234567", 500);   // will be over her limit: shows the warning tag

        static LineRequest Line(Product p, decimal qty) => new(p.Id, qty, p.SalePrice);

        Task Credit(Customer c, DateTime when, decimal paid, params (Product p, decimal qty)[] lines) =>
            _ledger.RecordSaleAsync(new SaleRequest
            {
                Type = TransactionType.CreditSale, CustomerId = c.Id, PaidAmount = paid, Date = when,
                Note = "Demo", AllowOverLimit = true, Items = lines.Select(l => Line(l.p, l.qty)).ToList()
            });
        Task Cash(DateTime when, params (Product p, decimal qty)[] lines) =>
            _ledger.RecordSaleAsync(new SaleRequest
            {
                Type = TransactionType.CashSale, Date = when, Note = "Demo",
                Items = lines.Select(l => Line(l.p, l.qty)).ToList()
            });

        // Inserted oldest-first: statements rely on BalanceAfter being a running balance in date order.
        await _ledger.RecordAdjustmentAsync(bilal.Id, 1500, "Opening balance", At(20, 9));
        await Credit(ali, At(9, 11), 0, (sugar, 5), (oil, 2));
        await Credit(sara, At(8, 15), 500, (flour, 20));
        await Credit(bilal, At(7, 10), 1000, (rice, 10));
        await _ledger.RecordPaymentAsync(ali.Id, 500, "Demo", At(6, 16));
        await Credit(usman, At(5, 12), 2000, (oil, 5), (tea, 3));
        await Cash(At(4, 13), (biscuits, 6));
        await _ledger.RecordPurchaseAsync(new List<LineRequest> { new(sugar.Id, 30, 140) }, "Demo restock", At(3, 9));
        await Credit(ali, At(3, 17), 0, (milk, 4));
        await _ledger.RecordPaymentAsync(bilal.Id, 1000, "Demo", At(2, 12));
        await Credit(fatima, At(2, 14), 0, (soap, 3), (tea, 1));
        await Cash(At(1, 11), (sugar, 2), (flour, 5));
        await Cash(today.AddMinutes(10), (soap, 2), (biscuits, 4));
        await _ledger.RecordPaymentAsync(ali.Id, 300, "Demo", today.AddMinutes(20));

        _settings.DemoDataSeeded = true;
    }

    public async Task ClearAllAsync()
    {
        await _db.InTransactionAsync(c =>
        {
            c.DeleteAll<TransactionItem>();
            c.DeleteAll<LedgerTransaction>();
            c.DeleteAll<StockMovement>();
            c.DeleteAll<Customer>();
            c.DeleteAll<Product>();
        });
        _settings.DemoDataSeeded = false;
    }
}
