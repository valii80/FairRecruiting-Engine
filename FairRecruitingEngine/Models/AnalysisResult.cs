using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace FairRecruitingEngine.Models
{
    public class AnalysisResult
    {
        [JsonPropertyName("discrimination_analysis")]
        public DiscriminationAnalysis DiscriminationAnalysis { get; set; } = new();

        [JsonPropertyName("automation_analysis")]
        public AutomationAnalysis AutomationAnalysis { get; set; } = new();

        [JsonPropertyName("analysis_confidence")]
        public int AnalysisConfidence { get; set; }

        [JsonPropertyName("explanation")]
        public Explanation Explanation { get; set; } = new();

        [JsonPropertyName("recommendation")]
        public Recommendation Recommendation { get; set; } = new();
    }

    public class DiscriminationAnalysis
    {
        [JsonPropertyName("overall_score")]
        public int OverallScore { get; set; }

        [JsonPropertyName("categories")]
        public DiscriminationCategories Categories { get; set; } = new();

        [JsonPropertyName("notes")]
        public List<string> Notes { get; set; } = new();
    }

    public class DiscriminationCategories
    {
        [JsonPropertyName("gender")]
        public int Gender { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }

        [JsonPropertyName("ethnicity")]
        public int Ethnicity { get; set; }

        [JsonPropertyName("religion")]
        public int Religion { get; set; }

        [JsonPropertyName("nationality")]
        public int Nationality { get; set; }

        [JsonPropertyName("disability")]
        public int Disability { get; set; }

        [JsonPropertyName("sexual_orientation")]
        public int SexualOrientation { get; set; }
    }

    public class AutomationAnalysis
    {
        [JsonPropertyName("automation_probability")]
        public int AutomationProbability { get; set; }

        [JsonPropertyName("automation_indicators")]
        public List<string> AutomationIndicators { get; set; } = new();
    }

    public class Explanation
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = "";

        [JsonPropertyName("template_patterns")]
        public List<string> TemplatePatterns { get; set; } = new();
    }

    public class Recommendation
    {
        [JsonPropertyName("for_recruiter")]
        public string ForRecruiter { get; set; } = "";

        [JsonPropertyName("for_applicant")]
        public string? ForApplicant { get; set; }
    }
}