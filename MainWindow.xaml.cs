using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace Q3TTS.Native
{
    public partial class MainWindow : Window
    {
        private TTSEngine _ttsEngine;
        private AudioEngine _audioEngine;
        private WhisperVerifier _whisperVerifier;
        private bool _isPlaying = false;
        private float[]? _lastGeneratedAudio;
        private string _lastInputText = "";

        public MainWindow()
        {
            InitializeComponent();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _ttsEngine = new TTSEngine(baseDir);
            _audioEngine = new AudioEngine();
            _whisperVerifier = new WhisperVerifier(baseDir);

            _audioEngine.PlaybackStopped += OnPlaybackStopped;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetStatus("Initializing Q3-TTS Engine...", 10);
            try
            {
                await _ttsEngine.LoadModelAsync(Qwen3ModelSize.Size1_7B, (msg, prog) =>
                {
                    Dispatcher.Invoke(() => SetStatus(msg, prog));
                });
                LoadDefaultVoicePrompt();
                LoadDefaultSampleText();
            }
            catch (Exception ex)
            {
                SetStatus($"Engine Init Error: {ex.Message}", 100);
            }
        }

        private void SetStatus(string message, float progress)
        {
            StatusText.Text = message;
            StatusProgress.Value = Math.Clamp(progress, 0, 100);
        }

        private void LoadDefaultVoicePrompt()
        {
            string defaultPrompt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "default_voice_us_female.wav");
            if (File.Exists(defaultPrompt))
            {
                VoicePromptPathText.Text = defaultPrompt;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _audioEngine?.Stop();
            Close();
        }

        private async void ModelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _ttsEngine == null) return;
            Qwen3ModelSize size = ModelCombo.SelectedIndex == 0 ? Qwen3ModelSize.Size1_7B : Qwen3ModelSize.Size0_6B;
            await _ttsEngine.LoadModelAsync(size, (msg, prog) =>
            {
                Dispatcher.Invoke(() => SetStatus(msg, prog));
            });
        }

        private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VoicePromptPanel == null || VoiceDesignPanel == null) return;
            if (ModeCombo.SelectedIndex == 0)
            {
                VoicePromptPanel.Visibility = Visibility.Visible;
                VoiceDesignPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                VoicePromptPanel.Visibility = Visibility.Collapsed;
                VoiceDesignPanel.Visibility = Visibility.Visible;
            }
        }

        private void SelectVoice_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "WAV Audio File (*.wav)|*.wav",
                Title = "Select Reference Voice Prompt"
            };

            if (dlg.ShowDialog() == true)
            {
                VoicePromptPathText.Text = dlg.FileName;
            }
        }

        private void VoicePrompt_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void VoicePrompt_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    VoicePromptPathText.Text = files[0];
                }
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharCount();
        }

        private void ParamText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                Slider? targetSlider = tb.Name switch
                {
                    "SpeedText" => SpeedSlider,
                    "ExaggerationText" => ExaggerationSlider,
                    "TemperatureText" => TemperatureSlider,
                    "CfgWeightText" => CfgWeightSlider,
                    "RepetitionPenaltyText" => RepetitionPenaltySlider,
                    _ => null
                };

                if (targetSlider != null && double.TryParse(tb.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    double clamped = Math.Clamp(val, targetSlider.Minimum, targetSlider.Maximum);
                    if (Math.Abs(targetSlider.Value - clamped) > 0.001)
                    {
                        targetSlider.Value = clamped;
                    }
                }
            }
        }

        private void ParamText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb)
            {
                ParamText_TextChanged(tb, null!);
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private void UpdateCharCount()
        {
            int count = InputTextBox.Text.Length;
            CharCountText.Text = $"{count} character{(count == 1 ? "" : "s")}";
        }

        private void LoadDefaultSampleText()
        {
            string samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample_sentences_en.txt");
            if (File.Exists(samplePath))
            {
                try
                {
                    InputTextBox.Text = File.ReadAllText(samplePath, Encoding.UTF8);
                }
                catch { }
            }
        }

        private void InputTextBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void InputTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && File.Exists(files[0]))
                {
                    try
                    {
                        string content = File.ReadAllText(files[0], Encoding.UTF8);
                        InputTextBox.Text = content;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to read file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Clear();
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                _audioEngine.Stop();
                OnPlaybackStopped();
                return;
            }

            string text = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Please enter English text to synthesize.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PlayButton.Content = "Stop / 停止";
            _isPlaying = true;
            SetStatus("Synthesizing American English speech...", 10);

            try
            {
                SynthesisMode mode = ModeCombo.SelectedIndex == 0 ? SynthesisMode.VoicePrompt : SynthesisMode.VoiceDesign;
                string promptPath = VoicePromptPathText.Text;
                string designPrompt = VoiceDesignText.Text;

                float speed = (float)SpeedSlider.Value;
                float exaggeration = (float)ExaggerationSlider.Value;
                float temp = (float)TemperatureSlider.Value;
                float cfg = (float)CfgWeightSlider.Value;
                float rep = (float)RepetitionPenaltySlider.Value;

                _lastInputText = text;
                _lastGeneratedAudio = await Task.Run(() => _ttsEngine.GenerateSpeechAsync(
                    text, mode, promptPath, designPrompt, exaggeration, temp, cfg, rep,
                    (msg, prog) => Dispatcher.Invoke(() => SetStatus(msg, prog))
                ));

                if (_lastGeneratedAudio != null && _lastGeneratedAudio.Length > 0)
                {
                    SetStatus("Playing audio...", 100);
                    _audioEngine.Play(_lastGeneratedAudio, speed);

                    if (DebugSttCheckBox.IsChecked == true)
                    {
                        string currentText = text;
                        float[] currentAudio = _lastGeneratedAudio;
                        float currentSpeed = speed;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                string norm = EnglishNormalizer.Normalize(currentText);
                                string playSpeechWav = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "q3tts_play_speech.wav");
                                _audioEngine.SaveWav(currentAudio, playSpeechWav, currentSpeed);
                                var result = await _whisperVerifier.VerifyAndLogAsync(currentText, norm, currentAudio, playSpeechWav);
                                string logName = Path.GetFileName(Path.ChangeExtension(playSpeechWav, ".debug.txt"));
                                Dispatcher.Invoke(() => SetStatus($"STT log saved: {logName} ({result.MatchPercentage:F1}% match)", 100));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Whisper STT Error] {ex.Message}");
                            }
                        });
                    }
                }
                else
                {
                    SetStatus("Speech synthesis yielded empty audio.", 100);
                    OnPlaybackStopped();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Speech synthesis error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Error during speech synthesis.", 100);
                OnPlaybackStopped();
            }
        }

        private void OnPlaybackStopped()
        {
            Dispatcher.Invoke(() =>
            {
                _isPlaying = false;
                PlayButton.Content = "Play Speech / 再生";
                SetStatus("Ready", 100);
            });
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string rawText = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(rawText))
            {
                MessageBox.Show("Please enter English text to generate WAV.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string[] lines = rawText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrEmpty(l))
                                    .ToArray();

            if (lines.Length == 0) return;

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "WAV Audio File (*.wav)|*.wav",
                FileName = lines.Length > 1
                    ? $"Q3TTS_Batch_{DateTime.Now:yyyyMMdd_HHmmss}.wav"
                    : $"Q3TTS_Output_{DateTime.Now:yyyyMMdd_HHmmss}.wav",
                Title = lines.Length > 1
                    ? $"Save Batch WAV Files ({lines.Length} lines)"
                    : "Save Synthesized Audio WAV File"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    SynthesisMode mode = ModeCombo.SelectedIndex == 0 ? SynthesisMode.VoicePrompt : SynthesisMode.VoiceDesign;
                    string promptPath = VoicePromptPathText.Text;
                    string designPrompt = VoiceDesignText.Text;

                    float speed = (float)SpeedSlider.Value;
                    float exaggeration = (float)ExaggerationSlider.Value;
                    float temp = (float)TemperatureSlider.Value;
                    float cfg = (float)CfgWeightSlider.Value;
                    float rep = (float)RepetitionPenaltySlider.Value;

                    List<string> savedFiles = new List<string>();

                    if (lines.Length == 1)
                    {
                        SetStatus("Generating speech WAV file...", 20);
                        float[] audio = await Task.Run(() => _ttsEngine.GenerateSpeechAsync(
                            lines[0], mode, promptPath, designPrompt, exaggeration, temp, cfg, rep,
                            (msg, prog) => Dispatcher.Invoke(() => SetStatus(msg, prog))
                        ));

                        if (audio != null && audio.Length > 0)
                        {
                            _audioEngine.SaveWav(audio, dlg.FileName, speed);
                            savedFiles.Add(dlg.FileName);

                            if (DebugSttCheckBox.IsChecked == true)
                            {
                                SetStatus("Generating Whisper STT verification log (.debug.txt)...", 95);
                                string norm = EnglishNormalizer.Normalize(lines[0]);
                                var res = await _whisperVerifier.VerifyAndLogAsync(lines[0], norm, audio, dlg.FileName);
                                string reportName = Path.GetFileName(Path.ChangeExtension(dlg.FileName, ".debug.txt"));
                                SetStatus($"STT verification log saved: {reportName} ({res.MatchPercentage:F1}% match)", 100);
                            }
                        }
                    }
                    else
                    {
                        string dir = Path.GetDirectoryName(dlg.FileName)!;
                        string baseName = Path.GetFileNameWithoutExtension(dlg.FileName);
                        string ext = Path.GetExtension(dlg.FileName);

                        List<BatchDebugItem> batchDebugItems = new List<BatchDebugItem>();
                        int digits = lines.Length >= 100 ? 3 : 2;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            float lineProg = ((float)i / lines.Length) * 100f;
                            SetStatus($"Generating WAV for line {i + 1} of {lines.Length}...", lineProg);

                            string lineFileName = $"{baseName}_{(i + 1).ToString($"D{digits}")}{ext}";
                            string lineFilePath = Path.Combine(dir, lineFileName);

                            float[] audio = await Task.Run(() => _ttsEngine.GenerateSpeechAsync(
                                lines[i], mode, promptPath, designPrompt, exaggeration, temp, cfg, rep,
                                (msg, prog) => Dispatcher.Invoke(() => SetStatus($"Generating WAV line {i + 1}/{lines.Length}: {msg}", lineProg))
                            ));

                            if (audio != null && audio.Length > 0)
                            {
                                _audioEngine.SaveWav(audio, lineFilePath, speed);
                                savedFiles.Add(lineFilePath);

                                if (DebugSttCheckBox.IsChecked == true)
                                {
                                    string norm = EnglishNormalizer.Normalize(lines[i]);
                                    batchDebugItems.Add(new BatchDebugItem
                                    {
                                        LineNumber = i + 1,
                                        OriginalText = lines[i],
                                        NormalizedText = norm,
                                        WavPath = lineFilePath,
                                        PcmData24kHz = audio
                                    });
                                }
                            }
                        }

                        if (DebugSttCheckBox.IsChecked == true && batchDebugItems.Count > 0)
                        {
                            SetStatus("Generating Whisper STT verification log (.debug.txt)...", 95);
                            string batchReportPath = Path.Combine(dir, $"{baseName}.debug.txt");
                            await Task.Run(async () =>
                            {
                                await _whisperVerifier.SaveBatchDebugReportAsync(batchDebugItems, batchReportPath);
                            });
                            SetStatus($"STT verification log saved: {Path.GetFileName(batchReportPath)}", 100);
                        }
                    }

                    if (savedFiles.Count > 0)
                    {
                        string debugNote = DebugSttCheckBox.IsChecked == true ? "\n\nSTT verification log (.debug.txt) created!" : "";
                        SetStatus($"Successfully generated {savedFiles.Count} WAV file{(savedFiles.Count == 1 ? "" : "s")}", 100);
                        string fileList = string.Join("\n", savedFiles.Select(f => Path.GetFileName(f)));
                        MessageBox.Show($"Generated {savedFiles.Count} WAV file{(savedFiles.Count == 1 ? "" : "s")}:\n\n{fileList}{debugNote}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        SetStatus("Speech synthesis yielded empty audio.", 100);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Save WAV error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SetStatus("Save WAV error.", 100);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _audioEngine?.Dispose();
            _ttsEngine?.Dispose();
            _whisperVerifier?.Dispose();
            base.OnClosed(e);
        }
    }
}
