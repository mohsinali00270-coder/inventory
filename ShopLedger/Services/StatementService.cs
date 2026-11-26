using ShopLedger.Data;
using ShopLedger.Models;

namespace ShopLedger.Services;

public record StatementRow(DateTime Date, string Description, decimal Debit, decimal Credit, decimal Balance);

public class Statement
{
    public Customer Customer { get; init; } = new();
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public decimal OpeningBalance { get; init; }
    public List<StatementRow> Rows { get; init; } = new();
    public decimal TotalDebit => Rows.Sum(r => r.Debit);
    public decimal TotalCredit => Rows.Sum(r => r.Credit);
    public decimal ClosingBalance => Rows.Count > 0 ? Rows[^1].Balance : OpeningBalance;
    public string Period => From is null ? Loc.T("Range_All") : $"{From:dd MMM yyyy} - {To:dd MMM yyyy}";
}

public interface IStatementService
{
    Task<Statement> GetAsync(int customerId, DateTime? from, DateTime? to);
}

/// <summary>Debit = customer owes more (credit sale, + manual entry). Credit = customer paid / owes less.
/// Cash sales never touch the balance, so they are not part of the statement.</summary>
public class StatementService : IStatementService
{
    private readonly AppDatabase _db;
    private readonly ICustomerRepository _customers;
    private readonly ITransactionRepository _txns;

    public StatementService(AppDatabase db, ICustomerRepository customers, ITransactionRepository txns)
    { _db = db; _customers = customers; _txns = txns; }

    public async Task<Statement> GetAsync(int customerId, DateTime? from, DateTime? to)
    {
        var customer = await _customers.GetAsync(customerId) ?? throw new InvalidOperationException(Loc.T("Err_CustNotFound"));

        var all = (await _txns.GetAsync(customerId: customerId))
            .Where(t => t.Type is TransactionType.CreditSale or TransactionType.Payment or TransactionType.Adjustment)
            .OrderBy(t => t.Date).ThenBy(t => t.Id).ToList();

        var before = from.HasValue ? all.Where(t => t.Date < from.Value.Date).ToList() : new List<LedgerTransaction>();
        var opening = before.Count > 0 ? before[^1].BalanceAfter : 0m;

        var inRange = all.Where(t => (!from.HasValue || t.Date >= from.Value.Date) &&
                                     (!to.HasValue || t.Date < to.Value.Date.AddDays(1))).ToList();

        await _db.InitAsync();
        var ids = inRange.Select(t => t.Id).ToHashSet();
        var items = (await _db.Connection.Table<TransactionItem>().ToListAsync())
            .Where(i => ids.Contains(i.TransactionId)).ToLookup(i => i.TransactionId);

        var rows = new List<StatementRow>();
        foreach (var t in inRange)
        {
            decimal debit = 0, credit = 0;
            switch (t.Type)
            {
                case TransactionType.CreditSale: debit = t.TotalAmount; credit = t.PaidAmount; break;
                case TransactionType.Payment: credit = t.PaidAmount; break;
                case TransactionType.Adjustment:
                    if (t.TotalAmount > 0) debit = t.TotalAmount; else credit = -t.TotalAmount; break;
            }
            var desc = Describe(t, items[t.Id]);
            rows.Add(new StatementRow(t.Date, desc, debit, credit, t.BalanceAfter));
        }

        return new Statement { Customer = customer, From = from, To = to, OpeningBalance = opening, Rows = rows };
    }

    private static string Describe(LedgerTransaction t, IEnumerable<TransactionItem> items)
    {
        var label = ViewModels.TransactionRow.Label(t.Type);
        var list = items.Select(i => $"{i.ProductName} x{i.Quantity:0.##}").ToList();
        var text = list.Count > 0 ? $"{label}: {string.Join(", ", list)}" : label;
        return string.IsNullOrWhiteSpace(t.Note) ? text : $"{text} ({t.Note})";
    }
}
