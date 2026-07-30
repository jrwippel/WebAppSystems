using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Helper;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    public class AjudaController : Controller
    {
        private readonly ISessao _sessao;

        public AjudaController(ISessao sessao)
        {
            _sessao = sessao;
        }

        public IActionResult Index()
        {
            try
            {
                var usuario = _sessao.BuscarSessaoDoUsuario();
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
