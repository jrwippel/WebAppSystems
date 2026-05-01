using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebAppSystems.Data;

namespace WebAppSystems.Services
{
    public class AIService
    {
        private readonly WebAppSystemsContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public AIService(WebAppSystemsContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(bool isConfigured, string errorMessage)> IsConfiguredAsync()
        {
            try
            {
                var config = await _context.AIConfiguration.FirstOrDefaultAsync();
                
                System.Diagnostics.Debug.WriteLine($"[AIService] IsConfigured - Config encontrada: {config != null}");
                if (config != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] Provider: {config.Provider}, IsActive: {config.IsActive}, HasApiKey: {!string.IsNullOrWhiteSpace(config.ApiKey)}");
                }
                
                if (config == null || !config.IsActive)
                {
                    return (false, "IA não configurada. Acesse as Configurações de IA para ativar.");
                }

                if (string.IsNullOrWhiteSpace(config.ApiKey))
                {
                    return (false, "Chave da API não configurada. Acesse as Configurações de IA.");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] Erro ao verificar configuração: {ex.Message}");
                return (false, "Erro ao verificar configuração de IA.");
            }
        }

        public async Task<string> GenerateContentAsync(string prompt, int maxTokens = 1024, double temperature = 0.3)
        {
            var config = await _context.AIConfiguration.FirstOrDefaultAsync();
            
            if (config == null || !config.IsActive || string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException("IA não configurada ou inativa.");
            }

            switch (config.Provider)
            {
                case "GoogleGemini":
                    return await CallGoogleGeminiAsync(config.ApiKey, config.Model, prompt, maxTokens, temperature);
                
                case "OpenAI":
                    return await CallOpenAIAsync(config.ApiKey, config.Model, prompt, maxTokens, temperature);
                
                case "Anthropic":
                    return await CallAnthropicAsync(config.ApiKey, config.Model, prompt, maxTokens, temperature);
                
                case "Groq":
                    return await CallGroqAsync(config.ApiKey, config.Model, prompt, maxTokens, temperature);
                
                default:
                    throw new NotSupportedException($"Provedor '{config.Provider}' não suportado.");
            }
        }

        private async Task<string> CallGoogleGeminiAsync(string apiKey, string model, string prompt, int maxTokens, double temperature)
        {
            try
            {
                // Modelos disponíveis confirmados via ListModels:
                // gemini-2.0-flash, gemini-2.0-flash-lite, gemini-2.5-flash, gemini-2.5-pro
                // gemini-1.5-x não existe mais nesta chave
                var modelName = model.ToLower().Replace("-latest", "") switch
                {
                    "gemini-2.5-pro"       => "gemini-2.5-pro",
                    "gemini-2.5-flash"     => "gemini-2.5-flash",
                    "gemini-2.0-flash"     => "gemini-2.0-flash",
                    "gemini-2.0-flash-lite"=> "gemini-2.0-flash-lite",
                    "gemini-1.5-flash"     => "gemini-2.0-flash",
                    "gemini-1.5-pro"       => "gemini-2.0-flash",
                    "gemini-pro"           => "gemini-2.0-flash",
                    _                      => "gemini-2.0-flash"
                };
                
                System.Diagnostics.Debug.WriteLine($"[AIService] Modelo original: {model}");
                System.Diagnostics.Debug.WriteLine($"[AIService] Modelo mapeado: {modelName}");
                
                // Usar API REST v1 diretamente
                var http = _httpClientFactory.CreateClient();
                var url = $"https://generativelanguage.googleapis.com/v1/models/{modelName}:generateContent?key={apiKey}";
                
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
                        temperature,
                        maxOutputTokens = maxTokens
                    }
                };
                
                var json = JsonSerializer.Serialize(requestBody);
                System.Diagnostics.Debug.WriteLine($"[AIService] Chamando API REST v1: {url.Replace(apiKey, "***")}");
                
                var response = await http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] Erro HTTP {response.StatusCode}: {responseBody}");
                    throw new HttpRequestException($"Erro na API Google Gemini: {response.StatusCode} - {responseBody}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[AIService] Resposta recebida com sucesso");
                
                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var text = result
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                
                return text?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] Erro ao chamar Google Gemini: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIService] Tipo de exceção: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[AIService] Stack: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] Inner Exception: {ex.InnerException.Message}");
                }
                
                throw new HttpRequestException($"Erro na API Google Gemini: {ex.Message}", ex);
            }
        }

        private async Task<string> CallOpenAIAsync(string apiKey, string model, string prompt, int maxTokens, double temperature)
        {
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var body = new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = maxTokens,
                temperature
            };
            
            var json = JsonSerializer.Serialize(body);
            var url = "https://api.openai.com/v1/chat/completions";
            var response = await http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Erro na API OpenAI: {response.StatusCode} - {errBody}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            return result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim() ?? string.Empty;
        }

        private async Task<string> CallAnthropicAsync(string apiKey, string model, string prompt, int maxTokens, double temperature)
        {
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            
            var body = new
            {
                model,
                max_tokens = maxTokens,
                temperature,
                messages = new[] { new { role = "user", content = prompt } }
            };
            
            var json = JsonSerializer.Serialize(body);
            var url = "https://api.anthropic.com/v1/messages";
            var response = await http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Erro na API Anthropic: {response.StatusCode} - {errBody}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            return result
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString()?.Trim() ?? string.Empty;
        }

        private async Task<string> CallGroqAsync(string apiKey, string model, string prompt, int maxTokens, double temperature)
        {
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var body = new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = maxTokens,
                temperature
            };
            
            var json = JsonSerializer.Serialize(body);
            var url = "https://api.groq.com/openai/v1/chat/completions";
            var response = await http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Erro na API Groq: {response.StatusCode} - {errBody}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            return result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim() ?? string.Empty;
        }
    }
}
