using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace Q3TTS.Native
{
    public class AudioEngine : IDisposable
    {
        private IWavePlayer? _player;
        private readonly int _sampleRate = 24000;

        public event Action? PlaybackStopped;

        public void Play(float[] audioData, float speed = 1.0f)
        {
            Stop();
            var processed = ProcessAudioPipeline(audioData, speed);

            var sampleProvider = new RawSampleProvider(processed, _sampleRate);
            var player = new WaveOutEvent();
            player.PlaybackStopped += (s, e) =>
            {
                PlaybackStopped?.Invoke();
            };
            _player = player;
            _player.Init(sampleProvider);
            _player.Play();
        }

        public void Stop()
        {
            if (_player != null)
            {
                var playerTemp = _player;
                _player = null;
                playerTemp.Stop();
                playerTemp.Dispose();
            }
        }

        public void SaveWav(float[] audioData, string filePath, float speed = 1.0f)
        {
            var processed = ProcessAudioPipeline(audioData, speed);
            using (var writer = new WaveFileWriter(filePath, new WaveFormat(_sampleRate, 16, 1)))
            {
                foreach (var sample in processed)
                {
                    writer.WriteSample(sample);
                }
            }
        }

        public float[] ProcessAudioPipeline(float[] input, float speed = 1.0f)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();

            // 1. 無音トリム
            var trimmed = TrimSilence(input, threshold: 0.005f);

            // 2. Broadcast Voice EQ Enhancement (Warmth & Air Clarity)
            var enhanced = ApplyPresenceAndWarmthEQ(trimmed);

            // 3. 音量正規化 (-1.0 dB ≒ 0.89)
            var normalized = Normalize(enhanced, targetPeak: 0.89f);

            // 4. WSOLA 伸縮 (話速制御)
            var stretched = StretchAudio(normalized, speed);

            // 5. Soft Limiter (ゼロクリッピング)
            var limited = ApplySoftLimiter(stretched, threshold: 0.95f);

            // 6. パディング (冒頭0.15秒、末尾0.10秒) + フェード (5ms)
            var padded = AddPaddingAndFade(limited);

            return padded;
        }

        public float[] TrimSilence(float[] input, float threshold = 0.005f)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();

            int start = 0;
            while (start < input.Length && Math.Abs(input[start]) < threshold)
            {
                start++;
            }

            int end = input.Length - 1;
            while (end > start && Math.Abs(input[end]) < threshold)
            {
                end--;
            }

            if (start >= end) return input;

            int len = end - start + 1;
            float[] result = new float[len];
            Array.Copy(input, start, result, 0, len);
            return result;
        }

        public float[] ApplyPresenceAndWarmthEQ(float[] input)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();

            float[] output = new float[input.Length];

            // Subtle bi-quad filter coefficients for vocal warmth & air
            // Low-mid warmth shelf (~200Hz, +1.0dB) & High presence shelf (~5000Hz, +1.2dB)
            double lowBoost = 0.05;
            double highBoost = 0.08;

            float lowState = 0f;
            float highState = 0f;

            for (int i = 0; i < input.Length; i++)
            {
                float x = input[i];

                // Simple 1-pole low-pass / high-pass separation
                lowState += 0.1f * (x - lowState);
                highState += 0.4f * (x - highState);

                float highPart = x - highState;
                float sample = x + (float)(lowState * lowBoost) + (float)(highPart * highBoost);
                output[i] = sample;
            }

            return output;
        }

        public float[] Normalize(float[] input, float targetPeak = 0.89f)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();

            float maxAbs = 0f;
            foreach (var sample in input)
            {
                float abs = Math.Abs(sample);
                if (abs > maxAbs) maxAbs = abs;
            }

            if (maxAbs < 1e-6f) return input;

            float factor = targetPeak / maxAbs;
            float[] output = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = input[i] * factor;
            }
            return output;
        }

        public float[] ApplySoftLimiter(float[] input, float threshold = 0.95f)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();

            float[] output = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                float x = input[i];
                if (Math.Abs(x) > threshold)
                {
                    // Hyperbolic tangent soft knee saturation
                    float sign = Math.Sign(x);
                    float absX = Math.Abs(x);
                    float limited = threshold + (1f - threshold) * (float)Math.Tanh((absX - threshold) / (1f - threshold));
                    output[i] = sign * limited;
                }
                else
                {
                    output[i] = x;
                }
            }
            return output;
        }

        public float[] AddPaddingAndFade(float[] input)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();

            int padStart = (int)(_sampleRate * 0.15); // 0.15s
            int padEnd = (int)(_sampleRate * 0.10);   // 0.10s
            int fadeSamples = (int)(_sampleRate * 0.005); // 5ms

            int totalLen = padStart + input.Length + padEnd;
            float[] output = new float[totalLen];

            Array.Copy(input, 0, output, padStart, input.Length);

            int fadeLenIn = Math.Min(fadeSamples, input.Length);
            for (int i = 0; i < fadeLenIn; i++)
            {
                float weight = (float)i / fadeLenIn;
                output[padStart + i] *= weight;
            }

            int fadeLenOut = Math.Min(fadeSamples, input.Length);
            for (int i = 0; i < fadeLenOut; i++)
            {
                float weight = (float)i / fadeLenOut;
                output[padStart + input.Length - 1 - i] *= weight;
            }

            return output;
        }

        public float[] CrossfadeJoinChunks(List<float[]> chunks, float crossfadeSeconds = 0.05f)
        {
            if (chunks == null || chunks.Count == 0) return Array.Empty<float>();
            if (chunks.Count == 1) return chunks[0];

            int crossfadeSamples = (int)(_sampleRate * crossfadeSeconds);
            List<float> result = new List<float>(chunks[0]);

            for (int i = 1; i < chunks.Count; i++)
            {
                float[] nextChunk = chunks[i];
                if (nextChunk.Length == 0) continue;

                int overlap = Math.Min(crossfadeSamples, Math.Min(result.Count, nextChunk.Length));
                if (overlap > 0)
                {
                    int startIndex = result.Count - overlap;
                    for (int j = 0; j < overlap; j++)
                    {
                        float fadeOutWeight = 1.0f - ((float)j / overlap);
                        float fadeInWeight = (float)j / overlap;
                        result[startIndex + j] = (result[startIndex + j] * fadeOutWeight) + (nextChunk[j] * fadeInWeight);
                    }
                    for (int j = overlap; j < nextChunk.Length; j++)
                    {
                        result.Add(nextChunk[j]);
                    }
                }
                else
                {
                    result.AddRange(nextChunk);
                }
            }

            return result.ToArray();
        }

        public float[] StretchAudio(float[] input, float speed)
        {
            float clampedSpeed = Math.Clamp(speed, 0.25f, 4.0f);
            if (Math.Abs(clampedSpeed - 1.0f) < 0.01f) return input;

            int frameSize = 1024;
            int hopAnalysis = 256;
            int hopSynthesis = Math.Max(1, (int)Math.Round((double)hopAnalysis / clampedSpeed));
            int maxSearchOffset = 128;
            int inputLen = input.Length;
            if (inputLen < frameSize) return input;

            float[] window = new float[frameSize];
            for (int i = 0; i < frameSize; i++)
            {
                window[i] = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * i / (frameSize - 1)));
            }

            int numFrames = (inputLen - frameSize) / hopAnalysis + 1;
            int outputLen = (numFrames - 1) * hopSynthesis + frameSize + maxSearchOffset * 2;
            float[] output = new float[outputLen];
            float[] outputWeights = new float[outputLen];

            for (int frame = 0; frame < numFrames; frame++)
            {
                int inputStart = frame * hopAnalysis;
                if (inputStart + frameSize > inputLen) break;

                int nominalOutputStart = frame * hopSynthesis;
                int bestOffset = 0;

                if (frame > 0)
                {
                    int overlapLen = Math.Max(128, frameSize - hopSynthesis);
                    if (overlapLen > frameSize) overlapLen = frameSize;

                    double bestCorr = double.NegativeInfinity;

                    for (int offset = -maxSearchOffset; offset <= maxSearchOffset; offset++)
                    {
                        int candidateStart = nominalOutputStart + offset;
                        if (candidateStart < 0 || candidateStart + overlapLen > outputLen) continue;
                        if (outputWeights[candidateStart] < 1e-4f) continue;

                        double num = 0, denA = 0, denB = 0;
                        for (int i = 0; i < overlapLen; i++)
                        {
                            float a = input[inputStart + i];
                            float b = outputWeights[candidateStart + i] > 1e-4f
                                ? output[candidateStart + i] / outputWeights[candidateStart + i]
                                : 0f;
                            num += a * b;
                            denA += a * a;
                            denB += b * b;
                        }
                        double den = Math.Sqrt(denA * denB);
                        double corr = (den > 1e-6) ? num / den : 0;

                        if (corr > bestCorr)
                        {
                            bestCorr = corr;
                            bestOffset = offset;
                        }
                    }
                }

                int actualStart = nominalOutputStart + bestOffset;
                if (actualStart < 0) actualStart = 0;

                for (int i = 0; i < frameSize; i++)
                {
                    int idx = actualStart + i;
                    if (idx >= outputLen) break;
                    output[idx] += input[inputStart + i] * window[i];
                    outputWeights[idx] += window[i];
                }
            }

            int actualLen = 0;
            for (int i = outputLen - 1; i >= 0; i--)
            {
                if (outputWeights[i] > 1e-4f) { actualLen = i + 1; break; }
            }

            float[] result = new float[actualLen];
            for (int i = 0; i < actualLen; i++)
            {
                result[i] = (outputWeights[i] > 1e-4f) ? output[i] / outputWeights[i] : 0f;
            }

            return result;
        }

        public void Dispose()
        {
            Stop();
        }

        private class RawSampleProvider : ISampleProvider
        {
            private readonly float[] _data;
            private int _offset;
            public WaveFormat WaveFormat { get; }

            public RawSampleProvider(float[] data, int sampleRate)
            {
                _data = data;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesToCopy = Math.Min(count, _data.Length - _offset);
                if (samplesToCopy > 0)
                {
                    Array.Copy(_data, _offset, buffer, offset, samplesToCopy);
                    _offset += samplesToCopy;
                    return samplesToCopy;
                }
                return 0;
            }
        }
    }
}
