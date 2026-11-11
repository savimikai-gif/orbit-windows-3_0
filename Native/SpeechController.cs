using System.Speech.Recognition;

namespace ORBIT;

/// <summary>Opt-in microphone control using Windows-installed speech recognition.
/// No continuous audio is transmitted to a cloud service. Recognition is limited by
/// the recognition languages installed on the user's Windows system.</summary>
internal sealed class SpeechController : IDisposable
{
    private SpeechRecognitionEngine? recognizer;
    private Grammar? wake;
    private DictationGrammar? dictation;
    private readonly System.Threading.Timer expiry;
    private readonly object gate = new();
    private DateTime commandDeadline = DateTime.MinValue;
    private bool handsFree;
    public event Action<string, string>? StatusChanged; // state, text
    public SpeechController() => expiry = new System.Threading.Timer(_ => Expire(), null, 900, 900);

    private void Report(string state, string message = "") => StatusChanged?.Invoke(state, message);
    private void Expire()
    {
        lock (gate)
        {
            if (commandDeadline != DateTime.MinValue && DateTime.UtcNow >= commandDeadline)
            {
                commandDeadline = DateTime.MinValue;
                if (handsFree) { if (dictation is not null) dictation.Enabled=false; if (wake is not null) wake.Enabled=true; Report("armed", "Say Hey ORBIT"); }
                else { StopInternal(); Report("off", "Microphone off"); }
            }
        }
    }
    internal object State() => new { active = recognizer is not null, handsFree, languages = SpeechRecognitionEngine.InstalledRecognizers().Select(x => x.Culture.Name).Distinct().ToArray() };
    internal object Start(bool wakeEnabled, string language)
    {
        lock (gate)
        {
            StopInternal();
            var all = SpeechRecognitionEngine.InstalledRecognizers();
            var info = all.FirstOrDefault(x => x.Culture.Name.Equals(language, StringComparison.OrdinalIgnoreCase));
            if (info is null) throw new InvalidOperationException($"Windows speech recognition for {language} is not installed. Add the speech language in Windows Settings, or use another installed language.");
            try
            {
                recognizer = new SpeechRecognitionEngine(info);
                var builder = new GrammarBuilder { Culture = info.Culture };
                builder.Append(new Choices("hey orbit", "okay orbit", "orbit"));
                wake = new Grammar(builder) { Name = "orbit-wake" };
                dictation = new DictationGrammar { Name = "orbit-command" };
                recognizer.LoadGrammar(wake);
                recognizer.LoadGrammar(dictation);
                wake.Enabled = wakeEnabled;
                dictation.Enabled = !wakeEnabled;
                // Always-on dictation events are discarded until the wake word or a PTT activation.
                recognizer.SpeechRecognized += OnRecognized;
                recognizer.RecognizeCompleted += (_, args) => { if (args.Error is not null) Report("error", args.Error.Message); };
                recognizer.SetInputToDefaultAudioDevice();
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
                handsFree = wakeEnabled;
                commandDeadline = wakeEnabled ? DateTime.MinValue : DateTime.UtcNow.AddSeconds(12);
                Report(wakeEnabled ? "armed" : "listening", wakeEnabled ? "Say Hey ORBIT" : "Speak a command");
                return State();
            }
            catch
            {
                StopInternal();
                throw;
            }
        }
    }
    internal void PushToTalk(string language)
    {
        lock (gate)
        {
            if (recognizer is null) { Start(false, language); return; }
            commandDeadline = DateTime.UtcNow.AddSeconds(12);
            if (wake is not null) wake.Enabled=false;
            if (dictation is not null) dictation.Enabled=true;
            Report("listening", "Speak a command");
        }
    }
    private void OnRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence < 0.50) return;
        string text = e.Result.Text.Trim();
        if (text.Length == 0) return;
        lock (gate)
        {
            bool wakeRecognized = e.Result.Grammar?.Name == "orbit-wake";
            if (wakeRecognized)
            {
                commandDeadline = DateTime.UtcNow.AddSeconds(12);
                if (wake is not null) wake.Enabled=false;
                if (dictation is not null) dictation.Enabled=true;
                Report("listening", "Listening for your command");
                return;
            }
            if (commandDeadline == DateTime.MinValue || DateTime.UtcNow > commandDeadline) return;
            commandDeadline = DateTime.MinValue;
            Report("heard", text);
            if (!handsFree) ThreadPool.QueueUserWorkItem(_ => Stop());
            else { if (dictation is not null) dictation.Enabled=false; if (wake is not null) wake.Enabled=true; Report("armed", "Say Hey ORBIT"); }
        }
    }
    internal void Stop() { lock (gate) StopInternal(); Report("off", "Microphone off"); }
    private void StopInternal()
    {
        commandDeadline = DateTime.MinValue;
        var engine = recognizer;
        recognizer = null; wake = null; dictation = null; handsFree = false;
        if (engine is not null)
        {
            try { engine.RecognizeAsyncCancel(); } catch { }
            try { engine.Dispose(); } catch { }
        }
    }
    public void Dispose() { expiry.Dispose(); Stop(); }
}
