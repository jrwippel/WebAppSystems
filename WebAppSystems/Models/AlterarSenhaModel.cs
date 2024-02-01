using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class AlterarSenhaModel
    {

        public int Id { get; set; }
        [Required(ErrorMessage = "Senha atual é obrigatório")]
        public string SenhaAtual { get; set; }

        [Required(ErrorMessage = "Senha nova é obrigatório")]
        public string SenhaNova { get; set; }

        [Required(ErrorMessage = "Confirme a nova senha")]
        [Compare("SenhaNova", ErrorMessage = "Senha não confere com a senha Nova")]
        public string ConfirmarNovaSenha { get; set; }

    }
}
