using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.IO;
using System.Threading.Tasks;
using WebAppSystems.Services;
using NAudio.Wave;

using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Net;

public class MeetingController : Controller
{
    private readonly ISpeechToTextService _speechToTextService;
    private readonly ISummaryService _summaryService;
    private readonly HttpClient _httpClient;
    public MeetingController(ISpeechToTextService speechToTextService, ISummaryService summaryService, HttpClient httpClient)
    {
        _speechToTextService = speechToTextService;
        _summaryService = summaryService;
        _httpClient = httpClient; // Injetando o HttpClient
    }

    public IActionResult Index()
    {
        return View();
    }

    public static async Task<string> RecognitionWithPushAudioStreamAsync()
    {
        var waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

        var config = SpeechConfig.FromSubscription("BwI6nZ1nb0q3qwTHASYNohHlNWkcpkmEHoHEsN7Pl2ywORIVZCvpJQQJ99ALACYeBjFXJ3w3AAAYACOGW561", "eastus");


        // Defina o idioma para português (Brasil)
        config.SpeechRecognitionLanguage = "pt-BR";

        var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        string transcribedText = string.Empty;

        using (var pushStream = AudioInputStream.CreatePushStream())
        {
            using (var audioInput = AudioConfig.FromStreamInput(pushStream))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    recognizer.Recognizing += (s, e) =>
                    {
                        Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                    };

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            transcribedText = e.Result.Text; // Armazena o texto transcrito
                            stopRecognition.TrySetResult(0);
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");
                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        }
                        stopRecognition.TrySetResult(0);
                    };

                    waveIn.DataAvailable += (s, e) =>
                    {
                        if (e.BytesRecorded != 0)
                        {
                            pushStream.Write(e.Buffer);
                        }
                    };

                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                    waveIn.StartRecording();

