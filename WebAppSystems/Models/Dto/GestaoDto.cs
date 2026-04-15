namespace WebAppSystems.Models.Dto
{
    public class GestaoColaboradorDto
    {
        public int AttorneyId { get; set; }
        public string Nome { get; set; }
        public double TotalHoras { get; set; }
        public int TotalRegistros { get; set; }
        public DateTime UltimoLancamento { get; set; }
    }

    public class GestaoDiarioDto
    {
        public DateTime Data { get; set; }
        public double TotalHoras { get; set; }
        public int TotalRegistros { get; set; }
    }

    public class GestaoTopClientesDto
    {
        public int AttorneyId { get; set; }
        public string Nome { get; set; }
        public double TotalHoras { get; set; }
        public List<TopClienteItem> TopClientes { get; set; }
    }

    public class TopClienteItem
    {
        public string Cliente { get; set; }
        public double Horas { get; set; }
        public double Percentual { get; set; }
    }

    public class GestaoConsistenciaDto
    {
        public string Nome { get; set; }
        public int DiasComLancamento { get; set; }
        public int DiasUteis { get; set; }
        public double Percentual { get; set; }
    }
}
