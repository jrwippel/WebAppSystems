using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class PercentualArea
    {
        public int Id { get; set; }
        public Department Department { get; set; }
        public int DepartmentId { get; set; }
        public Client Client { get; set; }
        public int ClientId { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Percentual { get; set; }
    }
}
