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

        public async Task<bool> EnviarAsync(string email, string assunto, string mensagem, string anexoPath = null)
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
                        Html = $@"
                        <html>
                            <body>
                                <h1>{assunto}</h1>
                                <p>{mensagem}</p>
                            </body>
                        </html>"
                    },
                    recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(email) })
                );

                if (!string.IsNullOrEmpty(anexoPath))
                {
                    // Extrai o nome do arquivo a partir do caminho completo
                    string nomeArquivo = Path.GetFileName(anexoPath);

                    var attachment = new EmailAttachment(
                        name: nomeArquivo,  // Nome do arquivo
                        content: BinaryData.FromBytes(File.ReadAllBytes(anexoPath)),  // Conteúdo do arquivo em bytes
                        contentType: "application/pdf" // Tipo de conteúdo (MIME type)
                    );

                    emailMessage.Attachments.Add(attachment); // Adiciona o anexo ao e-mail
                }



                // Envia o e-mail de forma assíncrona
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                    WaitUntil.Completed, // Aguarda a conclusão da operação
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
