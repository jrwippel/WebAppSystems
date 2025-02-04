using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebAppSystems.Services;

public class SummaryService : ISummaryService
{
    private readonly HttpClient _httpClient;

    public SummaryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateSummaryAsync(string text)
    {
        var requestBody = new { prompt = text, max_tokens = 200 }; // Ajuste conforme necessário
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/engines/davinci/completions", content);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();

        var json = JsonSerializer.Deserialize<JsonElement>(result);
        return json.GetProperty("choices")[0].GetProperty("text").GetString();
    }
}
