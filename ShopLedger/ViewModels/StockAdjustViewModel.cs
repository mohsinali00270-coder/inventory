using ShopLedger.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;

namespace ShopLedger.ViewModels;

[QueryProperty(nameof(IdParam), "id")]
public partial class StockAdjustViewModel : ObservableObject
{
    private readonly IProductRepository _products;
    private readonly IStockRepository _stock;
    private int _productId;

    public List<string> Reasons { get; } = new() { Loc.T("Reason_Add"), Loc.T("Reason_Damage"), Loc.T("Reason_Loss"), Loc.T("Reason_CorrPlus"), Loc.T("Reason_CorrMinus") };
    public ObservableCollection<StockMovement> Movements { get; } = new();

    [ObservableProperty] private string? idParam;
    [ObservableProperty] private string productName = "";
    [ObservableProperty] private string unit = "";
    [ObservableProperty] private decimal currentStock;
    [ObservableProperty] private string selectedReason = Loc.T("Reason_Add");
    [ObservableProperty] private decimal quantity;
    [ObservableProperty] private string? note;

    public StockAdjustViewModel(IProductRepository products, IStockRepository stock)
    { _products = products; _stock = stock; }

    async partial void OnIdParamChanged(string? value)
    {
        if (!int.TryParse(value, out _productId)) return;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var p = await _products.GetAsync(_productId);
        if (p is null) return;
        ProductName = p.Name; Unit = p.Unit; CurrentStock = p.Stock;
        Movements.Clear();
        foreach (var m in await _stock.GetMovementsAsync(_productId)) Movements.Add(m);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Quantity <= 0) { await Shell.Current.DisplayAlertAsync(Loc.T("Error"), Loc.T("Err_QtyZero"), Loc.T("OK")); return; }

        var idx = Reasons.IndexOf(SelectedReason);
        var (sign, reason) = idx switch
        {
            1 => (-1, StockReason.Damage),
            2 => (-1, StockReason.Loss),
            3 => (1, StockReason.Correction),
            4 => (-1, StockReason.Correction),
            _ => (1, StockReason.Purchase)
        };

        try
        {
            await _stock.AdjustAsync(_productId, sign * Quantity, reason, Note);
            Quantity = 0; Note = null;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(Loc.T("Error"), ex.Message, Loc.T("OK"));
        }
    }
}
