using ShopLedger.Services;
using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class VoiceEntryPage : ContentPage
{
    private readonly ITtsService _tts;
    public VoiceEntryPage(VoiceEntryViewModel vm, ITtsService tts) { InitializeComponent(); BindingContext = vm; _tts = tts; }
    protected override void OnDisappearing() { base.OnDisappearing(); _tts.Stop(); }
}
