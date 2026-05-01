using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebAppSystems.Models.ViewModels;

namespace WebAppSystems.Services
{
    public class DocumentAIAnalysisService
    {
        private readonly AIService _aiService;
        private readonly ILogger<DocumentAIAnalysisService> _logger;

        public DocumentAIAnalysisService(AIService aiService, ILogger<DocumentAIAnalysisService> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<DocumentAnalysisViewModel> AnalyzeDocumentAsync(string documentText, string fileName)
        {
            try
            {
                _logger.LogInformation($"[DocumentAI] Iniciando análise do documento: {fileName}");
                _logger.LogInformation($"[DocumentAI] Tamanho do texto: {documentText.Length} caracteres");
                
                // Verificar se a IA está configurada
                var (isConfigured, errorMessage) = await _aiService.IsConfiguredAsync();
                if (!isConfigured)
                {
                    _logger.LogError($"[DocumentAI] IA não configurada: {errorMessage}");
                    throw new InvalidOperationException(errorMessage);
                }

                var prompt = BuildAnalysisPrompt(documentText);
                _logger.LogInformation($"[DocumentAI] Prompt construído: {prompt.Length} caracteres");
                
                // Usar o AIService com SDK oficial do Google
                var response = await _aiService.GenerateContentAsync(prompt, maxTokens: 2048, temperature: 0.2);
                _logger.LogInformation($"[DocumentAI] Resposta recebida: {response.Length} caracteres");
                
                var analysis = ParseAnalysisResponse(response);
                _logger.LogInformation($"[DocumentAI] Análise parseada - LegalArea: {analysis.LegalArea}, ActionType: {analysis.ActionType}");
                
                analysis.FileName = fileName;
                analysis.AnalysisStatus = "Completed";
                analysis.AnalysisDate = DateTime.Now;

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DocumentAI] ERRO na análise");
                return new DocumentAnalysisViewModel
                {
                    FileName = fileName,
                    AnalysisStatus = "Error",
                    ErrorMessage = ex.Message,
                    AnalysisDate = DateTime.Now
                };
            }
        }

        private string BuildAnalysisPrompt(string documentText)
        {
            return $@"Você é um assistente jurídico especializado em análise de documentos legais brasileiros.

Analise o seguinte documento jurídico e extraia as informações em formato JSON estruturado:

DOCUMENTO:
{documentText}

Retorne APENAS um JSON válido com a seguinte estrutura (sem markdown, sem explicações):
{{
  ""summary"": ""Resumo executivo em 3-5 linhas"",
  ""legalArea"": ""Área do direito (Trabalhista, Cível, Tributário, Penal, etc)"",
  ""actionType"": ""Tipo específico de ação"",
  ""complexity"": ""Simples, Média ou Alta"",
  ""estimatedHours"": número estimado de horas,
  ""mainTopics"": [""tópico 1"", ""tópico 2"", ""tópico 3""],
  ""legalBasis"": [""Art. X da Lei Y"", ""Art. Z do Código W""],
  ""parties"": {{
    ""plaintiff"": ""Nome do autor/requerente"",
    ""defendant"": ""Nome do réu/requerido"",
    ""others"": [""outros envolvidos""]
  }},
  ""causeValue"": valor numérico ou null,
  ""deadlines"": [
    {{
      ""description"": ""Descrição do prazo"",
      ""days"": número de dias ou null
    }}
  ]
}}

IMPORTANTE: Retorne APENAS o JSON, sem texto adicional.";
        }

        private DocumentAnalysisViewModel ParseAnalysisResponse(string jsonResponse)
        {
            // Remove markdown code blocks se existirem
            jsonResponse = jsonResponse.Trim();
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Substring(7);
            }
            if (jsonResponse.StartsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(3);
            }
            if (jsonResponse.EndsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
            }
            jsonResponse = jsonResponse.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<JsonElement>(jsonResponse, options);

            var viewModel = new DocumentAnalysisViewModel
            {
                Summary = GetStringProperty(data, "summary"),
                LegalArea = GetStringProperty(data, "legalArea"),
                ActionType = GetStringProperty(data, "actionType"),
                Complexity = GetStringProperty(data, "complexity"),
                EstimatedHours = GetIntProperty(data, "estimatedHours"),
                MainTopics = GetStringArrayProperty(data, "mainTopics"),
                LegalBasis = GetStringArrayProperty(data, "legalBasis"),
                CauseValue = GetDecimalProperty(data, "causeValue"),
                Parties = ParseParties(data),
                Deadlines = ParseDeadlines(data)
            };

            return viewModel;
        }

        private PartyInfo ParseParties(JsonElement data)
        {
            try
            {
                if (data.TryGetProperty("parties", out var parties))
                {
                    return new PartyInfo
                    {
                        Plaintiff = GetStringProperty(parties, "plaintiff"),
                        Defendant = GetStringProperty(parties, "defendant"),
                        Others = GetStringArrayProperty(parties, "others")
                    };
                }
            }
            catch { }
            return new PartyInfo();
        }

        private List<DeadlineInfo> ParseDeadlines(JsonElement data)
        {
            var deadlines = new List<DeadlineInfo>();
            try
            {
                if (data.TryGetProperty("deadlines", out var deadlinesArray) && deadlinesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var deadline in deadlinesArray.EnumerateArray())
                    {
                        deadlines.Add(new DeadlineInfo
                        {
                            Description = GetStringProperty(deadline, "description"),
                            Days = GetIntProperty(deadline, "days")
                        });
                    }
                }
            }
            catch { }
            return deadlines;
        }

        private string GetStringProperty(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }
            catch { }
            return null;
        }

        private int? GetIntProperty(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    if (property.ValueKind == JsonValueKind.Number)
                    {
                        return property.GetInt32();
                    }
                }
            }
            catch { }
            return null;
        }

        private decimal? GetDecimalProperty(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    if (property.ValueKind == JsonValueKind.Number)
                    {
                        return property.GetDecimal();
                    }
                }
            }
            catch { }
            return null;
        }

        private List<string> GetStringArrayProperty(JsonElement element, string propertyName)
        {
            var list = new List<string>();
            try
            {
                if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            list.Add(item.GetString());
                        }
                    }
                }
            }
            catch { }
            return list;
        }
    }
}
