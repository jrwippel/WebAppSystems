using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebAppSystems.Models.ViewModels;

namespace WebAppSystems.Services
{
    public class AIDocumentAnalysisService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
        private readonly ILogger<AIDocumentAnalysisService> _logger;

        public AIDocumentAnalysisService(IConfiguration configuration, HttpClient httpClient, ILogger<AIDocumentAnalysisService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _apiKey = _configuration["GoogleAI:ApiKey"];
            _logger = logger;
        }

        public async Task<DocumentAnalysisViewModel> AnalyzeDocumentAsync(string documentText, string fileName)
        {
            try
            {
                _logger.LogInformation($"[AIService] Iniciando análise do documento: {fileName}");
                _logger.LogInformation($"[AIService] Tamanho do texto: {documentText.Length} caracteres");
                
                var prompt = BuildAnalysisPrompt(documentText);
                _logger.LogInformation($"[AIService] Prompt construído: {prompt.Length} caracteres");
                
                var response = await CallGeminiAPIAsync(prompt);
                _logger.LogInformation($"[AIService] Resposta recebida da API: {response.Length} caracteres");
                _logger.LogInformation($"[AIService] Resposta completa: {response}");
                
                var analysis = ParseAnalysisResponse(response);
                _logger.LogInformation($"[AIService] Análise parseada - Summary: {analysis.Summary?.Substring(0, Math.Min(50, analysis.Summary?.Length ?? 0))}");
                _logger.LogInformation($"[AIService] LegalArea: {analysis.LegalArea}, ActionType: {analysis.ActionType}");
                
                analysis.FileName = fileName;
                analysis.AnalysisStatus = "Completed";
                analysis.AnalysisDate = DateTime.Now;

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AIService] ERRO na análise");
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

        private async Task<string> CallGeminiAPIAsync(string prompt)
        {
            _logger.LogInformation($"[AIService] Verificando API Key...");
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("API Key do Google AI não configurada. Configure GoogleAI:ApiKey no appsettings.json");
            }
            _logger.LogInformation($"[AIService] API Key configurada: {_apiKey.Substring(0, 10)}...");

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 2048
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation($"[AIService] Request body preparado: {json.Length} caracteres");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_apiUrl}?key={_apiKey}";
            _logger.LogInformation($"[AIService] Enviando requisição para: {_apiUrl}");

            var response = await _httpClient.PostAsync(url, content);
            _logger.LogInformation($"[AIService] Status da resposta: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[AIService] Erro na API: {error}");
                throw new Exception($"Erro na API do Google AI: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"[AIService] Resposta JSON recebida: {responseJson.Length} caracteres");
            
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            var text = result
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            _logger.LogInformation($"[AIService] Texto extraído da resposta: {text.Length} caracteres");
            return text;
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
