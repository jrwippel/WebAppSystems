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
        public async Task<ValorCliente> GetValorForClienteAndUserAsync(int clientId, int userId)
        {
            return await _context.ValorCliente
                .Where(p => p.ClientId == clientId && p.AttorneyId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<ValorCliente> GetValorForClienteAndUserIdAsync(int clientId, int userId)
        {
            return await _context.ValorCliente
                .Include(p => p.Client)
                .Include(p => p.Attorney)
                .Where(p => p.ClientId == clientId && p.AttorneyId == userId)
                .FirstOrDefaultAsync();
        }

    }
}
