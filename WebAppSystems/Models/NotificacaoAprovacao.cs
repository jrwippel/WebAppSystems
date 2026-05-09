using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class NotificacaoAprovacao
    {
        public int Id { get; set; }
        
        [Required]
        public int UsuarioId { get; set; }
        public Attorney Usuario { get; set; }
        
        [Required]
        public int LoteAprovacaoId { get; set; }
        public LoteAprovacao LoteAprovacao { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TipoNotificacao { get; set; } // NovoLote, LoteAprovado, LoteRejeitado, Comentario
        
        [Required]
        [StringLength(500)]
        public string Mensagem { get; set; }
        
        [Required]
        public DateTime DataCriacao { get; set; }
        
        public bool Lida { get; set; } = false;
        
        public DateTime? DataLeitura { get; set; }
        
        public NotificacaoAprovacao()
        {
        }
    }
}
