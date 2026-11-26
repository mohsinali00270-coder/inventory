using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace ShopLedger.Services;

public interface IVoiceAssistant
{
    bool IsListening { get; }
    /// <summary>Asks for mic permission, listens in the selected language (en-US / ur-PK), returns the text or null.</summary>
    Task<string?> ListenAsync(IProgress<string>? partial = null);
    void StopListening();
}

public class VoiceAssistant : IVoiceAssistant
{
    private readonly ISpeechToText _stt;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    public bool IsListening { get; private set; }

    public VoiceAssistant(ISpeechToText stt, ISettingsService settings) { _stt = stt; _settings = settings; }

    public async Task<string?> ListenAsync(IProgress<string>? partial = null)
    {
        if (IsListening) return null;

        var granted = await _stt.RequestPermissions(CancellationToken.None);
        if (!granted)
        {
            await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_Mic"),
                Loc.T("V_MicPermission"), Loc.T("OK"));
            return null;
        }

        CultureInfo culture;
        try { culture = new CultureInfo(_settings.IsUrdu ? "ur-PK" : "en-US"); }
        catch { culture = new CultureInfo(_settings.IsUrdu ? "ur" : "en"); }

        var latest = "";
        var done = new TaskCompletionSource<SpeechToTextResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnUpdated(object? s, SpeechToTextRecognitionResultUpdatedEventArgs e)
        {
            latest = e.RecognitionResult;
            partial?.Report(latest);
        }
        void OnCompleted(object? s, SpeechToTextRecognitionResultCompletedEventArgs e) => done.TrySetResult(e.RecognitionResult);

        var cts = _cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var reg = cts.Token.Register(() => done.TrySetResult(null));
        _stt.RecognitionResultUpdated += OnUpdated;
        _stt.RecognitionResultCompleted += OnCompleted;
        IsListening = true;
        try
        {
            await _stt.StartListenAsync(new SpeechToTextOptions { Culture = culture, ShouldReportPartialResults = true }, cts.Token);
            var result = await done.Task;
            if (result is { IsSuccessful: true } && !string.IsNullOrWhiteSpace(result.Text)) return result.Text;
            return string.IsNullOrWhiteSpace(latest) ? null : latest;
        }
        catch (OperationCanceledException) { return string.IsNullOrWhiteSpace(latest) ? null : latest; }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(Loc.T("Dlg_VoiceInput"),
                _settings.IsUrdu
                    ? Loc.T("V_UrduSttMissing") + "\n\n" + ex.Message
                    : ex.Message, Loc.T("OK"));
            return null;
        }
        finally
        {
            _stt.RecognitionResultUpdated -= OnUpdated;
            _stt.RecognitionResultCompleted -= OnCompleted;
            try { await _stt.StopListenAsync(CancellationToken.None); } catch { /* already stopped */ }
            IsListening = false;
        }
    }

    public void StopListening() => _cts?.Cancel();
}
