using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppSystems.Data;
using WebAppSystems.Models;
using WebAppSystems.Models.Dto;
using WebAppSystems.Models.Enums;

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

        public async Task<List<ProcessRecord>> GetFinishedRecordsByDateAsync(DateTime from, DateTime to)
        {
            return await _context.ProcessRecord
                .Where(p => p.Date.Date >= from.Date && p.Date.Date <= to.Date
                         && p.HoraFinal != TimeSpan.Zero
                         && p.HoraFinal > p.HoraInicial)
                .ToListAsync();
        }

        public async Task<(IEnumerable<ProcessRecord> records, int totalRecords)> FindAllAsync(
            int page, int length, string searchValue = "", int orderColumn = 0,
            string orderDir = "desc", int? loggedUserId = null, ProfileEnum? perfil = null)
        {
            var query = _context.ProcessRecord
                .Include(pr => pr.Client)
                .Include(pr => pr.Attorney)
                .AsQueryable();

            if (perfil != ProfileEnum.Admin && loggedUserId.HasValue)
                query = query.Where(pr => pr.AttorneyId == loggedUserId.Value);

            if (!string.IsNullOrEmpty(searchValue))
                query = query.Where(pr =>
                    pr.Client.Name.ToLower().Contains(searchValue) ||
                    pr.Attorney.Name.ToLower().Contains(searchValue) ||
                    pr.Client.Solicitante.ToLower().Contains(searchValue) ||
                    pr.Description.ToLower().Contains(searchValue));

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
                _ => query.OrderByDescending(pr => pr.Date).ThenByDescending(pr => pr.HoraInicial)
            };

            int totalRecords = await query.CountAsync();
            var records = await query.Skip((page - 1) * length).Take(length).ToListAsync();
            return (records, totalRecords);
        }

        public async Task<List<GestaoColaboradorDto>> GetHorasPorColaboradorAsync(DateTime from, DateTime to)
        {
            var records = await _context.ProcessRecord
                .Include(p => p.Attorney)
                .Where(p => p.Date.Date >= from.Date && p.Date.Date <= to.Date
                         && p.HoraFinal != TimeSpan.Zero && p.HoraFinal > p.HoraInicial
                         && !p.Attorney.Inativo)
                .ToListAsync();

            return records
                .GroupBy(p => new { p.AttorneyId, p.Attorney.Name })
                .Select(g => new GestaoColaboradorDto
                {
                    AttorneyId = g.Key.AttorneyId,
                    Nome = g.Key.Name,
                    TotalHoras = Math.Round(g.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours), 2),
                    TotalRegistros = g.Count(),
                    UltimoLancamento = g.Max(p => p.Date)
                })
                .OrderByDescending(x => x.TotalHoras)
                .ToList();
        }

        public async Task<List<GestaoDiarioDto>> GetHorasPorDiaAsync(DateTime from, DateTime to)
        {
            var records = await _context.ProcessRecord
                .Where(p => p.Date.Date >= from.Date && p.Date.Date <= to.Date
                         && p.HoraFinal != TimeSpan.Zero && p.HoraFinal > p.HoraInicial)
                .ToListAsync();

            return records
                .GroupBy(p => p.Date.Date)
                .Select(g => new GestaoDiarioDto
                {
                    Data = g.Key,
                    TotalHoras = Math.Round(g.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours), 2),
                    TotalRegistros = g.Count()
                })
                .OrderBy(x => x.Data)
                .ToList();
        }

        public async Task<List<Attorney>> GetColaboradoresSemLancamentoAsync(int dias)
        {
            var desde = DateTime.Today.AddDays(-dias);
            var comLancamento = await _context.ProcessRecord
                .Where(p => p.Date.Date >= desde && p.HoraFinal != TimeSpan.Zero)
                .Select(p => p.AttorneyId).Distinct().ToListAsync();

            return await _context.Attorney
                .Where(a => !a.Inativo && !comLancamento.Contains(a.Id))
                .OrderBy(a => a.Name).ToListAsync();
        }

        public async Task<List<GestaoTopClientesDto>> GetTopClientesPorColaboradorAsync(DateTime from, DateTime to, int topN = 3)
        {
            var records = await _context.ProcessRecord
                .Include(p => p.Attorney).Include(p => p.Client)
                .Where(p => p.Date.Date >= from.Date && p.Date.Date <= to.Date
                         && p.HoraFinal != TimeSpan.Zero && p.HoraFinal > p.HoraInicial
                         && !p.Attorney.Inativo && !p.Client.ClienteInterno)
                .ToListAsync();

            return records
                .GroupBy(p => new { p.AttorneyId, p.Attorney.Name })
                .OrderByDescending(g => g.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours))
                .Select(g =>
                {
                    var total = g.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours);
                    return new GestaoTopClientesDto
                    {
                        AttorneyId = g.Key.AttorneyId,
                        Nome = g.Key.Name,
                        TotalHoras = Math.Round(total, 1),
                        TopClientes = g.GroupBy(p => new { p.ClientId, p.Client.Name })
                            .Select(cg => new TopClienteItem
                            {
                                Cliente = cg.Key.Name,
                                Horas = Math.Round(cg.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours), 1),
                                Percentual = total > 0
                                    ? Math.Round(cg.Sum(p => (p.HoraFinal - p.HoraInicial).TotalHours) / total * 100, 0)
                                    : 0
                            })
                            .OrderByDescending(c => c.Horas).Take(topN).ToList()
                    };
                })
                .ToList();
        }

        public async Task<List<GestaoConsistenciaDto>> GetConsistenciaLancamentosAsync(DateTime from, DateTime to)
        {
            var records = await _context.ProcessRecord
                .Include(p => p.Attorney)
                .Where(p => p.Date.Date >= from.Date && p.Date.Date <= to.Date
                         && p.HoraFinal != TimeSpan.Zero && p.HoraFinal > p.HoraInicial
                         && !p.Attorney.Inativo)
                .ToListAsync();

            var totalDias = (to.Date - from.Date).Days + 1;
            var diasUteis = Enumerable.Range(0, totalDias)
                .Select(i => from.Date.AddDays(i))
                .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);

            return records
                .GroupBy(p => new { p.AttorneyId, p.Attorney.Name })
                .Select(g => new GestaoConsistenciaDto
                {
                    Nome = g.Key.Name,
                    DiasComLancamento = g.Select(p => p.Date.Date).Distinct().Count(),
                    DiasUteis = diasUteis,
                    Percentual = diasUteis > 0
                        ? Math.Round(g.Select(p => p.Date.Date).Distinct().Count() * 100.0 / diasUteis, 0)
                        : 0
                })
                .OrderByDescending(x => x.Percentual)
                .ToList();
        }

        public ChartData GetChartData()
        {
            var m = DateTime.Now.Month; var y = DateTime.Now.Year;

            var clientHours = _context.ProcessRecord
               .Where(pr => pr.Date.Month == m && pr.Date.Year == y && pr.HoraFinal != TimeSpan.Zero)
               .Join(_context.Client.Where(c => !c.ClienteInterno),
                     pr => pr.ClientId, c => c.Id,
                     (pr, c) => new { c.Name, pr.HoraInicial, pr.HoraFinal })
               .AsEnumerable()
               .GroupBy(x => x.Name)
               .Select(g => new { ClientName = g.Key, TotalHours = g.Sum(x => (x.HoraFinal - x.HoraInicial).TotalHours) })
               .OrderByDescending(x => x.TotalHours)
               .ToList();

            return new ChartData
            {
                ClientNames = clientHours.Select(x => x.ClientName).ToList(),
                ClientValues = clientHours.Select(x => Math.Round(x.TotalHours, 2)).ToList()
            };
        }

        public ChartData GetChartDataByArea()
        {
            var m = DateTime.Now.Month; var y = DateTime.Now.Year;

            var areaHours = _context.ProcessRecord
                .Where(pr => pr.Date.Month == m && pr.Date.Year == y && pr.HoraFinal != TimeSpan.Zero)
                .Join(_context.Department, pr => pr.DepartmentId, d => d.Id,
                      (pr, d) => new { d.Name, pr.HoraInicial, pr.HoraFinal })
                .AsEnumerable()
                .GroupBy(x => x.Name)
                .Select(g => new { AreaName = g.Key, TotalHours = g.Sum(x => (x.HoraFinal - x.HoraInicial).TotalHours) })
                .OrderByDescending(x => x.TotalHours)
                .ToList();

            return new ChartData
            {
                ClientNames = areaHours.Select(x => x.AreaName).ToList(),
                ClientValues = areaHours.Select(x => Math.Round(x.TotalHours, 2)).ToList()
            };
        }

        public ChartData GetChartDataByRecordType()
        {
            var m = DateTime.Now.Month; var y = DateTime.Now.Year;
            var data = _context.ProcessRecord
                .Where(pr => pr.Date.Month == m && pr.Date.Year == y && pr.HoraFinal != TimeSpan.Zero)
                .AsEnumerable()
                .GroupBy(pr => pr.RecordType)
                .Select(g => new { RecordType = g.Key, TotalHours = g.Sum(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours) })
                .ToList();

            return new ChartData
            {
                ClientNames = data.Select(x => x.RecordType.ToString()).ToList(),
                ClientValues = data.Select(x => Math.Round(x.TotalHours, 2)).ToList()
            };
        }

        public ChartData GetChartDataByTimeline(string period = "month")
        {
            var now = DateTime.Now;
            var labels = new List<string>(); var values = new List<double>();

            if (period == "day")
            {
                var from = now.AddDays(-29).Date;
                var records = _context.ProcessRecord
                    .Where(pr => pr.Date.Date >= from && pr.HoraFinal != TimeSpan.Zero)
                    .Select(pr => new { pr.Date, pr.HoraInicial, pr.HoraFinal })
                    .AsEnumerable().ToList();

                for (int i = 29; i >= 0; i--)
                {
                    var date = now.AddDays(-i).Date;
                    var hours = records.Where(pr => pr.Date.Date == date).Sum(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours);
                    labels.Add(date.ToString("dd/MM")); values.Add(Math.Round(hours, 2));
                }
            }
            else if (period == "week")
            {
                var from = now.AddDays(-11 * 7 - (int)now.DayOfWeek).Date;
                var records = _context.ProcessRecord
                    .Where(pr => pr.Date.Date >= from && pr.HoraFinal != TimeSpan.Zero)
                    .Select(pr => new { pr.Date, pr.HoraInicial, pr.HoraFinal })
                    .AsEnumerable().ToList();

                for (int i = 11; i >= 0; i--)
                {
                    var start = now.AddDays(-i * 7 - (int)now.DayOfWeek).Date;
                    var end = start.AddDays(6);
                    var hours = records.Where(pr => pr.Date.Date >= start && pr.Date.Date <= end).Sum(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours);
                    labels.Add("Sem " + (12 - i)); values.Add(Math.Round(hours, 2));
                }
            }
            else
            {
                var from = now.AddMonths(-11);
                var fromDate = new DateTime(from.Year, from.Month, 1);
                var records = _context.ProcessRecord
                    .Where(pr => pr.Date >= fromDate && pr.HoraFinal != TimeSpan.Zero)
                    .Select(pr => new { pr.Date, pr.HoraInicial, pr.HoraFinal })
                    .AsEnumerable().ToList();

                for (int i = 11; i >= 0; i--)
                {
                    var date = now.AddMonths(-i);
                    var hours = records.Where(pr => pr.Date.Month == date.Month && pr.Date.Year == date.Year).Sum(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours);
                    labels.Add(date.ToString("MMM/yy")); values.Add(Math.Round(hours, 2));
                }
            }

            return new ChartData { ClientNames = labels, ClientValues = values };
        }
    }
}