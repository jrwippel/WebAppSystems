using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Models;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProcessRecordsService _processRecordsService;

        public HomeController(ProcessRecordsService processRecordsService)
        {
            _processRecordsService = processRecordsService;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                //var chartData = _processRecordsService.GetChartData();
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
                else
                {
                    return BadRequest("Tipo de gráfico inválido.");
                }

                return Json(new
                {
                    labels = chartData.ClientNames, // Nomes (Clientes ou Áreas)
                    values = chartData.ClientValues // Valores correspondentes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocorreu um erro ao gerar os dados do gráfico.");
            }
        }





        public IActionResult About()
        {
            return View();
        }
    }
}