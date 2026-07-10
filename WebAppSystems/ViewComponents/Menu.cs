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
        private readonly IWebHostEnvironment _env;

        public Menu(WebAppSystemsContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string sessaoUsuario = HttpContext.Session.GetString("sessaoUsuarioLogado");
            if (string.IsNullOrEmpty(sessaoUsuario)) return null;
            
            Attorney attorneyFromSession = JsonConvert.DeserializeObject<Attorney>(sessaoUsuario);
            
            // Buscar dados atualizados do banco (incluindo IsFinanceiro e IsAprovador)
            var attorney = await _context.Attorney
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == attorneyFromSession.Id);
            
            if (attorney == null) return null;
            
            // Nome do escritório — fictício em Development para distinguir do ambiente de produção
            ViewBag.NomeEscritorio = _env.IsDevelopment()
                ? "Silva & Associados Adv."
                : "Eberhardt, Carrascoza, BSM & CB Adv.";

            ViewBag.NomeEscritorioCompleto = _env.IsDevelopment()
                ? "Silva & Associados Advogados - AMBIENTE DE DESENVOLVIMENTO"
                : "Eberhardt, Carrascoza, Bossi, Silva, Matteussi & Costa Beber Advogados";
            
            // Calcular horas trabalhadas hoje (apenas registros finalizados)
            var today = DateTime.Today;
            var registrosHoje = await _context.ProcessRecord
                .Where(p => p.AttorneyId == attorney.Id 
                    && p.Date.Date == today 
                    && p.HoraFinal != TimeSpan.Zero 
                    && p.HoraFinal > p.HoraInicial)
                .ToListAsync();
            
            var horasHoje = registrosHoje.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours);
            
            ViewBag.HorasHoje = horasHoje;
            
            // Buscar parâmetros (logo do escritório)
            var parametros = await _context.Parametros.FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;
            
            return View(attorney);
        }
    }
}
