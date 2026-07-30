using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace Q3TTS.Native
{
    public class AutoDebugResult
    {
        public string OriginalText { get; set; } = "";
        public string NormalizedText { get; set; } = "";
        public string TranscribedText { get; set; } = "";
        public double MatchPercentage { get; set; }
        public int WordCountOriginal { get; set; }
        public int WordCountTranscribed { get; set; }
        public List<string> MissingWords { get; set; } = new();
        public List<string> ExtraWords { get; set; } = new();
        public List<string> SubstitutedWords { get; set; } = new();
        public string AudioWavPath { get; set; } = "";
        public double AudioDurationSeconds { get; set; }
    }

    public class DebugBatchItem
    {
        public int LineIndex { get; set; }
        public string OriginalText { get; set; } = "";
        public string WavPath { get; set; } = "";
        public float[] PcmWav24kHz { get; set; } = Array.Empty<float>();
    }

    public class WhisperVerifier : IDisposable
    {
        private readonly string _baseDir;
        private readonly string _modelDir;
        private string _modelPath;
        private WhisperFactory? _factory;
        private WhisperProcessor? _processor;

        public WhisperVerifier(string baseDir)
        {
            _baseDir = baseDir;
            _modelDir = Path.Combine(baseDir, "models", "whisper");
            _modelPath = Path.Combine(_modelDir, "ggml-base.en.bin");
        }

        public async Task EnsureModelExistsAsync(Action<string, float>? progressCallback = null)
        {
            if (!Directory.Exists(_modelDir))
            {
                Directory.CreateDirectory(_modelDir);
            }

            if (File.Exists(_modelPath) && new FileInfo(_modelPath).Length > 50_000_000)
            {
                progressCallback?.Invoke("Whisper (ggml-base.en) English model verified.", 100f);
                return;
            }

            progressCallback?.Invoke("Downloading Whisper STT (ggml-base.en) model (~142MB)...", 0f);
            try
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.BaseEn);
                using var fileStream = File.Create(_modelPath);
                await modelStream.CopyToAsync(fileStream);
                progressCallback?.Invoke("Whisper English model download complete.", 100f);
            }
            catch
            {
                // Fallback to Base multi-language if BaseEn fails
                _modelPath = Path.Combine(_modelDir, "ggml-base.bin");
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
                using var fileStream = File.Create(_modelPath);
                await modelStream.CopyToAsync(fileStream);
                progressCallback?.Invoke("Whisper Base model download complete.", 100f);
            }
        }

        public void LoadModel()
        {
            if (_factory != null) return;

            try
            {
                if (!File.Exists(_modelPath) || new FileInfo(_modelPath).Length < 50_000_000)
                {
                    EnsureModelExistsAsync().Wait();
                }

                if (File.Exists(_modelPath) && new FileInfo(_modelPath).Length > 50_000_000)
                {
                    _factory = WhisperFactory.FromPath(_modelPath);
                    _processor = _factory.CreateBuilder()
                        .WithLanguage("en")
                        .WithPrompt("The following is a clear English speech recording.")
                        .Build();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Whisper model load warning: {ex.Message}");
                _factory = null;
                _processor = null;
            }
        }

        public async Task<string> TranscribeAudioAsync(float[] pcmData24kHz)
        {
            LoadModel();
            if (_processor == null || pcmData24kHz == null || pcmData24kHz.Length == 0) return string.Empty;

            // Whisper expects 16kHz audio. Resample from 24kHz to 16kHz
            float[] pcm16kHz = Resample24To16(pcmData24kHz);

            List<string> segments = new();
            await foreach (var segment in _processor.ProcessAsync(pcm16kHz))
            {
                segments.Add(segment.Text);
            }

            return string.Join(" ", segments).Trim();
        }

        public async Task<AutoDebugResult> VerifyAndLogAsync(string originalText, string normalizedText, float[] pcmData24kHz, string outputWavPath)
        {
            string transcribed = await TranscribeAudioAsync(pcmData24kHz);

            double duration = (double)pcmData24kHz.Length / 24000.0;
            var result = CompareText(originalText, normalizedText, transcribed);
            result.AudioWavPath = outputWavPath;
            result.AudioDurationSeconds = duration;

            SaveDebugReportFile(result, outputWavPath);
            return result;
        }

        public AutoDebugResult CompareText(string originalText, string normalizedText, string transcribedText)
        {
            var result = new AutoDebugResult
            {
                OriginalText = originalText,
                NormalizedText = normalizedText,
                TranscribedText = transcribedText
            };

            string normTarget = CleanTextForComparison(normalizedText);
            string normTranscribed = CleanTextForComparison(transcribedText);

            string[] targetWords = normTarget.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] transWords = normTranscribed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            result.WordCountOriginal = targetWords.Length;
            result.WordCountTranscribed = transWords.Length;

            if (targetWords.Length == 0)
            {
                result.MatchPercentage = transWords.Length == 0 ? 100.0 : 0.0;
                return result;
            }

            int distance = ComputeLevenshteinDistance(targetWords, transWords);
            int maxLen = Math.Max(targetWords.Length, transWords.Length);

            result.MatchPercentage = Math.Max(0.0, Math.Round((1.0 - (double)distance / maxLen) * 100.0, 2));

            // Word diffs
            var targetSet = new HashSet<string>(targetWords, StringComparer.OrdinalIgnoreCase);
            var transSet = new HashSet<string>(transWords, StringComparer.OrdinalIgnoreCase);

            result.MissingWords = targetWords.Where(w => !transSet.Contains(w)).Distinct().ToList();
            result.ExtraWords = transWords.Where(w => !targetSet.Contains(w)).Distinct().ToList();

            return result;
        }

        private void SaveDebugReportFile(AutoDebugResult result, string wavPath)
        {
            try
            {
                string reportPath = Path.ChangeExtension(wavPath, ".debug.txt");
                using var writer = new StreamWriter(reportPath, false, System.Text.Encoding.UTF8);

                writer.WriteLine("=================================================");
                writer.WriteLine("          Q3-TTS STT Verification Report          ");
                writer.WriteLine("=================================================");
                writer.WriteLine($"Timestamp          : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"WAV File           : {Path.GetFileName(wavPath)}");
                writer.WriteLine($"Audio Duration     : {result.AudioDurationSeconds:F2} seconds");
                writer.WriteLine($"Accuracy Score     : {result.MatchPercentage:F2}%");
                writer.WriteLine("-------------------------------------------------");
                writer.WriteLine("[Original Input]");
                writer.WriteLine(result.OriginalText);
                writer.WriteLine();
                writer.WriteLine("[Normalized Text]");
                writer.WriteLine(result.NormalizedText);
                writer.WriteLine();
                writer.WriteLine("[Whisper Transcribed]");
                writer.WriteLine(result.TranscribedText);
                writer.WriteLine("-------------------------------------------------");
                writer.WriteLine($"Original Word Count   : {result.WordCountOriginal}");
                writer.WriteLine($"Transcribed Word Count: {result.WordCountTranscribed}");
                writer.WriteLine();
                writer.WriteLine($"Missing Words ({result.MissingWords.Count}):");
                writer.WriteLine(result.MissingWords.Count > 0 ? string.Join(", ", result.MissingWords) : "(None)");
                writer.WriteLine();
                writer.WriteLine($"Extra Words ({result.ExtraWords.Count}):");
                writer.WriteLine(result.ExtraWords.Count > 0 ? string.Join(", ", result.ExtraWords) : "(None)");
                writer.WriteLine("=================================================");
            }
            catch { }
        }

        private static string CleanTextForComparison(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string textNoBrackets = Regex.Replace(text, @"\[.*?\]|\(.*?\)", "");
            string cleaned = Regex.Replace(textNoBrackets.ToLower(), @"[^\w\s]", "");
            return Regex.Replace(cleaned, @"\s+", " ").Trim();
        }

        private static int ComputeLevenshteinDistance(string[] source, string[] target)
        {
            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = string.Equals(source[i - 1], target[j - 1], StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private static float[] Resample24To16(float[] input)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();

            double ratio = 16000.0 / 24000.0;
            int outputLength = (int)Math.Round(input.Length * ratio);
            float[] output = new float[outputLength];

            for (int i = 0; i < outputLength; i++)
            {
                double srcIdx = i / ratio;
                int idx0 = (int)Math.Floor(srcIdx);
                int idx1 = Math.Min(idx0 + 1, input.Length - 1);
                double frac = srcIdx - idx0;

                output[i] = (float)((1.0 - frac) * input[idx0] + frac * input[idx1]);
            }
            return output;
        }

        public void Dispose()
        {
            _processor?.Dispose();
            _factory?.Dispose();
            _processor = null;
            _factory = null;
        }
    }
}
