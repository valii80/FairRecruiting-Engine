using System;
using System.IO;
using System.Windows.Media.Imaging;
using Tesseract;

namespace FairRecruitingEngine.Services
{
    public class OcrService
    {
        private readonly string _tessDataPath;

        public OcrService()
        {
            // Standard Installationspfad von Tesseract
            _tessDataPath = @"C:\Program Files\Tesseract-OCR\tessdata";
        }

        public string ExtractTextFromImage(BitmapSource bitmap)
        {
            if (bitmap == null)
                return string.Empty;

            try
            {
                using var engine = new TesseractEngine(_tessDataPath, "deu+eng", EngineMode.Default);

                using var memoryStream = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(memoryStream);

                using var img = Pix.LoadFromMemory(memoryStream.ToArray());
                using var page = engine.Process(img);

                return page.GetText();
            }
            catch (Exception ex)
            {
                return $"OCR Fehler: {ex.Message}";
            }
        }
    }
}