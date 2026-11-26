using ShopLedger.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopLedger.Data;
using ShopLedger.Models;

namespace ShopLedger.ViewModels;

[QueryProperty(nameof(IdParam), "id")]
public partial class CustomerEditViewModel : ObservableObject
{
    private readonly ICustomerRepository _repo;
    private readonly ITransactionRepository _txns;
    private readonly AppDatabase _db;
    private readonly IVoiceAssistant _voice;
    private Customer _customer = new();

    [ObservableProperty] private string? idParam;
    [ObservableProperty] private string title = Loc.T("Title_NewCustomer");
    [ObservableProperty] private bool isNew = true;

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private decimal creditLimit;
    [ObservableProperty] private decimal openingBalance;   // old balance the customer already owes

    public bool IsExisting => !IsNew;

    public CustomerEditViewModel(ICustomerRepository repo, ITransactionRepository txns, AppDatabase db, IVoiceAssistant voice)
    { _repo = repo; _txns = txns; _db = db; _voice = voice; }

    async partial void OnIdParamChanged(string? value)
    {
        if (!int.TryParse(value, out var id)) return;
        var c = await _repo.GetAsync(id);
        if (c is null) return;
        _customer = c;
        IsNew = false; OnPropertyChanged(nameof(IsExisting));
        Title = Loc.T("Title_EditCustomer");
        Name = c.Name; Phone = c.Phone; Address = c.Address; Notes = c.Notes; CreditLimit = c.CreditLimit;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) { await Alert(Loc.T("Err_CustomerName")); return; }
        if (CreditLimit < 0) { await Alert(Loc.T("Err_LimitNeg")); return; }

        _customer.Name = Name.Trim();
        _customer.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        _customer.Address = Address; _customer.Notes = Notes;
        _customer.CreditLimit = CreditLimit;

        if (IsNew)
        {
            _customer.CurrentBalance = OpeningBalance;
            await _db.InTransactionAsync(c =>
            {
                c.Insert(_customer);   // sets _customer.Id
                if (OpeningBalance != 0)
                    c.Insert(new LedgerTransaction
                    {
                        CustomerId = _customer.Id, Type = TransactionType.Adjustment,
                        TotalAmount = OpeningBalance, BalanceAfter = OpeningBalance, Note = "Opening balance"
                    });
            });
        }
        else await _repo.SaveAsync(_customer);

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IsNew) return;
        if ((await _txns.GetAsync(_customer.Id)).Count > 0)
        { await Alert(Loc.T("Err_CustHistory")); return; }
        if (!await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_DeleteCustomer"), Loc.F("Q_DeleteCustomer", _customer.Name), Loc.T("Delete"), Loc.T("Cancel"))) return;
        await _repo.DeleteAsync(_customer);
        await Shell.Current.GoToAsync("../..");
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
            var cmd = VoiceCommandParser.Parse(text, VoiceIntent.AddCustomer);
            if (!string.IsNullOrWhiteSpace(cmd.CustomerName)) Name = cmd.CustomerName!;
            if (!string.IsNullOrWhiteSpace(cmd.Phone)) Phone = cmd.Phone;
            if (cmd.CreditLimit is decimal cl) CreditLimit = cl;
            if (IsNew && cmd.Amount is decimal ob) OpeningBalance = ob;
        }
        finally { IsListening = false; }
    }

    private static Task Alert(string msg) => Shell.Current.DisplayAlertAsync(Loc.T("Error"), msg, Loc.T("OK"));
}
