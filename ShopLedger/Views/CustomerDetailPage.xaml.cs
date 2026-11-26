using ShopLedger.Models;
using ShopLedger.Services;
using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class CustomerDetailPage : ContentPage
{
    private readonly CustomerDetailViewModel _vm;
    private readonly ITtsService _tts;
    private readonly ISettingsService _settings;

    public CustomerDetailPage(CustomerDetailViewModel vm, ITtsService tts, ISettingsService settings)
    { InitializeComponent(); BindingContext = _vm = vm; _tts = tts; _settings = settings; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }
    protected override void OnDisappearing() { base.OnDisappearing(); _tts.Stop(); }

    private async void OnSpeakClicked(object? s, EventArgs e)
    {
        if (_vm.Customer is null) return;
        var lang = _settings.Language;
        var lastPayment = _vm.Transactions.FirstOrDefault(t => t.Type == TransactionType.Payment);
        var parts = new List<string> { SpeechTextBuilder.Customer(_vm.Customer, lastPayment, lang) };
        parts.AddRange(_vm.Transactions.Take(8).Select(t =>
            SpeechTextBuilder.Transaction(new TransactionRow { Transaction = t, CustomerName = "" }, lang)));
        await _tts.SpeakAsync(parts, lang);
    }
    private void OnStopClicked(object? s, EventArgs e) => _tts.Stop();
}
