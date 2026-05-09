using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class HistoricoAprovacao
    {
        public int Id { get; set; }
        
        [Required]
        public int LoteAprovacaoId { get; set; }
        public LoteAprovacao LoteAprovacao { get; set; }
        
        [Required]
        public DateTime DataHora { get; set; }
        
        [Required]
        public int UsuarioId { get; set; }
        public Attorney Usuario { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TipoAcao { get; set; } // Criacao, Edicao, Abono, Aprovacao, Faturamento, Comentario
        
        [StringLength(2000)]
        public string? Detalhes { get; set; }
        
        public int? ProcessRecordId { get; set; }
        public ProcessRecord? ProcessRecord { get; set; }
        
        public HistoricoAprovacao()
        {
        }
    }
}
