using Microsoft.AspNetCore.Mvc;
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