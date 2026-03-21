namespace WebAppSystems.Models.ViewModels
{
    public class MensalistaDetalheViewModel
    {
        public MensalistaHoursViewModel MesAtual { get; set; }
        public MensalistaHoursViewModel Media3Meses { get; set; }
        public MensalistaHoursViewModel Acumulado3Meses { get; set; }

        // Parâmetros de navegação
        public string InputMonthYear { get; set; }
        public int? ClientId { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }
}
