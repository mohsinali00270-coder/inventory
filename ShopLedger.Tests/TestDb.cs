using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Services;

namespace ShopLedger.Tests;

/// <summary>A throw-away SQLite file plus the real repositories and ledger service on top of it.</summary>
public sealed class TestDb : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"shopledger_test_{Guid.NewGuid():N}.db3");
    public AppDatabase Db { get; }
    public ProductRepository Products { get; }
    public CustomerRepository Customers { get; }
    public LedgerService Ledger { get; }

    public TestDb()
    {
        Db = new AppDatabase(_path);
        Products = new ProductRepository(Db);
        Customers = new CustomerRepository(Db);
        Ledger = new LedgerService(Db);
    }

    public async Task<Product> AddProduct(string name = "Sugar", decimal stock = 50, decimal sale = 100, decimal cost = 80)
    {
        var p = new Product { Name = name, Unit = "kg", Stock = stock, SalePrice = sale, PurchasePrice = cost, LowStockLevel = 5 };
        await Products.SaveAsync(p);
        return p;
    }

    public async Task<Customer> AddCustomer(string name = "Ali", decimal limit = 0)
    {
        var c = new Customer { Name = name, CreditLimit = limit };
        await Customers.SaveAsync(c);
        return c;
    }

    public void Dispose()
    {
        try { Db.CloseAsync().GetAwaiter().GetResult(); } catch { }
        foreach (var f in new[] { _path, _path + "-wal", _path + "-shm" })
            try { if (File.Exists(f)) File.Delete(f); } catch { }
    }
}
