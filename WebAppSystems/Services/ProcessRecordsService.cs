using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<(List<ProcessRecord>, int)> FindAllAsync(int page, int pageSize)
        {
            var query = _context.ProcessRecord
                .Include(pr => pr.Attorney)
                .Include(pr => pr.Client)
                .Where(pr => pr.HoraInicial != TimeSpan.Zero && pr.HoraFinal != TimeSpan.Zero)
                .OrderByDescending(pr => pr.Date)
                .ThenByDescending(pr => pr.HoraInicial);

            int totalRecords = await query.CountAsync(); // Total de registros
            var processRecords = await query
                .Skip((page - 1) * pageSize) // Pula os registros das páginas anteriores
                .Take(pageSize) // Traz apenas o número de registros necessários
                .ToListAsync();

            return (processRecords, totalRecords);
        }



        public ChartData GetChartData()
        {
            // Obtém o mês e o ano correntes
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var clientHours = _context.ProcessRecord
               .Where(pr => pr.Date.Month == currentMonth && pr.Date.Year == currentYear && pr.HoraFinal != TimeSpan.Zero) // Verifica se HoraFinal não é zero
               .ToList() // Executa a consulta e traz os resultados para a memória
               .GroupBy(pr => pr.ClientId)
               .Select(g => new { ClientId = g.Key, TotalHours = g.Sum(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours) })
               .ToList();

            // Obtém os nomes dos clientes e suas horas gastas
            var clientNames = new List<string>();
            var clientValues = new List<double>(); // Alterado para List<double>

            foreach (var item in clientHours)
            {
                var client = _context.Client.FirstOrDefault(c => c.Id == item.ClientId && !c.ClienteInterno);
                if (client != null)
                {
                    clientNames.Add(client.Name);
                    clientValues.Add(Math.Round(item.TotalHours, 2)); // Arredonda para duas casas decimais
                }
            }

            // Retorna os dados como um objeto ChartData
            return new ChartData { ClientNames = clientNames, ClientValues = clientValues };
        }






    }
}
