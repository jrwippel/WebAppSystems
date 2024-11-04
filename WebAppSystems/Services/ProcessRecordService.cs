using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebAppSystems.Data;
using WebAppSystems.Migrations;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Models.ViewModels;

namespace WebAppSystems.Services
{
    public class ProcessRecordService
    {

        private readonly WebAppSystemsContext _context;

        public ProcessRecordService(WebAppSystemsContext context)
        {
            _context = context;
        }

        public async Task<List<ProcessRecord>> FindByDateAsync(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId, RecordType? recordType)
        {
            var result = from obj in _context.ProcessRecord select obj;
            if (minDate.HasValue)
            {
                result = result.Where(x => x.Date >= minDate.Value);
            }
            if (maxDate.HasValue)
            {
                result = result.Where(x => x.Date <= maxDate.Value);
            }

            // Inclua o filtro por cliente (clientId) caso ele tenha sido especificado
            if (clientId.HasValue)
            {
                result = result.Where(x => x.ClientId == clientId.Value);
            }
            if (attorneyId.HasValue)
            {
                result = result.Where(x => x.AttorneyId == attorneyId.Value);
            }

            if (recordType.HasValue)
            {
                result = result.Where(x => x.RecordType == recordType.Value);
            }

            return await result
                .Include(x => x.Attorney)
                .Include(x => x.Attorney.Department)
                .Include(x => x.Client)
                .Include(x => x.Department)
                .OrderBy(x => x.Date)  // Ordena primeiramente por Data em ordem crescente
                .ThenBy(x => x.HoraInicial)  // Ordena por Hora Inicial em ordem crescente
                .ToListAsync();
        }

        public async Task<List<MensalistaHoursViewModel>> FindByDateMensalistaAsync(DateTime? minDate, DateTime? maxDate, int? clientId, int? departmentId, QueryType queryType = QueryType.Monthly)

