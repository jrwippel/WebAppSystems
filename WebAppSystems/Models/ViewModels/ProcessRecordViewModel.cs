using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebAppSystems.Models.ViewModels
{
    public class ProcessRecordViewModel
    {
        public ProcessRecord ProcessRecord { get; set; }
        public Client Client { get; set; }
        public ICollection<ProcessRecord> ProcessRecords { get; set; }
        public ICollection<Attorney> Attorneys { get; set; }
        public ICollection<Client> Clients { get; set; }
        public IEnumerable<SelectListItem> ClientsOptions { get; set; }

        public ICollection<Department> Departments { get; set; }
        public IEnumerable<SelectListItem> DepartmentsOptions { get; set; }
        public IEnumerable<SelectListItem> RecordTypesOptions { get; set; } // Adicione esta linha

        public string Solicitante { get; set; }

        public int RecordType { get; set; }



    }
}
