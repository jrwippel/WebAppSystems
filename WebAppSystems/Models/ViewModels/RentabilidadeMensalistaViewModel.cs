namespace WebAppSystems.Models.ViewModels
{
    public class RentabilidadeMensalistaViewModel
    {
        public string Periodo { get; set; } = "mes"; // mes, semestre, custom
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<CardMensalista> Cards { get; set; } = new();
        
        // Totais
        public decimal TotalMensalidades { get; set; }
        public decimal TotalConsumido { get; set; }
        public decimal SaldoGeral { get; set; }
        public int TotalMensalistas { get; set; }
        public int QtdVerde { get; set; }
        public int QtdAmarelo { get; set; }
        public int QtdVermelho { get; set; }
        public int TotalEstourados { get; set; }
        public int TotalAtencao { get; set; }
        public int TotalEquilibrados { get; set; }
    }

    public class CardMensalista
    {
        public int MensalistaId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public byte[]? ClienteLogo { get; set; }
        public string? ClienteLogoMime { get; set; }
        
        // Valores
        public decimal ValorMensalidade { get; set; }
        public decimal ValorHoraVirtual { get; set; }
        public double HorasApontadas { get; set; }
        public decimal ValorConsumido { get; set; }
        public decimal Saldo { get; set; }
        public double PercentualConsumo { get; set; }
        
        // Status: verde, amarelo, vermelho
        public string Status { get; set; } = "verde";
        public string StatusTexto { get; set; } = "Equilibrado";
        public string StatusCor { get; set; } = "#48bb78";
    }
}
