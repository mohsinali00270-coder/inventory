using ShopLedger.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;

namespace ShopLedger.ViewModels;

[QueryProperty(nameof(IdParam), "id")]
public partial class ProductEditViewModel : ObservableObject
{
    private readonly IProductRepository _repo;
    private readonly IVoiceAssistant _voice;
    private Product _product = new();

    public List<string> Units { get; } = new() { "pcs", "kg", "gram", "litre", "dozen", "pack", "box" };

    [ObservableProperty] private string? idParam;
    [ObservableProperty] private string title = Loc.T("Title_NewProduct");
    [ObservableProperty] private bool isNew = true;

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string category = "General";
    [ObservableProperty] private string unit = "pcs";
    [ObservableProperty] private decimal purchasePrice;
    [ObservableProperty] private decimal salePrice;
    [ObservableProperty] private decimal stock;
    [ObservableProperty] private decimal lowStockLevel = 5;
    [ObservableProperty] private string? barcode;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasPhoto))] private string? photoPath;

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath);

    [RelayCommand] private Task PickPhotoAsync() => GetPhotoAsync(camera: false);
    [RelayCommand] private Task TakePhotoAsync() => GetPhotoAsync(camera: true);
    [RelayCommand] private void RemovePhoto() => PhotoPath = null;

    private async Task GetPhotoAsync(bool camera)
    {
        try
        {
            if (camera)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Photo"), Loc.T("Err_NoCamera"), Loc.T("OK")); return; }
                var st = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (st != PermissionStatus.Granted) st = await Permissions.RequestAsync<Permissions.Camera>();
                if (st != PermissionStatus.Granted)
                { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Photo"), Loc.T("Err_CameraDenied"), Loc.T("OK")); return; }
            }
            var file = camera
                ? await MediaPicker.Default.CapturePhotoAsync()
                : (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }))?.FirstOrDefault();
            if (file is null) return;

            // Copy into app storage so the picture survives even if the gallery copy is deleted.
            var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
            Directory.CreateDirectory(dir);
            var dest = Path.Combine(dir, $"p_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(file.FileName)}");
            await using (var src = await file.OpenReadAsync())
            await using (var dst = File.Create(dest))
                await src.CopyToAsync(dst);
            PhotoPath = dest;
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Photo"), ex.Message, Loc.T("OK")); }
    }

    public bool IsExisting => !IsNew;

    public ProductEditViewModel(IProductRepository repo, IVoiceAssistant voice) { _repo = repo; _voice = voice; }

    async partial void OnIdParamChanged(string? value)
    {
        if (!int.TryParse(value, out var id)) return;
        var p = await _repo.GetAsync(id);
        if (p is null) return;
        _product = p;
        IsNew = false; OnPropertyChanged(nameof(IsExisting));
        Title = Loc.T("Title_EditProduct");
        Name = p.Name; Category = p.Category; Unit = p.Unit;
        PurchasePrice = p.PurchasePrice; SalePrice = p.SalePrice;
        Stock = p.Stock; LowStockLevel = p.LowStockLevel; Barcode = p.Barcode; PhotoPath = p.PhotoPath;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) { await Alert(Loc.T("Err_ProductName")); return; }
        if (SalePrice < 0 || PurchasePrice < 0 || Stock < 0) { await Alert(Loc.T("Err_PricesNeg")); return; }
        if (SalePrice < PurchasePrice)
        {
            var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_CheckPrice"),
                Loc.T("Q_PriceLow"), Loc.T("Save"), Loc.T("Cancel"));
            if (!ok) return;
        }

        _product.Name = Name.Trim();
        _product.Category = string.IsNullOrWhiteSpace(Category) ? "General" : Category.Trim();
        _product.Unit = Unit;
        _product.PurchasePrice = PurchasePrice;
        _product.SalePrice = SalePrice;
        _product.LowStockLevel = LowStockLevel;
        _product.Barcode = string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim();
        _product.PhotoPath = PhotoPath;
        if (IsNew) _product.Stock = Stock;   // existing stock changes only via Stock Adjust

        await _repo.SaveAsync(_product);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IsNew) return;
        var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_DeleteProduct"), Loc.F("Q_DeleteProduct", _product.Name), Loc.T("Delete"), Loc.T("Cancel"));
        if (!ok) return;
        await _repo.DeleteAsync(_product);
        await Shell.Current.GoToAsync("..");
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(MicText))] private bool isListening;
    [ObservableProperty] private string? voiceStatus;
    public string MicText => IsListening ? Loc.T("Mic_Stop") : Loc.T("Mic_Voice");

    [RelayCommand]
    private async Task VoiceAsync()
    {
        if (IsListening) { _voice.StopListening(); return; }
        IsListening = true; VoiceStatus = Loc.T("Voice_Listening");
        try
        {
            var text = await _voice.ListenAsync(new Progress<string>(t => VoiceStatus = t));
            if (string.IsNullOrWhiteSpace(text)) { VoiceStatus = Loc.T("Voice_NothingHeard"); return; }
            VoiceStatus = text;
            var cmd = VoiceCommandParser.Parse(text, VoiceIntent.AddProduct);
            if (!string.IsNullOrWhiteSpace(cmd.ProductName)) Name = cmd.ProductName!;
            if (cmd.UnitPrice is decimal sp) SalePrice = sp;
            if (cmd.PurchasePrice is decimal pp) PurchasePrice = pp;
            if (IsNew && cmd.Quantity is decimal q) Stock = q;
            if (!string.IsNullOrEmpty(cmd.Unit) && Units.Contains(cmd.Unit!)) Unit = cmd.Unit!;
            if (!string.IsNullOrWhiteSpace(cmd.Category)) Category = cmd.Category!;
        }
        finally { IsListening = false; }
    }

    private static Task Alert(string msg) => Shell.Current.DisplayAlertAsync(Loc.T("Error"), msg, Loc.T("OK"));
}
