using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class LedgerEntryPage : ContentPage
{
    private readonly LedgerEntryViewModel _vm;
    public LedgerEntryPage(LedgerEntryViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }
}
