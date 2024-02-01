using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Digite a senha")]
        public string Login { get; set; }
        [Required(ErrorMessage = "Digite a senha")]
        public string Senha { get; set; }
    }
}
