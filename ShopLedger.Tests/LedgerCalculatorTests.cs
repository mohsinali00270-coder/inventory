using ShopLedger.Models;
using ShopLedger.Services;
using Xunit;

namespace ShopLedger.Tests;

public class LedgerCalculatorTests
{
    [Theory]
    [InlineData(2, 150, 300)]
    [InlineData(0.5, 150, 75)]
    [InlineData(1.5, 99.99, 149.99)]   // 149.985 rounds to 2 decimals
    [InlineData(0, 100, 0)]
    public void LineTotal_multiplies_and_rounds(double qty, double price, double expected) =>
        Assert.Equal((decimal)expected, LedgerCalculator.LineTotal((decimal)qty, (decimal)price));

    [Theory]
    [InlineData(TransactionType.CreditSale, 1000, 0, 1000)]     // all on credit
    [InlineData(TransactionType.CreditSale, 1000, 400, 600)]    // partly paid
    [InlineData(TransactionType.CreditSale, 1000, 1000, 0)]     // fully paid
    [InlineData(TransactionType.CashSale, 1000, 1000, 0)]       // cash never touches the balance
    [InlineData(TransactionType.Payment, 0, 300, -300)]         // payment reduces what they owe
    [InlineData(TransactionType.Payment, 0, 5000, -5000)]       // overpay -> negative (advance)
    [InlineData(TransactionType.Adjustment, 250, 0, 250)]
    [InlineData(TransactionType.Adjustment, -250, 0, -250)]
    [InlineData(TransactionType.Purchase, 900, 900, 0)]
    public void BalanceDelta_follows_ledger_rules(TransactionType type, double total, double paid, double expected) =>
        Assert.Equal((decimal)expected, LedgerCalculator.BalanceDelta(type, (decimal)total, (decimal)paid));

    [Theory]
    [InlineData(TransactionType.CreditSale, 3, -3)]
    [InlineData(TransactionType.CashSale, 3, -3)]
    [InlineData(TransactionType.Purchase, 10, 10)]
    [InlineData(TransactionType.Payment, 10, 0)]
    [InlineData(TransactionType.Adjustment, 10, 0)]
    public void StockDelta_follows_type(TransactionType type, double qty, double expected) =>
        Assert.Equal((decimal)expected, LedgerCalculator.StockDelta(type, (decimal)qty));

    [Theory]
    [InlineData(1000, 1000, false)]   // exactly at the limit is allowed
    [InlineData(1000, 1001, true)]
    [InlineData(0, 999999, false)]    // limit 0 = no limit
    [InlineData(500, 100, false)]
    public void ExceedsLimit_works(double limit, double newBalance, bool expected) =>
        Assert.Equal(expected, LedgerCalculator.ExceedsLimit((decimal)limit, (decimal)newBalance));
}
