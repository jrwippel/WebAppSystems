using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;

namespace WebAppSystems.Services
{
    public class ValorClienteService
    {
        private readonly WebAppSystemsContext _context;

        public ValorClienteService(WebAppSystemsContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Compatibilidade: retorna objeto ValorCliente com fallback padrão do cliente.
        /// </summary>
        public async Task<ValorCliente?> GetValorForClienteAndUserAsync(int clientId, int userId)
        {
            var excecao = await _context.ValorCliente
                .Where(v => v.ClientId == clientId && v.AttorneyId == userId)
                .FirstOrDefaultAsync();
            if (excecao != null) return excecao;

            return await _context.ValorCliente
                .Where(v => v.ClientId == clientId && v.AttorneyId == null)
                .FirstOrDefaultAsync();
        }

        public async Task<ValorCliente?> GetValorForClienteAndUserIdAsync(int clientId, int userId)
            => await GetValorForClienteAndUserAsync(clientId, userId);

        /// <summary>
        /// Retorna o valor/hora para um advogado num cliente.
        /// Prioridade: exceção do advogado > valor padrão do cliente > 0
        /// </summary>
        public async Task<double> GetValorAsync(int clientId, int attorneyId)
        {
            // 1. Exceção específica do advogado
            var excecao = await _context.ValorCliente
                .Where(v => v.ClientId == clientId && v.AttorneyId == attorneyId)
                .FirstOrDefaultAsync();
            if (excecao != null) return excecao.Valor;

            // 2. Valor padrão do cliente (AttorneyId == null)
            var padrao = await _context.ValorCliente
                .Where(v => v.ClientId == clientId && v.AttorneyId == null)
                .FirstOrDefaultAsync();
            return padrao?.Valor ?? 0.0;
        }

        /// <summary>
        /// Versão síncrona para uso dentro de loops já carregados em memória.
        /// Recebe a lista pré-carregada para evitar N+1 queries.
        /// </summary>
        public static double GetValor(IEnumerable<ValorCliente> valores, int clientId, int attorneyId)
        {
            var excecao = valores.FirstOrDefault(v => v.ClientId == clientId && v.AttorneyId == attorneyId);
            if (excecao != null) return excecao.Valor;

            var padrao = valores.FirstOrDefault(v => v.ClientId == clientId && v.AttorneyId == null);
            return padrao?.Valor ?? 0.0;
        }
    }
}
