using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class ProductEditPage : ContentPage
{
    public ProductEditPage(ProductEditViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
