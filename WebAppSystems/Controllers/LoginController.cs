using Microsoft.AspNetCore.Mvc;
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
        private readonly LoginAttemptService _loginAttemptService;

        public LoginController(AttorneyService attorneyService, ISessao sessao, IEmail email, LoginAttemptService loginAttemptService)
        {
            _attorneyService = attorneyService;
            _sessao = sessao;
            _email = email;
            _loginAttemptService = loginAttemptService;
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
                        string mensagemPlain = $"Sua nova senha temporária é: {novaSenha}";
                        string htmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f4f6f9;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6f9;padding:40px 0;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr>
          <td style='background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:32px 40px;text-align:center;'>
            <div style='font-size:28px;margin-bottom:6px;'>⏱️</div>
            <div style='color:#ffffff;font-size:20px;font-weight:700;letter-spacing:0.5px;'>Time Tracker</div>
            <div style='color:rgba(255,255,255,0.75);font-size:13px;margin-top:4px;'>Sistema de Controle Jurídico</div>
          </td>
        </tr>

        <!-- Body -->
        <tr>
          <td style='padding:36px 40px;'>
            <p style='margin:0 0 8px;font-size:22px;font-weight:700;color:#1a202c;'>Olá, {usuarioModel.Name}!</p>
            <p style='margin:0 0 28px;font-size:15px;color:#4a5568;line-height:1.6;'>
              Recebemos uma solicitação de redefinição de senha para sua conta.<br>
              Sua nova senha temporária está abaixo.
            </p>

            <!-- Senha box -->
            <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:28px;'>
              <tr>
                <td style='border:2px dashed #667eea;border-radius:10px;padding:20px;text-align:center;background:#f8f7ff;'>
                  <div style='font-size:11px;font-weight:700;letter-spacing:0.12em;color:#667eea;text-transform:uppercase;margin-bottom:10px;'>Senha Temporária</div>
                  <div style='font-size:28px;font-weight:700;color:#1a202c;letter-spacing:0.08em;font-family:monospace;'>{novaSenha}</div>
                </td>
              </tr>
            </table>

            <!-- Próximos passos -->
            <p style='margin:0 0 14px;font-size:14px;font-weight:700;color:#1a202c;'>Próximos passos:</p>
            <table width='100%' cellpadding='0' cellspacing='0'>
              <tr>
                <td style='padding:10px 0;border-bottom:1px solid #e2e8f0;'>
                  <table cellpadding='0' cellspacing='0'><tr>
                    <td style='width:28px;height:28px;background:#667eea;border-radius:50%;text-align:center;vertical-align:middle;'>
                      <span style='color:#fff;font-size:13px;font-weight:700;'>1</span>
                    </td>
                    <td style='padding-left:12px;font-size:14px;color:#4a5568;'>Acesse o sistema com esta senha temporária</td>
                  </tr></table>
                </td>
              </tr>
              <tr>
                <td style='padding:10px 0;border-bottom:1px solid #e2e8f0;'>
                  <table cellpadding='0' cellspacing='0'><tr>
                    <td style='width:28px;height:28px;background:#667eea;border-radius:50%;text-align:center;vertical-align:middle;'>
                      <span style='color:#fff;font-size:13px;font-weight:700;'>2</span>
                    </td>
                    <td style='padding-left:12px;font-size:14px;color:#4a5568;'>Vá em Configurações → Alterar Senha</td>
                  </tr></table>
                </td>
              </tr>
              <tr>
                <td style='padding:10px 0;'>
                  <table cellpadding='0' cellspacing='0'><tr>
                    <td style='width:28px;height:28px;background:#667eea;border-radius:50%;text-align:center;vertical-align:middle;'>
                      <span style='color:#fff;font-size:13px;font-weight:700;'>3</span>
                    </td>
                    <td style='padding-left:12px;font-size:14px;color:#4a5568;'>Defina uma senha pessoal e segura</td>
                  </tr></table>
                </td>
              </tr>
            </table>

            <!-- Aviso -->
            <table width='100%' cellpadding='0' cellspacing='0' style='margin-top:28px;'>
              <tr>
                <td style='background:#fffbeb;border-left:4px solid #f59e0b;border-radius:6px;padding:14px 16px;'>
                  <span style='font-size:13px;color:#92400e;'>
                    ⚠️ <strong>Atenção:</strong> Se você não solicitou a redefinição de senha, ignore este email. Sua senha atual permanece inalterada.
                  </span>
                </td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style='background:#f7fafc;padding:20px 40px;text-align:center;border-top:1px solid #e2e8f0;'>
            <p style='margin:0;font-size:12px;color:#a0aec0;'>Eberhardt, Carrascoza, Bossi, Silva, Matteussi &amp; Costa Beber Advogados</p>
            <p style='margin:4px 0 0;font-size:11px;color:#cbd5e0;'>Este é um email automático, não responda.</p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

                        bool emailEnviado = await _email.EnviarAsync(usuarioModel.Email, "Time Tracker — Sua nova senha temporária", mensagemPlain, htmlBody: htmlBody);
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
        public async Task<IActionResult> Entrar(LoginModel loginModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Verifica bloqueio por tentativas excessivas
                    if (_loginAttemptService.IsLockedOut(loginModel.Login))
                    {
                        var lockoutTime = _loginAttemptService.GetLockoutTimeRemaining(loginModel.Login);
                        TempData["MensagemErro"] = lockoutTime.HasValue
                            ? $"Conta bloqueada. Tente novamente em {lockoutTime.Value.Minutes}m {lockoutTime.Value.Seconds}s."
                            : "Conta temporariamente bloqueada. Tente novamente mais tarde.";
                        return View("Index");
                    }

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
                            _loginAttemptService.ResetAttempts(loginModel.Login);

                            // Upgrade automático de SHA1 → BCrypt na primeira vez que o usuário logar
                            if (usuario.NeedsPasswordUpgrade())
                            {
                                usuario.UpgradePasswordHash(loginModel.Senha);
                                await _attorneyService.AtualizarSenhaHashAsync(usuario);
                            }
                            _sessao.CriarSessaoDoUsuario(usuario);
                            return RedirectToAction("Index", "Home");
                        }

                        _loginAttemptService.RecordFailedAttempt(loginModel.Login);
                        var remaining = _loginAttemptService.GetRemainingAttempts(loginModel.Login);
                        TempData["MensagemErro"] = remaining > 0
                            ? $"Senha inválida. Você tem {remaining} tentativa(s) restante(s)."
                            : "Senha inválida. Conta bloqueada por 15 minutos.";
                    }
                    else
                    {
                        _loginAttemptService.RecordFailedAttempt(loginModel.Login);
                        var remaining = _loginAttemptService.GetRemainingAttempts(loginModel.Login);
                        TempData["MensagemErro"] = remaining > 0
                            ? $"Usuário e/ou senha inválido(s). Você tem {remaining} tentativa(s) restante(s)."
                            : "Usuário e/ou senha inválido(s). Conta bloqueada por 15 minutos.";
                    }
                }
                else
                {
                    TempData["MensagemErro"] = null;
                }

                return View("Index");
            }
            catch (Sessao.SessionExpiredException)
            {
                TempData["MensagemErro"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["MensagemErro"] = "Ops, não conseguimos realizar o seu login. Tente novamente.";
                return RedirectToAction("Index");
            }
        }


    }
}
