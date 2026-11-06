using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    // Unificação de clientes (origem -> destino) — acesso para Admin e Controladoria
    [PaginaParaAdminOuControladoria]
    public class ClientesDuplicadosController : Controller
    {
        private readonly ClienteDuplicadoService _service;
        private readonly ISessao _isessao;

        public ClientesDuplicadosController(ClienteDuplicadoService service, ISessao isessao)
        {
            _service = service;
            _isessao = isessao;
        }

        public IActionResult Index()
        {
            try
            {
                var usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.CurrentUserPerfil = usuario.Perfil;
                return View();
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpGet]
        public async Task<JsonResult> BuscarClientes(string termo)
        {
            var clientes = await _service.BuscarClientesAsync(termo);
            return Json(clientes);
        }

        [HttpGet]
        public async Task<JsonResult> Resumo(int clienteId)
        {
            var resumo = await _service.ResumoClienteAsync(clienteId);
            return Json(resumo);
        }

        [HttpGet]
        public async Task<JsonResult> Detalhes(int clienteId)
        {
            var itens = await _service.DetalhesLancamentosAsync(clienteId);
            return Json(itens);
        }

        [HttpGet]
        public async Task<JsonResult> Previa(int origemId, int destinoId)
        {
            var previa = await _service.ObterPreviaAsync(origemId, destinoId);
            return Json(previa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mover(int origemId, int destinoId)
        {
            var (sucesso, mensagem) = await _service.MoverAsync(origemId, destinoId);

            if (sucesso)
                TempData["MensagemSucesso"] = mensagem;
            else
                TempData["MensagemErro"] = mensagem;

            return RedirectToAction(nameof(Index));
        }
    }
}
