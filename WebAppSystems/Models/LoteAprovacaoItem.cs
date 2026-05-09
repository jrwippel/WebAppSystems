using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class LoteAprovacaoItem
    {
        public int Id { get; set; }
        
        [Required]
        public int LoteAprovacaoId { get; set; }
        public LoteAprovacao LoteAprovacao { get; set; }
        
        [Required]
        public int ProcessRecordId { get; set; }
        public ProcessRecord ProcessRecord { get; set; }
        
        [Required]
        public StatusItemAprovacao Status { get; set; }
        
        public bool Abonado { get; set; } = false;
        
        public DateTime? DataRevisao { get; set; }
        
        public string? ObservacaoRevisao { get; set; }
        
        // Campos para rastrear edições
        public bool FoiEditado { get; set; } = false;
        
        public string? DescricaoOriginal { get; set; }
        public TimeSpan? HoraInicialOriginal { get; set; }
        public TimeSpan? HoraFinalOriginal { get; set; }
        
        public LoteAprovacaoItem()
        {
        }
    }
    
    public enum StatusItemAprovacao
    {
        Pendente = 1,
        Aprovado = 2,
        Abonado = 3
    }
}
