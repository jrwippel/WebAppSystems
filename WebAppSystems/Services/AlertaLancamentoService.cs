using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppSystems.Data;
using WebAppSystems.Helper;

namespace WebAppSystems.Services
{
    /// <summary>
    /// Background service que roda diariamente às 8h e envia alertas
    /// para usuários que estão há mais de 48h sem lançar horas,
    /// copiando o gestor da respectiva área.
    /// </summary>
    public class AlertaLancamentoService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertaLancamentoService> _logger;

        public AlertaLancamentoService(IServiceProvider serviceProvider, ILogger<AlertaLancamentoService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertaLancamentoService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var agora = DateTime.Now;

                // Calcula o próximo disparo às 8h do próximo dia útil
                var proximoDisparo = new DateTime(agora.Year, agora.Month, agora.Day, 8, 0, 0);
                if (agora >= proximoDisparo)
                    proximoDisparo = proximoDisparo.AddDays(1);

                // Pular fins de semana — só dispara em dias úteis (seg-sex)
                while (proximoDisparo.DayOfWeek == DayOfWeek.Saturday || proximoDisparo.DayOfWeek == DayOfWeek.Sunday)
                    proximoDisparo = proximoDisparo.AddDays(1);

                var delay = proximoDisparo - agora;
                _logger.LogInformation("Próximo envio de alertas de lançamento: {Hora}", proximoDisparo);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                    await EnviarAlertasAsync();
            }
        }

        private async Task EnviarAlertasAsync()
        {
            _logger.LogInformation("Executando verificação de alertas de lançamento...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WebAppSystemsContext>();
            var email = scope.ServiceProvider.GetRequiredService<IEmail>();

            var limiteHoras = 48;
            var hoje = DateTime.Now.Date;

            var usuarios = await context.Attorney
                .Include(a => a.Department)
                .Where(a => !a.Inativo && !a.IsGestor)
                .ToListAsync();

            // Buscar último lançamento de cada usuário
            var ultimosLancamentos = await context.ProcessRecord
                .Where(p => p.HoraFinal != TimeSpan.Zero)
                .GroupBy(p => p.AttorneyId)
                .Select(g => new { AttorneyId = g.Key, Ultimo = g.Max(p => p.Date) })
                .ToListAsync();

            // Buscar gestores por área
            var gestores = await context.Attorney
                .Where(a => a.IsGestor && !a.Inativo)
                .ToListAsync();

            var alertasEnviados = 0;

            foreach (var usuario in usuarios)
            {
                var ultimo = ultimosLancamentos.FirstOrDefault(u => u.AttorneyId == usuario.Id);
                var dataUltimo = ultimo?.Ultimo ?? DateTime.MinValue;

                // Verifica se o último lançamento foi há mais de 2 dias úteis
                if (dataUltimo == DateTime.MinValue || ContarDiasUteis(dataUltimo.Date, hoje) >= 2)
                {
                    var diasSem = dataUltimo == DateTime.MinValue
                        ? "nunca lançou horas"
                        : $"último lançamento em {dataUltimo:dd/MM/yyyy}";

                    var gestor = gestores.FirstOrDefault(g => g.DepartmentId == usuario.DepartmentId);

                    // Só envia se existir gestor na área
                    if (gestor == null) continue;

                    // Um único email para o usuário, com CC para o gestor da área (se existir)
                    var assunto = "⏰ Lembrete: Regularize seus lançamentos de horas";

                    // Calcular dias sem lançamento
                    var diasSemLancamento = dataUltimo == DateTime.MinValue ? 999 : (DateTime.Now - dataUltimo).Days;
                    var ultimoLancamentoTexto = dataUltimo == DateTime.MinValue ? "Nenhum lançamento encontrado" : dataUltimo.ToString("dd/MM/yyyy (dddd)");

                    // Listar dias úteis que faltam lançar
                    var diasFaltantes = new List<string>();
                    for (int d = 1; d <= 10 && diasFaltantes.Count < 5; d++)
                    {
                        var dia = DateTime.Now.AddDays(-d);
                        if (dia.DayOfWeek != DayOfWeek.Saturday && dia.DayOfWeek != DayOfWeek.Sunday && dia.Date > dataUltimo.Date)
                            diasFaltantes.Add(dia.ToString("dd/MM/yyyy (dddd)"));
                    }
                    var diasFaltantesHtml = diasFaltantes.Any()
                        ? string.Join("", diasFaltantes.Select(df => $"<li style='padding:4px 0;color:#4a5568;'>{df}</li>"))
                        : "<li style='padding:4px 0;color:#48bb78;'>Nenhum dia pendente identificado</li>";

                    var htmlUsuario = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f4f6f9;font-family:Arial,Helvetica,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6f9;padding:40px 0;'>
<tr><td align='center'>
<table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>
<tr>
<td style='background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:32px 40px;text-align:center;'>
  <div style='font-size:32px;margin-bottom:8px;'>⏱️</div>
  <h1 style='color:white;margin:0;font-size:22px;font-weight:700;'>TimeSheet</h1>
  <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;font-size:13px;'>Sistema de Controle de Horas</p>
</td>
</tr>
<tr>
<td style='padding:36px 40px;'>
  <h2 style='color:#2d3748;font-size:18px;margin:0 0 16px;'>Olá, {usuario.Name}!</h2>
  <p style='font-size:14px;color:#4a5568;line-height:1.6;margin:0 0 20px;'>
    Identificamos que você está <strong style='color:#e53e3e;'>há {diasSemLancamento} dia{(diasSemLancamento > 1 ? "s" : "")}</strong> sem registrar lançamentos no sistema.
  </p>
  <div style='background:#f7fafc;border-left:4px solid #667eea;border-radius:6px;padding:14px 18px;margin-bottom:20px;'>
    <div style='font-size:11px;text-transform:uppercase;letter-spacing:0.5px;color:#718096;font-weight:600;margin-bottom:6px;'>Último Lançamento</div>
    <div style='font-size:15px;color:#2d3748;font-weight:600;'>{ultimoLancamentoTexto}</div>
  </div>
  <div style='background:#fff5f5;border-left:4px solid #e53e3e;border-radius:6px;padding:14px 18px;margin-bottom:24px;'>
    <div style='font-size:11px;text-transform:uppercase;letter-spacing:0.5px;color:#e53e3e;font-weight:600;margin-bottom:8px;'>Dias sem lançamento</div>
    <ul style='margin:0;padding:0 0 0 18px;font-size:13px;'>{diasFaltantesHtml}</ul>
  </div>
  <div style='text-align:center;margin-top:28px;'>
    <a href='https://ecadvogados.azurewebsites.net' style='display:inline-block;padding:14px 32px;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);color:white;border-radius:8px;text-decoration:none;font-weight:600;font-size:14px;'>Acessar o TimeSheet</a>
  </div>
</td>
</tr>
<tr>
<td style='background:#f7fafc;padding:24px 40px;text-align:center;border-top:1px solid #e2e8f0;'>
  <p style='margin:0 0 6px;font-size:13px;color:#4a5568;font-weight:600;'>TimeSheet — Sistema de Controle de Horas</p>
  <p style='margin:0 0 4px;font-size:11px;color:#a0aec0;'>Eberhardt, Carrascoza, Bossi, Silva, Matteussi & Costa Beber Advogados</p>
  <p style='margin:0;font-size:10px;color:#cbd5e0;'>Este é um email automático. Não responda.</p>
</td>
</tr>
</table>
</td></tr>
</table>
</body>
</html>";

                    var emailCc = gestor?.Email;
                    var enviou = await email.EnviarAsync(
                        usuario.Email,
                        assunto,
                        $"Olá {usuario.Name}, você está há mais de {limiteHoras}h sem lançamentos. ({diasSem})",
                        htmlBody: htmlUsuario,
                        emailCc: emailCc);

                    if (enviou) alertasEnviados++;
                }
            }

            _logger.LogInformation("Alertas de lançamento enviados: {Total}", alertasEnviados);
        }

        private static int ContarDiasUteis(DateTime inicio, DateTime fim)
        {
            var dias = 0;
            var atual = inicio.AddDays(1); // começa no dia seguinte ao último lançamento
            while (atual <= fim)
            {
                if (atual.DayOfWeek != DayOfWeek.Saturday && atual.DayOfWeek != DayOfWeek.Sunday)
                    dias++;
                atual = atual.AddDays(1);
            }
            return dias;
        }
    }
}
