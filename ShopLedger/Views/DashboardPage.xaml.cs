using ShopLedger.Services;
using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly ITtsService _tts;
    private readonly ISettingsService _settings;

    public DashboardPage(DashboardViewModel vm, ITtsService tts, ISettingsService settings)
    { InitializeComponent(); this.AddLanguageToggle(); BindingContext = _vm = vm; _tts = tts; _settings = settings; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }
    protected override void OnDisappearing() { base.OnDisappearing(); _tts.Stop(); }

    private async void OnSpeakClicked(object? s, EventArgs e)
    {
        var lang = _settings.Language;
        await _tts.SpeakAsync(SpeechTextBuilder.Dashboard(_vm.TodaySales, _vm.TodayCash, _vm.TotalReceivable, _vm.LowStockCount, lang), lang);
    }
    private void OnStopClicked(object? s, EventArgs e) => _tts.Stop();
}
