using WebAppSystems.Models;

namespace WebAppSystems.Models.ViewModels
{
    public class FaturamentoGrupoViewModel
    {
        public Client Cliente { get; set; } = null!;
        public List<ProcessRecord> Lancamentos { get; set; } = null!;
        public double TotalHoras { get; set; }
        public double ValorEstimado { get; set; }
    }

    public class MarcarFaturadoRequest
    {
        public List<int> ProcessRecordIds { get; set; } = new List<int>();
    }

    public class ResumoExecutivoRequest
    {
        public string MesAno { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public int? DepartamentoId { get; set; }
        public int? AdvogadoId { get; set; }
    }

    public class ResumoExecutivoPDFRequest
    {
        public string MesAno { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public int? DepartamentoId { get; set; }
        public int? AdvogadoId { get; set; }
        public string Resumo { get; set; } = string.Empty;
    }
}
