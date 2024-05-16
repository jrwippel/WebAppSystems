using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Models;
using WebAppSystems.Services;

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
            var chartData = _processRecordsService.GetChartData();

            return View(chartData);
        }
        public IActionResult About()
        {           
            return View();
        }
    }
}
