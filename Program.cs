using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Q3TTS.Native
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].Equals("--test", StringComparison.OrdinalIgnoreCase))
                {
                    RunTestHarnessAsync().Wait();
                    return;
                }
                else if (args[0].Equals("--auto-debug", StringComparison.OrdinalIgnoreCase))
                {
                    RunAutoDebugHarnessAsync().Wait();
                    return;
                }
            }

            // Launch WPF GUI App
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static async Task RunTestHarnessAsync()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("        Q3-TTS Native CLI Test Harness          ");
            Console.WriteLine("=================================================");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            using var ttsEngine = new TTSEngine(baseDir);
            using var audioEngine = new AudioEngine();

            Console.WriteLine("Loading Qwen3-TTS 1.7B Model...");
            await ttsEngine.LoadModelAsync(Qwen3ModelSize.Size1_7B, (msg, prog) => Console.WriteLine($"[{prog:F0}%] {msg}"));

            string testSentence = "Welcome to Q3-TTS. This software is specifically engineered for fluent American English speech synthesis.";
            Console.WriteLine($"Synthesizing Test Sentence: \"{testSentence}\"");

            float[] audio = await ttsEngine.GenerateSpeechAsync(
                testSentence, SynthesisMode.VoicePrompt, "default_voice_us_female.wav", "",
                exaggeration: 0.5f, temperature: 0.6f, cfgWeight: 0.25f, repetitionPenalty: 1.20f,
                progressCallback: (msg, prog) => Console.WriteLine($"[{prog:F0}%] {msg}")
            );

            string outPath = Path.Combine(baseDir, "test_q3tts_output.wav");
            audioEngine.SaveWav(audio, outPath, 1.0f);
            Console.WriteLine($"SUCCESS: Test WAV exported to: {outPath}");
            Console.WriteLine("=================================================");
        }

        private static async Task RunAutoDebugHarnessAsync()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("     Q3-TTS STT Verification Debug Harness       ");
            Console.WriteLine("=================================================");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            using var ttsEngine = new TTSEngine(baseDir);
            using var audioEngine = new AudioEngine();
            using var whisper = new WhisperVerifier(baseDir);

            await whisper.EnsureModelExistsAsync((msg, prog) => Console.WriteLine($"[{prog:F0}%] {msg}"));
            await ttsEngine.LoadModelAsync(Qwen3ModelSize.Size1_7B, (msg, prog) => Console.WriteLine($"[{prog:F0}%] {msg}"));

            string sampleText = "The quick brown fox jumps over the lazy dog. Q3-TTS delivers articulate American English pronunciation.";
            string normText = EnglishNormalizer.Normalize(sampleText);

            Console.WriteLine($"Input Text     : {sampleText}");
            Console.WriteLine($"Normalized Text: {normText}");

            float[] audio = await ttsEngine.GenerateSpeechAsync(
                sampleText, SynthesisMode.VoicePrompt, "default_voice_us_female.wav", "",
                exaggeration: 0.5f, temperature: 0.6f, cfgWeight: 0.25f, repetitionPenalty: 1.20f
            );

            string outWav = Path.Combine(baseDir, "auto_debug_q3tts.wav");
            audioEngine.SaveWav(audio, outWav, 1.0f);

            var report = await whisper.VerifyAndLogAsync(sampleText, normText, audio, outWav);

            // Also test transcription on reference US English voice audio asset
            string refWavPath = Path.Combine(baseDir, "assets", "default_voice_us_female.wav");
            if (File.Exists(refWavPath))
            {
                using var reader = new NAudio.Wave.AudioFileReader(refWavPath);
                int sampleCount = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
                float[] refPcm = new float[sampleCount];
                reader.Read(refPcm, 0, sampleCount);

                string refTranscribed = await whisper.TranscribeAudioAsync(refPcm);
                Console.WriteLine($"[Reference Voice STT Test] Transcribed: \"{refTranscribed}\"");
            }

            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine($"Transcribed Text : {report.TranscribedText}");
            Console.WriteLine($"Accuracy Score   : {report.MatchPercentage:F2}%");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine($"Debug report written to: {Path.ChangeExtension(outWav, ".debug.txt")}");
            Console.WriteLine("=================================================");
        }
    }
}
