using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models.Dto;
using WebAppSystems.Models.Enums;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;
using System.Text;
using System.Text.Json;

namespace WebAppSystems.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class PainelGestaoController : Controller
    {
        private readonly ProcessRecordsService _service;
        private readonly ISessao _isessao;
        private readonly WebAppSystemsContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AIService _aiService;
        private readonly AIUsageLimitService _aiUsageLimitService;

        public PainelGestaoController(ProcessRecordsService service, ISessao isessao, WebAppSystemsContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory, AIService aiService, AIUsageLimitService aiUsageLimitService)
        {
            _service = service;
            _isessao = isessao;
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _aiService = aiService;
            _aiUsageLimitService = aiUsageLimitService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                var aiConfig = await _context.AIConfiguration.FirstOrDefaultAsync();
                ViewBag.AIAtivo = aiConfig != null && aiConfig.IsActive;
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

        [HttpPost]
        public async Task<IActionResult> AnalisarGrafico([FromBody] AnalisarGraficoRequest request)
        {
            if (request == null)
                return BadRequest(new { erro = "Requisição inválida." });

            try
            {
                var usuario = _isessao.BuscarSessaoDoUsuario();

                // Verificar limite de uso de IA
                var (canUse, remainingUses, limitMessage) = await _aiUsageLimitService.CanUseAIAsync(usuario.Id);
                if (!canUse)
                {
                    return StatusCode(429, new { erro = limitMessage });
                }

                // Verificar se a IA está configurada
                var (isConfigured, errorMessage) = await _aiService.IsConfiguredAsync();
                if (!isConfigured)
                    return StatusCode(503, new { erro = errorMessage });

                var prompt = ConstruirPromptAnalise(request);

                var insight = await _aiService.GenerateContentAsync(prompt, maxTokens: 1024, temperature: 0.7);
                
                // Registrar o uso da IA
                await _aiUsageLimitService.RegisterAIUsageAsync(usuario.Id);
                
                return Ok(new { insight, remainingUses = remainingUses - 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { erro = $"Erro ao gerar análise: {ex.Message}" });
            }
        }

        private string ConstruirPromptAnalise(AnalisarGraficoRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Você é um consultor de gestão jurídica. Analise os dados do gráfico '{request.TituloGrafico}' e forneça insights executivos concisos:");
            sb.AppendLine();
            sb.AppendLine($"Período: {request.PeriodoInicio} até {request.PeriodoFim}");
            sb.AppendLine();
            sb.AppendLine("Dados:");
            sb.AppendLine(JsonSerializer.Serialize(request.Dados, new JsonSerializerOptions { WriteIndented = true }));
            sb.AppendLine();
            sb.AppendLine("Forneça uma análise executiva em formato de bullet points:");
            sb.AppendLine("• **O que chama atenção:** 1-2 observações mais importantes dos dados");
            sb.AppendLine("• **Como melhorar:** 1-2 sugestões práticas para otimizar resultados");
            sb.AppendLine("• **Ponto de atenção:** 1 aspecto que merece cuidado (se houver)");
            sb.AppendLine();
            sb.AppendLine("DIRETRIZES IMPORTANTES:");
            sb.AppendLine("- Máximo 6 linhas no total");
            sb.AppendLine("- Use números e percentuais específicos");
            sb.AppendLine("- SEMPRE analise a relação entre horas trabalhadas e quantidade de registros");
            sb.AppendLine("- Calcule a média de horas por registro para identificar padrões de eficiência");
            sb.AppendLine("- Muitos registros com poucas horas = tarefas fragmentadas");
            sb.AppendLine("- Poucos registros com muitas horas = tarefas concentradas");
            sb.AppendLine("- Use linguagem natural e direta");

            return sb.ToString();
        }

        [HttpGet]
        public async Task<IActionResult> GetAIUsageStats()
        {
            try
            {
                var usuario = _isessao.BuscarSessaoDoUsuario();
                var stats = await _aiUsageLimitService.GetUsageStatsAsync(usuario.Id);
                
                // Log para debug
                Console.WriteLine($"[DEBUG] GetAIUsageStats para usuário {usuario.Id}: {System.Text.Json.JsonSerializer.Serialize(stats)}");
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetAIUsageStats: {ex.Message}");
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }

    public class AnalisarGraficoRequest
    {
        public string TituloGrafico { get; set; } = string.Empty;
        public string PeriodoInicio { get; set; } = string.Empty;
        public string PeriodoFim { get; set; } = string.Empty;
        public object Dados { get; set; } = new { };
    }
}
