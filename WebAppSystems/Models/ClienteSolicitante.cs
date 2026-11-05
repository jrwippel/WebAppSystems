using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class ClienteSolicitante
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [Required(ErrorMessage = "O nome do solicitante é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        // Solicitante sugerido ao selecionar o cliente nos lançamentos (no máximo um por cliente)
        public bool Padrao { get; set; }

        public ClienteSolicitante()
        {
        }

        public ClienteSolicitante(int clientId, string nome)
        {
            ClientId = clientId;
            Nome = nome;
        }
    }
}
