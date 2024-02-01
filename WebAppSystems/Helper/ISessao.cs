using WebAppSystems.Models;

namespace WebAppSystems.Helper
{
    public interface ISessao
    {
        void CriarSessaoDoUsuario(Attorney attorney);
        void RemoverSessaoDoUsuario();
        Attorney BuscarSessaoDoUsuario();

    }
}
