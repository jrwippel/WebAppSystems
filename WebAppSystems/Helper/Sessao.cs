using Newtonsoft.Json;
using WebAppSystems.Models;

namespace WebAppSystems.Helper
{
    public class Sessao : ISessao
    {

        private readonly IHttpContextAccessor _httpContext;

        public Sessao(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }
        public Attorney BuscarSessaoDoUsuario()
        {
            string sessaoUsuario = _httpContext.HttpContext.Session.GetString("sessaoUsuarioLogado");
            if (string.IsNullOrEmpty(sessaoUsuario))
            {
                throw new SessionExpiredException("A sessão expirou. Por favor, faça login novamente.");
            }
            return JsonConvert.DeserializeObject<Attorney>(sessaoUsuario);
        }


        public void CriarSessaoDoUsuario(Attorney attorney)
        {
            // Não serializar o hash da senha na sessão por segurança
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var sessionData = new
            {
                attorney.Id,
                attorney.Name,
                attorney.Email,
                attorney.Login,
                attorney.Phone,
                attorney.BirthDate,
                attorney.DepartmentId,
                attorney.Perfil,
                attorney.RegisterDate,
                attorney.UpdateDate,
                attorney.UseBorder,
                attorney.UseCronometroAlwaysOnTop,
                attorney.Inativo
            };
            string valor = JsonConvert.SerializeObject(sessionData, settings);
            _httpContext.HttpContext.Session.SetString("sessaoUsuarioLogado", valor);
        }

        public void RemoverSessaoDoUsuario()
        {
            _httpContext.HttpContext.Session.Remove("sessaoUsuarioLogado");
        }

        public class SessionExpiredException : Exception
        {
            public SessionExpiredException(string message) : base(message)
            {
            }
        }

    }
}