        {

            if ((queryType == QueryType.Average || queryType == QueryType.Cumulative) && maxDate.HasValue)
            {
                // Define minDate para o primeiro dia do mês, 2 meses antes
                minDate = new DateTime(maxDate.Value.AddMonths(-2).Year, maxDate.Value.AddMonths(-2).Month, 1);
                // Mantemos o maxDate como está, representando o final do terceiro mês
            }
            //var mensalistas = _context.Mensalista.Include(m => m.MensalistaDepartments).Include(m => m.Client).AsQueryable();

            var mensalistas = _context.Mensalista.Include(m => m.Client).AsQueryable();


            // Filtro por cliente
            if (clientId.HasValue)
            {
                mensalistas = mensalistas.Where(x => x.ClientId == clientId.Value);
            }

            var mensalistasList = await mensalistas.ToListAsync();

            List<MensalistaHoursViewModel> results = new List<MensalistaHoursViewModel>();

            foreach (var mensalista in mensalistasList)
            {
                var processRecords = _context.ProcessRecord.Where(x => x.ClientId == mensalista.ClientId);               

                // Filtros de data
                if (minDate.HasValue)
                {
                    processRecords = processRecords.Where(x => x.Date >= minDate.Value);
                }
                if (maxDate.HasValue)
                {
                    processRecords = processRecords.Where(x => x.Date <= maxDate.Value);
                }

                if (departmentId.HasValue)
                {
                    processRecords = processRecords.Where(x => x.DepartmentId == departmentId.Value);
                }
                var recordsList = await processRecords.ToListAsync();
                var totalHours = Convert.ToDecimal(recordsList.Sum(x => x.CalculoHorasDecimal()));
                if (queryType == QueryType.Average)
                {
                    totalHours /= 3;  // Divida por 3 para obter a média mensal
                }

                decimal valorTotalHoras = 0;
                foreach (var record in recordsList)
                {
                    var userId = record.AttorneyId;
                    //var valorPorHora = Convert.ToDecimal(_context.PrecoCliente
                    var valorPorHora = Convert.ToDecimal(_context.ValorCliente
                        .FirstOrDefault(pc => pc.ClientId == mensalista.ClientId && pc.AttorneyId == userId)?.Valor ?? 0.0);

                    valorTotalHoras += (decimal)(record.CalculoHorasDecimal()) * valorPorHora;
                }

                if (queryType == QueryType.Average)
                {
                    valorTotalHoras /= 3;  // Divida por 3 para obter a média mensal
                }
                decimal percentual = 0;
                if (departmentId.HasValue)
                {
                    percentual = GetMensalistaDepartmentPercentual(mensalista, departmentId.Value);
                }                

                decimal tributos = mensalista.ValorMensalBruto * (decimal)0.1453;
                decimal valorMensalLiquido = mensalista.ValorMensalBruto - tributos - mensalista.ComissaoParceiro - mensalista.ComissaoSocio;
                decimal valorHoraTecLiquida = valorTotalHoras - (valorTotalHoras * (decimal)0.1453);
                decimal valorAreaLiquido = valorMensalLiquido * percentual / 100;
                decimal valorResultadoBruto = (mensalista.ValorMensalBruto * percentual / 100) - valorTotalHoras;
                decimal valorResultadoLiquido = valorAreaLiquido - valorHoraTecLiquida;

                if (queryType == QueryType.Cumulative)
                {
                    valorResultadoBruto = ((mensalista.ValorMensalBruto * percentual / 100) * 3) - valorTotalHoras;
                    valorResultadoLiquido = (valorAreaLiquido * 3) - valorHoraTecLiquida;
                }

                // Inicialize a lista TotalHoursPerMonth
                var totalHoursPerMonth = new List<double>();

                // Determine o primeiro mês e ano no intervalo fornecido
                DateTime currentMonth = new DateTime(minDate.Value.Year, minDate.Value.Month, 1);



                results.Add(new MensalistaHoursViewModel
                {
                    Mensalista = mensalista,
                    TotalHours = (double)totalHours,
                    ValorTotalHoras = valorTotalHoras,
                    Percentual = percentual,
                    ValorAreaBruto = mensalista.ValorMensalBruto * percentual / 100,                   
                    ValorResultadoBruto = valorResultadoBruto,                    
                    Tributos = tributos,
                    ValorMensalLiquido = valorMensalLiquido,                    
                    ValorHoraTecLiquida = valorHoraTecLiquida,
                    ValorAreaLiquido = valorAreaLiquido,
                    ValorResultadoLiquido = valorResultadoLiquido
                  
                });
            }
            return results;
        }

    //    private decimal GetMensalistaDepartmentPercentual(Mensalista mensalista, int departmentId)
   //     {
  //          return mensalista.MensalistaDepartments
  //                           .FirstOrDefault(md => md.DepartmentId == departmentId)?.Percentual ?? 0;
  //      }

        private decimal GetMensalistaDepartmentPercentual(Mensalista mensalista, int departmentId)
        {
            // Busca as informações de PercentualArea diretamente no banco de dados
            PercentualArea percentualArea = _context.PercentualArea
                .FirstOrDefault(pa =>
                    pa.ClientId == mensalista.ClientId && pa.DepartmentId == departmentId);

            return percentualArea?.Percentual ?? 0;
        }


        public async Task<List<IGrouping<Department, ProcessRecord>>> FindByDateGroupingAsync(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId)
        {
            var result = from obj in _context.ProcessRecord select obj;
            if (minDate.HasValue)
            {
                result = result.Where(x => x.Date >= minDate.Value);
            }
            if (maxDate.HasValue)
            {
                result = result.Where(x => x.Date <= maxDate.Value);
            }

            var data = await result
                .Include(x => x.Attorney)
                .Include(x => x.Attorney.Department)
                .Include(x => x.Client)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
            return data.GroupBy(x => x.Attorney.Department).ToList();
        }

    }
}
