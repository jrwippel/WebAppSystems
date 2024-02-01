using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;

namespace WebAppSystems.Services
{
    public class PrecoClienteService
    {
        private readonly WebAppSystemsContext _context;

        public PrecoClienteService(WebAppSystemsContext context)
        {
            _context = context;
        }
        public async Task<PrecoCliente> GetPrecoForClienteAndDepartmentAsync(int clientId, int departmentId)
        {
            return await _context.PrecoCliente
                .Where(p => p.ClientId == clientId && p.DepartmentId == departmentId)
                .FirstOrDefaultAsync();
        }

        public async Task<PrecoCliente> GetPrecoForClienteAndDepartmentIdAsync(int clientId, int departmentId)
        {
            return await _context.PrecoCliente
                .Include(p => p.Client)
                .Include(p => p.Department)
                .Where(p => p.ClientId == clientId && p.DepartmentId == departmentId)
                .FirstOrDefaultAsync();
        }

    }
}
