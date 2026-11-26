using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Services;

namespace ShopLedger.ViewModels;

[QueryProperty(nameof(CustomerIdParam), "customerId")]
public partial class StatementViewModel : ObservableObject
{
    private readonly IStatementService _statements;
    private readonly IPdfService _pdf;
    private readonly IShareService _share;

    public ObservableCollection<StatementRow> Rows { get; } = new();
    public List<string> Ranges => DateRangeHelper.Names;

    [ObservableProperty] private string? customerIdParam;
    [ObservableProperty] private string selectedRange = DateRangeHelper.Label("ThisMonth");
    [ObservableProperty] private bool isCustom;
    [ObservableProperty] private DateTime customFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime customTo = DateTime.Today;
    [ObservableProperty] private Statement? statement;

    public StatementViewModel(IStatementService statements, IPdfService pdf, IShareService share)
    { _statements = statements; _pdf = pdf; _share = share; }

    partial void OnSelectedRangeChanged(string value) { IsCustom = DateRangeHelper.IsCustom(value); _ = LoadAsync(); }
    partial void OnCustomFromChanged(DateTime value) { if (IsCustom) _ = LoadAsync(); }
    partial void OnCustomToChanged(DateTime value) { if (IsCustom) _ = LoadAsync(); }
    partial void OnCustomerIdParamChanged(string? value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!int.TryParse(CustomerIdParam, out var id)) return;
        var (from, to) = DateRangeHelper.Resolve(SelectedRange, CustomFrom, CustomTo);
        Statement = await _statements.GetAsync(id, from, to);
        Rows.Clear();
        foreach (var r in Statement.Rows) Rows.Add(r);
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (Statement is null) return;
        try
        {
            var path = await Task.Run(() => _pdf.CreateStatementPdf(Statement));
            await _share.ShareFileAsync(path, $"Statement - {Statement.Customer.Name}");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_PdfError"), ex.Message, Loc.T("OK")); }
    }
}
