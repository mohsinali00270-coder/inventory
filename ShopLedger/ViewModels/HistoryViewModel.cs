using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Services;

namespace ShopLedger.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private static string AllCustomers => Loc.T("All_Customers");
    private static string AllTypes => Loc.T("All_Types");

    private readonly ITransactionRepository _txns;
    private readonly ICustomerRepository _customers;
    private Dictionary<string, int> _customerIds = new();
    private bool _ready;

    public ObservableCollection<TransactionRow> Rows { get; } = new();
    public ObservableCollection<string> CustomerNames { get; } = new();
    public List<string> Ranges => DateRangeHelper.Names;
    public List<string> TypeNames { get; } =
        new[] { AllTypes }.Concat(Enum.GetValues<TransactionType>().Select(TransactionRow.Label)).ToList();

    [ObservableProperty] private string selectedRange = DateRangeHelper.Label("ThisMonth");
    [ObservableProperty] private bool isCustom;
    [ObservableProperty] private DateTime customFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime customTo = DateTime.Today;
    [ObservableProperty] private string selectedCustomer = AllCustomers;
    [ObservableProperty] private string selectedType = AllTypes;
    [ObservableProperty] private string summary = "";

    public HistoryViewModel(ITransactionRepository txns, ICustomerRepository customers)
    { _txns = txns; _customers = customers; }

    partial void OnSelectedRangeChanged(string value) { IsCustom = DateRangeHelper.IsCustom(value); Refresh(); }
    partial void OnCustomFromChanged(DateTime value) { if (IsCustom) Refresh(); }
    partial void OnCustomToChanged(DateTime value) { if (IsCustom) Refresh(); }
    partial void OnSelectedCustomerChanged(string value) => Refresh();
    partial void OnSelectedTypeChanged(string value) => Refresh();

    private void Refresh() { if (_ready) _ = LoadAsync(); }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = Rows.Count == 0;
        Error = null;
        try { await LoadRowsAsync(); }
        catch (Exception ex) { Error = ex.Message; }
        finally { IsBusy = false; }
    }

    private async Task LoadRowsAsync()
    {
        if (CustomerNames.Count == 0)
        {
            var all = await _customers.GetAllAsync();
            _customerIds = all.ToDictionary(c => c.Name + $" #{c.Id}", c => c.Id);
            CustomerNames.Add(AllCustomers);
            foreach (var k in _customerIds.Keys) CustomerNames.Add(k);
        }
        _ready = true;

        var (from, to) = DateRangeHelper.Resolve(SelectedRange, CustomFrom, CustomTo);
        int? cid = _customerIds.TryGetValue(SelectedCustomer, out var id) ? id : null;
        TransactionType? type = TypeNames.IndexOf(SelectedType) is > 0 and var i ? (TransactionType)(i - 1) : null;

        var list = await _txns.GetAsync(cid, type, from, to);
        var names = _customerIds.ToDictionary(kv => kv.Value, kv => kv.Key[..kv.Key.LastIndexOf(" #")]);
        Rows.Clear();
        foreach (var t in list)
            Rows.Add(new TransactionRow { Transaction = t,
                CustomerName = t.CustomerId is int c && names.TryGetValue(c, out var n) ? n : "" });

        var sales = list.Where(t => t.Type is TransactionType.CreditSale or TransactionType.CashSale).Sum(t => t.TotalAmount);
        var received = list.Where(t => t.Type == TransactionType.Payment).Sum(t => t.PaidAmount);
        Summary = Loc.F("H_Summary", list.Count, sales, received);
    }

    [RelayCommand]
    private async Task ShowDetailsAsync(TransactionRow row)
    {
        var t = row.Transaction;
        var items = await _txns.GetItemsAsync(t.Id);
        var lines = items.Select(i => $"{i.ProductName}  {i.Quantity:0.##} x {i.UnitPrice:N0} = {i.LineTotal:N0}").ToList();
        var body = (lines.Count > 0 ? string.Join("\n", lines) + "\n\n" : "") +
                   Loc.F("H_TotalPaid", t.TotalAmount, t.PaidAmount) +
                   (t.CustomerId is null ? "" : Loc.F("H_BalAfter", t.BalanceAfter)) +
                   (string.IsNullOrWhiteSpace(t.Note) ? "" : Loc.F("H_Note", t.Note));
        await Shell.Current.DisplayAlertAsync(row.Title, body, Loc.T("OK"));
    }
}
