using System;
using System.Collections.Generic;
using System.Text;

namespace FairRecruitingEngine.Services
{
    public static class PromptFactory
    {
        public static string BuildPrompt(string model, string text)
        {
            if (model.ToLower().Contains("llama"))
                return LlamaPrompt(text);

            if (model.ToLower().Contains("deepseek"))
                return DeepSeekPrompt(text);

            return DefaultPrompt(text);
        }

        private static string BaseStructure(string text) => $@"
Du bist ein KI-Analyse-System für Recruiting-Transparenz.

WICHTIG:
- Antworte ausschließlich im JSON-Format.
- Kein Markdown.
- Kein zusätzlicher Text.
- Nur valides JSON.

Format:

{{
  ""discrimination"": true/false,
  ""automation_probability"": 0-100,
  ""template_type"": ""Template/Hybrid/Individuell"",
  ""confidence_score"": 0.0-1.0,
  ""risk_level"": ""Low/Medium/High"",
  ""explanation"": ""Kurze Begründung"",
  ""recommendation"": ""Konkrete Empfehlung""
}}

E-Mail:
-----------------
{text}
-----------------
";

        private static string LlamaPrompt(string text)
        {
            return BaseStructure(text);
        }

        private static string DeepSeekPrompt(string text)
        {
            return BaseStructure(text);
        }

        private static string DefaultPrompt(string text)
        {
            return BaseStructure(text);
        }
    }
}