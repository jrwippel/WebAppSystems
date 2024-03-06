using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;

namespace WebAppSystems.Services
{
    public class ProcessRecordsService
    {

        private readonly WebAppSystemsContext _context;

        public ProcessRecordsService(WebAppSystemsContext context)
        {
            _context = context;
        }
        public async Task<ProcessRecord> FindByIdAsync(int id)
        {
            return await _context.ProcessRecord
                .Include(obj => obj.Attorney)
                .Include(obj => obj.Client)
                .Include(obj => obj.Department)
                .FirstOrDefaultAsync(obj => obj.Id == id);
        }
        public async Task<List<ProcessRecord>> FindAllAsync()
        {
            var processRecords = await _context.ProcessRecord
                .Include(pr => pr.Attorney)
                .OrderByDescending(pr => pr.Date)
                .ThenByDescending(pr => pr.HoraInicial) // Ordena por hora inicial em ordem descendente
                .ToListAsync();

            return processRecords;
        }



        /*
        public async Task<List<ProcessRecord>> FindAllAsync()
        {
            return await _context.ProcessRecord.OrderByDescending(pr => pr.Date).ToListAsync();
        }
        */
    }
}
