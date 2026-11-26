using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Views;

namespace ShopLedger.ViewModels;

[QueryProperty(nameof(IdParam), "id")]
public partial class CustomerDetailViewModel : ObservableObject
{
    private readonly ICustomerRepository _repo;
    private readonly ITransactionRepository _txns;

    public ObservableCollection<LedgerTransaction> Transactions { get; } = new();

    [ObservableProperty] private string? idParam;
    [ObservableProperty] private Customer? customer;

    public CustomerDetailViewModel(ICustomerRepository repo, ITransactionRepository txns)
    { _repo = repo; _txns = txns; }

    partial void OnIdParamChanged(string? value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!int.TryParse(IdParam, out var id)) return;
        Customer = await _repo.GetAsync(id);
        Transactions.Clear();
        foreach (var t in await _txns.GetAsync(id)) Transactions.Add(t);
    }

    [RelayCommand]
    private Task NewCreditSaleAsync() =>
        Customer is null ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{nameof(NewSalePage)}?mode=sale&type=credit&customerId={Customer.Id}");

    [RelayCommand]
    private Task ReceivePaymentAsync() =>
        Customer is null ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{nameof(LedgerEntryPage)}?mode=payment&customerId={Customer.Id}");

    [RelayCommand]
    private Task OpenStatementAsync() =>
        Customer is null ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{nameof(StatementPage)}?customerId={Customer.Id}");

    [RelayCommand]
    private Task EditAsync() =>
        Customer is null ? Task.CompletedTask
                         : Shell.Current.GoToAsync($"{nameof(CustomerEditPage)}?id={Customer.Id}");
}
