using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Filters;

namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    public class RestritoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
