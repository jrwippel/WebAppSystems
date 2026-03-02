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
                return View(chartData);
            }
            catch (SessionExpiredException)
            {
                // Redirecione para a página de login se a sessão expirou
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