using ShopLedger.Data;
using ShopLedger.Models;

namespace ShopLedger.Services;

public record DashboardSummary(decimal TodaySales, decimal TodayCashReceived, decimal TotalReceivable,
                               List<Product> LowStock, List<Customer> TopCustomers)
{
    /// <summary>Active products.</summary>
    public int TotalProducts { get; init; }
    public int CategoryCount { get; init; }
    /// <summary>Stock on hand valued at purchase price.</summary>
    public decimal StockValue { get; init; }
    public int OutOfStockCount { get; init; }
    public int OverLimitCount { get; init; }
}

public interface IReportService { Task<DashboardSummary> GetDashboardAsync(); }

public class ReportService : IReportService
{
    private readonly ITransactionRepository _txns;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;

    public ReportService(ITransactionRepository txns, ICustomerRepository customers, IProductRepository products)
    { _txns = txns; _customers = customers; _products = products; }

    public async Task<DashboardSummary> GetDashboardAsync()
    {
        var today = await _txns.GetAsync(from: DateTime.Today, to: DateTime.Today);
        var sales = today.Where(t => t.Type is TransactionType.CreditSale or TransactionType.CashSale).Sum(t => t.TotalAmount);
        // cash in hand received today: cash sales + down payments on credit sales + customer payments
        var cash = today.Where(t => t.Type is TransactionType.CashSale or TransactionType.CreditSale or TransactionType.Payment)
                        .Sum(t => t.Type == TransactionType.CashSale ? t.TotalAmount : t.PaidAmount);

        var customers = await _customers.GetAllAsync();
        var top = customers.Where(c => c.CurrentBalance > 0).OrderByDescending(c => c.CurrentBalance).Take(5).ToList();

        var products = await _products.GetAllAsync();
        var low = products.Where(p => p.IsLowStock).ToList();

        return new DashboardSummary(sales, cash, await _customers.GetTotalReceivableAsync(), low, top)
        {
            TotalProducts = products.Count,
            CategoryCount = products.Select(p => p.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().Count(),
            StockValue = products.Where(p => p.Stock > 0).Sum(p => p.Stock * p.PurchasePrice),
            OutOfStockCount = products.Count(p => p.IsOutOfStock),
            OverLimitCount = customers.Count(c => c.IsOverLimit),
        };
    }
}
