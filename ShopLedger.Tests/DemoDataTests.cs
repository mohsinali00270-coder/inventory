using SQLite;
using ShopLedger.Models;
using ShopLedger.Services;
using Xunit;

namespace ShopLedger.Tests;

public class DemoDataTests
{
    private static DemoDataService Make(TestDb t) =>
        new(t.Db, t.Products, t.Customers, t.Ledger, new SettingsService());

    [Fact]
    public async Task Seed_fills_an_empty_database()
    {
        using var t = new TestDb();
        var demo = Make(t);
        Assert.True(await demo.IsEmptyAsync());
        await demo.SeedAsync();
        Assert.False(await demo.IsEmptyAsync());
        Assert.True((await t.Products.GetAllAsync()).Count >= 5);
        Assert.True((await t.Customers.GetAllAsync()).Count >= 3);
    }

    [Fact]
    public async Task Seed_refuses_when_data_exists()
    {
        using var t = new TestDb();
        await t.AddProduct();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Make(t).SeedAsync());
    }

    [Fact]
    public async Task Seeded_balances_match_the_transaction_history()
    {
        using var t = new TestDb();
        await Make(t).SeedAsync();
        var txs = await t.Db.Connection.Table<LedgerTransaction>().ToListAsync();

        foreach (var c in await t.Customers.GetAllAsync())
        {
            var delta = txs.Where(x => x.CustomerId == c.Id)
                           .Sum(x => LedgerCalculator.BalanceDelta(x.Type, x.TotalAmount, x.PaidAmount));
            Assert.Equal(delta, c.CurrentBalance);

            var last = txs.Where(x => x.CustomerId == c.Id).OrderBy(x => x.Date).ThenBy(x => x.Id).LastOrDefault();
            if (last is not null) Assert.Equal(c.CurrentBalance, last.BalanceAfter);   // running balance is consistent
        }
    }

    [Fact]
    public async Task Seeded_stock_is_never_negative()
    {
        using var t = new TestDb();
        await Make(t).SeedAsync();
        Assert.All(await t.Products.GetAllAsync(), p => Assert.True(p.Stock >= 0, p.Name));
    }

    [Fact]
    public async Task ClearAll_empties_everything()
    {
        using var t = new TestDb();
        var demo = Make(t);
        await demo.SeedAsync();
        await demo.ClearAllAsync();
        Assert.True(await demo.IsEmptyAsync());
        Assert.Equal(0, await t.Db.Connection.Table<TransactionItem>().CountAsync());
        Assert.Equal(0, await t.Db.Connection.Table<StockMovement>().CountAsync());
    }
}
