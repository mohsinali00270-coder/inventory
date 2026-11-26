using ShopLedger.Models;
using ShopLedger.Services;
using Xunit;

namespace ShopLedger.Tests;

public class LedgerServiceTests
{
    private static SaleRequest Sale(TransactionType type, int? customer, Product p, decimal qty, decimal paid = 0, bool over = false) =>
        new() { Type = type, CustomerId = customer, PaidAmount = paid, AllowOverLimit = over,
                Items = new() { new LineRequest(p.Id, qty, p.SalePrice) } };

    [Fact]
    public async Task CreditSale_reduces_stock_and_raises_balance()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(stock: 50, sale: 100);
        var c = await t.AddCustomer();

        var tx = await t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 4, paid: 100));

        Assert.Equal(400m, tx.TotalAmount);
        Assert.Equal(300m, tx.BalanceAfter);
        Assert.Equal(46m, (await t.Products.GetAsync(p.Id))!.Stock);
        Assert.Equal(300m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }

    [Fact]
    public async Task CashSale_reduces_stock_but_not_balance()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(stock: 10, sale: 50);
        var c = await t.AddCustomer();

        await t.Ledger.RecordSaleAsync(Sale(TransactionType.CashSale, null, p, 3));

        Assert.Equal(7m, (await t.Products.GetAsync(p.Id))!.Stock);
        Assert.Equal(0m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }

    [Fact]
    public async Task Sale_with_too_little_stock_throws_and_changes_nothing()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(stock: 2);
        var c = await t.AddCustomer();

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 5)));

        Assert.Equal(2m, (await t.Products.GetAsync(p.Id))!.Stock);
        Assert.Equal(0m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }

    [Fact]
    public async Task Failure_on_second_line_rolls_back_the_first_line()
    {
        using var t = new TestDb();
        var a = await t.AddProduct("A", stock: 10);
        var b = await t.AddProduct("B", stock: 1);
        var c = await t.AddCustomer();
        var req = new SaleRequest
        {
            Type = TransactionType.CreditSale, CustomerId = c.Id,
            Items = new() { new LineRequest(a.Id, 3, 100), new LineRequest(b.Id, 5, 100) }
        };

        await Assert.ThrowsAsync<InsufficientStockException>(() => t.Ledger.RecordSaleAsync(req));

        Assert.Equal(10m, (await t.Products.GetAsync(a.Id))!.Stock);   // untouched
        Assert.Equal(0m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }

    [Fact]
    public async Task Credit_limit_blocks_unless_overridden()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(stock: 100, sale: 100);
        var c = await t.AddCustomer(limit: 500);

        await Assert.ThrowsAsync<CreditLimitExceededException>(() =>
            t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 6)));      // 600 > 500

        await t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 6, over: true));
        Assert.Equal(600m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }

    [Fact]
    public async Task Payment_reduces_balance_and_overpayment_goes_negative()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(sale: 100);
        var c = await t.AddCustomer();
        await t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 5));    // owes 500

        var t1 = await t.Ledger.RecordPaymentAsync(c.Id, 200);
        Assert.Equal(300m, t1.BalanceAfter);

        var t2 = await t.Ledger.RecordPaymentAsync(c.Id, 400);
        Assert.Equal(-100m, t2.BalanceAfter);                                           // advance
        Assert.Equal(-100m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }

    [Fact]
    public async Task Adjustment_is_signed()
    {
        using var t = new TestDb();
        var c = await t.AddCustomer();
        await t.Ledger.RecordAdjustmentAsync(c.Id, 1000, "opening");
        await t.Ledger.RecordAdjustmentAsync(c.Id, -250, "correction");
        Assert.Equal(750m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }

    [Fact]
    public async Task Purchase_adds_stock()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(stock: 5, cost: 80);
        await t.Ledger.RecordPurchaseAsync(new() { new LineRequest(p.Id, 20, 90) });
        var after = (await t.Products.GetAsync(p.Id))!;
        Assert.Equal(25m, after.Stock);
        Assert.Equal(90m, after.PurchasePrice);   // purchase price follows the latest purchase
    }

    [Fact]
    public async Task Credit_sale_without_customer_is_rejected()
    {
        using var t = new TestDb();
        var p = await t.AddProduct();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, null, p, 1)));
    }

    [Fact]
    public async Task Paid_amount_cannot_exceed_total()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(sale: 100);
        var c = await t.AddCustomer();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 1, paid: 150)));
    }

    [Fact]
    public async Task Balance_equals_sum_of_deltas_over_a_sequence()
    {
        using var t = new TestDb();
        var p = await t.AddProduct(stock: 100, sale: 10);
        var c = await t.AddCustomer();
        await t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 10, paid: 20));  // +80
        await t.Ledger.RecordPaymentAsync(c.Id, 30);                                              // -30
        await t.Ledger.RecordAdjustmentAsync(c.Id, 5);                                            // +5
        await t.Ledger.RecordSaleAsync(Sale(TransactionType.CreditSale, c.Id, p, 3));             // +30
        Assert.Equal(85m, (await t.Customers.GetAsync(c.Id))!.CurrentBalance);
    }
}
