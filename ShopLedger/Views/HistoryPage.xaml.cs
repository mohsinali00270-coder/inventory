using ShopLedger.Services;
using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _vm;
    private readonly ITtsService _tts;
    private readonly ISettingsService _settings;

    public HistoryPage(HistoryViewModel vm, ITtsService tts, ISettingsService settings)
    { InitializeComponent(); BindingContext = _vm = vm; _tts = tts; _settings = settings; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }
    protected override void OnDisappearing() { base.OnDisappearing(); _tts.Stop(); }

    private async void OnSpeakClicked(object? s, EventArgs e)
    {
        var lang = _settings.Language;
        var parts = _vm.Rows.Take(30).Select(r => SpeechTextBuilder.Transaction(r, lang)).ToList();
        if (parts.Count == 0) parts.Add(SpeechTextBuilder.Empty(lang));
        await _tts.SpeakAsync(parts, lang);
    }
    private void OnStopClicked(object? s, EventArgs e) => _tts.Stop();
}
