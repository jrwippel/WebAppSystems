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
            string valor = JsonConvert.SerializeObject(attorney);
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
