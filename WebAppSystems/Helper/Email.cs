using System.Net.Mail;
using System.Net;
using Azure;
using Azure.Communication.Email;

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
                string connectionString = _configuration["AzureEmail:ConnectionString"];
                string senderAddress = _configuration["AzureEmail:SenderAddress"];

                var emailClient = new EmailClient(connectionString);

                var emailMessage = new EmailMessage(
                    senderAddress: senderAddress,
                    content: new EmailContent(assunto)
                    {
                        PlainText = mensagem,
                        Html = htmlBody ?? $@"<html><body><h1>{assunto}</h1><p>{mensagem}</p></body></html>"
                    },
                    recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(email) })
                );

                if (!string.IsNullOrEmpty(anexoPath))
                {
                    string nomeArquivo = Path.GetFileName(anexoPath);
                    var attachment = new EmailAttachment(
                        name: nomeArquivo,
                        content: BinaryData.FromBytes(File.ReadAllBytes(anexoPath)),
                        contentType: "application/pdf"
                    );
                    emailMessage.Attachments.Add(attachment);
                }

                EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                    WaitUntil.Completed,
                    emailMessage
                );

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
