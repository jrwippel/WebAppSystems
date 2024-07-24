using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WebAppSystems.Models.Enums;

namespace WebAppSystems.Models
{
    public class ProcessRecord
    {
        public int Id { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        
        public DateTime Date { get; set; }
        [JsonIgnore] // Adicione esta linha para que ao enviar o JSON, essa propriedade seja ignorada
        [ValidateNever]
        public Attorney Attorney { get; set; }
        public int AttorneyId { get; set; }
        [JsonIgnore] // Adicione esta linha para que ao enviar o JSON, essa propriedade seja ignorada
        [ValidateNever]
        public Client Client { get; set; }
        public int ClientId { get; set; }

        [Required(ErrorMessage = "A hora inicial é obrigatória.")]
        //[RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "A hora inicial deve estar no formato HH:MM.")]
        
        public TimeSpan HoraInicial { get; set; }

        [Required(ErrorMessage = "A hora final é obrigatória.")]
        //[RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "A hora inicial deve estar no formato HH:MM.")]
        
        public TimeSpan HoraFinal { get; set; }

        [StringLength(200, MinimumLength = 10, ErrorMessage = "O campo Descrição deve ter pelo menos 10 caracteres")]
        public string Description { get; set; }

        [JsonIgnore] // Adicione esta linha para que ao enviar o JSON, essa propriedade seja ignorada

        [ValidateNever]
        public Department Department { get; set; }
        public int DepartmentId { get; set; }

        public string Solicitante { get; set; }
        public ProcessRecord()
        {
        }

        public ProcessRecord(DateTime date, Attorney attorney, Client client, TimeSpan horaInicial, TimeSpan horaFinal, string description, Department department, string solicitante)
        {
            Date = date;

            Attorney = attorney;
            Client = client;
            HoraInicial = horaInicial;
            HoraFinal = horaFinal;
            Description = description;
            Solicitante = solicitante;
            Department = department;
            Solicitante = solicitante;
                
        }

        public ProcessRecord(int id, DateTime date, Attorney attorney, Client client, TimeSpan horaInicial, TimeSpan horaFinal, string description, Department department, string solicitante)
        {
            Id = id;
            Date = date; 
            Attorney = attorney;
            Client = client;
            HoraInicial = horaInicial;
            HoraFinal = horaFinal;
            Description = description;
            Department = department;
            Solicitante = solicitante;

        }

        public string CalculoHoras()
        {
            TimeSpan diferenca = HoraFinal - HoraInicial;
            int horas = diferenca.Hours;
            int minutos = diferenca.Minutes;
            string diferencaFormatada = string.Format("{0:00}:{1:00}", horas, minutos);
            return diferencaFormatada;
        }
        public double CalculoHorasDecimal()
        {
            TimeSpan diferenca = HoraFinal - HoraInicial;
            double totalHoras = diferenca.TotalHours;
            return totalHoras;
        }

        public TimeSpan CalculoHorasTotal()
        {
            TimeSpan diferenca = HoraFinal - HoraInicial;

            if (diferenca < TimeSpan.Zero)
            {
                diferenca += TimeSpan.FromDays(1); // Adiciona 1 dia completo
            }

            return diferenca;
        }

        public bool IsStartTimeLessEndTime()
        {
            if (HoraInicial > HoraFinal)
            {
                Console.WriteLine("A hora inicial deve ser menor que a hora final.");
                return false;
            }
            return true;
        }
    }
}
