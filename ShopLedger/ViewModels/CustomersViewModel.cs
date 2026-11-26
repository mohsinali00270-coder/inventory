using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Views;

namespace ShopLedger.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly ICustomerRepository _repo;
    public ObservableCollection<Customer> Customers { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private decimal totalReceivable;

    public CustomersViewModel(ICustomerRepository repo) => _repo = repo;

    partial void OnSearchTextChanged(string? value) => _ = LoadAsync();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = Customers.Count == 0;
        Error = null;
        try { await LoadCustomersAsync(); }
        catch (Exception ex) { Error = ex.Message; }
        finally { IsBusy = false; }
    }

    private async Task LoadCustomersAsync()
    {
        var list = await _repo.GetAllAsync(SearchText);
        Customers.Clear();
        foreach (var c in list) Customers.Add(c);
        TotalReceivable = await _repo.GetTotalReceivableAsync();
    }

    [RelayCommand] private Task AddAsync() => Shell.Current.GoToAsync(nameof(CustomerEditPage));
    [RelayCommand] private Task OpenAsync(Customer c) => Shell.Current.GoToAsync($"{nameof(CustomerDetailPage)}?id={c.Id}");
}
