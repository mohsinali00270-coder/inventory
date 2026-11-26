using ShopLedger.Models;

namespace ShopLedger.Data;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(string? search = null, string? category = null);
    Task<Product?> GetAsync(int id);
    Task<List<Product>> GetLowStockAsync();
    Task<List<string>> GetCategoriesAsync();
    Task<int> SaveAsync(Product p);
    Task<int> DeleteAsync(Product p);
}

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(string? search = null);
    Task<Customer?> GetAsync(int id);
    Task<decimal> GetTotalReceivableAsync();
    Task<int> SaveAsync(Customer c);
    Task<int> DeleteAsync(Customer c);
}

public interface ITransactionRepository
{
    Task<List<LedgerTransaction>> GetAsync(int? customerId = null, TransactionType? type = null,
        DateTime? from = null, DateTime? to = null);
    Task<List<TransactionItem>> GetItemsAsync(int transactionId);
}

public class ProductRepository : IProductRepository
{
    private readonly AppDatabase _db;
    public ProductRepository(AppDatabase db) => _db = db;

    public async Task<List<Product>> GetAllAsync(string? search = null, string? category = null)
    {
        await _db.InitAsync();
        var list = await _db.Connection.Table<Product>().Where(p => p.IsActive)
                            .OrderBy(p => p.Name).ToListAsync();
        if (!string.IsNullOrWhiteSpace(search))
            list = list.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || (p.Barcode ?? "") == search).ToList();
        if (!string.IsNullOrWhiteSpace(category))
            list = list.Where(p => p.Category == category).ToList();
        return list;
    }

    public async Task<Product?> GetAsync(int id)
    {
        await _db.InitAsync();
        return await _db.Connection.FindAsync<Product>(id);
    }

    public async Task<List<Product>> GetLowStockAsync()
    {
        var all = await GetAllAsync();
        return all.Where(p => p.IsLowStock).ToList();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var all = await GetAllAsync();
        return all.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
    }

    public async Task<int> SaveAsync(Product p)
    {
        await _db.InitAsync();
        if (p.Id == 0)
        {
            await _db.Connection.InsertAsync(p);
            if (p.Stock != 0)
                await _db.Connection.InsertAsync(new StockMovement
                { ProductId = p.Id, Quantity = p.Stock, Reason = StockReason.Initial, Note = "Opening stock" });
            return 1;
        }
        return await _db.Connection.UpdateAsync(p);
    }

    // Soft delete keeps history intact.
    public async Task<int> DeleteAsync(Product p)
    {
        await _db.InitAsync();
        p.IsActive = false;
        return await _db.Connection.UpdateAsync(p);
    }
}

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDatabase _db;
    public CustomerRepository(AppDatabase db) => _db = db;

    public async Task<List<Customer>> GetAllAsync(string? search = null)
    {
        await _db.InitAsync();
        var list = await _db.Connection.Table<Customer>().OrderBy(c => c.Name).ToListAsync();
        if (!string.IsNullOrWhiteSpace(search))
            list = list.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || (c.Phone ?? "").Contains(search)).ToList();
        return list;
    }

    public async Task<Customer?> GetAsync(int id)
    {
        await _db.InitAsync();
        return await _db.Connection.FindAsync<Customer>(id);
    }

    public async Task<decimal> GetTotalReceivableAsync()
    {
        var all = await GetAllAsync();
        return all.Where(c => c.CurrentBalance > 0).Sum(c => c.CurrentBalance);
    }

    public async Task<int> SaveAsync(Customer c)
    {
        await _db.InitAsync();
        return c.Id == 0 ? await _db.Connection.InsertAsync(c) : await _db.Connection.UpdateAsync(c);
    }

    public async Task<int> DeleteAsync(Customer c)
    {
        await _db.InitAsync();
        return await _db.Connection.DeleteAsync(c);
    }
}

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDatabase _db;
    public TransactionRepository(AppDatabase db) => _db = db;

    public async Task<List<LedgerTransaction>> GetAsync(int? customerId = null, TransactionType? type = null,
        DateTime? from = null, DateTime? to = null)
    {
        await _db.InitAsync();
        var list = await _db.Connection.Table<LedgerTransaction>().OrderByDescending(t => t.Date).ToListAsync();
        if (customerId.HasValue) list = list.Where(t => t.CustomerId == customerId).ToList();
        if (type.HasValue) list = list.Where(t => t.Type == type).ToList();
        if (from.HasValue) list = list.Where(t => t.Date >= from.Value.Date).ToList();
        if (to.HasValue) list = list.Where(t => t.Date < to.Value.Date.AddDays(1)).ToList();
        return list;
    }

    public async Task<List<TransactionItem>> GetItemsAsync(int transactionId)
    {
        await _db.InitAsync();
        return await _db.Connection.Table<TransactionItem>().Where(i => i.TransactionId == transactionId).ToListAsync();
    }
}
