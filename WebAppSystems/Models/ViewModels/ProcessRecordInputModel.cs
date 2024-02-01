namespace WebAppSystems.Models.ViewModels
{
    public class ProcessRecordInputModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int AttorneyId { get; set; }
        public int ClientId { get; set; }
        public TimeSpan HoraInicial { get; set; }
        public TimeSpan HoraFinal { get; set; }
        public string Description { get; set; }
        public int DepartmentId { get; set; }
        public string Solicitante { get; set; }
    }
}
