using ShopLedger.ViewModels;

namespace ShopLedger.Views;

public partial class CustomerEditPage : ContentPage
{
    public CustomerEditPage(CustomerEditViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
