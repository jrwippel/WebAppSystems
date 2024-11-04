using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class Parametros
    {
        [Key] // Adicionando a chave primária
        public int Id { get; set; }

        [NotMapped]
        [Required]
        public IFormFile Logo { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A largura deve ser maior que 0")]
        public int Width { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A altura deve ser maior que 0")]
        public int Height { get; set; }

        // Propriedades para armazenar a imagem no banco de dados
        public byte[] LogoData { get; set; }
        public string LogoMimeType { get; set; }
    }
}
