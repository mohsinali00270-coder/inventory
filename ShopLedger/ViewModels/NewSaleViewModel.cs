using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Services;

namespace ShopLedger.ViewModels;

/// <summary>Used for credit sale, cash sale and stock purchase (mode=purchase).</summary>
[QueryProperty(nameof(ModeParam), "mode")]
[QueryProperty(nameof(CustomerIdParam), "customerId")]
[QueryProperty(nameof(TypeParam), "type")]
public partial class NewSaleViewModel : ObservableObject
{
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;
    private readonly ILedgerService _ledger;
    private readonly IVoiceAssistant _voice;
    private List<Product> _allProducts = new();
    private bool _presetApplied;
    private static readonly Customer WalkIn = new() { Id = 0, Name = "Walk-in (no customer)" };

    public ObservableCollection<CartLine> Cart { get; } = new();
    public ObservableCollection<Product> SearchResults { get; } = new();
    public ObservableCollection<Customer> Customers { get; } = new();

    [ObservableProperty] private string? modeParam;
    [ObservableProperty] private string? typeParam;
    [ObservableProperty] private string? customerIdParam;

    [ObservableProperty] private string title = Loc.T("Title_NewSale");
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsSale)), NotifyPropertyChangedFor(nameof(ShowPaid))] private bool isPurchase;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowPaid)), NotifyPropertyChangedFor(nameof(ShowBalanceInfo))] private bool isCreditSale = true;
    [ObservableProperty] private Customer? selectedCustomer = WalkIn;
    [ObservableProperty] private string? productSearch;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private decimal paidAmount;
    [ObservableProperty] private decimal dueAmount;
    [ObservableProperty] private decimal balanceAfter;
    [ObservableProperty] private string? note;

    public bool IsSale => !IsPurchase;
    public bool ShowPaid => IsSale && IsCreditSale;
    public bool ShowBalanceInfo => IsSale && IsCreditSale;
    public bool HasSearchResults => SearchResults.Count > 0;

    public NewSaleViewModel(IProductRepository products, ICustomerRepository customers, ILedgerService ledger, IVoiceAssistant voice)
    { _products = products; _customers = customers; _ledger = ledger; _voice = voice; }

    partial void OnModeParamChanged(string? value)
    {
        IsPurchase = value == "purchase";
        Title = IsPurchase ? Loc.T("Title_Purchase") : Loc.T("Title_NewSale");
    }
    partial void OnTypeParamChanged(string? value) { if (value == "cash") IsCreditSale = false; else if (value == "credit") IsCreditSale = true; }
    partial void OnProductSearchChanged(string? value) => FilterProducts();
    partial void OnPaidAmountChanged(decimal value) => Recalculate();
    partial void OnSelectedCustomerChanged(Customer? value) => Recalculate();
    partial void OnIsCreditSaleChanged(bool value) => Recalculate();

    [RelayCommand]
    public async Task LoadAsync()
    {
        _allProducts = await _products.GetAllAsync();
        if (Customers.Count == 0)
        {
            Customers.Add(WalkIn);
            foreach (var c in await _customers.GetAllAsync()) Customers.Add(c);
        }
        if (!_presetApplied)
        {
            _presetApplied = true;
            if (int.TryParse(CustomerIdParam, out var id))
                SelectedCustomer = Customers.FirstOrDefault(c => c.Id == id) ?? WalkIn;
        }
        FilterProducts();
    }

    private void FilterProducts()
    {
        SearchResults.Clear();
        if (!string.IsNullOrWhiteSpace(ProductSearch))
            foreach (var p in _allProducts
                .Where(p => p.Name.Contains(ProductSearch, StringComparison.OrdinalIgnoreCase) || (p.Barcode ?? "") == ProductSearch)
                .Take(6))
                SearchResults.Add(p);
        OnPropertyChanged(nameof(HasSearchResults));
    }

    [RelayCommand]
    private void AddProduct(Product p)
    {
        var existing = Cart.FirstOrDefault(l => l.Product.Id == p.Id);
        if (existing is not null) existing.Quantity += 1;
        else
        {
            var line = new CartLine(p, IsPurchase ? p.PurchasePrice : p.SalePrice);
            line.PropertyChanged += (_, _) => Recalculate();
            Cart.Add(line);
        }
        ProductSearch = null;
        Recalculate();
    }

    [RelayCommand]
    private void RemoveLine(CartLine line) { Cart.Remove(line); Recalculate(); }

    private void Recalculate()
    {
        Total = Cart.Sum(l => l.LineTotal);
        DueAmount = IsCreditSale ? Math.Max(0, Total - PaidAmount) : 0;
        var current = SelectedCustomer is { Id: > 0 } c ? c.CurrentBalance : 0;
        BalanceAfter = current + (IsCreditSale ? Total - PaidAmount : 0);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Cart.Count == 0) { await Alert(Loc.T("Err_AddProduct")); return; }
        if (!IsPurchase && IsCreditSale && SelectedCustomer is not { Id: > 0 })
        { await Alert(Loc.T("Err_CreditNeedsCustomer")); return; }
        if (!IsPurchase && IsCreditSale && (PaidAmount < 0 || PaidAmount > Total))
        { await Alert(Loc.T("Err_PaidRange")); return; }

        await SubmitAsync(false);
    }

    private async Task SubmitAsync(bool allowOverLimit)
    {
        var items = Cart.Select(l => new LineRequest(l.Product.Id, l.Quantity, l.UnitPrice)).ToList();
        try
        {
            if (IsPurchase) await _ledger.RecordPurchaseAsync(items, Note);
            else
            {
                await _ledger.RecordSaleAsync(new SaleRequest
                {
                    Type = IsCreditSale ? TransactionType.CreditSale : TransactionType.CashSale,
                    CustomerId = SelectedCustomer is { Id: > 0 } c ? c.Id : null,
                    Items = items, PaidAmount = PaidAmount, Note = Note, AllowOverLimit = allowOverLimit
                });
            }
            await Shell.Current.GoToAsync("..");
        }
        catch (CreditLimitExceededException ex)
        {
            var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_CreditLimit"), ex.Message + "\n\n" + Loc.T("Q_SaveAnyway"), Loc.T("SaveAnyway"), Loc.T("Cancel"));
            if (ok) await SubmitAsync(true);
        }
        catch (Exception ex) { await Alert(ex.Message); }
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(MicText))] private bool isListening;
    [ObservableProperty] private string? voiceStatus;
    public string MicText => IsListening ? Loc.T("Mic_Stop") : Loc.T("Mic_Voice");

    [RelayCommand]
    private async Task VoiceAsync()
    {
        if (IsListening) { _voice.StopListening(); return; }
        IsListening = true; VoiceStatus = Loc.T("Voice_Listening");
        try
        {
            var text = await _voice.ListenAsync(new Progress<string>(t => VoiceStatus = t));
            if (string.IsNullOrWhiteSpace(text)) { VoiceStatus = Loc.T("Voice_NothingHeard"); return; }
            VoiceStatus = text;
            var cmd = VoiceCommandParser.Parse(text, IsCreditSale ? VoiceIntent.CreditSale : VoiceIntent.CashSale);
            if (!IsPurchase)
            {
                if (!string.IsNullOrWhiteSpace(cmd.CustomerName))
                {
                    var cust = FuzzyMatcher.Best(Customers.Where(c => c.Id > 0), c => c.Name, cmd.CustomerName!);
                    if (cust is null) await Alert(Loc.F("V_CustNotFound", cmd.CustomerName));
                    else SelectedCustomer = cust;
                }
                IsCreditSale = cmd.Intent == VoiceIntent.CreditSale;
            }
            if (!string.IsNullOrWhiteSpace(cmd.ProductName))
            {
                var prod = FuzzyMatcher.Best(_allProducts, x => x.Name, cmd.ProductName!);
                if (prod is null) await Alert(Loc.F("V_ProdNotFound", cmd.ProductName));
                else
                {
                    AddProduct(prod);
                    var line = Cart.First(l => l.Product.Id == prod.Id);
                    if (cmd.Quantity is decimal q) line.Quantity = q;
                    if (cmd.UnitPrice is decimal pr) line.UnitPrice = pr;
                }
            }
        }
        finally { IsListening = false; }
    }

    private static Task Alert(string msg) => Shell.Current.DisplayAlertAsync(Loc.T("Error"), msg, Loc.T("OK"));
}
