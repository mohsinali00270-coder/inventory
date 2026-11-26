using CommunityToolkit.Mvvm.ComponentModel;
using ShopLedger.Models;
using ShopLedger.Services;

namespace ShopLedger.ViewModels;

public partial class CartLine : ObservableObject
{
    public Product Product { get; }
    public string Name => Product.Name;
    public string Unit => Product.Unit;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(LineTotal))] private decimal quantity = 1;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LineTotal))] private decimal unitPrice;

    public decimal LineTotal => LedgerCalculator.LineTotal(Quantity, UnitPrice);

    public CartLine(Product p, decimal unitPrice) { Product = p; this.unitPrice = unitPrice; }
}
