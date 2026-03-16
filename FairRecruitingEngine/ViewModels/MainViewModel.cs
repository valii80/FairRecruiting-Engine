using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using FairRecruitingEngine.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UglyToad.PdfPig;

namespace FairRecruitingEngine.ViewModels
{
    public class AiModelInfo : ObservableObject
    {
        public string Tag { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string RamInfo { get; set; } = "";
        public bool CanSeeImages { get; set; } = false;

        public SolidColorBrush _activeColor = Brushes.White;
        private SolidColorBrush _displayColor = Brushes.Gray;

        public SolidColorBrush DisplayColor
        {
            get => _displayColor;
            set => SetProperty(ref _displayColor, value);
        }

        private bool _isInstalled;

        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                SetProperty(ref _isInstalled, value);
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
                DisplayColor = value ? _activeColor : Brushes.Gray;
            }
        }

        public string StatusIcon => IsInstalled ? "✓" : "✕";
        public SolidColorBrush StatusColor => IsInstalled ? Brushes.SpringGreen : Brushes.Red;
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly OllamaService _ollamaService = new();
        private readonly OcrService _ocrService;

        private const string WelcomeText =
            "👋 BEREIT FÜR DIE ANALYSE!\n" +
            "1. Modell wählen\n" +
            "2. Datei laden ODER Screenshot per STRG+V\n" +
            "3. Analyse starten";

        [ObservableProperty] private string _statusMessage = WelcomeText;
        [ObservableProperty] private string _jobDescription = "";
        [ObservableProperty] private ObservableCollection<AiModelInfo> _models = new();
        [ObservableProperty] private AiModelInfo? _selectedModelItem;
        [ObservableProperty] private ImageSource? _attachedImageSource;
        [ObservableProperty] private bool _hasImage = false;

        public bool ShowVisionHint =>
            HasImage && (SelectedModelItem == null || !SelectedModelItem.CanSeeImages);

        public MainViewModel()
        {
            _ocrService = new OcrService();

            var brushConverter = new BrushConverter();

            Models.Add(new AiModelInfo
            {
                Tag = "deepseek-r1:14b",
                Name = "DeepSeek R1 (14B) – Standard Analyse",
                Description = "Empfohlen für leistungsstarke Systeme (≥ 16 GB RAM)",
                _activeColor = (SolidColorBrush)brushConverter.ConvertFromString("#AB8DFF")!
            });

            Models.Add(new AiModelInfo
            {
                Tag = "deepseek-r1:8b",
                Name = "DeepSeek R1 (8B) – Effizienz-Modus",
                Description = "Optimiert für geringere Systemressourcen (< 16 GB RAM)",
                _activeColor = (SolidColorBrush)brushConverter.ConvertFromString("#FF7043")!
            });

            Models.Add(new AiModelInfo
            {
                Tag = "llama3:8b",
                Name = "Llama 3 (8B) – Effizienz-Modus",
                Description = "Optimiert für geringere Systemressourcen (< 16 GB RAM)",
                _activeColor = (SolidColorBrush)brushConverter.ConvertFromString("#FF7043")!
            });

            Task.Run(CheckInstalledModels);
        }

        [RelayCommand]
        private void ResetApp()
        {
            JobDescription = "";
            AttachedImageSource = null;
            HasImage = false;
            StatusMessage = WelcomeText;
        }

