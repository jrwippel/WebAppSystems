using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Models.ViewModels;

namespace WebAppSystems.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class FaturamentoController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ILogger<FaturamentoController> _logger;
        private readonly ISessao _isessao;

        public FaturamentoController(WebAppSystemsContext context, ILogger<FaturamentoController> logger, ISessao isessao)
        {
            _context = context;
            _logger = logger;
            _isessao = isessao;
        }

        // GET: Faturamento
        public async Task<IActionResult> Index(
            string? mesAno,
            DateTime? dataInicio,
            DateTime? dataFim,
            int? clienteId,
            int? departamentoId,
            int? advogadoId,
            bool? apenasNaoFaturados = true,
            int page = 1)
        {
            const int pageSize = 20;

            if (!string.IsNullOrEmpty(mesAno))
            {
                var partes = mesAno.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[0], out int ano) && int.TryParse(partes[1], out int mes))
                {
                    dataInicio = new DateTime(ano, mes, 1);
                    dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                }
            }
            else if (!dataInicio.HasValue)
            {
                dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                mesAno = dataInicio.Value.ToString("yyyy-MM");
            }
            
            if (!dataFim.HasValue && dataInicio.HasValue)
                dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);

            var query = _context.ProcessRecord
                .Include(p => p.Attorney)
                .Include(p => p.Client)
                .Include(p => p.Department)
                .Include(p => p.FaturadoPor)
                .Where(p => p.Date >= dataInicio && p.Date <= dataFim);

            if (apenasNaoFaturados == true)
                query = query.Where(p => !p.IsFaturado);

            if (clienteId.HasValue)
                query = query.Where(p => p.ClientId == clienteId);

            if (departamentoId.HasValue)
                query = query.Where(p => p.DepartmentId == departamentoId);

            if (advogadoId.HasValue)
                query = query.Where(p => p.AttorneyId == advogadoId);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var lancamentos = await query
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Client.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Buscar valores por hora de cada cliente (valor padrão onde AttorneyId é null)
            var clienteIds = lancamentos.Select(l => l.ClientId).Distinct().ToList();
            var valoresClientes = await _context.ValorCliente
                .Where(v => clienteIds.Contains(v.ClientId) && v.AttorneyId == null)
                .ToDictionaryAsync(v => v.ClientId, v => v.Valor);

            // Log para debug
            _logger.LogInformation($"Total de clientes únicos: {clienteIds.Count}");
            _logger.LogInformation($"Valores encontrados no banco: {valoresClientes.Count}");
            foreach (var kv in valoresClientes)
            {
                _logger.LogInformation($"ClienteId {kv.Key}: R$ {kv.Value}");
            }

            var agrupados = lancamentos
                .GroupBy(p => p.Client)
                .Select(g => new FaturamentoGrupoViewModel
                {
                    Cliente = g.Key,
                    Lancamentos = g.OrderBy(l => l.Date).ToList(),
                    TotalHoras = g.Sum(l => l.CalculoHorasDecimal()),
                    ValorEstimado = g.Sum(l => {
                        var horas = l.CalculoHorasDecimal();
                        var valorHora = valoresClientes.ContainsKey(l.ClientId) ? valoresClientes[l.ClientId] : 0;
                        var valorCalculado = horas * valorHora;
                        
                        // Log detalhado para cada lançamento
                        _logger.LogInformation($"Cliente: {l.Client.Name} (ID: {l.ClientId}), Horas: {horas}, ValorHora: {valorHora}, Total: {valorCalculado}");
                        
                        return valorCalculado;
                    })
                })
                .OrderBy(g => g.Cliente.Name)
                .ToList();

            ViewBag.MesAno = mesAno;
            ViewBag.DataInicio = dataInicio?.ToString("yyyy-MM-dd");
            ViewBag.DataFim = dataFim?.ToString("yyyy-MM-dd");
            ViewBag.ClienteId = clienteId;
            ViewBag.DepartamentoId = departamentoId;
            ViewBag.AdvogadoId = advogadoId;
            ViewBag.ApenasNaoFaturados = apenasNaoFaturados;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            ViewBag.Clientes = _context.Client != null 
                ? await _context.Client.OrderBy(c => c.Name).ToListAsync() 
                : new List<Client>();
            ViewBag.Departamentos = await _context.Department.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Advogados = await _context.Attorney.OrderBy(a => a.Name).ToListAsync();

            return View(agrupados);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoFaturado([FromBody] MarcarFaturadoRequest request)
        {
            try
            {
                var usuario = _isessao.BuscarSessaoDoUsuario();

                if (usuario?.Perfil != ProfileEnum.Admin)
                {
                    return Json(new { success = false, message = "Apenas administradores podem marcar lançamentos como faturados." });
                }

                var lancamentos = await _context.ProcessRecord
                    .Where(p => request.ProcessRecordIds.Contains(p.Id))
                    .ToListAsync();

                if (!lancamentos.Any())
                {
                    return Json(new { success = false, message = "Nenhum lançamento encontrado." });
                }

                var jaFaturados = lancamentos.Where(l => l.IsFaturado).ToList();
                if (jaFaturados.Any())
                {
                    return Json(new 
                    { 
                        success = false, 
                        message = $"{jaFaturados.Count} lançamento(s) já está(ão) faturado(s)." 
                    });
                }

                foreach (var lancamento in lancamentos)
                {
                    lancamento.IsFaturado = true;
                    lancamento.DataFaturamento = DateTime.Now;
                    lancamento.FaturadoPorId = usuario.Id;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuário {usuario.Name} marcou {lancamentos.Count} lançamentos como faturados.");

                return Json(new 
                { 
                    success = true, 
                    message = $"{lancamentos.Count} lançamento(s) marcado(s) como faturado(s) com sucesso!" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao marcar lançamentos como faturados");
                return Json(new { success = false, message = "Erro ao processar solicitação." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DesmarcarFaturado([FromBody] MarcarFaturadoRequest request)
        {
            try
            {
                var usuario = _isessao.BuscarSessaoDoUsuario();

                if (usuario?.Perfil != ProfileEnum.Admin)
                {
                    return Json(new { success = false, message = "Apenas administradores podem desmarcar lançamentos faturados." });
                }

                var lancamentos = await _context.ProcessRecord
                    .Where(p => request.ProcessRecordIds.Contains(p.Id))
                    .ToListAsync();

                if (!lancamentos.Any())
                {
                    return Json(new { success = false, message = "Nenhum lançamento encontrado." });
                }

                foreach (var lancamento in lancamentos)
                {
                    lancamento.IsFaturado = false;
                    lancamento.DataFaturamento = null;
                    lancamento.FaturadoPorId = null;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuário {usuario.Name} desmarcou {lancamentos.Count} lançamentos como faturados.");

                return Json(new 
                { 
                    success = true, 
                    message = $"{lancamentos.Count} lançamento(s) desmarcado(s) com sucesso!" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desmarcar lançamentos faturados");
                return Json(new { success = false, message = "Erro ao processar solicitação." });
            }
        }

        public async Task<IActionResult> GerarExcel(string? mesAno, int? clienteId, int? departamentoId, int? advogadoId)
        {
            DateTime? dataInicio = null;
            DateTime? dataFim = null;

            if (!string.IsNullOrEmpty(mesAno))
            {
                var partes = mesAno.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[0], out int ano) && int.TryParse(partes[1], out int mes))
                {
                    dataInicio = new DateTime(ano, mes, 1);
                    dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                }
            }

            var queryFaturados = _context.ProcessRecord.Where(p => p.IsFaturado == true);

            if (dataInicio.HasValue && dataFim.HasValue)
                queryFaturados = queryFaturados.Where(p => p.Date >= dataInicio && p.Date <= dataFim);

            if (clienteId.HasValue)
                queryFaturados = queryFaturados.Where(p => p.ClientId == clienteId);

            if (departamentoId.HasValue)
                queryFaturados = queryFaturados.Where(p => p.DepartmentId == departamentoId);

            if (advogadoId.HasValue)
                queryFaturados = queryFaturados.Where(p => p.AttorneyId == advogadoId);

            var faturadosIds = await queryFaturados.Select(p => p.Id).ToListAsync();

            if (!faturadosIds.Any())
            {
                TempData["MensagemErro"] = "Não há registros faturados para gerar o Excel.";
                return RedirectToAction("Index", new { mesAno, clienteId, departamentoId, advogadoId });
            }

            var queryTodos = _context.ProcessRecord.AsQueryable();
            
            if (dataInicio.HasValue && dataFim.HasValue)
                queryTodos = queryTodos.Where(p => p.Date >= dataInicio && p.Date <= dataFim);

            if (clienteId.HasValue)
                queryTodos = queryTodos.Where(p => p.ClientId == clienteId);

            if (departamentoId.HasValue)
                queryTodos = queryTodos.Where(p => p.DepartmentId == departamentoId);

            if (advogadoId.HasValue)
                queryTodos = queryTodos.Where(p => p.AttorneyId == advogadoId);

            var todosIds = await queryTodos.Select(p => p.Id).ToListAsync();
            var ignoredIds = todosIds.Except(faturadosIds).ToList();
            var ignoredIdsString = string.Join(",", ignoredIds);

            _logger.LogInformation($"GerarExcel: Total de registros faturados: {faturadosIds.Count}, Total a ignorar: {ignoredIds.Count}");

            var clientIds = clienteId.HasValue ? clienteId.ToString() : "";

            return RedirectToAction("DownloadReport", "ProcessRecord", new
            {
                minDate = dataInicio,
                maxDate = dataFim,
                clientIds = clientIds,
                attorneyId = advogadoId,
                departmentId = departamentoId,
                format = "xlsx",
                ignoredIds = ignoredIdsString
            });
        }

        public async Task<IActionResult> GerarPreFatura(string? mesAno, int? clienteId, int? departamentoId, int? advogadoId)
        {
            DateTime? dataInicio = null;
            DateTime? dataFim = null;

            if (!string.IsNullOrEmpty(mesAno))
            {
                var partes = mesAno.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[0], out int ano) && int.TryParse(partes[1], out int mes))
                {
                    dataInicio = new DateTime(ano, mes, 1);
                    dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                }
            }

            var queryFaturados = _context.ProcessRecord.Where(p => p.IsFaturado == true);

            if (dataInicio.HasValue && dataFim.HasValue)
                queryFaturados = queryFaturados.Where(p => p.Date >= dataInicio && p.Date <= dataFim);

            if (clienteId.HasValue)
                queryFaturados = queryFaturados.Where(p => p.ClientId == clienteId);

            if (departamentoId.HasValue)
                queryFaturados = queryFaturados.Where(p => p.DepartmentId == departamentoId);

            if (advogadoId.HasValue)
                queryFaturados = queryFaturados.Where(p => p.AttorneyId == advogadoId);

            var faturadosIds = await queryFaturados.Select(p => p.Id).ToListAsync();

            if (!faturadosIds.Any())
            {
                TempData["MensagemErro"] = "Não há registros faturados para gerar a Pré-Fatura.";
                return RedirectToAction("Index", new { mesAno, clienteId, departamentoId, advogadoId });
            }

            var queryTodos = _context.ProcessRecord.AsQueryable();
            
            if (dataInicio.HasValue && dataFim.HasValue)
                queryTodos = queryTodos.Where(p => p.Date >= dataInicio && p.Date <= dataFim);

            if (clienteId.HasValue)
                queryTodos = queryTodos.Where(p => p.ClientId == clienteId);

            if (departamentoId.HasValue)
                queryTodos = queryTodos.Where(p => p.DepartmentId == departamentoId);

            if (advogadoId.HasValue)
                queryTodos = queryTodos.Where(p => p.AttorneyId == advogadoId);

            var todosIds = await queryTodos.Select(p => p.Id).ToListAsync();
            var ignoredIds = todosIds.Except(faturadosIds).ToList();
            var ignoredIdsString = string.Join(",", ignoredIds);

            _logger.LogInformation($"GerarPreFatura: Total de registros faturados: {faturadosIds.Count}, Total a ignorar: {ignoredIds.Count}");

            var clientIds = clienteId.HasValue ? clienteId.ToString() : "";

            return RedirectToAction("PreFatura", "ProcessRecord", new
            {
                minDate = dataInicio,
                maxDate = dataFim,
                clientIds = clientIds,
                attorneyId = advogadoId,
                departmentId = departamentoId,
                ignoredIds = ignoredIdsString
            });
        }

        [HttpPost]
        public async Task<IActionResult> GerarResumoExecutivo([FromBody] ResumoExecutivoRequest request)
        {
            try
            {
                DateTime? dataInicio = null;
                DateTime? dataFim = null;

                if (!string.IsNullOrEmpty(request.MesAno))
                {
                    var partes = request.MesAno.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[0], out int ano) && int.TryParse(partes[1], out int mes))
                    {
                        dataInicio = new DateTime(ano, mes, 1);
                        dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                    }
                }

                var clientIds = request.ClienteId.HasValue ? request.ClienteId.ToString() : "";
                
                return RedirectToAction("GerarResumoExecutivo", "ProcessRecord", new
                {
                    minDate = dataInicio,
                    maxDate = dataFim,
                    clientIds = clientIds,
                    attorneyId = request.AdvogadoId,
                    departmentId = request.DepartamentoId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar resumo executivo");
                return Json(new { success = false, message = "Erro ao gerar resumo executivo." });
            }
        }

        // Endpoint de diagnóstico para verificar valores no banco
        [HttpGet]
        public async Task<IActionResult> DiagnosticoValores(int? clienteId = null)
        {
            var query = _context.ValorCliente
                .Include(v => v.Client)
                .Include(v => v.Attorney)
                .AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(v => v.ClientId == clienteId);

            var valores = await query
                .OrderBy(v => v.Client.Name)
                .ThenBy(v => v.AttorneyId)
                .ToListAsync();

            var resultado = valores.Select(v => new
            {
                Id = v.Id,
                Cliente = v.Client.Name,
                ClienteId = v.ClientId,
                Advogado = v.Attorney?.Name ?? "PADRÃO (null)",
                AdvogadoId = v.AttorneyId,
                Valor = v.Valor
            }).ToList();

            return Json(new
            {
                total = resultado.Count,
                valores = resultado
            });
        }

        [HttpPost]
        public async Task<IActionResult> GerarResumoExecutivoPDF([FromBody] ResumoExecutivoPDFRequest request)
        {
            try
            {
                DateTime? dataInicio = null;
                DateTime? dataFim = null;

                if (!string.IsNullOrEmpty(request.MesAno))
                {
                    var partes = request.MesAno.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[0], out int ano) && int.TryParse(partes[1], out int mes))
                    {
                        dataInicio = new DateTime(ano, mes, 1);
                        dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                    }
                }

                var clientIds = request.ClienteId.HasValue ? request.ClienteId.ToString() : "";

                return RedirectToAction("GerarResumoExecutivoPDF", "ProcessRecord", new
                {
                    minDate = dataInicio,
                    maxDate = dataFim,
                    clientIds = clientIds,
                    attorneyId = request.AdvogadoId,
                    departmentId = request.DepartamentoId,
                    resumo = request.Resumo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF do resumo executivo");
                return StatusCode(500);
            }
        }
    }
}
