import sys
import azure.cognitiveservices.speech as speechsdk
from azure.cognitiveservices.speech import AutoDetectSourceLanguageConfig

def transcribe_audio(file_path):
    try:
        # Initialize configuration (move your keys to environment variables in production)
        speech_key = "KEY"
        service_region = "eastus"
        
        speech_config = speechsdk.SpeechConfig(subscription=speech_key, region=service_region)
        audio_config = speechsdk.audio.AudioConfig(filename=file_path)
        auto_config = AutoDetectSourceLanguageConfig(languages=["en-US"])

        # Create transcriber
        conversation_transcriber = speechsdk.transcription.ConversationTranscriber(
            speech_config=speech_config,
            audio_config=audio_config,
            auto_detect_source_language_config=auto_config
        )

        # Event handlers
        def result_callback(evt):
            if evt.result.reason == speechsdk.ResultReason.RecognizedSpeech:
                print(f"Speaker {evt.result.speaker_id}: {evt.result.text}")
            elif evt.result.reason == speechsdk.ResultReason.NoMatch:
                print("NOMATCH: Speech could not be recognized.")

        done = False
        def session_stopped(evt):
            nonlocal done
            print("Session stopped.")
            done = True

        # Connect callbacks
        conversation_transcriber.transcribed.connect(result_callback)
        conversation_transcriber.session_stopped.connect(session_stopped)

        # Start transcription
        conversation_transcriber.start_transcribing_async().get()
        while not done:
            pass
        conversation_transcriber.stop_transcribing_async().get()

    except Exception as ex:
        print(f"Error during transcription: {str(ex)}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python transcribe.py <audio_file_path>", file=sys.stderr)
        sys.exit(1)
    
    input_file = sys.argv[1]
    transcribe_audio(input_file)