        [RelayCommand]
        private void PasteImage()
        {
            if (Clipboard.ContainsImage())
            {
                var bitmap = Clipboard.GetImage();
                if (bitmap == null) return;

                AttachedImageSource = bitmap;
                HasImage = true;

                var extractedText = _ocrService.ExtractTextFromImage(bitmap);

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    JobDescription += "\n\n--- OCR ERKANNT ---\n" + extractedText;
                }

                StatusMessage = "✅ Screenshot erkannt + Text extrahiert!";
            }
        }

        [RelayCommand]
        private void LoadFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Dateien|*.pdf;*.docx;*.txt;*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string ext = Path.GetExtension(openFileDialog.FileName).ToLower();

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    AttachedImageSource = new BitmapImage(new Uri(openFileDialog.FileName));
                    HasImage = true;
                }
                else
                {
                    string text = "";

                    if (ext == ".pdf")
                    {
                        using var pdf = PdfDocument.Open(openFileDialog.FileName);
                        var sb = new StringBuilder();

                        foreach (var p in pdf.GetPages())
                            sb.AppendLine(p.Text);

                        text = sb.ToString();
                    }
                    else if (ext == ".docx")
                    {
                        using var doc = WordprocessingDocument.Open(openFileDialog.FileName, false);
                        text = doc.MainDocumentPart?.Document?.Body?.InnerText ?? "";
                    }
                    else
                    {
                        text = File.ReadAllText(openFileDialog.FileName);
                    }

                    JobDescription += "\n" + text;
                }
            }
        }

        private string FormatAnalysis(string jsonText)
        {
            try
            {
                var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                int automation =
                    root.GetProperty("automation_analysis")
                        .GetProperty("automation_probability")
                        .GetInt32();

                int confidence =
                    root.GetProperty("analysis_confidence")
                        .GetInt32();

                int discrimination =
                    root.GetProperty("discrimination_analysis")
                        .GetProperty("overall_score")
                        .GetInt32();

                string summary =
                    root.GetProperty("explanation")
                        .GetProperty("summary")
                        .GetString() ?? "Keine Erklärung verfügbar.";

                string recruiterAdvice =
                    root.GetProperty("recommendation")
                        .GetProperty("for_recruiter")
                        .GetString() ?? "Keine Empfehlung verfügbar.";

                return $@"
=== ANALYSEERGEBNIS ===

Automatisierungs-Wahrscheinlichkeit:
{automation} %

Analyse-Sicherheit:
{confidence} %

Diskriminierungs-Gesamtscore:
{discrimination} %

Erklärung:
{summary}

Empfehlung für Recruiter:
{recruiterAdvice}
";
            }
            catch
            {
                return "⚠ Analyse konnte nicht korrekt gelesen werden.\n\n" + jsonText;
            }
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task TestOllama()
        {
            if (SelectedModelItem == null)
            {
                StatusMessage = "⚠️ Wähle zuerst ein Modell.";
                return;
            }

            if (string.IsNullOrWhiteSpace(JobDescription) && !HasImage)
            {
                StatusMessage = "⚠️ Bitte Text eingeben oder Screenshot einfügen.";
                return;
            }

            string trimmedInput =
                JobDescription.Length > 4000
                ? JobDescription.Substring(0, 4000)
                : JobDescription;

            string finalPrompt =
                PromptFactory.BuildPrompt(SelectedModelItem.Tag, trimmedInput);

            int progress = 0;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(120);

            timer.Tick += (s, e) =>
            {
                if (progress < 90)
                {
                    progress += 2;

                    int blocks = progress / 5;

                    string bar =
                        new string('█', blocks) +
                        new string('░', 20 - blocks);

                    StatusMessage =
                        $"🔄 Analyse läuft mit {SelectedModelItem.Name}\n\n{bar} {progress}%";
                }
            };

            timer.Start();

            try
            {
                var result = await _ollamaService.AnalyzeJobActionAsync(
                    finalPrompt,
                    SelectedModelItem.Tag,
                    null);

                timer.Stop();

                string formatted = FormatAnalysis(result);

                StatusMessage =
                    $"✔ Analyse abgeschlossen\n\n{formatted}";
            }
            catch (Exception ex)
            {
                timer.Stop();
                StatusMessage = "❌ Verbindung zu Ollama verloren: " + ex.Message;
            }
        }

        private async Task CheckInstalledModels()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "list",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var m in Models)
                        m.IsInstalled = output.Contains(m.Tag);
                });
            }
            catch { }
        }
    }
}