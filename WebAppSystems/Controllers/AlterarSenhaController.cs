using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    public class AlterarSenhaController : Controller
    {
        private readonly AttorneyService _attorneyService;
        private readonly ISessao _sessao;

        public AlterarSenhaController(AttorneyService attorneyService, ISessao sessao)
        {
            _attorneyService = attorneyService;
            _sessao = sessao;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]

        public IActionResult Alterar(AlterarSenhaModel alterarSenhaModel)
        {
            try
            {
                // Verifica se há um usuário logado antes de acessar a sessão
                Attorney usuarioLogado = _sessao.BuscarSessaoDoUsuario();

                if (usuarioLogado == null)
                {
                    // Redireciona para o login se a sessão não está ativa
                    TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                    return RedirectToAction("Index", "Login");
                }

                alterarSenhaModel.Id = usuarioLogado.Id;

                if (ModelState.IsValid)
                {
                    _attorneyService.AlterarSenha(alterarSenhaModel);
                    TempData["MensagemSucesso"] = "Senha alterada com sucesso";
                    return View("Index", alterarSenhaModel);
                }

                return View("Index", alterarSenhaModel);
            }
            catch (SessionExpiredException)
            {
                // Redireciona para o login se a sessão expirar durante o processo
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, não conseguimos alterar a senha, detalhe do erro: {erro.Message}";
                return View("Index", alterarSenhaModel);
            }
        }

    }
}

