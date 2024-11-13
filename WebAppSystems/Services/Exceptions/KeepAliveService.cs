namespace WebAppSystems.Services.Exceptions
{
    public class KeepAliveService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly HttpClient _httpClient;

        public KeepAliveService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Define um intervalo para chamar o KeepAlive periodicamente
            _timer = new Timer(KeepAlive, null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); // Executa a cada 5 minutos
            return Task.CompletedTask;
        }

        private async void KeepAlive(object state)
        {
            try
            {
                // Chama o endpoint KeepAlive
                var response = await _httpClient.GetAsync("https://ecadvogados.azurewebsites.net/KeepAlive");
                //var response = await _httpClient.GetAsync("http://localhost:8000/KeepAlive");
                if (response.IsSuccessStatusCode)
                {
                    // Sucesso
                    Console.WriteLine("Keep-alive executado com sucesso.");
                }
                else
                {
                    // Caso não consiga manter o keep-alive
                    Console.WriteLine("Erro ao executar keep-alive.");
                }
            }
            catch (Exception ex)
            {
                // Log de erro em caso de falha na chamada
                Console.WriteLine($"Erro no Keep-alive: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
