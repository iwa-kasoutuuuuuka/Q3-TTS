using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Q3TTS.Native
{
    public enum Qwen3ModelSize
    {
        Size1_7B,
        Size0_6B
    }

    public enum SynthesisMode
    {
        VoicePrompt,
        VoiceDesign
    }

    public class TTSEngine : IDisposable
    {
        private readonly string _modelsDir;
        private Qwen3ModelSize _currentModelSize = Qwen3ModelSize.Size1_7B;
        public string ActiveBackend { get; private set; } = "Initializing...";
        public bool IsLoaded { get; private set; } = false;
        public string SelectedSpeaker { get; set; } = "Ryan";

        private readonly HttpClient _httpClient;
        private Process? _serverProcess;

        public TTSEngine(string baseDir)
        {
            _modelsDir = Path.Combine(baseDir, "models");
            if (!Directory.Exists(_modelsDir))
            {
                Directory.CreateDirectory(_modelsDir);
            }

            // Neural inference can take 10-60+ seconds depending on text length
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        }

        public async Task LoadModelAsync(Qwen3ModelSize size, Action<string, float>? progressCallback = null)
        {
            _currentModelSize = size;
            string modelName = size == Qwen3ModelSize.Size1_7B ? "Qwen3-TTS 1.7B" : "Qwen3-TTS 0.6B";
            progressCallback?.Invoke($"Loading {modelName}...", 10f);

            bool ready = await InitializeInferenceBackendAsync(progressCallback);
            if (ready)
            {
                IsLoaded = true;
                progressCallback?.Invoke($"{modelName} loaded on {ActiveBackend}.", 100f);
            }
            else
            {
                IsLoaded = false;
                ActiveBackend = "Not Connected";
                progressCallback?.Invoke($"ERROR: {modelName} server not available. Start qwen3_server.py first.", 100f);
            }
        }

        private async Task<bool> InitializeInferenceBackendAsync(Action<string, float>? progressCallback)
        {
            // Step 1: Check if server is already running
            progressCallback?.Invoke("Checking Qwen3-TTS CUDA Neural Server...", 30f);
            if (await CheckServerHealthAsync())
            {
                ActiveBackend = "CUDA (Qwen3-TTS Neural)";
                return true;
            }

            // Step 2: Try to auto-start the server
            progressCallback?.Invoke("Starting Qwen3-TTS CUDA Neural Server...", 50f);
            string? scriptPath = FindServerScript();
            if (scriptPath != null)
            {
                try
                {
                    string sizeArg = _currentModelSize == Qwen3ModelSize.Size1_7B ? "1.7B" : "0.6B";
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "uv",
                        Arguments = $"run --no-project --with qwen-tts,torch,soundfile,fastapi,uvicorn,pydantic python \"{scriptPath}\" --port 8080 --size {sizeArg}",
                        WorkingDirectory = Path.GetDirectoryName(scriptPath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };
                    _serverProcess = Process.Start(startInfo);

                    // Wait up to 120 seconds for model to load (large model + CUDA init)
                    for (int i = 0; i < 240; i++)
                    {
                        await Task.Delay(500);
                        float prog = 50f + (i / 240f) * 45f;
                        progressCallback?.Invoke($"Waiting for neural model to load... ({i / 2}s)", prog);

                        if (await CheckServerHealthAsync())
                        {
                            ActiveBackend = "CUDA (Qwen3-TTS Neural)";
                            return true;
                        }

                        // Check if process crashed
                        if (_serverProcess != null && _serverProcess.HasExited)
                        {
                            string stderr = await _serverProcess.StandardError.ReadToEndAsync();
                            progressCallback?.Invoke($"Server crashed: {stderr.Substring(0, Math.Min(200, stderr.Length))}", 95f);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Failed to start server: {ex.Message}", 95f);
                }
            }

            return false;
        }

        private string? FindServerScript()
        {
            // Search for qwen3_server.py in multiple locations
            string[] searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qwen3_server.py"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "qwen3_server.py"),
                @"E:\Q3-TTS\qwen3_server.py"
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path)) return Path.GetFullPath(path);
            }
            return null;
        }

        private async Task<bool> CheckServerHealthAsync()
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                var res = await _httpClient.GetAsync("http://127.0.0.1:8080/health", cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    string json = await res.Content.ReadAsStringAsync();
                    // Only report healthy if model is actually loaded
                    return json.Contains("\"model_loaded\":true") || json.Contains("\"model_loaded\": true");
                }
            }
            catch { }
            return false;
        }

        public async Task<float[]> GenerateSpeechAsync(
            string text,
            SynthesisMode mode,
            string voicePromptPath,
            string voiceDesignPrompt,
            float exaggeration = 0.35f,
            float temperature = 0.55f,
            float cfgWeight = 0.40f,
            float repetitionPenalty = 1.30f,
            Action<string, float>? progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<float>();

            progressCallback?.Invoke("Normalizing English text...", 10f);
            string normalizedText = EnglishNormalizer.Normalize(text);

            if (!IsLoaded || !await CheckServerHealthAsync())
            {
                progressCallback?.Invoke("ERROR: Neural server not available. Please start qwen3_server.py.", 100f);
                return Array.Empty<float>();
            }

            progressCallback?.Invoke("Sending to Qwen3-TTS neural engine...", 30f);

            List<string> sentences = SplitTextIntoSentences(normalizedText, maxChars: 180);
            List<float[]> generatedAudioChunks = new List<float[]>();

            AudioEngine audioEngine = new AudioEngine();

            for (int i = 0; i < sentences.Count; i++)
            {
                float prog = 30f + ((float)i / sentences.Count) * 60f;
                progressCallback?.Invoke($"Neural synthesis: chunk {i + 1} of {sentences.Count}...", prog);

                float[] chunk = await SynthesizeSingleSentenceAsync(sentences[i], temperature);
                if (chunk != null && chunk.Length > 0)
                {
                    generatedAudioChunks.Add(chunk);
                }
                else
                {
                    progressCallback?.Invoke($"WARNING: Sentence {i + 1} returned empty audio.", prog);
                }
            }

            if (generatedAudioChunks.Count == 0)
            {
                progressCallback?.Invoke("ERROR: No audio generated. Check server logs.", 100f);
                return Array.Empty<float>();
            }

            progressCallback?.Invoke("Joining audio chunks...", 95f);
            float[] finalAudio = audioEngine.CrossfadeJoinChunks(generatedAudioChunks, crossfadeSeconds: 0.02f);

            progressCallback?.Invoke("Audio synthesis complete.", 100f);
            return finalAudio;
        }

        private async Task<float[]> SynthesizeSingleSentenceAsync(string sentence, float temperature = 0.55f)
        {
            try
            {
                var payload = new
                {
                    text = sentence,
                    speaker = SelectedSpeaker,
                    language = "English",
                    instruct = "",
                    temperature = Math.Clamp(temperature, 0.1f, 1.0f),
                    top_p = 0.9,
                    max_new_tokens = 2048
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("http://127.0.0.1:8080/synthesize", content);

                if (response.IsSuccessStatusCode)
                {
                    byte[] wavBytes = await response.Content.ReadAsByteArrayAsync();
                    return ExtractPcmFromWavBytes(wavBytes);
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[TTSEngine] Server returned {response.StatusCode}: {errorBody}");
                }
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[TTSEngine] Request timed out (>120s). Text may be too long.");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTSEngine] Connection error: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTSEngine] Unexpected error: {ex.Message}");
            }

            return Array.Empty<float>();
        }

        private float[] ExtractPcmFromWavBytes(byte[] wavBytes)
        {
            try
            {
                using var ms = new MemoryStream(wavBytes);
                using var reader = new NAudio.Wave.WaveFileReader(ms);

                // Calculate sample count properly
                int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                int channels = reader.WaveFormat.Channels;
                long totalSamples = reader.SampleCount;

                // Read all samples as float
                List<float> samples = new List<float>((int)totalSamples);
                float[] buffer = new float[4096];
                var sampleReader = new NAudio.Wave.SampleProviders.Pcm16BitToSampleProvider(reader);
                int read;
                while ((read = sampleReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                        samples.Add(buffer[i]);
                }

                // If stereo, take left channel only
                if (channels == 2)
                {
                    var mono = new List<float>(samples.Count / 2);
                    for (int i = 0; i < samples.Count; i += 2)
                        mono.Add(samples[i]);
                    return mono.ToArray();
                }

                return samples.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTSEngine] WAV decode error: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        private List<string> SplitTextIntoSentences(string text, int maxChars = 180)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            string[] rawSentences = Regex.Split(text, @"(?<=[.!?;\n])\s+");
            StringBuilder currentChunk = new StringBuilder();

            foreach (var s in rawSentences)
            {
                string trimmed = s.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.Length > maxChars)
                {
                    if (currentChunk.Length > 0)
                    {
                        result.Add(currentChunk.ToString());
                        currentChunk.Clear();
                    }

                    string[] subClauses = Regex.Split(trimmed, @"(?<=,)\s+");
                    foreach (var clause in subClauses)
                    {
                        string cTrim = clause.Trim();
                        if (string.IsNullOrEmpty(cTrim)) continue;

                        if (currentChunk.Length + cTrim.Length + 1 <= maxChars)
                        {
                            if (currentChunk.Length > 0) currentChunk.Append(" ");
                            currentChunk.Append(cTrim);
                        }
                        else
                        {
                            if (currentChunk.Length > 0)
                            {
                                result.Add(currentChunk.ToString());
                                currentChunk.Clear();
                            }
                            currentChunk.Append(cTrim);
                        }
                    }
                }
                else if (currentChunk.Length + trimmed.Length + 1 <= maxChars)
                {
                    if (currentChunk.Length > 0) currentChunk.Append(" ");
                    currentChunk.Append(trimmed);
                }
                else
                {
                    if (currentChunk.Length > 0)
                    {
                        result.Add(currentChunk.ToString());
                        currentChunk.Clear();
                    }
                    currentChunk.Append(trimmed);
                }
            }

            if (currentChunk.Length > 0)
            {
                result.Add(currentChunk.ToString());
            }

            return result;
        }

        public void Dispose()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try { _serverProcess.Kill(entireProcessTree: true); } catch { }
                _serverProcess = null;
            }
            _httpClient?.Dispose();
        }
    }
}
