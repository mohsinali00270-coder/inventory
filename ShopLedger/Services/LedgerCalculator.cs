using ShopLedger.Models;

namespace ShopLedger.Services;

/// <summary>Pure, side-effect-free ledger math. Unit-tested in Phase 7.</summary>
public static class LedgerCalculator
{
    public static decimal LineTotal(decimal quantity, decimal unitPrice) => Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

    /// <summary>How much the customer's balance (what they owe) changes.</summary>
    public static decimal BalanceDelta(TransactionType type, decimal total, decimal paid) => type switch
    {
        TransactionType.CreditSale => total - paid,
        TransactionType.Payment => -paid,
        TransactionType.Adjustment => total,   // signed
        _ => 0m                                // CashSale, Purchase
    };

    /// <summary>How much product stock changes for a given quantity.</summary>
    public static decimal StockDelta(TransactionType type, decimal quantity) => type switch
    {
        TransactionType.CreditSale or TransactionType.CashSale => -quantity,
        TransactionType.Purchase => quantity,
        _ => 0m
    };

    public static bool ExceedsLimit(decimal creditLimit, decimal newBalance) =>
        creditLimit > 0 && newBalance > creditLimit;
}
