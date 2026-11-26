using ShopLedger.Models;

namespace ShopLedger.Data;

public interface IStockRepository
{
    /// <summary>qty is signed: + adds stock, - removes. Atomic: product stock + movement log.</summary>
    Task AdjustAsync(int productId, decimal qty, StockReason reason, string? note);
    Task<List<StockMovement>> GetMovementsAsync(int productId);
}

public class StockRepository : IStockRepository
{
    private readonly AppDatabase _db;
    public StockRepository(AppDatabase db) => _db = db;

    public async Task AdjustAsync(int productId, decimal qty, StockReason reason, string? note)
    {
        await _db.InTransactionAsync(c =>
        {
            var p = c.Find<Product>(productId) ?? throw new InvalidOperationException("Product not found.");
            if (p.Stock + qty < 0) throw new InvalidOperationException("Stock cannot go below zero.");
            p.Stock += qty;
            c.Update(p);
            c.Insert(new StockMovement { ProductId = productId, Quantity = qty, Reason = reason, Note = note });
        });
    }

    public async Task<List<StockMovement>> GetMovementsAsync(int productId)
    {
        await _db.InitAsync();
        return await _db.Connection.Table<StockMovement>()
            .Where(m => m.ProductId == productId).OrderByDescending(m => m.Date).ToListAsync();
    }
}
