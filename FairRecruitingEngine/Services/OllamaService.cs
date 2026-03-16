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
                        NumPredict = 1200,
                        Temperature = 0.2f,
                        TopP = 0.9f
                    }
                };

                await foreach (var stream in _client.GenerateAsync(request))
                {
                    if (stream?.Response != null)
                        fullResponse.Append(stream.Response);
                }

                var raw = fullResponse.ToString().Trim();

                if (!raw.Contains("}"))
                {
                    return "⚠️ KI-Antwort unvollständig:\n\n" + raw;
                }

                if (string.IsNullOrWhiteSpace(raw))
                    return "⚠️ Modell hat keine Antwort generiert.";

                // JSON robust extrahieren
                int start = raw.IndexOf("{");
                int end = raw.LastIndexOf("}");

                if (start < 0)
                    return raw;

                if (end < start)
                {
                    return "⚠️ Modell hat unvollständiges JSON geliefert:\n\n" + raw;
                }

                string json = raw.Substring(start, end - start + 1);

                AnalysisResult? result = null;

                try
                {
                    result = JsonSerializer.Deserialize<AnalysisResult>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
                catch
                {
                    return raw;
                }

                if (result == null)
                    return raw;

                var templates =
                    result.Explanation?.TemplatePatterns != null
                    ? string.Join(", ", result.Explanation.TemplatePatterns)
                    : "Keine erkannt";

                return $@"
=== ANALYSEERGEBNIS ===

Automatisierungs-Wahrscheinlichkeit:
{result.AutomationAnalysis?.AutomationProbability ?? 0} %

Analyse-Sicherheit:
{result.AnalysisConfidence} %

Diskriminierungs-Gesamtscore:
{result.DiscriminationAnalysis?.OverallScore ?? 0} %

Erklärung:
{result.Explanation?.Summary ?? "Keine Erklärung verfügbar."}

Erkannte Template-Muster:
{templates}

Empfehlung für Recruiter:
{result.Recommendation?.ForRecruiter ?? "Keine Empfehlung."}
";
            }
            catch (Exception ex)
            {
                return $"❌ Ollama Fehler: {ex.Message}";
            }
        }
    }
}