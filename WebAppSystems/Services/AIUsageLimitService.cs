using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;

namespace WebAppSystems.Services
{
    public class AIUsageLimitService
    {
        private readonly WebAppSystemsContext _context;
        private readonly ILogger<AIUsageLimitService> _logger;

        public AIUsageLimitService(WebAppSystemsContext context, ILogger<AIUsageLimitService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Verifica se o usuário pode usar a IA (não ultrapassou o limite diário)
        /// </summary>
        public async Task<(bool CanUse, int RemainingUses, string Message)> CanUseAIAsync(int attorneyId)
        {
            try
            {
                var today = DateTime.Today;
                var usage = await GetOrCreateDailyUsageAsync(attorneyId, today);

                var remainingUses = usage.DailyLimit - usage.UsageCount;
                
                if (usage.UsageCount >= usage.DailyLimit)
                {
                    return (false, 0, $"Limite diário de {usage.DailyLimit} consultas de IA atingido. Aguarde até amanhã ou entre em contato com o administrador para upgrade do plano.");
                }

                return (true, remainingUses, $"Você ainda tem {remainingUses} consultas disponíveis hoje.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar limite de uso de IA para o usuário {attorneyId}");
                return (false, 0, "Erro interno. Tente novamente.");
            }
        }

        /// <summary>
        /// Registra o uso da IA pelo usuário
        /// </summary>
        public async Task<bool> RegisterAIUsageAsync(int attorneyId)
        {
            try
            {
                var today = DateTime.Today;
                var usage = await GetOrCreateDailyUsageAsync(attorneyId, today);

                if (usage.UsageCount >= usage.DailyLimit)
                {
                    _logger.LogWarning($"Tentativa de uso de IA acima do limite para usuário {attorneyId}");
                    return false;
                }

                usage.UsageCount++;
                usage.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Uso de IA registrado para usuário {attorneyId}. Uso atual: {usage.UsageCount}/{usage.DailyLimit}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar uso de IA para o usuário {attorneyId}");
                return false;
            }
        }

        /// <summary>
        /// Obtém ou cria o registro de uso diário para o usuário
        /// </summary>
        private async Task<AIUsageLimit> GetOrCreateDailyUsageAsync(int attorneyId, DateTime date)
        {
            var usage = await _context.AIUsageLimit
                .FirstOrDefaultAsync(u => u.AttorneyId == attorneyId && u.Date.Date == date.Date);

            if (usage == null)
            {
                usage = new AIUsageLimit
                {
                    AttorneyId = attorneyId,
                    Date = date,
                    UsageCount = 0,
                    DailyLimit = 10 // Limite padrão
                };

                _context.AIUsageLimit.Add(usage);
                await _context.SaveChangesAsync();
            }

            return usage;
        }

        /// <summary>
        /// Obtém estatísticas de uso do usuário
        /// </summary>
        public async Task<object> GetUsageStatsAsync(int attorneyId)
        {
            try
            {
                var today = DateTime.Today;
                var usage = await GetOrCreateDailyUsageAsync(attorneyId, today);

                var last7Days = await _context.AIUsageLimit
                    .Where(u => u.AttorneyId == attorneyId && u.Date >= today.AddDays(-7))
                    .OrderByDescending(u => u.Date)
                    .Select(u => new { u.Date, u.UsageCount, u.DailyLimit })
                    .ToListAsync();

                var result = new
                {
                    Today = new
                    {
                        Used = usage.UsageCount,
                        Limit = usage.DailyLimit,
                        Remaining = usage.DailyLimit - usage.UsageCount
                    },
                    Last7Days = last7Days
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter estatísticas de uso para o usuário {attorneyId}");
                return null;
            }
        }

        /// <summary>
        /// Atualiza o limite diário para um usuário (apenas admin)
        /// </summary>
        public async Task<bool> UpdateDailyLimitAsync(int attorneyId, int newLimit)
        {
            try
            {
                var today = DateTime.Today;
                var usage = await GetOrCreateDailyUsageAsync(attorneyId, today);
                
                usage.DailyLimit = newLimit;
                usage.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Limite diário atualizado para usuário {attorneyId}: {newLimit}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar limite diário para o usuário {attorneyId}");
                return false;
            }
        }

        /// <summary>
        /// Limpa registros antigos (manter apenas últimos 30 dias)
        /// </summary>
        public async Task CleanupOldRecordsAsync()
        {
            try
            {
                var cutoffDate = DateTime.Today.AddDays(-30);
                var oldRecords = await _context.AIUsageLimit
                    .Where(u => u.Date < cutoffDate)
                    .ToListAsync();

                if (oldRecords.Any())
                {
                    _context.AIUsageLimit.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Removidos {oldRecords.Count} registros antigos de uso de IA");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar registros antigos de uso de IA");
            }
        }
    }
}