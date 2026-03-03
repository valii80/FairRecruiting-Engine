using System.Text.Json.Serialization;

namespace FairRecruitingEngine.Models
{
    public class AnalysisResult
    {
        [JsonPropertyName("discrimination")]
        public bool Discrimination { get; set; }

        [JsonPropertyName("automation_probability")]
        public int AutomationProbability { get; set; }

        [JsonPropertyName("template_type")]
        public string TemplateType { get; set; } = "";

        [JsonPropertyName("confidence_score")]
        public double ConfidenceScore { get; set; }

        [JsonPropertyName("risk_level")]
        public string RiskLevel { get; set; } = "";

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = "";

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = "";
    }
}