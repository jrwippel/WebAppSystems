namespace WebAppSystems.Helper
{
    public interface IEmail
    {
        Task<bool> EnviarAsync(string email, string assunto, string mensagem, string anexoPath = null); // Definindo o parâmetro com valor padrão null

    }
}
