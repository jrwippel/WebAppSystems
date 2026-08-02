using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProcessRecordsService _processRecordsService;
        private readonly ISessao _isessao;

        public HomeController(ProcessRecordsService processRecordsService, ISessao isessao)
        {
            _processRecordsService = processRecordsService;
            _isessao = isessao;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                ViewBag.CurrentUserPerfil = usuario.Perfil;

                // Admin: redirect para Rentabilidade desabilitado temporariamente
                // if (usuario.Perfil == Models.Enums.ProfileEnum.Admin && !Request.Query.ContainsKey("tab"))
                // {
                //     return RedirectToAction("Rentabilidade", "Mensalista");
                // }

                var chartData = _processRecordsService.GetChartData();

                // KPIs
                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

                var registrosHoje = await _processRecordsService.GetFinishedRecordsByDateAsync(today, today);
                var registrosMes = await _processRecordsService.GetFinishedRecordsByDateAsync(firstDayOfMonth, today);
                var registrosOntem = await _processRecordsService.GetFinishedRecordsByDateAsync(today.AddDays(-1), today.AddDays(-1));

                var horasHoje = registrosHoje.Sum(r => r.CalculoHorasDecimal());
                var horasMes = registrosMes.Sum(r => r.CalculoHorasDecimal());
                var horasOntem = registrosOntem.Sum(r => r.CalculoHorasDecimal());
                var clientesAtivos = registrosMes.Select(r => r.ClientId).Distinct().Count();

                ViewBag.HorasHoje = horasHoje;
                ViewBag.HorasMes = horasMes;
                ViewBag.HorasOntem = horasOntem;
                ViewBag.RegistrosHoje = registrosHoje.Count;
                ViewBag.ClientesAtivos = clientesAtivos;

                return View(chartData);
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string type)
        {
            try
            {
                ChartData chartData;

                if (type == "cliente")
                {
                    chartData = _processRecordsService.GetChartData();
                }
                else if (type == "tipo")
                {
                    chartData = _processRecordsService.GetChartDataByRecordType();
                }
                else if (type == "area")
                {
                    chartData = _processRecordsService.GetChartDataByArea();
                }
                else if (type == "timeline")
                {
                    string period = Request.Query["period"].ToString();
                    if (string.IsNullOrEmpty(period)) period = "month";
                    chartData = _processRecordsService.GetChartDataByTimeline(period);
                }
                else
                {
                    return BadRequest("Tipo de gráfico inválido.");
                }

                return Json(new
                {
                    labels = chartData.ClientNames,
                    values = chartData.ClientValues
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocorreu um erro ao gerar os dados do gráfico.");
            }
        }





        // TEMPORÁRIO: endpoint para testar alerta de lançamentos manualmente
        [Route("TestarAlerta")]
        public async Task<IActionResult> TestarAlerta()
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WebAppSystems.Data.WebAppSystemsContext>();
            var email = scope.ServiceProvider.GetRequiredService<WebAppSystems.Helper.IEmail>();

            var limiteHoras = 48;
            var limite = DateTime.Now.AddHours(-limiteHoras);

            var usuarios = await context.Attorney
                .Include(a => a.Department)
                .Where(a => !a.Inativo && !a.IsGestor)
                .ToListAsync();

            var ultimosLancamentos = await context.ProcessRecord
                .Where(p => p.HoraFinal != TimeSpan.Zero)
                .GroupBy(p => p.AttorneyId)
                .Select(g => new { AttorneyId = g.Key, Ultimo = g.Max(p => p.Date) })
                .ToListAsync();

            var gestores = await context.Attorney
                .Where(a => a.IsGestor && !a.Inativo)
                .ToListAsync();

            var resultados = new List<string>();

            foreach (var usuario in usuarios)
            {
                var ultimo = ultimosLancamentos.FirstOrDefault(u => u.AttorneyId == usuario.Id);
                var dataUltimo = ultimo?.Ultimo ?? DateTime.MinValue;

                if (dataUltimo <= limite)
                {
                    var diasSem = dataUltimo == DateTime.MinValue ? "nunca lançou" : $"último: {dataUltimo:dd/MM/yyyy}";
                    var gestor = gestores.FirstOrDefault(g => g.DepartmentId == usuario.DepartmentId);

                    // Só envia se existir gestor na área
                    if (gestor == null) continue;

                    resultados.Add($"✉️ {usuario.Name} ({usuario.Email}) — {diasSem} — CC: {gestor.Name} ({gestor.Email})");

                    // Calcular dias sem lançamento
                    var diasSemLancamento = dataUltimo == DateTime.MinValue ? 999 : (DateTime.Now - dataUltimo).Days;
                    var ultimoLancamentoTexto = dataUltimo == DateTime.MinValue ? "Nenhum lançamento encontrado" : dataUltimo.ToString("dd/MM/yyyy (dddd)");

                    // Listar dias úteis que faltam lançar (últimos 7 dias úteis)
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

                    var assunto = "⏰ Lembrete: Regularize seus lançamentos de horas";
                    var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f4f6f9;font-family:Arial,Helvetica,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6f9;padding:40px 0;'>
<tr><td align='center'>
<table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

<!-- Header com gradiente do sistema -->
<tr>
<td style='background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:32px 40px;text-align:center;'>
  <div style='font-size:32px;margin-bottom:8px;'>⏱️</div>
  <h1 style='color:white;margin:0;font-size:22px;font-weight:700;letter-spacing:0.5px;'>TimeSheet</h1>
  <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;font-size:13px;'>Sistema de Controle de Horas</p>
</td>
</tr>

<!-- Corpo -->
<tr>
<td style='padding:36px 40px;'>
  <h2 style='color:#2d3748;font-size:18px;margin:0 0 16px;'>Olá, {usuario.Name}!</h2>
  
  <p style='font-size:14px;color:#4a5568;line-height:1.6;margin:0 0 20px;'>
    Identificamos que você está <strong style='color:#e53e3e;'>há {diasSemLancamento} dia{(diasSemLancamento > 1 ? "s" : "")}</strong> sem registrar lançamentos no sistema.
    Por favor, regularize o quanto antes.
  </p>

  <!-- Card de último lançamento -->
  <div style='background:#f7fafc;border-left:4px solid #667eea;border-radius:6px;padding:14px 18px;margin-bottom:20px;'>
    <div style='font-size:11px;text-transform:uppercase;letter-spacing:0.5px;color:#718096;font-weight:600;margin-bottom:6px;'>Último Lançamento</div>
    <div style='font-size:15px;color:#2d3748;font-weight:600;'>{ultimoLancamentoTexto}</div>
  </div>

  <!-- Dias pendentes -->
  <div style='background:#fff5f5;border-left:4px solid #e53e3e;border-radius:6px;padding:14px 18px;margin-bottom:24px;'>
    <div style='font-size:11px;text-transform:uppercase;letter-spacing:0.5px;color:#e53e3e;font-weight:600;margin-bottom:8px;'>Dias sem lançamento</div>
    <ul style='margin:0;padding:0 0 0 18px;font-size:13px;'>
      {diasFaltantesHtml}
    </ul>
  </div>

  <!-- Botão -->
  <div style='text-align:center;margin-top:28px;'>
    <a href='https://ecadvogados.azurewebsites.net' style='display:inline-block;padding:14px 32px;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);color:white;border-radius:8px;text-decoration:none;font-weight:600;font-size:14px;box-shadow:0 4px 12px rgba(102,126,234,0.3);'>
      Acessar o TimeSheet
    </a>
  </div>
</td>
</tr>

<!-- Rodapé -->
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

                    await email.EnviarAsync(usuario.Email, assunto, $"Olá {usuario.Name}, regularize seus lançamentos. ({diasSem})", htmlBody: html, emailCc: gestor.Email);
                }
            }

            if (!resultados.Any())
                return Content("Nenhum usuário elegível para alerta (todos lançaram nas últimas 48h).");

            return Content(string.Join("\n", resultados), "text/plain");
        }

        public IActionResult About()
        {
            try
            {
                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                ViewBag.CurrentUserPerfil = usuario.Perfil;
                return View();
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }
    }
}