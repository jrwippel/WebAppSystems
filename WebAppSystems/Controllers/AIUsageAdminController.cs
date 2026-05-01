using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class AIUsageAdminController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ISessao _sessao;
        private readonly AIUsageLimitService _aiUsageLimitService;
        private readonly ILogger<AIUsageAdminController> _logger;

        public AIUsageAdminController(
            WebAppSystemsContext context,
            ISessao sessao,
            AIUsageLimitService aiUsageLimitService,
            ILogger<AIUsageAdminController> logger)
        {
            _context = context;
            _sessao = sessao;
            _aiUsageLimitService = aiUsageLimitService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var usuario = _sessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;

                var today = DateTime.Today;
                var last30Days = today.AddDays(-30);

                // Buscar todos os registros de uso dos últimos 30 dias (incluindo admins)
                var usageRecords = await _context.AIUsageLimit
                    .Include(u => u.Attorney)
                    .Where(u => u.Date >= last30Days)
                    .ToListAsync();
                
                // Filtrar apenas registros com uso real
                var recordsWithUsage = usageRecords.Where(u => u.UsageCount > 0).ToList();

                // Se não houver registros com uso, mostrar mensagem
                if (!recordsWithUsage.Any())
                {
                    ViewBag.NoUsageData = true;
                    return View(new List<dynamic>());
                }

                // Agrupar e processar os dados em memória - SIMPLIFICADO
                var usageData = recordsWithUsage
                    .GroupBy(u => new { u.AttorneyId, u.Attorney.Name, u.Attorney.Email })
                    .Select(g => {
                        var todayRecord = g.FirstOrDefault(u => u.Date.Date == today);
                        var usageToday = todayRecord?.UsageCount ?? 0;
                        var dailyLimit = todayRecord?.DailyLimit ?? 10;
                        
                        return new
                        {
                            AttorneyId = g.Key.AttorneyId,
                            AttorneyName = g.Key.Name,
                            Email = g.Key.Email,
                            UsageCount = usageToday,
                            DailyLimit = dailyLimit,
                            Remaining = dailyLimit - usageToday,
                            LastUsed = g.Max(u => u.Date),
                            TotalUsage = g.Sum(u => u.UsageCount)
                        };
                    })
                    .Where(x => x.TotalUsage > 0) // Garantir que só mostra usuários com uso
                    .OrderByDescending(x => x.LastUsed)
                    .ThenByDescending(x => x.TotalUsage)
                    .ToList();

                // Garantir que não está passando ViewBag.NoUsageData = true se há dados
                ViewBag.NoUsageData = false;

                return View(usageData);
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no Index do AIUsageAdmin");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLimit(int attorneyId, int newLimit)
        {
            try
            {
                if (newLimit < 0 || newLimit > 1000)
                {
                    return Json(new { success = false, message = "Limite deve estar entre 0 e 1000." });
                }

                var success = await _aiUsageLimitService.UpdateDailyLimitAsync(attorneyId, newLimit);
                
                if (success)
                {
                    return Json(new { success = true, message = "Limite atualizado com sucesso!" });
                }
                else
                {
                    return Json(new { success = false, message = "Erro ao atualizar limite." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar limite para usuário {attorneyId}");
                return Json(new { success = false, message = "Erro interno." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsageHistory(int attorneyId, int days = 30)
        {
            try
            {
                var startDate = DateTime.Today.AddDays(-days);
                var history = await _context.AIUsageLimit
                    .Where(u => u.AttorneyId == attorneyId && u.Date >= startDate)
                    .OrderByDescending(u => u.Date)
                    .ToListAsync();

                var result = history.Select(u => new
                {
                    Date = u.Date.ToString("dd/MM/yyyy"),
                    u.UsageCount,
                    u.DailyLimit,
                    Remaining = u.DailyLimit - u.UsageCount
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar histórico para usuário {attorneyId}");
                return Json(new { success = false, message = "Erro ao buscar histórico." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetDailyUsage(int attorneyId)
        {
            try
            {
                var today = DateTime.Today;
                var usage = await _context.AIUsageLimit
                    .FirstOrDefaultAsync(u => u.AttorneyId == attorneyId && u.Date.Date == today);

                if (usage != null)
                {
                    usage.UsageCount = 0;
                    usage.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Uso diário resetado com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao resetar uso diário para usuário {attorneyId}");
                return Json(new { success = false, message = "Erro ao resetar uso diário." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOverallStats()
        {
            try
            {
                var today = DateTime.Today;
                var last7Days = today.AddDays(-7);
                var last30Days = today.AddDays(-30);

                // Buscar todos os registros necessários de uma vez
                var allUsage = await _context.AIUsageLimit
                    .Where(u => u.Date >= last30Days)
                    .ToListAsync();

                var todayUsage = allUsage.Where(u => u.Date.Date == today).ToList();
                var last7DaysUsage = allUsage.Where(u => u.Date >= last7Days).ToList();

                var stats = new
                {
                    TotalUsersWithAI = allUsage
                        .Where(u => u.UsageCount > 0)
                        .Select(u => u.AttorneyId)
                        .Distinct()
                        .Count(),
                    
                    TodayUsage = todayUsage.Sum(u => u.UsageCount),
                    
                    Last7DaysUsage = last7DaysUsage.Sum(u => u.UsageCount),
                    
                    Last30DaysUsage = allUsage.Sum(u => u.UsageCount),
                    
                    UsersAtLimit = todayUsage
                        .Where(u => u.UsageCount >= u.DailyLimit)
                        .Count(),

                    ActiveUsersToday = todayUsage
                        .Where(u => u.UsageCount > 0)
                        .Count(),

                    AverageUsageToday = todayUsage
                        .Where(u => u.UsageCount > 0)
                        .Average(u => (double?)u.UsageCount) ?? 0
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas gerais");
                return Json(new { success = false, message = "Erro ao buscar estatísticas." });
            }
        }
    }
}