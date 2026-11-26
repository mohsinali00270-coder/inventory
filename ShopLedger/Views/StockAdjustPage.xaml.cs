using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class StockAdjustPage : ContentPage
{
    public StockAdjustPage(StockAdjustViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
