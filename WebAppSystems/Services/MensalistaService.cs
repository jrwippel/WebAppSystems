using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;

namespace WebAppSystems.Services
{
    public class MensalistaService
    {
        private readonly WebAppSystemsContext _context;

        public MensalistaService(WebAppSystemsContext context)
        {
            _context = context;
        }

        public async Task<List<Mensalista>> FindAllAsync()
        {
            // return await _context.Attorney.ToListAsync();
            var mensalistas = await _context.Mensalista.ToListAsync();
            return mensalistas;

        }

        public async Task<Mensalista> FindByIdAsync(int id)
        {
            return await _context.Mensalista.FirstOrDefaultAsync(mensalista => mensalista.Id == id);
        }

    }
}
