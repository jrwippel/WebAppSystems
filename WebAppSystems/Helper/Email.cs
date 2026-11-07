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

        public async Task<bool> EnviarAsync(string email, string assunto, string mensagem, string anexoPath = null, string htmlBody = null, string emailCc = null)
        {
            try
            {
                string connectionString = _configuration["AzureEmail:ConnectionString"];
                string senderAddress = _configuration["AzureEmail:SenderAddress"];

                var emailClient = new EmailClient(connectionString);

                // emailCc pode conter vários endereços separados por vírgula ou ponto e vírgula
                List<EmailAddress> ccList = null;
                if (!string.IsNullOrWhiteSpace(emailCc))
                {
                    ccList = emailCc
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => e.Length > 0)
                        .Select(e => new EmailAddress(e))
                        .ToList();
                    if (ccList.Count == 0) ccList = null;
                }

                var recipients = new EmailRecipients(
                    to: new List<EmailAddress> { new EmailAddress(email) },
                    cc: ccList
                );

                var emailMessage = new EmailMessage(
                    senderAddress: senderAddress,
                    content: new EmailContent(assunto)
                    {
                        PlainText = mensagem,
                        Html = htmlBody ?? $@"<html><body><h1>{assunto}</h1><p>{mensagem}</p></body></html>"
                    },
                    recipients: recipients
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
