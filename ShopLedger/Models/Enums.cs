namespace ShopLedger.Models;

public enum TransactionType { CreditSale = 0, CashSale = 1, Payment = 2, Adjustment = 3, Purchase = 4 }

public enum StockReason { Initial = 0, Purchase = 1, Sale = 2, Damage = 3, Loss = 4, Correction = 5 }
