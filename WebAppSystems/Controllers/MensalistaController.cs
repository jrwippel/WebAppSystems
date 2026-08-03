using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.Pkcs;
using System.Globalization;
using System.Text;
using WebAppSystems.Filters;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using WebAppSystems.Models.Enums;
using NPOI.SS.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;


namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    [PaginaRestritaSomenteAdmin]
    public class MensalistaController : Controller
    {


        private readonly ProcessRecordService _processRecordService;

        private readonly ClientService _clientService;

        private readonly AttorneyService _attorneyService;        

        private readonly DepartmentService _departmentService;

        private readonly IWebHostEnvironment _env;

        private readonly MensalistaService _mensalistaService;
        private readonly WebAppSystemsContext _context;
        private ICellStyle lightGrayStyle;
        private ICellStyle veryLightGrayStyle;

        public MensalistaController(ProcessRecordService processRecordService, ClientService clientService, AttorneyService attorneyService, IWebHostEnvironment env,
            DepartmentService departmentService, MensalistaService mensalistaService, WebAppSystemsContext context)
        {
            _processRecordService = processRecordService;
            _clientService = clientService;
            _attorneyService = attorneyService;            
            _env = env;
            _departmentService = departmentService;
            _mensalistaService = mensalistaService;
            _context = context;

        }

        public async Task<IActionResult> Index(string monthYearString, int? clientId, int? departmentId)
        {
            await PopulateViewBag();

            if (string.IsNullOrEmpty(monthYearString) && !clientId.HasValue && !departmentId.HasValue)
                return View(null);

            DateTime monthYear = DateTime.Now;
            if (!string.IsNullOrEmpty(monthYearString) && monthYearString.Length == 6)
            {
                int month = int.Parse(monthYearString.Substring(0, 2));
                int year  = int.Parse(monthYearString.Substring(2, 4));
                monthYear = new DateTime(year, month, 1);
            }

            ConvertMonthYearToRange(monthYear, out DateTime minDate, out DateTime maxDate);
            PopulateViewData(monthYear, clientId, departmentId);
            ViewData["inputMonthYear"] = monthYearString;

            var result = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId);
            result.Sort((a, b) => a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido));

            return View(result);
        }

        // Mantido para compatibilidade com links antigos
        public IActionResult SimpleSearch(string monthYearString, int? clientId, int? departmentId)
        {
            return RedirectToAction(nameof(Index), new { monthYearString, clientId, departmentId });
        }


        #region Private Helpers

        private void ConvertMonthYearToRange(DateTime monthYear, out DateTime minDate, out DateTime maxDate)
        {
            minDate = new DateTime(monthYear.Year, monthYear.Month, 1);
            maxDate = minDate.AddMonths(1).AddDays(-1);
        }


        private void SetDefaultDateValues(ref DateTime? minDate, ref DateTime? maxDate)
        {
            if (!minDate.HasValue)
            {
                minDate = new DateTime(DateTime.Now.Year, 1, 1);
            }
            if (!maxDate.HasValue)
            {
                maxDate = DateTime.Now;
            }
        }

        // -- Painel de Rentabilidade dos Mensalistas --------------------------

        public async Task<IActionResult> Rentabilidade(string? periodo, DateTime? dataInicio, DateTime? dataFim, string? clienteIds)
        {
            // Definir período
            DateTime inicio, fim;
            string periodoAtual = periodo ?? "mes";

            switch (periodoAtual)
            {
                case "semestre":
                    inicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);
                    fim = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
                    break;
                case "custom":
                    inicio = dataInicio ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    fim = dataFim ?? DateTime.Now.Date;
                    // Limitar período máximo de 12 meses
                    if ((fim - inicio).TotalDays > 366)
                    {
                        inicio = fim.AddMonths(-12);
                        TempData["MensagemAviso"] = "Período limitado a 12 meses.";
                    }
                    break;
                default: // mes
                    periodoAtual = "mes";
                    inicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    fim = DateTime.Now.Date;
                    break;            }

            // Buscar todos os mensalistas com seus clientes
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var mensalistas = await _mensalistaService.FindAllAsync();
            var clientIdsMensalistas = mensalistas.Select(m => m.ClientId).ToList();
            var t1 = sw.ElapsedMilliseconds;
            
            // Buscar clientes dos mensalistas (com logo)
            var clients = await _context.Client
                .AsNoTracking()
                .Where(c => clientIdsMensalistas.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.ImageData, c.ImageMimeType })
                .ToListAsync();
            var t2 = sw.ElapsedMilliseconds;

            // Query otimizada: busca apenas ClientId + HoraInicial + HoraFinal sem joins
            var registrosResumo = await _context.ProcessRecord
                .AsNoTracking()
                .Where(p => p.Date >= inicio && p.Date <= fim && clientIdsMensalistas.Contains(p.ClientId))
                .Select(p => new { p.ClientId, p.HoraInicial, p.HoraFinal })
                .ToListAsync();
            var t3 = sw.ElapsedMilliseconds;
            sw.Stop();

            // Debug: tempo de cada query
            ViewBag.DebugTempo = $"Mensalistas: {t1}ms | Clientes: {t2 - t1}ms | Registros ({registrosResumo.Count}): {t3 - t2}ms | Total: {t3}ms";

            var horasDict = registrosResumo
                .GroupBy(r => r.ClientId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => (r.HoraFinal - r.HoraInicial).TotalHours)
                );

            var cards = new List<CardMensalista>();

            foreach (var m in mensalistas)
            {
                var client = clients.FirstOrDefault(c => c.Id == m.ClientId);
                if (client == null) continue;

                // Pegar horas do dicionário pré-calculado
                var horasApontadas = horasDict.ContainsKey(m.ClientId) ? horasDict[m.ClientId] : 0;

                var valorHora = m.GetValorHoraEfetivo();
                var valorConsumido = (decimal)horasApontadas * valorHora;

                // Se for semestre, dividir a mensalidade pelo número de meses para comparar
                decimal mensalidadeReferencia = m.ValorMensalBruto;
                if (periodoAtual == "semestre")
                    mensalidadeReferencia = m.ValorMensalBruto * 6;
                else if (periodoAtual == "custom")
                {
                    // Calcular quantos meses abrange o período
                    var meses = ((fim.Year - inicio.Year) * 12) + fim.Month - inicio.Month + 1;
                    mensalidadeReferencia = m.ValorMensalBruto * meses;
                }

                var saldo = mensalidadeReferencia - valorConsumido;
                var percentual = mensalidadeReferencia > 0 ? (double)(valorConsumido / mensalidadeReferencia) * 100 : 0;

                string status, statusTexto;
                if (percentual > 100)
                {
                    status = "vermelho";
                    statusTexto = "Revisão Recomendada";
                }
                else if (percentual >= 80)
                {
                    status = "amarelo";
                    statusTexto = "Atenção";
                }
                else
                {
                    status = "verde";
                    statusTexto = "Equilibrado";
                }

                cards.Add(new CardMensalista
                {
                    MensalistaId = m.Id,
                    ClienteId = client.Id,
                    ClienteNome = client.Name,
                    ClienteLogo = client.ImageData != null && client.ImageData.Length != 13536 ? client.ImageData : null,
                    ClienteLogoMime = client.ImageData != null && client.ImageData.Length != 13536 ? client.ImageMimeType : null,
                    ValorMensalidade = mensalidadeReferencia,
                    ValorHoraVirtual = valorHora,
                    HorasApontadas = horasApontadas,
                    ValorConsumido = valorConsumido,
                    Saldo = saldo,
                    PercentualConsumo = percentual,
                    Status = status,
                    StatusTexto = statusTexto
                });
            }

            // Ordenar: vermelhos primeiro, depois amarelos, depois verdes
            cards = cards
                .OrderByDescending(c => c.Status == "vermelho" ? 3 : c.Status == "amarelo" ? 2 : 1)
                .ThenByDescending(c => c.PercentualConsumo)
                .ToList();

            // Filtro por cliente (aplicado após montar todos os cards para o dropdown ter todos)
            List<int> clienteIdsFiltro = null;
            if (!string.IsNullOrWhiteSpace(clienteIds))
            {
                clienteIdsFiltro = clienteIds.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
            }
            ViewBag.ClienteIdsFiltro = clienteIdsFiltro;
            ViewBag.TodosCards = cards; // Para o dropdown sempre ter todos
            var cardsFiltrados = clienteIdsFiltro != null && clienteIdsFiltro.Any()
                ? cards.Where(c => clienteIdsFiltro.Contains(c.ClienteId)).ToList()
                : cards;

            var viewModel = new RentabilidadeMensalistaViewModel
            {
                Periodo = periodoAtual,
                DataInicio = inicio,
                DataFim = fim,
                Cards = cardsFiltrados,
                TotalMensalidades = cardsFiltrados.Sum(c => c.ValorMensalidade),
                TotalConsumido = cardsFiltrados.Sum(c => c.ValorConsumido),
                SaldoGeral = cardsFiltrados.Sum(c => c.Saldo),
                TotalEstourados = cardsFiltrados.Count(c => c.Status == "vermelho"),
                TotalAtencao = cardsFiltrados.Count(c => c.Status == "amarelo"),
                TotalEquilibrados = cardsFiltrados.Count(c => c.Status == "verde")
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> RentabilidadeDetalhe(int clienteId, DateTime dataInicio, DateTime dataFim)
        {
            var registros = await _context.ProcessRecord
                .AsNoTracking()
                .Include(p => p.Attorney)
                .Include(p => p.Department)
                .Where(p => p.Date >= dataInicio && p.Date <= dataFim && p.ClientId == clienteId)
                .ToListAsync();

            var porAdvogado = registros
                .GroupBy(r => r.Attorney.Name)
                .Select(g => new { Nome = g.Key, Horas = Math.Round(g.Sum(r => (r.HoraFinal - r.HoraInicial).TotalHours), 1) })
                .OrderByDescending(x => x.Horas)
                .ToList();

            var porArea = registros
                .GroupBy(r => r.Department.Name)
                .Select(g => new { Nome = g.Key, Horas = Math.Round(g.Sum(r => (r.HoraFinal - r.HoraInicial).TotalHours), 1) })
                .OrderByDescending(x => x.Horas)
                .ToList();

            var porTipo = registros
                .GroupBy(r => r.RecordType.ToString())
                .Select(g => new { Nome = g.Key, Horas = Math.Round(g.Sum(r => (r.HoraFinal - r.HoraInicial).TotalHours), 1) })
                .OrderByDescending(x => x.Horas)
                .ToList();

            return Json(new { porAdvogado, porArea, porTipo });
        }

        [HttpGet]
        public async Task<IActionResult> RentabilidadePorArea(int clienteId, int mensalistaId, DateTime dataInicio, DateTime dataFim, string periodo)
        {
            // Buscar mensalista
            var mensalista = mensalistaId > 0 ? await _mensalistaService.FindByIdAsync(mensalistaId) : null;
            
            // Buscar percentuais de área do cliente
            var percentuais = await _context.Set<PercentualArea>()
                .AsNoTracking()
                .Where(pa => pa.ClientId == clienteId)
                .Include(pa => pa.Department)
                .ToListAsync();

            if (!percentuais.Any() || mensalista == null)
            {
                return Json(new { areas = new List<object>(), semPercentual = true });
            }

            // Calcular mensalidade de referência para o período
            decimal mensalidadeTotal = mensalista.ValorMensalBruto;
            if (periodo == "semestre")
                mensalidadeTotal = mensalista.ValorMensalBruto * 6;
            else if (periodo == "custom")
            {
                var meses = ((dataFim.Year - dataInicio.Year) * 12) + dataFim.Month - dataInicio.Month + 1;
                mensalidadeTotal = mensalista.ValorMensalBruto * meses;
            }

            // Buscar horas por área
            var registros = await _context.ProcessRecord
                .AsNoTracking()
                .Where(p => p.Date >= dataInicio && p.Date <= dataFim && p.ClientId == clienteId)
                .Select(p => new { p.DepartmentId, p.HoraInicial, p.HoraFinal })
                .ToListAsync();

            var horasPorArea = registros
                .GroupBy(r => r.DepartmentId)
                .ToDictionary(g => g.Key, g => g.Sum(r => (r.HoraFinal - r.HoraInicial).TotalHours));

            var valorHora = mensalista.GetValorHoraEfetivo();

            var areas = percentuais.Select(pa =>
            {
                var mensalidadeArea = mensalidadeTotal * (pa.Percentual / 100m);
                var horas = horasPorArea.ContainsKey(pa.DepartmentId) ? horasPorArea[pa.DepartmentId] : 0;
                var consumido = (decimal)horas * valorHora;
                var saldo = mensalidadeArea - consumido;
                var percentual = mensalidadeArea > 0 ? (double)(consumido / mensalidadeArea) * 100 : 0;

                string status;
                if (percentual > 100) status = "vermelho";
                else if (percentual >= 80) status = "amarelo";
                else status = "verde";

                return new
                {
                    area = pa.Department.Name,
                    percentualArea = pa.Percentual,
                    mensalidade = Math.Round(mensalidadeArea, 0),
                    horas = Math.Round(horas, 1),
                    consumido = Math.Round(consumido, 0),
                    saldo = Math.Round(saldo, 0),
                    percentualConsumo = Math.Round(percentual, 0),
                    status
                };
            })
            .OrderByDescending(a => a.percentualConsumo)
            .ToList();

            return Json(new { areas, semPercentual = false });
        }

        // -- Painel de Faturamento dos Horistas ------------------------------

        public async Task<IActionResult> RentabilidadeHoristas(string? periodo, DateTime? dataInicio, DateTime? dataFim, string? clienteIds)
        {
            // Definir período
            DateTime inicio, fim;
            string periodoAtual = periodo ?? "mes";

            switch (periodoAtual)
            {
                case "semestre":
                    inicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);
                    fim = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
                    break;
                case "custom":
                    inicio = dataInicio ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    fim = dataFim ?? DateTime.Now.Date;
                    // Limitar período máximo de 12 meses
                    if ((fim - inicio).TotalDays > 366)
                    {
                        inicio = fim.AddMonths(-12);
                        TempData["MensagemAviso"] = "Período limitado a 12 meses.";
                    }
                    break;
                default:
                    periodoAtual = "mes";
                    inicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    fim = DateTime.Now.Date;
                    break;
            }

            // Buscar clientes com valor hora cadastrado (excluir mensalistas)
            var mensalistaClientIds = await _mensalistaService.FindClientIdsAsync();
            var valoresClientes = await _context.ValorCliente
                .AsNoTracking()
                .Where(v => v.AttorneyId == null && v.Valor > 0 && !mensalistaClientIds.Contains(v.ClientId))
                .ToListAsync();

            if (!valoresClientes.Any())
            {
                ViewBag.ClienteIdsFiltro = (List<int>?)null;
                ViewBag.TodosCards = new List<CardMensalista>();
                ViewBag.DebugTempo = "";
                return View(new RentabilidadeMensalistaViewModel
                {
                    Periodo = periodoAtual,
                    DataInicio = inicio,
                    DataFim = fim
                });
            }

            var clientIdsHoristas = valoresClientes.Select(v => v.ClientId).ToList();

            // Buscar horas por cliente primeiro (pra saber quais têm lançamento)
            var registrosResumo = await _context.ProcessRecord
                .AsNoTracking()
                .Where(p => p.Date >= inicio && p.Date <= fim && clientIdsHoristas.Contains(p.ClientId))
                .Select(p => new { p.ClientId, p.HoraInicial, p.HoraFinal })
                .ToListAsync();

            var horasDict = registrosResumo
                .GroupBy(r => r.ClientId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => (r.HoraFinal - r.HoraInicial).TotalHours)
                );

            // Buscar clientes COM logo — apenas os que têm horas (são poucos)
            var clientIdsComHoras = horasDict.Where(h => h.Value > 0).Select(h => h.Key).ToList();
            var clients = await _context.Client
                .AsNoTracking()
                .Where(c => clientIdsComHoras.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.ImageData, c.ImageMimeType })
                .ToListAsync();

            var valoresDict = valoresClientes.ToDictionary(v => v.ClientId, v => v.Valor);

            var cards = new List<CardMensalista>();

            foreach (var vc in valoresClientes)
            {
                var client = clients.FirstOrDefault(c => c.Id == vc.ClientId);
                if (client == null) continue;

                var horasApontadas = horasDict.ContainsKey(vc.ClientId) ? horasDict[vc.ClientId] : 0;
                
                // Só mostra clientes com lançamentos no período
                if (horasApontadas <= 0) continue;

                var valorHora = (decimal)vc.Valor;
                var valorFaturado = (decimal)horasApontadas * valorHora;

                cards.Add(new CardMensalista
                {
                    MensalistaId = 0,
                    ClienteId = client.Id,
                    ClienteNome = client.Name,
                    ClienteLogo = client.ImageData != null && client.ImageData.Length != 13536 ? client.ImageData : null,
                    ClienteLogoMime = client.ImageData != null && client.ImageData.Length != 13536 ? client.ImageMimeType : null,
                    ValorMensalidade = 0,
                    ValorHoraVirtual = valorHora,
                    HorasApontadas = horasApontadas,
                    ValorConsumido = valorFaturado,
                    Saldo = valorFaturado,
                    PercentualConsumo = 0,
                    Status = "verde",
                    StatusTexto = $"{valorFaturado:C0}"
                });
            }

            // Ordenar por valor faturado (maior primeiro)
            cards = cards.OrderByDescending(c => c.ValorConsumido).ToList();

            // Filtro por cliente
            List<int> clienteIdsFiltro = null;
            if (!string.IsNullOrWhiteSpace(clienteIds))
            {
                clienteIdsFiltro = clienteIds.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
            }
            ViewBag.ClienteIdsFiltro = clienteIdsFiltro;
            ViewBag.TodosCards = cards;
            ViewBag.DebugTempo = "";

            var cardsFiltrados = clienteIdsFiltro != null && clienteIdsFiltro.Any()
                ? cards.Where(c => clienteIdsFiltro.Contains(c.ClienteId)).ToList()
                : cards;

            var viewModel = new RentabilidadeMensalistaViewModel
            {
                Periodo = periodoAtual,
                DataInicio = inicio,
                DataFim = fim,
                Cards = cardsFiltrados,
                TotalMensalidades = 0,
                TotalConsumido = cardsFiltrados.Sum(c => c.ValorConsumido),
                SaldoGeral = cardsFiltrados.Sum(c => c.ValorConsumido),
                TotalEstourados = 0,
                TotalAtencao = 0,
                TotalEquilibrados = cardsFiltrados.Count
            };

            return View(viewModel);
        }

        private void PopulateViewData(DateTime monthYear, int? clientId, int? departmentId)
        {
            ViewData["monthYear"] = monthYear.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;
        }


        private async Task PopulateViewBag()
        {
            var mensalistaClientIds = await _mensalistaService.FindClientIdsAsync();
            var allClients = await _clientService.FindAllAsync();
            ViewBag.Clients = allClients.Where(c => mensalistaClientIds.Contains(c.Id)).OrderBy(c => c.Name).ToList();
            ViewBag.Attorneys = await _attorneyService.FindAllAsync();
            ViewBag.Department = await _departmentService.FindAllAsync();
        }

        public async Task<IActionResult> ResultadoMes(int id, DateTime? monthYear, int? clientId, int? departmentId)
        {
            var mensalista = await _mensalistaService.FindByIdAsync(id);
            if (mensalista == null)
            {
                return NotFound();
            }

            // Se monthYear não tiver valor, definimos para a data atual
            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            // Convertendo monthYear para o intervalo de datas
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            // Obtendo as informações de MensalistaHoursViewModel usando os parâmetros

            var mensalistaHours = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId, QueryType.Monthly);

            var specificMensalistaHours = mensalistaHours.FirstOrDefault(m => m.Mensalista.Id == id);

            if (specificMensalistaHours == null)
            {
                return NotFound();
            }

            // Armazenar os parâmetros no ViewData
            ViewData["monthYear"] = monthYear.Value.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;
            ViewData["inputMonthYear"] = monthYear.Value.ToString("MM/yyyy");

            return View(new List<MensalistaHoursViewModel> { specificMensalistaHours });
        }


        public async Task<IActionResult> ResultadoMedia(int id, DateTime? monthYear, int? clientId, int? departmentId)
        {
            var mensalista = await _mensalistaService.FindByIdAsync(id);
            if (mensalista == null)
            {
                return NotFound();
            }

            // Se monthYear não tiver valor, definimos para a data atual
            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            // Convertendo monthYear para o intervalo de datas dos últimos três meses
            DateTime startOfSelectedMonth = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
            DateTime endOfSelectedMonth = startOfSelectedMonth.AddMonths(1).AddDays(-1);
            DateTime startOfThreeMonthsAgo = startOfSelectedMonth.AddMonths(-3);

            // Obtendo as informações de MensalistaHoursViewModel usando os parâmetros
            var mensalistaHours = await _processRecordService.FindByDateMensalistaAsync(startOfThreeMonthsAgo, endOfSelectedMonth, clientId, departmentId, QueryType.Average);


            var specificMensalistaHours = mensalistaHours.FirstOrDefault(m => m.Mensalista.Id == id);

            if (specificMensalistaHours == null)
            {
                return NotFound();
            }

            // Armazenar os parâmetros no ViewData
            ViewData["monthYear"] = monthYear.Value.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;

            return View(new List<MensalistaHoursViewModel> { specificMensalistaHours });
        }
        
        public async Task<IActionResult> ResultadoAcumulado(int id, DateTime? monthYear, int? clientId, int? departmentId)
        {
            var mensalista = await _mensalistaService.FindByIdAsync(id);
            if (mensalista == null)
            {
                return NotFound();
            }

            // Se monthYear não tiver valor, definimos para a data atual
            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            // Convertendo monthYear para o intervalo de datas dos últimos três meses
            DateTime startOfSelectedMonth = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
            DateTime endOfSelectedMonth = startOfSelectedMonth.AddMonths(1).AddDays(-1);
            DateTime startOfThreeMonthsAgo = startOfSelectedMonth.AddMonths(-3);

            // Obtendo as informações de MensalistaHoursViewModel usando os parâmetros

            var mensalistaHours = await _processRecordService.FindByDateMensalistaAsync(startOfThreeMonthsAgo, endOfSelectedMonth, clientId, departmentId, QueryType.Cumulative);

            var specificMensalistaHours = mensalistaHours.FirstOrDefault(m => m.Mensalista.Id == id);

            if (specificMensalistaHours == null)
            {
                return NotFound();
            }            

            // Armazenar os parâmetros no ViewData
            ViewData["monthYear"] = monthYear.Value.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;

            return View(new List<MensalistaHoursViewModel> { specificMensalistaHours });
        }    

        public async Task<IActionResult> Detalhe(int id, string monthYear, int? clientId, int? departmentId)
        {
            var mensalista = await _mensalistaService.FindByIdAsync(id);
            if (mensalista == null) return NotFound();

            DateTime parsedDate = DateTime.Now;
            if (!string.IsNullOrEmpty(monthYear) && monthYear.Length == 7)
            {
                parsedDate = DateTime.ParseExact(monthYear, "MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }

            // Mês atual
            ConvertMonthYearToRange(parsedDate, out DateTime minDate, out DateTime maxDate);
            var mesAtualList = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId, QueryType.Monthly);
            var mesAtual = mesAtualList.FirstOrDefault(m => m.Mensalista.Id == id);

            // Média 3 meses
            DateTime start3m = new DateTime(parsedDate.Year, parsedDate.Month, 1).AddMonths(-3);
            DateTime end3m = new DateTime(parsedDate.Year, parsedDate.Month, 1).AddMonths(1).AddDays(-1);
            var mediaList = await _processRecordService.FindByDateMensalistaAsync(start3m, end3m, clientId, departmentId, QueryType.Average);
            var media = mediaList.FirstOrDefault(m => m.Mensalista.Id == id);

            // Acumulado 3 meses
            var acumList = await _processRecordService.FindByDateMensalistaAsync(start3m, end3m, clientId, departmentId, QueryType.Cumulative);
            var acum = acumList.FirstOrDefault(m => m.Mensalista.Id == id);

            string deptName = "";
            if (departmentId.HasValue)
            {
                var dept = (await _departmentService.FindAllAsync()).FirstOrDefault(d => d.Id == departmentId.Value);
                deptName = dept?.Name ?? "";
            }

            var vm = new MensalistaDetalheViewModel
            {
                MesAtual = mesAtual,
                Media3Meses = media,
                Acumulado3Meses = acum,
                InputMonthYear = parsedDate.ToString("MM/yyyy"),
                ClientId = clientId,
                DepartmentId = departmentId,
                DepartmentName = deptName
            };

            return View(vm);
        }

        public async Task<IActionResult> DownloadReport(string monthYearString, int? clientId, int? departmentId, string recordType = null, string format = "xlsx")
        {
            DateTime? monthYear = null;

            if (!string.IsNullOrEmpty(monthYearString) && monthYearString.Length == 6)
            {
                int month = int.Parse(monthYearString.Substring(0, 2));
                int year = int.Parse(monthYearString.Substring(2, 4));

                monthYear = new DateTime(year, month, 1);
            }

            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
            {
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);
            }

            var filteredRecords = await _processRecordService.FindByDateAsyncRes(minDate, maxDate, clientId, departmentId, recordTypeEnum);

            if (format != "xlsx")
            {
                return BadRequest("Formato inválido");
            }

            var workbook = new XSSFWorkbook();
            // CreateMainSheet(workbook, filteredRecords, clientId);

        

            await CreateMensalidadeSheet(workbook, clientId, departmentId);
            await CreateResultadoMesSheet(workbook, clientId, departmentId, monthYear);
            await CreateMediaMesesSheet(workbook, clientId, departmentId, monthYear);
            await CreateAcumuladoMesesSheet(workbook, clientId, departmentId, monthYear);           

            string fileName = await GenerateFileName(clientId);
            return ConvertWorkbookToFile(workbook, fileName);
        }




        private async Task CreateMensalidadeSheet(XSSFWorkbook workbook, int? clientId, int? departmentId)
        {
            DateTime? monthYear = DateTime.Now;
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId);
            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });

            var sheet = workbook.CreateSheet("Mensalidades");

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;

            // Modificando lightGrayStyle para também ter o formato de número:
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");


            // Define bordas brancas para os estilos
            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";  // Caso departmentName seja nulo ou vazio, use "%" como padrão.
            }

            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("NOME");
            headerRow.CreateCell(1).SetCellValue("VALOR MENSAL BRUTO");
            headerRow.CreateCell(2).SetCellValue("TRIBUTOS");
            headerRow.CreateCell(3).SetCellValue("COMISSÃO PARCEIRO");
            headerRow.CreateCell(4).SetCellValue("COMISSÃO SÓCIO");
            headerRow.CreateCell(5).SetCellValue("VALOR MENSAL LÍQUIDO");
            headerRow.CreateCell(6).SetCellValue(departmentName);
            headerRow.CreateCell(7).SetCellValue("VALOR DA ÁREA BRUTO");
            headerRow.CreateCell(8).SetCellValue("VALOR MENSAL LÍQUIDO");

            for (int j = 0; j < 9; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho
            sheet.SetAutoFilter(new CellRangeAddress(0, 0, 0, 8));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 1);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 9; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                row.GetCell(1).SetCellValue((double)item.Mensalista.ValorMensalBruto);
                row.GetCell(2).SetCellValue((double)item.Tributos);
                row.GetCell(3).SetCellValue((double)item.Mensalista.ComissaoParceiro);
                row.GetCell(4).SetCellValue((double)item.Mensalista.ComissaoSocio);
                row.GetCell(5).SetCellValue((double)item.ValorMensalLiquido);
                row.GetCell(6).SetCellValue($"{item.Percentual:0.00}");
                row.GetCell(7).SetCellValue((double)item.ValorAreaBruto);
                row.GetCell(8).SetCellValue((double)item.ValorAreaLiquido);
            }


            for (int colIndex = 0; colIndex < 9; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }

        private async Task CreateResultadoMesSheet(XSSFWorkbook workbook, int? clientId, int? departmentId, DateTime? monthYear)

        {            
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId);
            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });
            var sheet = workbook.CreateSheet("Resultado do Mês");

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";  // Caso departmentName seja nulo ou vazio, use "%" como padrão.
            }
            var titleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            var darkGreyColor = new XSSFColor(new byte[] { 169, 169, 169 }); // RGB para cinza escuro

            titleStyle.SetFillForegroundColor(darkGreyColor);
            titleStyle.FillPattern = FillPattern.SolidForeground;
            titleStyle.Alignment = HorizontalAlignment.Center; // Centralizar o texto

            // Estilizando a fonte
            var titleFont = workbook.CreateFont();
            titleFont.Color = HSSFColor.White.Index; // Fonte branca
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);

            // Inserir a primeira linha com o nome do departamento
            var titleRow = sheet.CreateRow(0);
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue($"CLIENTES {departmentName.ToUpper()}");
            titleCell.CellStyle = titleStyle;


            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;

            // Modificando lightGrayStyle para também ter o formato de número:
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");


            // Define bordas brancas para os estilos
            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }


            var headerRow = sheet.CreateRow(1);
            headerRow.CreateCell(0).SetCellValue("NOME");
            if (monthYear.HasValue)
            {
                string monthName = monthYear.Value.ToString("MMM", new CultureInfo("pt-BR")).TrimEnd('.').ToLower();
                headerRow.CreateCell(1).SetCellValue($"{monthName}/{monthYear.Value:yy}");

            }
            else
            {
                headerRow.CreateCell(1).SetCellValue("MÊS");
            }

            headerRow.CreateCell(2).SetCellValue("HORA TÉCNICA BRUTA");
            headerRow.CreateCell(3).SetCellValue("HORA TÉCNICA LÍQUIDA");
            headerRow.CreateCell(4).SetCellValue("RESULTADO BRUTO");
            headerRow.CreateCell(5).SetCellValue("RESULTADO LÍQUIDO");
            for (int j = 0; j < 6; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho para todas as colunas
            sheet.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 2);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 6; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                  

                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                double totalHours = Math.Floor(item.TotalHours);
                double totalMinutes = (item.TotalHours - totalHours) * 60;

                row.GetCell(1).SetCellValue($"{totalHours}:{Math.Round(totalMinutes)}");                
                //row.GetCell(2).SetCellValue((double)item.ValorTotalHoras);

                double valotTotalHoras = Math.Round((double)item.ValorTotalHoras, 2);
                row.GetCell(2).SetCellValue(valotTotalHoras);

                //row.GetCell(3).SetCellValue(Math.Round((double)item.ValorHoraTecLiquida, 2));

                double valorHoraTecnicaLiquida = Math.Round((double)item.ValorHoraTecLiquida, 2);
                row.GetCell(3).SetCellValue(valorHoraTecnicaLiquida);

                //row.GetCell(4).SetCellValue((double)item.ValorResultadoBruto);

                double valorResultadoBruto = Math.Round((double)item.ValorResultadoBruto, 2);
                row.GetCell(4).SetCellValue(valorResultadoBruto);


                double valorResultadoLiquido = Math.Round((double)item.ValorResultadoLiquido, 2);
                row.GetCell(5).SetCellValue(valorResultadoLiquido);

                // Aplicar formatação condicional para todas as colunas de valor
                for (int col = 1; col <= 5; col++)
                {
                    double cellValue;
                    var cell = row.GetCell(col);
                    if (cell != null && cell.CellType == CellType.Numeric)
                    {
                        cellValue = cell.NumericCellValue;
                        IFont font = workbook.CreateFont();

                        if (cellValue < 0)
                        {
                            font.Color = HSSFColor.Red.Index; // Fonte vermelha
                        }
                        else if (cellValue > 0)
                        {
                            font.Color = HSSFColor.Green.Index; // Fonte verde
                        }

                        ICellStyle conditionalStyle = workbook.CreateCellStyle();
                        conditionalStyle.CloneStyleFrom(currentStyle);
                        conditionalStyle.SetFont(font);
                        cell.CellStyle = conditionalStyle;
                    }
                }



            }

            for (int colIndex = 0; colIndex < 6; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }

   

        private async Task CreateMediaMesesSheet(XSSFWorkbook workbook, int? clientId, int? departmentId, DateTime? monthYear)

        {
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId, QueryType.Average);

            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });
            var sheet = workbook.CreateSheet("Média 3 meses");

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";  // Caso departmentName seja nulo ou vazio, use "%" como padrão.
            }
            var titleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            var darkGreyColor = new XSSFColor(new byte[] { 169, 169, 169 }); // RGB para cinza escuro

            titleStyle.SetFillForegroundColor(darkGreyColor);
            titleStyle.FillPattern = FillPattern.SolidForeground;
            titleStyle.Alignment = HorizontalAlignment.Center; // Centralizar o texto

            // Estilizando a fonte
            var titleFont = workbook.CreateFont();
            titleFont.Color = HSSFColor.White.Index; // Fonte branca
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);

            // Inserir a primeira linha com o nome do departamento
            var titleRow = sheet.CreateRow(0);
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue($"CLIENTES {departmentName.ToUpper()}");
            titleCell.CellStyle = titleStyle;


            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;

            // Modificando lightGrayStyle para também ter o formato de número:
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");


            // Define bordas brancas para os estilos
            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }


            var headerRow = sheet.CreateRow(1);
            headerRow.CreateCell(0).SetCellValue("NOME");
            headerRow.CreateCell(1).SetCellValue("Média últimos 3 meses");
            headerRow.CreateCell(2).SetCellValue("Média bruta ú. 3 mês");
            headerRow.CreateCell(3).SetCellValue("Média Líquida últimos 3 meses");
            headerRow.CreateCell(4).SetCellValue("Bruto últimos 3 meses");
            headerRow.CreateCell(5).SetCellValue("Líquido últimos 3 meses");
            for (int j = 0; j < 6; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho para todas as colunas
            sheet.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 2);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 6; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                double totalHours = Math.Floor(item.TotalHours);
                double totalMinutes = (item.TotalHours - totalHours) * 60;

                row.GetCell(1).SetCellValue($"{totalHours}:{Math.Round(totalMinutes)}");
                //row.GetCell(2).SetCellValue((double)item.ValorTotalHoras);

                double valotTotalHoras = Math.Round((double)item.ValorTotalHoras, 2);
                row.GetCell(2).SetCellValue(valotTotalHoras);

                //row.GetCell(3).SetCellValue(Math.Round((double)item.ValorHoraTecLiquida, 2));

                double valorHoraTecnicaLiquida = Math.Round((double)item.ValorHoraTecLiquida, 2);
                row.GetCell(3).SetCellValue(valorHoraTecnicaLiquida);

                //row.GetCell(4).SetCellValue((double)item.ValorResultadoBruto);

                double valorResultadoBruto = Math.Round((double)item.ValorResultadoBruto, 2);
                row.GetCell(4).SetCellValue(valorResultadoBruto);


                double valorResultadoLiquido = Math.Round((double)item.ValorResultadoLiquido, 2);
                row.GetCell(5).SetCellValue(valorResultadoLiquido);

                // Aplicar formatação condicional para todas as colunas de valor
                for (int col = 1; col <= 5; col++)
                {
                    double cellValue;
                    var cell = row.GetCell(col);
                    if (cell != null && cell.CellType == CellType.Numeric)
                    {
                        cellValue = cell.NumericCellValue;
                        IFont font = workbook.CreateFont();

                        if (cellValue < 0)
                        {
                            font.Color = HSSFColor.Red.Index; // Fonte vermelha
                        }
                        else if (cellValue > 0)
                        {
                            font.Color = HSSFColor.Green.Index; // Fonte verde
                        }

                        ICellStyle conditionalStyle = workbook.CreateCellStyle();
                        conditionalStyle.CloneStyleFrom(currentStyle);
                        conditionalStyle.SetFont(font);
                        cell.CellStyle = conditionalStyle;
                    }
                }



            }

            for (int colIndex = 0; colIndex < 6; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }

        private async Task CreateAcumuladoMesesSheet(XSSFWorkbook workbook, int? clientId, int? departmentId, DateTime? monthYear)
        {
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId, QueryType.Cumulative);

            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });

            var sheet = workbook.CreateSheet("Acumulado 3 meses");

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";
            }

            var titleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            var darkGreyColor = new XSSFColor(new byte[] { 169, 169, 169 });
            titleStyle.SetFillForegroundColor(darkGreyColor);
            titleStyle.FillPattern = FillPattern.SolidForeground;
            titleStyle.Alignment = HorizontalAlignment.Center;

            var titleFont = workbook.CreateFont();
            titleFont.Color = HSSFColor.White.Index;
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);

            var titleRow = sheet.CreateRow(0);
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue($"CLIENTES {departmentName.ToUpper()}");
            titleCell.CellStyle = titleStyle;

            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }

            var headerRow = sheet.CreateRow(1);
            headerRow.CreateCell(0).SetCellValue("NOME");
            headerRow.CreateCell(1).SetCellValue("Acumulado últimos 3 meses");
            headerRow.CreateCell(2).SetCellValue("Bruto últimos 3 meses");
            headerRow.CreateCell(3).SetCellValue("Líquido últimos 3 meses");
            headerRow.CreateCell(4).SetCellValue("Resultado Bruto últimos 3 meses");
            headerRow.CreateCell(5).SetCellValue("Líquido últimos 3 meses");
            for (int j = 0; j < 6; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho para todas as colunas
            sheet.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 2);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 6; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                double totalHours = Math.Floor(item.TotalHours);
                double totalMinutes = (item.TotalHours - totalHours) * 60;  

                row.GetCell(1).SetCellValue($"{totalHours}:{Math.Round(totalMinutes)}");
                //row.GetCell(2).SetCellValue((double)item.ValorTotalHoras);

                double valotTotalHoras = Math.Round((double)item.ValorTotalHoras, 2);
                row.GetCell(2).SetCellValue(valotTotalHoras);

                //row.GetCell(3).SetCellValue(Math.Round((double)item.ValorHoraTecLiquida, 2));

                double valorHoraTecnicaLiquida = Math.Round((double)item.ValorHoraTecLiquida, 2);
                row.GetCell(3).SetCellValue(valorHoraTecnicaLiquida);

                //row.GetCell(4).SetCellValue((double)item.ValorResultadoBruto);

                double valorResultadoBruto = Math.Round((double)item.ValorResultadoBruto, 2);
                row.GetCell(4).SetCellValue(valorResultadoBruto);


                double valorResultadoLiquido = Math.Round((double)item.ValorResultadoLiquido, 2);
                row.GetCell(5).SetCellValue(valorResultadoLiquido);

                // Aplicar formatação condicional para todas as colunas de valor
                for (int col = 1; col <= 5; col++)
                {
                    double cellValue;
                    var cell = row.GetCell(col);
                    if (cell != null && cell.CellType == CellType.Numeric)
                    {
                        cellValue = cell.NumericCellValue;
                        IFont font = workbook.CreateFont();

                        if (cellValue < 0)
                        {
                            font.Color = HSSFColor.Red.Index; // Fonte vermelha
                        }
                        else if (cellValue > 0)
                        {
                            font.Color = HSSFColor.Green.Index; // Fonte verde
                        }

                        ICellStyle conditionalStyle = workbook.CreateCellStyle();
                        conditionalStyle.CloneStyleFrom(currentStyle);
                        conditionalStyle.SetFont(font);
                        cell.CellStyle = conditionalStyle;
                    }
                }

            }

            for (int colIndex = 0; colIndex < 6; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }



        private async Task<string> GenerateFileName(int? clientId)
        {
            string clientName = null;
            if (clientId.HasValue)
            {
                var client = await _clientService.FindByIdAsync(clientId.Value);
                if (client != null)
                {
                    clientName = client.Name;
                }
            }

            string fileName = "Relatório_TimeSheet";
            if (!string.IsNullOrEmpty(clientName))
            {
                fileName += $"_{clientName}";
            }
            fileName += ".xlsx";
            return fileName;
        }


        private IActionResult ConvertWorkbookToFile(XSSFWorkbook workbook, string fileName)
        {
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var content = stream.ToArray();

                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
        }



        #endregion

    }
}
