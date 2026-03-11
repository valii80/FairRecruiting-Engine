using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using FairRecruitingEngine.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        public SolidColorBrush DisplayColor { get => _displayColor; set => SetProperty(ref _displayColor, value); }
        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set { SetProperty(ref _isInstalled, value); OnPropertyChanged(nameof(StatusIcon)); OnPropertyChanged(nameof(StatusColor)); DisplayColor = value ? _activeColor : Brushes.Gray; }
        }
        public string StatusIcon => IsInstalled ? "✓" : "✕";
        public SolidColorBrush StatusColor => IsInstalled ? Brushes.SpringGreen : Brushes.Red;
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly OllamaService _ollamaService = new();
        private readonly OcrService _ocrService;
        private const string WelcomeText = "👋 BEREIT FÜR DIE ANALYSE!\n1. Modell wählen\n2. Datei laden ODER Screenshot per STRG+V\n3. Analyse starten";

        [ObservableProperty] private string _statusMessage = WelcomeText;
        [ObservableProperty] private string _jobDescription = "";
        [ObservableProperty] private ObservableCollection<AiModelInfo> _models = new();
        [ObservableProperty][NotifyPropertyChangedFor(nameof(ShowVisionHint))] private AiModelInfo? _selectedModelItem;
        [ObservableProperty] private ImageSource? _attachedImageSource;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(ShowVisionHint))] private bool _hasImage = false;

        public bool ShowVisionHint => HasImage && (SelectedModelItem == null || !SelectedModelItem.CanSeeImages);

        public MainViewModel()
        {
            _ocrService = new OcrService();
            var brushConverter = new BrushConverter();


            // --- KI-MODELLE (Liste) ---

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

        [RelayCommand] private void ResetApp() { JobDescription = ""; AttachedImageSource = null; HasImage = false; StatusMessage = WelcomeText; }

        [RelayCommand]
        private void PasteImage()
        {
            if (Clipboard.ContainsImage())
            {
                var bitmap = Clipboard.GetImage();
                if (bitmap == null) return;

                AttachedImageSource = bitmap;
                HasImage = true;

                // 🔥 OCR direkt ausführen
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
            var openFileDialog = new OpenFileDialog { Filter = "Dateien|*.pdf;*.docx;*.txt;*.png;*.jpg;*.jpeg" };
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
                        var sb = new StringBuilder(); foreach (var p in pdf.GetPages()) sb.AppendLine(p.Text);
                        text = sb.ToString();
                    }
                    else if (ext == ".docx")
                    {
                        using var doc = WordprocessingDocument.Open(openFileDialog.FileName, false);
                        text = doc.MainDocumentPart?.Document?.Body?.InnerText ?? "";
                    }
                    else { text = File.ReadAllText(openFileDialog.FileName); }
                    JobDescription += "\n" + text;
                }
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

            // 🔴 HIER ist der entscheidende Fix
            if (string.IsNullOrWhiteSpace(JobDescription) && !HasImage)
            {
                StatusMessage = "⚠️ Bitte Text eingeben oder Screenshot einfügen, bevor du die Analyse startest.";
                return;
            }

            string trimmedInput = JobDescription.Length > 4000
                ? JobDescription.Substring(0, 4000)
                : JobDescription;

            string finalPrompt = PromptFactory.BuildPrompt(SelectedModelItem.Tag, trimmedInput);

            StatusMessage = $"⏳ {SelectedModelItem.Name} arbeitet...";

            try
            {
                var result = await _ollamaService.AnalyzeJobActionAsync(
                    finalPrompt,
                    SelectedModelItem.Tag,
                    null);

                StatusMessage = result;
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Verbindung zu Ollama verloren: " + ex.Message;
            }
        }

        private async Task CheckInstalledModels()
        {
            try
            {
                var process = new Process { StartInfo = new ProcessStartInfo { FileName = "ollama", Arguments = "list", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true } };
                process.Start(); string output = await process.StandardOutput.ReadToEndAsync(); process.WaitForExit();
                Application.Current.Dispatcher.Invoke(() => { foreach (var m in Models) m.IsInstalled = output.Contains(m.Tag); });
            }
            catch { }
        }
    }
}