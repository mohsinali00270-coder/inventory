using ShopLedger.Services;
using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class StatementPage : ContentPage
{
    private readonly StatementViewModel _vm;
    private readonly ITtsService _tts;
    private readonly ISettingsService _settings;

    public StatementPage(StatementViewModel vm, ITtsService tts, ISettingsService settings)
    { InitializeComponent(); BindingContext = _vm = vm; _tts = tts; _settings = settings; }

    protected override void OnDisappearing() { base.OnDisappearing(); _tts.Stop(); }

    private async void OnSpeakClicked(object? s, EventArgs e)
    {
        var lang = _settings.Language;
        if (_vm.Statement is null) { await _tts.SpeakAsync(new[] { SpeechTextBuilder.Empty(lang) }, lang); return; }
        await _tts.SpeakAsync(SpeechTextBuilder.Statement(_vm.Statement, lang), lang);
    }
    private void OnStopClicked(object? s, EventArgs e) => _tts.Stop();
}
