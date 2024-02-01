using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Services;

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

                Attorney usuarioLogado = _sessao.BuscarSessaoDoUsuario();
                alterarSenhaModel.Id = usuarioLogado.Id;
                if (ModelState.IsValid)
                {
                    _attorneyService.AlterarSenha(alterarSenhaModel);
                    TempData["MensagemSucesso"] = $"Senha alterada com sucesso";
                    return View("Index", alterarSenhaModel);
                }

                return View("Index", alterarSenhaModel);

            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, não conseguimos alterar a senha, detalhe do erro:{erro.Message}";
                return View("Index", alterarSenhaModel);
            }
        }
    }
}

