using System.Net;
using System.Net.Mail;

namespace WebAppSystems.Helper
{
    public class Email : IEmail
    {
        private readonly IConfiguration _configuration;

        public Email(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> EnviarAsync(string email, string assunto, string mensagem, string anexoPath = null, string htmlBody = null)
        {
            try
            {
                string host = _configuration["SMTP:Host"];
                int porta = int.Parse(_configuration["SMTP:Porta"]);
                string username = _configuration["SMTP:Username"];
                string senha = _configuration["SMTP:Senha"];
                string nome = _configuration["SMTP:Name"];

                var smtpClient = new SmtpClient(host, porta)
                {
                    Credentials = new NetworkCredential(username, senha),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(username, nome),
                    Subject = assunto,
                    Body = htmlBody ?? $"<html><body><p>{mensagem}</p></body></html>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                if (!string.IsNullOrEmpty(anexoPath) && System.IO.File.Exists(anexoPath))
                {
                    mailMessage.Attachments.Add(new Attachment(anexoPath));
                }

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email: {ex.Message}");
                return false;
            }
        }
    }
}
