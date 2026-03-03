using OllamaSharp;
using OllamaSharp.Models;
using System;
using System.Text;
using System.Threading.Tasks;
using FairRecruitingEngine.Models;
using System.Text.Json;

namespace FairRecruitingEngine.Services
{
    public class OllamaService
    {
        private readonly OllamaApiClient _client;

        public OllamaService()
        {
            _client = new OllamaApiClient("http://localhost:11434");
        }

        public async Task<string> AnalyzeJobActionAsync(string prompt, string modelTag, string? imageBase64)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "⚠️ Kein Text zur Analyse vorhanden.";

            _client.SelectedModel = modelTag;

            var fullResponse = new StringBuilder();

            try
            {
                var request = new GenerateRequest
                {
                    Model = modelTag,
                    Prompt = prompt,
                    Stream = true,
                    Options = new RequestOptions
                    {
                        NumPredict = 300,
                        Temperature = 0.3f
                    }
                };

                await foreach (var stream in _client.GenerateAsync(request))
                {
                    if (stream?.Response != null)
                    {
                        fullResponse.Append(stream.Response);
                    }

                    if (stream?.Done == true)
                        break;
                }

                var raw = fullResponse.ToString().Trim();

                if (string.IsNullOrWhiteSpace(raw))
                    return "⚠️ Modell hat keine Antwort generiert.";

                // Versuchen JSON zu extrahieren
                var start = raw.IndexOf("{");
                var end = raw.LastIndexOf("}");

                if (start == -1 || end == -1)
                    return raw;

                var json = raw.Substring(start, end - start + 1);

                var result = JsonSerializer.Deserialize<AnalysisResult>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result == null)
                    return raw;

                return $@"
=== ANALYSEERGEBNIS ===

Risikolevel: {result.RiskLevel}
Automation-Wahrscheinlichkeit: {result.AutomationProbability}%
Confidence: {result.ConfidenceScore}

Erklärung:
{result.Explanation}

Empfehlung:
{result.Recommendation}
";
            }
            catch (Exception ex)
            {
                return $"❌ Ollama Fehler: {ex.Message}";
            }
        }
    }
}