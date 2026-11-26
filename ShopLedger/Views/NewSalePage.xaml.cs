using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class NewSalePage : ContentPage
{
    private readonly NewSaleViewModel _vm;
    public NewSalePage(NewSaleViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }
}
