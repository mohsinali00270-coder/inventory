using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Models;
using ShopLedger.Services;
using ShopLedger.Views;

namespace ShopLedger.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IReportService _reports;
    public ObservableCollection<Product> LowStock { get; } = new();
    public ObservableCollection<Customer> TopCustomers { get; } = new();

    [ObservableProperty] private decimal todaySales;
    [ObservableProperty] private decimal todayCash;
    [ObservableProperty] private decimal totalReceivable;
    [ObservableProperty] private int lowStockCount;

    // Inventory KPIs
    [ObservableProperty] private int totalProducts;
    [ObservableProperty] private int categoryCount;
    [ObservableProperty] private decimal stockValue;
    [ObservableProperty] private int outOfStockCount;
    [ObservableProperty] private int overLimitCount;
    [ObservableProperty] private int alertCount;
    [ObservableProperty] private string alertsCaption = "";

    // Page state
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public string Today => DateTime.Today.ToString("dddd, d MMMM yyyy", Loc.Culture);

    public DashboardViewModel(IReportService reports) => _reports = reports;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = TotalProducts == 0 && TopCustomers.Count == 0;   // spinner only on the first (empty) load
        Error = null;
        try
        {
            var d = await _reports.GetDashboardAsync();
            TodaySales = d.TodaySales; TodayCash = d.TodayCashReceived; TotalReceivable = d.TotalReceivable;
            LowStockCount = d.LowStock.Count;
            TotalProducts = d.TotalProducts;
            CategoryCount = d.CategoryCount;
            StockValue = d.StockValue;
            OutOfStockCount = d.OutOfStockCount;
            OverLimitCount = d.OverLimitCount;
            AlertCount = d.OutOfStockCount + d.OverLimitCount;
            AlertsCaption = Loc.F("UI_AlertsDetail", d.OutOfStockCount, d.OverLimitCount);
            LowStock.Clear(); foreach (var p in d.LowStock.OrderBy(p => p.Stock).Take(8)) LowStock.Add(p);
            TopCustomers.Clear(); foreach (var c in d.TopCustomers) TopCustomers.Add(c);
        }
        catch (Exception ex) { Error = ex.Message; }
        finally { IsBusy = false; }
    }

    // Quick actions: same routes the Sales tab uses
    [RelayCommand] private Task NewSaleAsync() => Shell.Current.GoToAsync($"{nameof(NewSalePage)}?mode=sale");
    [RelayCommand] private Task ReceivePaymentAsync() => Shell.Current.GoToAsync($"{nameof(LedgerEntryPage)}?mode=payment");
    [RelayCommand] private Task NewPurchaseAsync() => Shell.Current.GoToAsync($"{nameof(NewSalePage)}?mode=purchase");
    [RelayCommand] private Task VoiceEntryAsync() => Shell.Current.GoToAsync(nameof(VoiceEntryPage));
    [RelayCommand] private Task OpenProductsAsync() => Shell.Current.GoToAsync("//products");
    [RelayCommand] private Task OpenCustomersAsync() => Shell.Current.GoToAsync("//customers");
}
