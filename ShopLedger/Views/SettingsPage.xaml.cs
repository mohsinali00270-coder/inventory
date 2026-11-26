using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm) { InitializeComponent(); this.AddLanguageToggle(); BindingContext = vm; }
}
