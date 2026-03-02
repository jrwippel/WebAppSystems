using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebAppSystems.Data;
using WebAppSystems.Models;

namespace WebAppSystems.ViewComponents
{
    public class Menu : ViewComponent
    {
        private readonly WebAppSystemsContext _context;

        public Menu(WebAppSystemsContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string sessaoUsuario = HttpContext.Session.GetString("sessaoUsuarioLogado");
            if (string.IsNullOrEmpty(sessaoUsuario)) return null;
            
            Attorney attorney = JsonConvert.DeserializeObject<Attorney>(sessaoUsuario);
            
            // Calcular horas trabalhadas hoje (apenas registros finalizados)
            var today = DateTime.Today;
            var registrosHoje = await _context.ProcessRecord
                .Where(p => p.AttorneyId == attorney.Id 
                    && p.Date.Date == today 
                    && p.HoraFinal != TimeSpan.Zero 
                    && p.HoraFinal > p.HoraInicial) // Apenas registros finalizados
                .ToListAsync();
            
            var horasHoje = registrosHoje.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours);
            
            ViewBag.HorasHoje = horasHoje;
            
            return View(attorney);
        }
    }
}
