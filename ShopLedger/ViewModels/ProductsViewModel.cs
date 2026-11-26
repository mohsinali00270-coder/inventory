using ShopLedger.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Views;

namespace ShopLedger.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private static string AllCategories => Loc.T("All");
    private readonly IProductRepository _repo;
    private bool _loading;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string selectedCategory = AllCategories;
    [ObservableProperty] private int lowStockCount;

    public ProductsViewModel(IProductRepository repo) => _repo = repo;

    partial void OnSearchTextChanged(string? value) => _ = LoadAsync();
    partial void OnSelectedCategoryChanged(string value) { if (!_loading) _ = LoadAsync(); }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = Products.Count == 0;   // spinner only while the list is still empty
        Error = null;
        try { await LoadProductsAsync(); }
        catch (Exception ex) { Error = ex.Message; }
        finally { IsBusy = false; }
    }

    private async Task LoadProductsAsync()
    {
        _loading = true;
        try
        {
            var cat = SelectedCategory == AllCategories ? null : SelectedCategory;
            var items = await _repo.GetAllAsync(SearchText, cat);
            Products.Clear();
            foreach (var p in items) Products.Add(p);

            var cats = await _repo.GetCategoriesAsync();
            var keep = SelectedCategory;
            Categories.Clear();
            Categories.Add(AllCategories);
            foreach (var c in cats) Categories.Add(c);
            SelectedCategory = Categories.Contains(keep) ? keep : AllCategories;

            LowStockCount = (await _repo.GetLowStockAsync()).Count;
        }
        finally { _loading = false; }
    }

    [RelayCommand] private Task AddAsync() => Shell.Current.GoToAsync(nameof(ProductEditPage));
    [RelayCommand] private Task EditAsync(Product p) => Shell.Current.GoToAsync($"{nameof(ProductEditPage)}?id={p.Id}");
    [RelayCommand] private Task AdjustAsync(Product p) => Shell.Current.GoToAsync($"{nameof(StockAdjustPage)}?id={p.Id}");
}
