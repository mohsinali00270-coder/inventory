using Microsoft.Maui.Media;

namespace ShopLedger.Services;

public interface ITtsService
{
    bool IsSpeaking { get; }
    /// <summary>Speaks each part in turn, in the given language ("en" or "ur"). Stop() interrupts.</summary>
    Task SpeakAsync(IEnumerable<string> parts, string language);
    void Stop();
}

public class TtsService : ITtsService
{
    private CancellationTokenSource? _cts;
    private IEnumerable<Locale>? _locales;
    private bool _warnedNoUrdu;

    public bool IsSpeaking { get; private set; }

    public async Task SpeakAsync(IEnumerable<string> parts, string language)
    {
        Stop();
        var cts = _cts = new CancellationTokenSource();
        try
        {
            var locale = await FindLocaleAsync(language);
            if (locale is null && language == "ur" && !_warnedNoUrdu)
            {
                _warnedNoUrdu = true;
                await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_UrduVoice"),
                    Loc.T("V_InstallUrduVoice"), Loc.T("OK"));
            }

            IsSpeaking = true;
            foreach (var part in parts)
            {
                cts.Token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(part)) continue;
                await TextToSpeech.Default.SpeakAsync(part, new SpeechOptions { Locale = locale }, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_ReadAloud"), ex.Message, Loc.T("OK")); }
        finally { if (_cts == cts) IsSpeaking = false; }
    }

    public void Stop() { _cts?.Cancel(); IsSpeaking = false; }

    private async Task<Locale?> FindLocaleAsync(string language)
    {
        _locales ??= await TextToSpeech.Default.GetLocalesAsync();
        var matches = _locales.Where(l => string.Equals(l.Language, language, StringComparison.OrdinalIgnoreCase)).ToList();
        var preferred = language == "ur" ? "PK" : "US";
        return matches.FirstOrDefault(l => string.Equals(l.Country, preferred, StringComparison.OrdinalIgnoreCase))
               ?? matches.FirstOrDefault();
    }
}
