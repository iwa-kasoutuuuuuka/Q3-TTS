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
                        _ = Task.Run(async () =>
                        {
                            string norm = EnglishNormalizer.Normalize(text);
                            string tempWav = Path.Combine(Path.GetTempPath(), $"q3tts_debug_{DateTime.Now.Ticks}.wav");
                            _audioEngine.SaveWav(_lastGeneratedAudio, tempWav, speed);
                            await _whisperVerifier.VerifyAndLogAsync(text, norm, _lastGeneratedAudio, tempWav);
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
                                string norm = EnglishNormalizer.Normalize(lines[0]);
                                await _whisperVerifier.VerifyAndLogAsync(lines[0], norm, audio, dlg.FileName);
                            }
                        }
                    }
                    else
                    {
                        string dir = Path.GetDirectoryName(dlg.FileName)!;
                        string baseName = Path.GetFileNameWithoutExtension(dlg.FileName);
                        string ext = Path.GetExtension(dlg.FileName);

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
                                    string currentLine = lines[i];
                                    string currentPath = lineFilePath;
                                    float[] currentAudio = audio;
                                    _ = Task.Run(async () =>
                                    {
                                        string norm = EnglishNormalizer.Normalize(currentLine);
                                        await _whisperVerifier.VerifyAndLogAsync(currentLine, norm, currentAudio, currentPath);
                                    });
                                }
                            }
                        }
                    }

                    if (savedFiles.Count > 0)
                    {
                        SetStatus($"Successfully generated {savedFiles.Count} WAV file{(savedFiles.Count == 1 ? "" : "s")}", 100);
                        string fileList = string.Join("\n", savedFiles.Select(f => Path.GetFileName(f)));
                        MessageBox.Show($"Generated {savedFiles.Count} WAV file{(savedFiles.Count == 1 ? "" : "s")}:\n\n{fileList}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
