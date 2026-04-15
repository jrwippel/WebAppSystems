using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models.Dto;
using WebAppSystems.Models.Enums;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class PainelGestaoController : Controller
    {
        private readonly ProcessRecordsService _service;
        private readonly ISessao _isessao;
        private readonly WebAppSystemsContext _context;

        public PainelGestaoController(ProcessRecordsService service, ISessao isessao, WebAppSystemsContext context)
        {
            _service = service;
            _isessao = isessao;
            _context = context;
        }

        public IActionResult Index()
        {
            try
            {
                var usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                return View();
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDados(string de, string ate)
        {
            try
            {
                var from = string.IsNullOrEmpty(de)
                    ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                    : DateTime.Parse(de);
                var to = string.IsNullOrEmpty(ate) ? DateTime.Today : DateTime.Parse(ate);

                var colaboradores = await _service.GetHorasPorColaboradorAsync(from, to);
                var porDia = await _service.GetHorasPorDiaAsync(from, to);
                var semLancamento = await _service.GetColaboradoresSemLancamentoAsync(7);
                var topClientes = await _service.GetTopClientesPorColaboradorAsync(from, to);
                var consistencia = await _service.GetConsistenciaLancamentosAsync(from, to);

                var totalHoras = colaboradores.Sum(c => c.TotalHoras);
                var mediaHoras = colaboradores.Count > 0
                    ? Math.Round(totalHoras / colaboradores.Count, 2)
                    : 0;

                var totalUsuariosAtivos = await _context.Attorney.CountAsync(a => !a.Inativo);

                return Json(new
                {
                    kpis = new
                    {
                        totalHoras = Math.Round(totalHoras, 2),
                        mediaHoras,
                        totalColaboradores = colaboradores.Count,
                        totalUsuariosAtivos,
                        semLancamento7dias = semLancamento.Count
                    },
                    colaboradores = colaboradores.Select(c => new
                    {
                        c.Nome,
                        c.TotalHoras,
                        c.TotalRegistros,
                        ultimoLancamento = c.UltimoLancamento.ToString("dd/MM/yyyy")
                    }),
                    porDia = porDia.Select(d => new
                    {
                        data = d.Data.ToString("dd/MM"),
                        d.TotalHoras,
                        d.TotalRegistros
                    }),
                    alertas = semLancamento.Select(a => new { a.Name }),
                    topClientesPorColaborador = topClientes.Select(t => new
                    {
                        t.Nome,
                        t.TotalHoras,
                        clientes = t.TopClientes.Select(c => new { c.Cliente, c.Horas, c.Percentual })
                    }),
                    consistencia = consistencia.Select(c => new
                    {
                        c.Nome,
                        c.DiasComLancamento,
                        c.DiasUteis,
                        c.Percentual
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
