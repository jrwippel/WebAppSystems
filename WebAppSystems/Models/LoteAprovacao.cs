using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class LoteAprovacao
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime DataCriacao { get; set; }
        
        [Required]
        public int CriadoPorId { get; set; }
        public Attorney CriadoPor { get; set; }
        
        [Required]
        public int ClienteId { get; set; }
        public Client Cliente { get; set; }
        
        [Required]
        public DateTime PeriodoInicio { get; set; }
        
        [Required]
        public DateTime PeriodoFim { get; set; }
        
        [Required]
        public StatusLoteAprovacao Status { get; set; }
        
        public double TotalHoras { get; set; }
        
        public double ValorEstimado { get; set; }
        
        public DateTime? DataAprovacao { get; set; }
        
        public int? AprovadoPorId { get; set; }
        public Attorney? AprovadoPor { get; set; }
        
        public string? ComentarioAprovador { get; set; }
        
        public DateTime? DataFaturamento { get; set; }
        
        public int? FaturadoPorId { get; set; }
        public Attorney? FaturadoPor { get; set; }
        
        // Relacionamento com lançamentos
        public ICollection<LoteAprovacaoItem> Itens { get; set; } = new List<LoteAprovacaoItem>();
        
        // Histórico de ações
        public ICollection<HistoricoAprovacao> Historico { get; set; } = new List<HistoricoAprovacao>();

        // IDs de departamentos que já liberaram sua área (separados por vírgula)
        public string? AreasLiberadas { get; set; }

        // Helpers
        public List<int> GetAreasLiberadasIds()
        {
            if (string.IsNullOrEmpty(AreasLiberadas)) return new List<int>();
            return AreasLiberadas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }

        public bool IsAreaLiberada(int departmentId)
            => GetAreasLiberadasIds().Contains(departmentId);

        public void LiberarArea(int departmentId)
        {
            var ids = GetAreasLiberadasIds();
            if (!ids.Contains(departmentId))
            {
                ids.Add(departmentId);
                AreasLiberadas = string.Join(",", ids);
            }
        }
        
        public LoteAprovacao()
        {
        }
    }
    
    public enum StatusLoteAprovacao
    {
        Pendente = 1,
        Aprovado = 2,
        Rejeitado = 3,
        Cancelado = 4,
        Faturado = 5,
        ParcialmenteAprovado = 6
    }
}
