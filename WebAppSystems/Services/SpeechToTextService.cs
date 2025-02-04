using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using WebAppSystems.Services;

public class SpeechToTextService : ISpeechToTextService
{
    private readonly string subscriptionKey = "BwI6nZ1nb0q3qwTHASYNohHlNWkcpkmEHoHEsN7Pl2ywORIVZCvpJQQJ99ALACYeBjFXJ3w3AAAYACOGW561"; // Chave do Azure Speech
    private readonly string region = "eastus";     // Região do Azure Speech

    public async Task<string> TranscribeAudioAsync(string audioFilePath)
    {

        try
        {
            var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);           


            

            var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);


            var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // Defina o evento de cancelamento
            recognizer.Canceled += (s, e) =>
            {
                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"Erro: {e.ErrorDetails}");
                }
            };

            // Realiza o reconhecimento de fala de forma síncrona ou assíncrona
            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                return result.Text;
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                return "Erro no reconhecimento: ";
            }
            else
            {
                return "Fala não reconhecida ou erro desconhecido.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            throw;
        }
    }
}
