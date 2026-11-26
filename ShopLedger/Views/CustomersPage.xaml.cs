using ShopLedger.Services;
using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class CustomersPage : ContentPage
{
    private readonly CustomersViewModel _vm;
    private readonly ITtsService _tts;
    private readonly ISettingsService _settings;

    public CustomersPage(CustomersViewModel vm, ITtsService tts, ISettingsService settings)
    { InitializeComponent(); this.AddLanguageToggle(); BindingContext = _vm = vm; _tts = tts; _settings = settings; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }
    protected override void OnDisappearing() { base.OnDisappearing(); _tts.Stop(); }

    private async void OnSpeakClicked(object? s, EventArgs e)
    {
        var lang = _settings.Language;
        var parts = new List<string>();
        parts.Add(lang == "ur" ? $"کل بقایا {_vm.TotalReceivable:N0} روپے۔" : $"Total receivable {_vm.TotalReceivable:N0} rupees.");
        parts.AddRange(_vm.Customers.Take(50).Select(c => SpeechTextBuilder.Customer(c, null, lang)));
        if (_vm.Customers.Count == 0) parts.Add(SpeechTextBuilder.Empty(lang));
        await _tts.SpeakAsync(parts, lang);
    }
    private void OnStopClicked(object? s, EventArgs e) => _tts.Stop();
}
