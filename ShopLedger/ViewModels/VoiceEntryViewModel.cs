using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Services;

namespace ShopLedger.ViewModels;

/// <summary>Speak or type a command -> understand -> preview -> confirm -> save -> read back.</summary>
public partial class VoiceEntryViewModel : ObservableObject
{
    private readonly IVoiceAssistant _voice;
    private readonly ITtsService _tts;
    private readonly ISettingsService _settings;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly ILedgerService _ledger;

    private class Pending
    {
        public VoiceIntent Intent;
        public Customer? Customer; public Product? Product;
        public decimal Qty, Price, Amount, PurchasePrice, CreditLimit;
        public string? Name, Phone, Unit, Category;
    }
    private Pending? _pending;

    [ObservableProperty] private string transcript = "";
    [ObservableProperty] private string? voiceStatus;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(MicText))] private bool isListening;
    [ObservableProperty] private string preview = "";
    [ObservableProperty] private string warning = "";
    [ObservableProperty] private bool canConfirm;

    public string MicText => IsListening ? Loc.T("Mic_Stop") : Loc.T("Mic_Tap");
    public string Hints => Loc.T("V_Hints");

    public VoiceEntryViewModel(IVoiceAssistant voice, ITtsService tts, ISettingsService settings,
        ICustomerRepository customers, IProductRepository products, ILedgerService ledger)
    { _voice = voice; _tts = tts; _settings = settings; _customers = customers; _products = products; _ledger = ledger; }

    [RelayCommand]
    private async Task ListenAsync()
    {
        if (IsListening) { _voice.StopListening(); return; }
        IsListening = true; VoiceStatus = Loc.T("Voice_Listening");
        try
        {
            var text = await _voice.ListenAsync(new Progress<string>(t => Transcript = t));
            if (string.IsNullOrWhiteSpace(text)) { VoiceStatus = Loc.T("Voice_NothingHeard"); return; }
            Transcript = text; VoiceStatus = null;
        }
        finally { IsListening = false; }
        await UnderstandAsync();
    }

    [RelayCommand]
    private async Task UnderstandAsync()
    {
        _pending = null; Preview = ""; Warning = ""; CanConfirm = false;
        if (string.IsNullOrWhiteSpace(Transcript)) return;

        var lang = _settings.Language;
        var cmd = VoiceCommandParser.Parse(Transcript);

        switch (cmd.Intent)
        {
            case VoiceIntent.CreditSale:
            case VoiceIntent.CashSale:
            {
                var credit = cmd.Intent == VoiceIntent.CreditSale;
                if (string.IsNullOrWhiteSpace(cmd.ProductName)) { Warning = Loc.T("V_NoProduct"); return; }
                var product = FuzzyMatcher.Best(await _products.GetAllAsync(), p => p.Name, cmd.ProductName!);
                if (product is null) { Warning = Loc.F("V_ProdNotFoundAdd", cmd.ProductName); return; }

                Customer? customer = null;
                if (!string.IsNullOrWhiteSpace(cmd.CustomerName))
                    customer = FuzzyMatcher.Best(await _customers.GetAllAsync(), c => c.Name, cmd.CustomerName!);
                if (credit && customer is null)
                { Warning = Loc.F("V_CustNotFoundCredit", cmd.CustomerName); return; }

                var qty = cmd.Quantity ?? 1;
                var price = cmd.UnitPrice ?? product.SalePrice;
                _pending = new Pending { Intent = cmd.Intent, Customer = customer, Product = product, Qty = qty, Price = price };
                Preview = SpeechTextBuilder.SaleSummary(credit, customer?.Name, product.Name, qty, product.Unit, price,
                                                        LedgerCalculator.LineTotal(qty, price), lang);
                break;
            }
            case VoiceIntent.Payment:
            {
                if (cmd.Amount is not decimal amount || amount <= 0) { Warning = Loc.T("V_NoAmount"); return; }
                if (string.IsNullOrWhiteSpace(cmd.CustomerName)) { Warning = Loc.T("V_NoCustomer"); return; }
                var customer = FuzzyMatcher.Best(await _customers.GetAllAsync(), c => c.Name, cmd.CustomerName!);
                if (customer is null) { Warning = Loc.F("V_CustNotFound", cmd.CustomerName); return; }
                _pending = new Pending { Intent = cmd.Intent, Customer = customer, Amount = amount };
                Preview = SpeechTextBuilder.PaymentSummary(customer.Name, amount, lang);
                break;
            }
            case VoiceIntent.AddCustomer:
            {
                if (string.IsNullOrWhiteSpace(cmd.CustomerName)) { Warning = Loc.T("V_NoCustomer"); return; }
                var all = await _customers.GetAllAsync();
                if (all.Any(c => string.Equals(c.Name, cmd.CustomerName, StringComparison.OrdinalIgnoreCase)))
                { Warning = Loc.F("V_CustExists", cmd.CustomerName); return; }
                _pending = new Pending { Intent = cmd.Intent, Name = cmd.CustomerName, Phone = cmd.Phone,
                                         CreditLimit = cmd.CreditLimit ?? 0, Amount = cmd.Amount ?? 0 };
                Preview = SpeechTextBuilder.NewCustomerSummary(cmd.CustomerName!, cmd.Phone, cmd.CreditLimit, lang);
                break;
            }
            case VoiceIntent.AddProduct:
            {
                if (string.IsNullOrWhiteSpace(cmd.ProductName)) { Warning = Loc.T("V_NoProduct"); return; }
                var all = await _products.GetAllAsync();
                if (all.Any(p => string.Equals(p.Name, cmd.ProductName, StringComparison.OrdinalIgnoreCase)))
                { Warning = Loc.F("V_ProdExists", cmd.ProductName); return; }
                _pending = new Pending { Intent = cmd.Intent, Name = cmd.ProductName, Price = cmd.UnitPrice ?? 0,
                                         PurchasePrice = cmd.PurchasePrice ?? 0, Qty = cmd.Quantity ?? 0,
                                         Unit = cmd.Unit ?? "pcs", Category = cmd.Category ?? "General" };
                Preview = SpeechTextBuilder.NewProductSummary(cmd.ProductName!, cmd.UnitPrice ?? 0, cmd.Quantity ?? 0, cmd.Unit ?? "pcs", lang);
                break;
            }
            default:
                Warning = Loc.T("V_NotUnderstood") + " " + Hints;
                return;
        }
        CanConfirm = true;
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (_pending is null) return;
        await ExecuteAsync(false);
    }

    private async Task ExecuteAsync(bool allowOverLimit)
    {
        var p = _pending!;
        var lang = _settings.Language;
        string? who = null; decimal? balance = null;
        try
        {
            switch (p.Intent)
            {
                case VoiceIntent.CreditSale:
                case VoiceIntent.CashSale:
                {
                    var t = await _ledger.RecordSaleAsync(new SaleRequest
                    {
                        Type = p.Intent == VoiceIntent.CreditSale ? TransactionType.CreditSale : TransactionType.CashSale,
                        CustomerId = p.Customer?.Id,
                        Items = { new LineRequest(p.Product!.Id, p.Qty, p.Price) },
                        PaidAmount = 0, Note = "Voice entry", AllowOverLimit = allowOverLimit
                    });
                    if (p.Intent == VoiceIntent.CreditSale) { who = p.Customer!.Name; balance = t.BalanceAfter; }
                    break;
                }
                case VoiceIntent.Payment:
                {
                    var t = await _ledger.RecordPaymentAsync(p.Customer!.Id, p.Amount, "Voice entry");
                    who = p.Customer.Name; balance = t.BalanceAfter;
                    break;
                }
                case VoiceIntent.AddCustomer:
                {
                    var c = new Customer { Name = p.Name!, Phone = p.Phone, CreditLimit = p.CreditLimit };
                    await _customers.SaveAsync(c);
                    if (p.Amount > 0) await _ledger.RecordAdjustmentAsync(c.Id, p.Amount, "Opening balance");
                    break;
                }
                case VoiceIntent.AddProduct:
                    await _products.SaveAsync(new Product
                    {
                        Name = p.Name!, SalePrice = p.Price, PurchasePrice = p.PurchasePrice, Stock = p.Qty,
                        Unit = p.Unit ?? "pcs", Category = p.Category ?? "General"
                    });
                    break;
            }
        }
        catch (CreditLimitExceededException ex)
        {
            var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_CreditLimit"), ex.Message + "\n\n" + Loc.T("Q_SaveAnyway"), Loc.T("SaveAnyway"), Loc.T("Cancel"));
            if (ok) await ExecuteAsync(true);
            return;
        }
        catch (Exception ex) { Warning = ex.Message; return; }

        var message = SpeechTextBuilder.Saved(who, balance, lang);
        VoiceStatus = message;
        Transcript = ""; Preview = ""; Warning = ""; CanConfirm = false; _pending = null;
        _ = _tts.SpeakAsync(new[] { message }, lang);      // read the result back
    }

    [RelayCommand]
    private void Clear() { Transcript = ""; Preview = ""; Warning = ""; VoiceStatus = null; CanConfirm = false; _pending = null; _tts.Stop(); }
}
