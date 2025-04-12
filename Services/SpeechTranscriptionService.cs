// Services/SpeechTranscriptionService.cs
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TAnalyzer.Services;

public class SpeechTranscriptionService
{
    private readonly string _subscriptionKey;
    private readonly string _region;

    public SpeechTranscriptionService(string subscriptionKey, string region)
    {
        _subscriptionKey = subscriptionKey;
        _region = region;
    }

    public async Task<string> TranscribeConversationAsync(string audioFilePath, Action<string> realTimeUpdate)
    {
        var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);
        
        
        // Produces a detailed log
        speechConfig.SetProperty(PropertyId.Speech_LogFilename, "speech_debug.log");
        
        
        var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);

        
        var autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages(new[] { "en-US" });

        using var transcriber = new ConversationTranscriber(speechConfig, autoDetectConfig, audioConfig);
        var completionSource = new TaskCompletionSource<string>();
        var fullTranscript = new System.Text.StringBuilder();

        transcriber.Transcribed += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                var line = $"Speaker {e.Result.SpeakerId}: {e.Result.Text}";
                Console.WriteLine(line);
                fullTranscript.AppendLine(line);
                realTimeUpdate?.Invoke(line); // Update UI in real-time
            }
        };

        transcriber.SessionStopped += (s, e) => completionSource.TrySetResult(fullTranscript.ToString());

        await transcriber.StartTranscribingAsync();
        var result = await completionSource.Task;
        await transcriber.StopTranscribingAsync();

        return result;
    }
}