using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebAppSystems.Models;

namespace WebAppSystems.ViewComponents
{
    public class Menu : ViewComponent
    {

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string sessaoUsuario = HttpContext.Session.GetString("sessaoUsuarioLogado");
            if (string.IsNullOrEmpty(sessaoUsuario)) return null;
            Attorney attorney = JsonConvert.DeserializeObject<Attorney>(sessaoUsuario);
            return View(attorney);
        }
    }
}
