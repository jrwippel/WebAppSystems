using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Services;

namespace WebAppSystems.Controllers
{
    [Route("api/[controller]")] // Esta linha define a rota base para este controller
    [ApiController] // Esta linha indica que este é um Controller de API
    public class LoginApiController : Controller
    {
        private readonly AttorneyService _attorneyService;
        private readonly ISessao _sessao;
        private readonly IEmail _email;

        public LoginApiController(AttorneyService attorneyService, ISessao sessao, IEmail email)
        {
            _attorneyService = attorneyService;
            _sessao = sessao;
            _email = email;
            
        }

        [HttpPost("authenticate")]
        public IActionResult Authenticate(LoginModel loginModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Attorney usuario = _attorneyService.FindByLoginAsync(loginModel.Login);
                    if (usuario != null && usuario.ValidaSenha(loginModel.Senha))
                    {
                        return Ok(new { message = "Usuário autenticado com sucesso!", userId = usuario.Id, userName = usuario.Name, usuario.DepartmentId });
                    }
                    return Unauthorized(new { message = "Usuário e/ou senha inválidos!" });
                }
                return BadRequest(new { message = "Dados inválidos." });
            }
            catch (Exception erro)
            {
                return StatusCode(500, new { message = $"Erro interno do servidor: {erro.Message}" });
            }
        }

        public IActionResult Index()
        {
            // Se usuario estiver logado direcionar ele para a Home
            if ( _sessao.BuscarSessaoDoUsuario() != null ) return RedirectToAction("Index", "Home");
            return View();
        }
        public IActionResult TimeTracking()
        {
            return View();
        }


        public IActionResult RedefinirSenha()
        {
            return View();
        }
        public IActionResult Sair()
        {
            _sessao.RemoverSessaoDoUsuario();
            return RedirectToAction("Index", "Login");
        }

        [HttpPost]
        public IActionResult EnviarLinkParaRedefinirSenha(RedefinirSenhaModel redefinirSenhaModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Attorney usuarioModel = _attorneyService.BuscarPorEmailLogin(redefinirSenhaModel.Email, redefinirSenhaModel.Login);

                    if (usuarioModel != null)
                    {
                        string novaSenha = usuarioModel.GerarNovaSenha();
                        string mensagem = $"Sua nova senha é: {novaSenha}";
                        bool emailEnviado = _email.Enviar(usuarioModel.Email, "Sistema de Controle Jurídico - Nova Senha", mensagem);
                        if (emailEnviado)
                        {
                            _attorneyService.AtualizarSenha(usuarioModel);
                            TempData["MensagemSucesso"] = $"Enviamos para o seu email cadastrado uma nova senha.";
                        }
                        else
                        {
                            TempData["MensagemErro"] = $"Não conseguimos enviar email. Tente novamente";
                        }
                        return RedirectToAction("Index", "Login");
                    }
                    TempData["MensagemErro"] = $"Não conseguimos redefinir sua senha. Dados informados inválidos";
                }

                return View("Index");
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, não conseguimos redefinir sua senha, tente novamente, detalhe do erro: {erro.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Entrar(LoginModel loginModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Attorney usuario = _attorneyService.FindByLoginAsync(loginModel.Login);
                    if (usuario != null)
                      {
                          if (usuario.ValidaSenha(loginModel.Senha))
                          {
                             _sessao.CriarSessaoDoUsuario(usuario);
                             return RedirectToAction("Index", "Home");
                            
                        }
                          TempData["MensagemErro"] = $"Senha do Usuário é Inválida";
                    }
                    TempData["MensagemErro"] = $"Usuário e/ou Senha Inválido(s)";

                }

                return View("Index");
                
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, não conseguimos realizar o seu login, mais detalhes no erro: {erro.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
