using WebAppSystems.Models.Enums;

namespace WebAppSystems.Models.ViewModels
{
    public class RelatorioGerencialViewModel
    {
        // Filtros aplicados
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<int>? AdvogadoIds { get; set; }
        public List<int>? ClienteIds { get; set; }
        public List<int>? AreaIds { get; set; }
        public List<RecordType>? TiposRegistro { get; set; }

        // Indicadores resumidos
        public double TotalHoras { get; set; }
        public double MediaHorasDiaUtil { get; set; }
        public int QuantidadeAtividades { get; set; }
        public int QuantidadeClientes { get; set; }
        public int QuantidadeAdvogados { get; set; }
        public int DiasComLancamento { get; set; }

        // Dados agrupados
        public List<GrupoAdvogado> PorAdvogado { get; set; } = new();
        public List<GrupoCliente> PorCliente { get; set; } = new();
        public List<GrupoArea> PorArea { get; set; } = new();
        public List<GrupoTipo> PorTipo { get; set; } = new();

        // Evolução diária (para gráfico)
        public List<EvolucaoDiaria> EvolucaoDiaria { get; set; } = new();

        // Listas para dropdowns
        public List<Attorney> Advogados { get; set; } = new();
        public List<Client> Clientes { get; set; } = new();
        public List<Department> Areas { get; set; } = new();
    }

    public class GrupoAdvogado
    {
        public int AdvogadoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public double TotalHoras { get; set; }
        public int QuantidadeAtividades { get; set; }
        public int DiasComLancamento { get; set; }
        public double MediaHorasDia { get; set; }
        public double PercentualTotal { get; set; }
        public List<SubGrupoCliente> Clientes { get; set; } = new();
    }

    public class GrupoCliente
    {
        public int ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public double TotalHoras { get; set; }
        public int QuantidadeAtividades { get; set; }
        public double PercentualTotal { get; set; }
        public List<SubGrupoAdvogado> Advogados { get; set; } = new();
    }

    public class GrupoArea
    {
        public int AreaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public double TotalHoras { get; set; }
        public int QuantidadeAtividades { get; set; }
        public double PercentualTotal { get; set; }
    }

    public class GrupoTipo
    {
        public RecordType Tipo { get; set; }
        public string Nome { get; set; } = string.Empty;
        public double TotalHoras { get; set; }
        public int QuantidadeAtividades { get; set; }
        public double PercentualTotal { get; set; }
    }

    public class SubGrupoCliente
    {
        public string Nome { get; set; } = string.Empty;
        public double TotalHoras { get; set; }
        public int QuantidadeAtividades { get; set; }
    }

    public class SubGrupoAdvogado
    {
        public string Nome { get; set; } = string.Empty;
        public double TotalHoras { get; set; }
        public int QuantidadeAtividades { get; set; }
    }

    public class EvolucaoDiaria
    {
        public string Data { get; set; } = string.Empty;
        public double Horas { get; set; }
        public int Atividades { get; set; }
    }
}
