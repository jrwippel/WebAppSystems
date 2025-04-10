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


        public async Task<(IEnumerable<ProcessRecord> records, int totalRecords)> FindAllAsync(
            int page,
            int length,
            string searchValue = "",
            int orderColumn = 0,
            string orderDir = "desc"           
            )
        {
            var query = _context.ProcessRecord
                .Include(pr => pr.Client)
                .Include(pr => pr.Attorney)
                .AsQueryable();

            // Filtro de pesquisa
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(pr =>
                    pr.Client.Name.ToLower().Contains(searchValue) ||
                    pr.Attorney.Name.ToLower().Contains(searchValue) ||
                    pr.Client.Solicitante.ToLower().Contains(searchValue) ||                    
                    pr.Description.ToLower().Contains(searchValue));
            }  

            // Ordenação: padrão é Data desc e HoraInicial desc
            query = orderColumn switch
            {
                0 => orderDir == "desc"
                    ? query.OrderBy(pr => pr.Date).ThenBy(pr => pr.HoraInicial)
                    : query.OrderByDescending(pr => pr.Date).ThenByDescending(pr => pr.HoraInicial),
                1 => orderDir == "asc"
                    ? query.OrderBy(pr => pr.HoraInicial).ThenBy(pr => pr.Date)
                    : query.OrderByDescending(pr => pr.HoraInicial).ThenByDescending(pr => pr.Date),
                2 => orderDir == "asc"
                    ? query.OrderBy(pr => pr.Client.Name)
                    : query.OrderByDescending(pr => pr.Client.Name),
                _ => query.OrderByDescending(pr => pr.Date).ThenByDescending(pr => pr.HoraInicial) // Default
            };

            // Total de registros filtrados
            int totalRecords = await query.CountAsync();

            // Paginação após ordenação
            var records = await query
                .Skip((page - 1) * length)
                .Take(length)
                .ToListAsync();

            return (records, totalRecords);
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

        public ChartData GetChartDataByArea()
        {
            // Obtém o mês e o ano correntes
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var areaHours = _context.ProcessRecord
                .Where(pr => pr.Date.Month == currentMonth && pr.Date.Year == currentYear && pr.HoraFinal != TimeSpan.Zero)
                .ToList()
                .GroupBy(pr => pr.DepartmentId) // Agrupar por área
                .Select(g => new { AreaId = g.Key, TotalHours = g.Sum(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours) })
                .ToList();

            var areaNames = new List<string>();
            var areaValues = new List<double>();

            foreach (var item in areaHours)
            {
                var area = _context.Department.FirstOrDefault(d => d.Id == item.AreaId);
                if (area != null)
                {
                    areaNames.Add(area.Name);
                    areaValues.Add(Math.Round(item.TotalHours, 2));
                }
            }

            return new ChartData { ClientNames = areaNames, ClientValues = areaValues };
        }


    }
}
