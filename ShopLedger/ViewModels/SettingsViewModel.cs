using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Services;

namespace ShopLedger.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackupService _backup;
    private readonly IShareService _share;
    private readonly IDemoDataService _demo;

    public List<string> ThemeOptions { get; } = new()
        { Loc.T("Theme_System"), Loc.T("Theme_Light"), Loc.T("Theme_Dark") };

    private int _themeIndex = ThemeService.Index;
    public int ThemeIndex
    {
        get => _themeIndex;
        set { if (SetProperty(ref _themeIndex, value) && value >= 0) ThemeService.Index = value; }
    }

    [ObservableProperty] private string status = "";

    public bool IsEnglish => !Loc.IsUrdu;
    public bool IsUrdu => Loc.IsUrdu;
    public string AppVersion => AppInfo.Current.VersionString;

    [RelayCommand] private void SetEnglish() => LanguageSwitcher.Set("en");
    [RelayCommand] private void SetUrdu() => LanguageSwitcher.Set("ur");

    public SettingsViewModel(IBackupService backup, IShareService share, IDemoDataService demo)
    { _backup = backup; _share = share; _demo = demo; }

    [RelayCommand]
    private async Task LoadDemoAsync()
    {
        try
        {
            if (!await _demo.IsEmptyAsync())
            { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Demo"), Loc.T("Demo_NotEmpty"), Loc.T("OK")); return; }
            var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Demo"), Loc.T("Q_LoadDemo"), Loc.T("Yes"), Loc.T("Cancel"));
            if (!ok) return;
            await _demo.SeedAsync();
            Status = Loc.T("S_DemoLoaded");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Demo"), ex.Message, Loc.T("OK")); }
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_ClearAll"), Loc.T("Q_ClearAll"), Loc.T("Delete"), Loc.T("Cancel"));
        if (!ok) return;
        try { await _demo.ClearAllAsync(); Status = Loc.T("S_Cleared"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_ClearAll"), ex.Message, Loc.T("OK")); }
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        try
        {
            var path = await _backup.CreateBackupAsync();
            Status = Loc.F("S_BackupCreated", Path.GetFileName(path));
            await _share.ShareFileAsync(path, "ShopLedger backup");   // save to Drive / WhatsApp / email
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_BackupFailed"), ex.Message, Loc.T("OK")); }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_RestoreBackup"),
            Loc.T("Q_Restore"), Loc.T("Restore"), Loc.T("Cancel"));
        if (!ok) return;
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = Loc.T("S_PickFile") });
            if (file is null) return;

            var tmp = Path.Combine(FileSystem.CacheDirectory, "restore.db3");
            await using (var src = await file.OpenReadAsync())
            await using (var dst = File.Create(tmp))
                await src.CopyToAsync(dst);

            await _backup.RestoreAsync(tmp);
            Status = Loc.T("S_Restored");
            await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Done"), Loc.T("S_RestoredOk"), Loc.T("OK"));
        }
        catch (InvalidDataException ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_InvalidFile"), ex.Message, Loc.T("OK")); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_RestoreFailed"), ex.Message, Loc.T("OK")); }
    }
}
