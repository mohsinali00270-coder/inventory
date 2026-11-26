using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Views;

namespace ShopLedger.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly ITransactionRepository _txns;
    private readonly ICustomerRepository _customers;

    public ObservableCollection<TransactionRow> Recent { get; } = new();

    public SalesViewModel(ITransactionRepository txns, ICustomerRepository customers)
    { _txns = txns; _customers = customers; }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = Recent.Count == 0;
        Error = null;
        try { await LoadRecentAsync(); }
        catch (Exception ex) { Error = ex.Message; }
        finally { IsBusy = false; }
    }

    private async Task LoadRecentAsync()
    {
        var names = (await _customers.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
        var list = (await _txns.GetAsync()).Take(50);
        Recent.Clear();
        foreach (var t in list)
            Recent.Add(new TransactionRow
            {
                Transaction = t,
                CustomerName = t.CustomerId is int id && names.TryGetValue(id, out var n) ? n : ""
            });
    }

    [RelayCommand] private Task VoiceEntryAsync() => Shell.Current.GoToAsync(nameof(VoiceEntryPage));
    [RelayCommand] private Task NewSaleAsync() => Shell.Current.GoToAsync($"{nameof(NewSalePage)}?mode=sale");
    [RelayCommand] private Task NewPurchaseAsync() => Shell.Current.GoToAsync($"{nameof(NewSalePage)}?mode=purchase");
    [RelayCommand] private Task ReceivePaymentAsync() => Shell.Current.GoToAsync($"{nameof(LedgerEntryPage)}?mode=payment");
    [RelayCommand] private Task ManualEntryAsync() => Shell.Current.GoToAsync($"{nameof(LedgerEntryPage)}?mode=adjust");
}