                    Task.WaitAny(new[] { stopRecognition.Task });

                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    waveIn.StopRecording();
                }
            }
        }

        return transcribedText; // Retorna o texto transcrito
    }

    // Método para chamar a API do OpenAI e gerar o resumo
    public async Task<string> GenerateSummaryFromAzure(string transcribedText)
    {
        var apiKey = "FkNXNwXBOS7Re9TozmdtxJ5K2VCf8QwLMFO3QLBg3KmlOHm33eOYJQQJ99ALACYeBjFXJ3w3AAAEACOG6aG3";
        var endpoint = "https://generatesummaryfromopenai.cognitiveservices.azure.com/language/analyze-text/jobs?api-version=2023-04-01";

        // Montar o corpo da requisição para a primeira API
        var requestBody = new
        {
            displayName = "Text Abstractive Summarization Task Example",
            analysisInput = new
            {
                documents = new[]
                {
                new
                {
                    id = "1",
                    language = "pt",
                    text = transcribedText
                }
            }
            },
            tasks = new[]
            {
            new
            {
                kind = "AbstractiveSummarization",
                taskName = "Text Abstractive Summarization Task 1"
            }
        }
        };

        using var httpClient = new HttpClient();

        // Configurar a primeira requisição
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };
        requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

        try
        {
            // Enviar a primeira requisição
            var response = await httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erro na primeira API: {response.StatusCode}, {errorContent}");
                return "Erro ao enviar o texto para a API.";
            }

            // Obter o cabeçalho operation-location
            if (!response.Headers.TryGetValues("operation-location", out var operationLocationValues))
            {
                return "Erro: Cabeçalho 'operation-location' não encontrado na resposta.";
            }

            var operationLocation = operationLocationValues.FirstOrDefault();
            if (string.IsNullOrEmpty(operationLocation))
            {
                return "Erro: URL do Job não retornada pela API.";
            }

            // Verificar o status do job e obter o resumo
            string summary = null;
            for (int i = 0; i < 10; i++) // Tentativas limitadas
            {
                var statusRequestMessage = new HttpRequestMessage(HttpMethod.Get, operationLocation);
                statusRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

                var statusResponse = await httpClient.SendAsync(statusRequestMessage);

                if (!statusResponse.IsSuccessStatusCode)
                {
                    var errorStatusContent = await statusResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Erro na verificação do status: {statusResponse.StatusCode}, {errorStatusContent}");
                    return "Erro ao verificar o status do resumo.";
                }

                var statusContent = await statusResponse.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(statusContent))
                {
                    return "Erro: Resposta vazia da API ao verificar o status.";
                }

                var statusJson = JsonDocument.Parse(statusContent);
                var status = statusJson.RootElement.GetProperty("status").GetString();

                if (status == "succeeded")
                {
                    try
                    {
                        // Validar e acessar as propriedades do JSON
                        if (statusJson.RootElement.TryGetProperty("tasks", out var tasksElement) &&
                            tasksElement.TryGetProperty("items", out var itemsElement) &&
                            itemsElement.GetArrayLength() > 0)
                        {
                            var firstTask = itemsElement[0];

                            if (firstTask.TryGetProperty("results", out var resultsElement) &&
                                resultsElement.TryGetProperty("documents", out var documentsElement) &&
                                documentsElement.GetArrayLength() > 0)
                            {
                                var firstDocument = documentsElement[0];

                                if (firstDocument.TryGetProperty("summaries", out var summariesElement) &&
                                    summariesElement.GetArrayLength() > 0)
                                {
                                    // Obter o texto do primeiro resumo
                                    summary = summariesElement[0]
                                        .GetProperty("text")
                                        .GetString();
                                }
                                else
                                {
                                    return "Erro: Estrutura de 'summaries' ausente ou inválida.";
                                }
                            }
                            else
                            {
                                return "Erro: Estrutura de 'results.documents' ausente ou inválida.";
                            }
                        }
                        else
                        {
                            return "Erro: Estrutura de 'tasks.items' ausente ou inválida.";
                        }

                        break; // Saída do loop se o resumo foi obtido
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao processar JSON: {ex.Message}");
                        return "Erro ao processar a resposta JSON.";
                    }
                }


                // Aguardar antes de tentar novamente
                await Task.Delay(5000);
            }

            return summary ?? "Erro: Não foi possível obter o resumo.";
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro na requisição: {ex.Message}");
            return "Erro na requisição HTTP.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex.Message}");
            return "Erro inesperado ao processar a resposta.";
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateSummary()
    {
        try
        {
            // Obter o arquivo de áudio enviado
            var audioFile = Request.Form.Files.FirstOrDefault();
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("Arquivo de áudio inválido.");
            }

            // Salvar temporariamente o arquivo
            var tempFilePath = Path.Combine(Path.GetTempPath(), audioFile.FileName);
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(fileStream);
            }

            // Transcrever o áudio
            var transcribedText = await RecognitionWithPushAudioStreamAsync();

            // Gerar o resumo
            var summary = await GenerateSummaryFromAzure(transcribedText);

            // Excluir o arquivo temporário
            System.IO.File.Delete(tempFilePath);

            return Json(new { success = true, summary });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            return StatusCode(500, "Erro interno no servidor.");
        }
    }




    // Uso na ação do controller:
    [HttpPost]  
    public async Task<IActionResult> GenerateSummary1(IFormFile audioFile)
    {
        try
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("Por favor, envie um arquivo de áudio válido.");
            }

            // Salvar arquivo temporário
            var filePath = Path.Combine(Path.GetTempPath(), audioFile.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // Chama o reconhecimento de fala
            var transcribedText = await RecognitionWithPushAudioStreamAsync();

            // Chama a API do OpenAI para gerar o resumo
            var summary = await GenerateSummaryFromAzure(transcribedText);

            // Remover arquivo temporário
            System.IO.File.Delete(filePath);

            // Retornar o resumo como texto
            return Ok(summary);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro em GenerateSummary: {ex.Message}, StackTrace: {ex.StackTrace}");
            return StatusCode(500, "Erro interno no servidor ao gerar o resumo.");
        }
    }
 
    [HttpPost]
    public async Task<IActionResult> ProcessFile(IFormFile uploadedFile)
    {
        if (uploadedFile == null || uploadedFile.Length == 0)
        {
            return BadRequest("Nenhum arquivo foi enviado ou o arquivo está vazio.");
        }

        try
        {
            // Lê o conteúdo do arquivo
            using var stream = new MemoryStream();
            await uploadedFile.CopyToAsync(stream);
            var fileContent = Encoding.UTF8.GetString(stream.ToArray());

            // Define os dados para a API
            var apiKey = "FkNXNwXBOS7Re9TozmdtxJ5K2VCf8QwLMFO3QLBg3KmlOHm33eOYJQQJ99ALACYeBjFXJ3w3AAAEACOG6aG3";
            var endpoint = "https://generatesummaryfromopenai.cognitiveservices.azure.com/language/analyze-text/jobs?api-version=2023-04-01";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            // Prepara o payload
            var requestBody = new
            {
                analysisInput = new
                {
                    documents = new[]
                    {
                    new { id = "1", language = "en", text = fileContent }
                }
                },
                tasks = new[]
                {
                new { kind = "ExtractiveSummarization", parameters = new { sentenceCount = 3 } }
            }
            };

            // Serializa o payload
            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Faz a chamada para a API
            var response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            // Lê a resposta da API
            var responseContent = await response.Content.ReadAsStringAsync();

            // Retorna o resumo para a tela
            return Json(new { success = true, summary = responseContent });
        }
        catch (Exception ex)
        {
            // Em caso de erro
            return Json(new { success = false, message = ex.Message });
        }
    }
}
