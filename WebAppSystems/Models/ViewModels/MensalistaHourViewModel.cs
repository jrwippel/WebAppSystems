namespace WebAppSystems.Models.ViewModels
{
    public class MensalistaHoursViewModel
    {
        public Mensalista Mensalista { get; set; }
        public double TotalHours { get; set; }
        public decimal ValorTotalHoras { get; set; }

        public decimal Percentual { get; set; }

        public decimal ValorAreaBruto { get; set; }

        public decimal ValorResultadoBruto { get; set; }

        public decimal Tributos { get; set; }          
        public decimal ValorMensalLiquido { get; set; }

        public decimal ValorAreaLiquido { get; set; }
        public decimal ValorResultadoLiquido { get; set; }

        public decimal ValorHoraTecLiquida { get; set; }

    }
}
