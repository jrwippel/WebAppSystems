using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class PrecoCliente
    {
        public int Id { get; set; }
        public Client Client { get; set; }
        public int ClientId { get; set; }
        public Department Department { get; set; }
        public int DepartmentId { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]        
        public double Valor { get; set; }

        public PrecoCliente()
        {
        }

        public PrecoCliente(Client client, Department department, double valor)
        {
            Client = client;
           
            Department = department;          
            Valor = valor;
        }
    }
}
