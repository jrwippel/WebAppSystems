using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;

namespace WebAppSystems.Filters
{
    /// <summary>
    /// Restringe o acesso a usuários com perfil Admin E com a flag IsFinanceiro ativa.
    /// Controladoria e demais perfis não têm acesso, independente da flag IsFinanceiro.
    /// </summary>
    public class PaginaParaAdminFinanceiro : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string sessaoUsuario = context.HttpContext.Session.GetString("sessaoUsuarioLogado");

            if (string.IsNullOrEmpty(sessaoUsuario))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Login" },
                    { "action", "Index" }
                });
                return;
            }

            Attorney attorney = JsonConvert.DeserializeObject<Attorney>(sessaoUsuario);

            if (attorney == null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Login" },
                    { "action", "Index" }
                });
                return;
            }

            // Apenas Admin com IsFinanceiro pode acessar
            if (attorney.Perfil != ProfileEnum.Admin)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Restrito" },
                    { "action", "Index" }
                });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
