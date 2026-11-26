using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;
using ShopLedger.Services;

namespace ShopLedger.ViewModels;

/// <summary>Payment received (mode=payment) or manual +/- entry (mode=adjust).</summary>
[QueryProperty(nameof(ModeParam), "mode")]
[QueryProperty(nameof(CustomerIdParam), "customerId")]
public partial class LedgerEntryViewModel : ObservableObject
{
    private readonly ICustomerRepository _customers;
    private readonly ILedgerService _ledger;
    private readonly IVoiceAssistant _voice;
    private bool _presetApplied;

    public ObservableCollection<Customer> Customers { get; } = new();
    public List<string> Directions { get; } = new() { Loc.T("Dir_More"), Loc.T("Dir_Less") };

    [ObservableProperty] private string? modeParam;
    [ObservableProperty] private string? customerIdParam;
    [ObservableProperty] private string title = Loc.T("Title_Payment");
    [ObservableProperty] private bool isAdjust;
    [ObservableProperty] private Customer? selectedCustomer;
    [ObservableProperty] private string selectedDirection = Loc.T("Dir_More");
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string? note;

    public LedgerEntryViewModel(ICustomerRepository customers, ILedgerService ledger, IVoiceAssistant voice)
    { _customers = customers; _ledger = ledger; _voice = voice; }

    partial void OnModeParamChanged(string? value)
    {
        IsAdjust = value == "adjust";
        Title = IsAdjust ? Loc.T("Title_Manual") : Loc.T("Title_Payment");
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (Customers.Count == 0)
            foreach (var c in await _customers.GetAllAsync()) Customers.Add(c);
        if (!_presetApplied)
        {
            _presetApplied = true;
            if (int.TryParse(CustomerIdParam, out var id))
                SelectedCustomer = Customers.FirstOrDefault(c => c.Id == id);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCustomer is null) { await Alert(Loc.T("Err_SelectCustomer")); return; }
        if (Amount <= 0) { await Alert(Loc.T("Err_AmountZero")); return; }

        try
        {
            if (IsAdjust)
            {
                var signed = SelectedDirection == Directions[0] ? Amount : -Amount;
                await _ledger.RecordAdjustmentAsync(SelectedCustomer.Id, signed, Note);
            }
            else
            {
                if (Amount > SelectedCustomer.CurrentBalance)
                {
                    var ok = await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_MoreThanBalance"),
                        Loc.F("Q_Owes", SelectedCustomer.Name, Math.Max(0, SelectedCustomer.CurrentBalance)) +
                        Loc.T("Q_AdvanceCredit"), Loc.T("Continue"), Loc.T("Cancel"));
                    if (!ok) return;
                }
                await _ledger.RecordPaymentAsync(SelectedCustomer.Id, Amount, Note);
            }
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { await Alert(ex.Message); }
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
            var cmd = VoiceCommandParser.Parse(text, VoiceIntent.Payment);
            if (!string.IsNullOrWhiteSpace(cmd.CustomerName))
            {
                var cust = FuzzyMatcher.Best(Customers, c => c.Name, cmd.CustomerName!);
                if (cust is null) await Alert(Loc.F("V_CustNotFound", cmd.CustomerName));
                else SelectedCustomer = cust;
            }
            if (cmd.Amount is decimal a) Amount = a;
            if (IsAdjust)
                SelectedDirection = System.Text.RegularExpressions.Regex.IsMatch(text.ToLowerInvariant(),
                    @"\b(less|minus|reduce|deduct|decrease)\b|کم|منفی") ? Directions[1] : Directions[0];
        }
        finally { IsListening = false; }
    }

    private static Task Alert(string msg) => Shell.Current.DisplayAlertAsync(Loc.T("Error"), msg, Loc.T("OK"));
}
