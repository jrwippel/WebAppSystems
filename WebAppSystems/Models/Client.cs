using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAppSystems.Models.Enums;

namespace WebAppSystems.Models
{
    public class Client
    {

        public int Id { get; set; }

       
        [StringLength(60, MinimumLength = 3, ErrorMessage = "{0} Tamanho deveria ser entre 3 e 60")]
        public string Name { get; set; }
       
        public string? Document { get; set; }

        [EmailAddress(ErrorMessage = "Digite um email válido")]
       
        public string? Email { get; set; }
        public string? Telephone { get; set; }        
        public byte[]? ImageData { get; set; }

        public string? ImageMimeType { get; set; }
        public ICollection<ProcessRecord> ProcessRecords { get; set; } = new List<ProcessRecord>();

        public string? Solicitante { get; set; }

        public bool ClienteInterno { get; set; }

        public Client()
        {
        }

        public Client(string name, string document, string email, string telephone)
        {
            Name = name;
            Document = document;
            Email = email;
            Telephone = telephone;
        }

        public Client(int id, string name, string document, string email, string telephone)
        {
            Id = id;
            Name = name;
            Document = document;
            Email = email;
            Telephone = telephone;
        }
    }
}
