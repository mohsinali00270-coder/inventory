using ShopLedger.Services;
using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class SalesPage : ContentPage
{
    private readonly SalesViewModel _vm;
    private readonly ITtsService _tts;
    private readonly ISettingsService _settings;

    public SalesPage(SalesViewModel vm, ITtsService tts, ISettingsService settings)
    { InitializeComponent(); this.AddLanguageToggle(); BindingContext = _vm = vm; _tts = tts; _settings = settings; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }
    protected override void OnDisappearing() { base.OnDisappearing(); _tts.Stop(); }

    private async void OnHistoryClicked(object? s, EventArgs e) => await Shell.Current.GoToAsync(nameof(HistoryPage));

    private async void OnSpeakClicked(object? s, EventArgs e)
    {
        var lang = _settings.Language;
        var parts = _vm.Recent.Take(15).Select(r => SpeechTextBuilder.Transaction(r, lang)).ToList();
        if (parts.Count == 0) parts.Add(SpeechTextBuilder.Empty(lang));
        await _tts.SpeakAsync(parts, lang);
    }
    private void OnStopClicked(object? s, EventArgs e) => _tts.Stop();
}
