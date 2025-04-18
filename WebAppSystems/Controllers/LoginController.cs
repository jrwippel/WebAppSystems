﻿using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Services;

namespace WebAppSystems.Controllers
{
    public class LoginController : Controller
    {
        private readonly AttorneyService _attorneyService;
        private readonly ISessao _sessao;
        private readonly IEmail _email;

        public LoginController(AttorneyService attorneyService, ISessao sessao, IEmail email)
        {
            _attorneyService = attorneyService;
            _sessao = sessao;
            _email = email;
        }

        public IActionResult Index()
        {
            string currentController = RouteData.Values["controller"]?.ToString();
            string currentAction = RouteData.Values["action"]?.ToString();

            if (currentController != "Login" || currentAction != "Index")
            {
                try
                {
                    // Tenta buscar a sessão do usuário e redireciona para a Home se estiver logado
                    if (_sessao.BuscarSessaoDoUsuario() != null)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (Sessao.SessionExpiredException)
                {
                    // Exibe uma mensagem amigável se a sessão expirou
                    TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                }
            }
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
        public async Task<IActionResult> EnviarLinkParaRedefinirSenha(RedefinirSenhaModel redefinirSenhaModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var usuarioModel = _attorneyService.BuscarPorEmailLogin(redefinirSenhaModel.Email, redefinirSenhaModel.Login);

                    if (usuarioModel != null)
                    {
                        string novaSenha = usuarioModel.GerarNovaSenha();
                        string mensagem = $"Sua nova senha é: {novaSenha}";
                        bool emailEnviado = await _email.EnviarAsync(usuarioModel.Email, "Sistema de Controle Jurídico - Nova Senha", mensagem);
                        //bool emailEnviado = await _email.EnviarAsync(usuarioModel.Email, "Stradale - Sistema de Controle Recolhas - Nova Senha", mensagem);

                        if (emailEnviado)
                        {
                            _attorneyService.AtualizarSenha(usuarioModel);
                            TempData["MensagemSucesso"] = "Enviamos para o seu email cadastrado uma nova senha.";
                        }
                        else
                        {
                            TempData["MensagemErro"] = "Não conseguimos enviar o email. Tente novamente.";
                        }
                        return RedirectToAction("Index", "Login");
                    }
                    TempData["MensagemErro"] = "Não conseguimos redefinir sua senha. Dados informados inválidos.";
                }

                return View("Index");
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, não conseguimos redefinir sua senha, tente novamente. Detalhes do erro: {erro.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Entrar(LoginModel loginModel)
        {
            try
            {
                // Verifica se o estado do modelo é válido
                if (ModelState.IsValid)
                {
                    var usuario = _attorneyService.FindByLoginAsync(loginModel.Login);

                    if (usuario != null)
                    {
                        if (usuario.Inativo)
                        {
                            TempData["MensagemErro"] = "Usuário inativo. Contate o administrador para mais informações.";
                            return View("Index");
                        }

                        if (usuario.ValidaSenha(loginModel.Senha))
                        {
                            _sessao.CriarSessaoDoUsuario(usuario);
                            return RedirectToAction("Index", "Home");
                        }

                        TempData["MensagemErro"] = "Senha do usuário é inválida.";
                    }
                    else
                    {
                        TempData["MensagemErro"] = "Usuário e/ou senha inválido(s).";
                    }
                }
                else
                {
                    // Remove mensagens de erro desnecessárias caso a validação inicial falhe
                    TempData["MensagemErro"] = null;
                }

                return View("Index");
            }
            catch (Sessao.SessionExpiredException)
            {
                TempData["MensagemErro"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index");
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, não conseguimos realizar o seu login. Mais detalhes no erro: {erro.Message}";
                return RedirectToAction("Index");
            }
        }


    }
}
