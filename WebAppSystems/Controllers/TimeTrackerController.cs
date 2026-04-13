using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using WebAppSystems.Data;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;

//teste

namespace WebAppSystems.Controllers
{
    public class TimeTrackerController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ProcessRecordsService _processRecordsService;
        private readonly ISessao _isessao;
        private readonly ClientService _clientService;
        private readonly DepartmentService _departmentService;

        public TimeTrackerController(WebAppSystemsContext context, ProcessRecordsService processRecordsService, ISessao isessao, ClientService clientService, DepartmentService departmentService)
        {
            _context = context;
            _processRecordsService = processRecordsService;
            _isessao = isessao;
            _clientService = clientService;
            _departmentService = departmentService;
        }

        [HttpPost]
        public async Task<IActionResult> StartTimer([FromBody] StartTimerRequest request)
        {
            // Verifica��o dos dados de entrada
            if (request == null || string.IsNullOrWhiteSpace(request.Description) || request.ClientId <= 0 || request.DepartmentId <= 0)
            {
                return BadRequest("Todos os campos da tela devem ser preenchidos");
            }

            if (!Enum.IsDefined(typeof(RecordType), request.RecordType))
            {
                return BadRequest("Tipo de registro inv�lido");
            }

            // Verifica se a sess�o do usu�rio est� ativa
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            if (usuario == null)
            {
                // Retorna uma mensagem informando que a sess�o expirou
                return Unauthorized("Sess�o expirada. Por favor, fa�a login novamente.");
            }

            // Obt�m o ID do usu�rio a partir da sess�o
            var attorneyId = usuario.Id;

            // Verifica se j� existe um timer em execu��o para este usu�rio
            var activeTimer = await _context.ProcessRecord
                .Where(pr => pr.AttorneyId == attorneyId &&
                             (pr.HoraFinal == null || pr.HoraFinal == TimeSpan.Zero))
                .FirstOrDefaultAsync();

            if (activeTimer != null)
            {
                return BadRequest("J� existe um timer em execu��o. Pare o timer atual antes de iniciar um novo.");
            }

            // Configura o hor�rio usando o fuso hor�rio de Bras�lia
            var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var nowInBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaTimeZone);

            // Cria o registro de processo com as informa��es fornecidas
            var processRecord = new ProcessRecord
            {
                AttorneyId = attorneyId,
                ClientId = request.ClientId,
                DepartmentId = request.DepartmentId,
                Date = nowInBrasilia.Date,
                HoraInicial = new TimeSpan(nowInBrasilia.Hour, nowInBrasilia.Minute, nowInBrasilia.Second),
                Description = request.Description,
                RecordType = (RecordType)request.RecordType,
                Solicitante = request.Solicitante,
            };

            _context.ProcessRecord.Add(processRecord);
            await _context.SaveChangesAsync();

            // Retorna o ID do processo registrado
            return Ok(processRecord.Id);
        }


        [HttpPost]
        public async Task<IActionResult> StopTimer([FromBody] StopTimerRequest request)
        {
            if (request == null || request.ProcessRecordId == 0)
            {
                return BadRequest("ProcessRecord ID is required.");
            }

            var processRecord = await _context.ProcessRecord
                .FirstOrDefaultAsync(p => p.Id == request.ProcessRecordId);

            if (processRecord == null)
            {
                return NotFound();
            }

            // Aplica TODOS os campos editados pelo usuário
            if (!string.IsNullOrEmpty(request.Description))
                processRecord.Description = request.Description;
            if (request.ClientId > 0)
                processRecord.ClientId = request.ClientId;
            if (request.DepartmentId > 0)
                processRecord.DepartmentId = request.DepartmentId;
            if (!string.IsNullOrEmpty(request.Solicitante))
                processRecord.Solicitante = request.Solicitante;
            if (Enum.IsDefined(typeof(RecordType), request.RecordType))
                processRecord.RecordType = (RecordType)request.RecordType;

            var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var nowInBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaTimeZone);
            var horaFinalAtual = new TimeSpan(nowInBrasilia.Hour, nowInBrasilia.Minute, nowInBrasilia.Second);
            var dataAtual = nowInBrasilia.Date;

            // Verifica se o timer passou da meia-noite (data atual diferente da data de in�cio)
            if (dataAtual > processRecord.Date)
            {
                // Calcula quantos dias se passaram
                var diasPassados = (dataAtual - processRecord.Date).Days;

                // Fecha o registro do primeiro dia at� 23:59:59
                processRecord.HoraFinal = new TimeSpan(23, 59, 59);
                await _context.SaveChangesAsync();

                // Cria registros para os dias intermedi�rios (se houver)
                for (int i = 1; i < diasPassados; i++)
                {
                    var diaIntermediario = processRecord.Date.AddDays(i);
                    var registroIntermediario = new ProcessRecord
                    {
                        AttorneyId = processRecord.AttorneyId,
                        ClientId = processRecord.ClientId,
                        DepartmentId = processRecord.DepartmentId,
                        Date = diaIntermediario,
                        HoraInicial = new TimeSpan(0, 0, 0),
                        HoraFinal = new TimeSpan(23, 59, 59),
                        Description = processRecord.Description + " (continua��o)",
                        RecordType = processRecord.RecordType,
                        Solicitante = processRecord.Solicitante
                    };
                    _context.ProcessRecord.Add(registroIntermediario);
                }

                // Cria registro para o dia atual desde 00:00:00 at� a hora atual
                var registroFinal = new ProcessRecord
                {
                    AttorneyId = processRecord.AttorneyId,
                    ClientId = processRecord.ClientId,
                    DepartmentId = processRecord.DepartmentId,
                    Date = dataAtual,
                    HoraInicial = new TimeSpan(0, 0, 0),
                    HoraFinal = horaFinalAtual,
                    Description = processRecord.Description + " (continua��o)",
                    RecordType = processRecord.RecordType,
                    Solicitante = processRecord.Solicitante
                };
                _context.ProcessRecord.Add(registroFinal);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Timer parado no mesmo dia - comportamento normal
                processRecord.HoraFinal = horaFinalAtual;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        // M�TODO DE TESTE - Simula timer que passou da meia-noite
        [HttpPost]
        public async Task<IActionResult> TestMidnightScenario([FromBody] TestMidnightRequest request)
        {
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            if (usuario == null)
            {
                return Unauthorized("Sess�o expirada.");
            }

            // Cria um registro simulando que come�ou ontem �s 18:00
            var ontem = DateTime.Now.Date.AddDays(-1);
            var processRecord = new ProcessRecord
            {
                AttorneyId = usuario.Id,
                ClientId = request.ClientId,
                DepartmentId = request.DepartmentId,
                Date = ontem,
                HoraInicial = new TimeSpan(18, 0, 0), // 18:00
                HoraFinal = new TimeSpan(0, 0, 0), // Ainda rodando
                Description = request.Description + " (TESTE - Iniciado ontem �s 18:00)",
                RecordType = (RecordType)request.RecordType,
                Solicitante = request.Solicitante
            };

            _context.ProcessRecord.Add(processRecord);
            await _context.SaveChangesAsync();

            // Agora simula o stop hoje �s 08:00
            var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var nowInBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaTimeZone);
            var horaFinalSimulada = new TimeSpan(8, 0, 0); // Simula 08:00
            var dataAtual = DateTime.Now.Date;

            // Fecha o registro de ontem at� 23:59:59
            processRecord.HoraFinal = new TimeSpan(23, 59, 59);
            await _context.SaveChangesAsync();

            // Cria registro para hoje desde 00:00:00 at� 08:00:00
            var registroHoje = new ProcessRecord
            {
                AttorneyId = processRecord.AttorneyId,
                ClientId = processRecord.ClientId,
                DepartmentId = processRecord.DepartmentId,
                Date = dataAtual,
                HoraInicial = new TimeSpan(0, 0, 0),
                HoraFinal = horaFinalSimulada,
                Description = processRecord.Description + " (continua��o - parado hoje �s 08:00)",
                RecordType = processRecord.RecordType,
                Solicitante = processRecord.Solicitante
            };
            _context.ProcessRecord.Add(registroHoje);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Teste criado com sucesso!", 
                registroOntem = new { 
                    data = ontem.ToString("dd/MM/yyyy"), 
                    inicio = "18:00:00", 
                    fim = "23:59:59",
                    duracao = "5h 59min 59s"
                },
                registroHoje = new { 
                    data = dataAtual.ToString("dd/MM/yyyy"), 
                    inicio = "00:00:00", 
                    fim = "08:00:00",
                    duracao = "8h"
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clients = await _clientService.FindAllAsync();
            var departments = await _departmentService.FindAllAsync();
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.LoggedUserId = usuario.Id;

            var clientsOptions = clients
                .Where(c => !c.ClienteInativo) // Filtra apenas clientes ativos
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .Prepend(new SelectListItem { Value = "0", Text = "Selecione o Cliente" })
                .ToList();

            // Carregar as op��es de departamentos e pr�-selecionar a �rea do usu�rio
            var departmentsOptions = departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name,
                    Selected = d.Id == usuario.DepartmentId // Marcar a area do usuario como selecionada
                })
                .Prepend(new SelectListItem { Value = "0", Text = "Selecione a Area" })
                .ToList();

            var recordTypeOptions = Enum.GetValues(typeof(RecordType))
                .Cast<RecordType>()
                .Select(rt => new SelectListItem
                {
                    Value = ((int)rt).ToString(),
                    Text = rt.ToString()
                })                
                .ToList();

            var viewModel = new ProcessRecordViewModel
            {
                ClientsOptions = clientsOptions,
                DepartmentsOptions = departmentsOptions,
                RecordTypesOptions = recordTypeOptions
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveTimer(int attorneyId)
        {
            var activeRecord = await _context.ProcessRecord
                .Where(pr => pr.AttorneyId == attorneyId &&
                             (pr.HoraFinal == null || pr.HoraFinal == TimeSpan.Zero))
                .OrderByDescending(pr => pr.Date)
                .ThenByDescending(pr => pr.HoraInicial)
                .FirstOrDefaultAsync();

            if (activeRecord == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                ProcessRecordId = activeRecord.Id,
                Date = activeRecord.Date.ToString("yyyy-MM-dd"),
                HoraInicial = activeRecord.HoraInicial.ToString(@"hh\:mm\:ss"),
                ClientId = activeRecord.ClientId,
                DepartmentId = activeRecord.DepartmentId,
                Description = activeRecord.Description,
                Solicitante = activeRecord.Solicitante,
                RecordType = activeRecord.RecordType
            });
        }


        public class StartTimerRequest
        {
            public int ClientId { get; set; }
            public string Description { get; set; }
            public int DepartmentId { get; set; } 
            public string Solicitante { get; set; }

            public int RecordType { get; set; }
        }

        public class StopTimerRequest
        {
            public int ProcessRecordId { get; set; }
            public string Description { get; set; }
            public int ClientId { get; set; }
            public int DepartmentId { get; set; }
            public string Solicitante { get; set; }
            public int RecordType { get; set; }
        }

        public class TestMidnightRequest
        {
            public int ClientId { get; set; }
            public string Description { get; set; }
            public int DepartmentId { get; set; }
            public string Solicitante { get; set; }
            public int RecordType { get; set; }
        }
        /*
        [HttpGet]
        public async Task<IActionResult> GetRecordsForToday(int attorneyId)
        {
            var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var nowInBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaTimeZone);
            var today = nowInBrasilia.Date;
            
            var records = await _context.ProcessRecord
                .Where(r => r.AttorneyId == attorneyId && 
                           r.HoraFinal != null && 
                           r.HoraFinal != TimeSpan.Zero)
                .Include(r => r.Client)
                .ToListAsync();

            // Filtrar por data de hoje ap�s carregar do banco
            var todayRecords = records               
                .OrderByDescending(r => r.Id) // Ordenar por ID (mais recente primeiro)
                .ToList();

            return Json(todayRecords.Select(r => new
            {
                r.Id,
                r.Description,
                ClienteNome = r.Client.Name,
                r.HoraInicial,
                r.HoraFinal,
                r.RecordType,
                r.Solicitante,
                r.Date
            }));
        }
        */


        [HttpGet]
        public async Task<IActionResult> GetRecordsForToday(int attorneyId, int page = 1, int pageSize = 10, string search = "")
        {
            var query = _context.ProcessRecord
                .Where(r => r.AttorneyId == attorneyId && 
                           r.HoraFinal != null && 
                           r.HoraFinal != TimeSpan.Zero)
                .Include(r => r.Client)
                .AsQueryable();

            // Aplicar filtro de busca
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(r => 
                    r.Description.ToLower().Contains(search) ||
                    r.Client.Name.ToLower().Contains(search) ||
                    (r.Solicitante != null && r.Solicitante.ToLower().Contains(search))
                );
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var records = await query
                .OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Json(new
            {
                records = records.Select(r => new
                {
                    r.Id,
                    r.Description,
                    ClienteNome = r.Client.Name,
                    ClientId = r.ClientId,
                    HoraInicial = r.HoraInicial.ToString(@"hh\:mm\:ss"),
                    HoraFinal = r.HoraFinal.ToString(@"hh\:mm\:ss"),
                    r.RecordType,
                    r.Solicitante,
                    r.Date
                }),
                currentPage = page,
                totalPages = totalPages,
                totalRecords = totalRecords,
                pageSize = pageSize
            });
        }



        [HttpGet]
        public async Task<IActionResult> GetRecordById(int recordId)
        {
            var record = await _context.ProcessRecord
                .Include(r => r.Client) // Inclui o cliente no retorno
                .Include(r => r.Department) // Inclui o departamento, se necess�rio
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                ProcessRecordId = record.Id,
                HoraInicial = record.HoraInicial.ToString(@"hh\:mm\:ss"),
                HoraFinal = record.HoraFinal.ToString(@"hh\:mm\:ss"),
                ClientId = record.ClientId,
                DepartmentId = record.DepartmentId,
                Description = record.Description,
                Solicitante = record.Solicitante,
                RecordType = record.RecordType
            });
        }

        [HttpGet]
        public async Task<IActionResult> Calendar()
        {
            var clients = await _clientService.FindAllAsync();
            var departments = await _departmentService.FindAllAsync();

            ViewBag.Clients = clients;
            ViewBag.Departments = departments;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitTimeEntry([FromBody] TimeEntryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description) || request.ClientId <= 0 || request.DepartmentId <= 0)
            {
                return BadRequest("Todos os campos devem ser preenchidos.");
            }

            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            var attorneyId = usuario.Id;

            var processRecord = new ProcessRecord
            {
                AttorneyId = attorneyId,
                ClientId = request.ClientId,
                DepartmentId = request.DepartmentId,
                Date = DateTime.Parse(request.StartTime).Date,
                HoraInicial = TimeSpan.Parse(request.StartTime.Split(' ')[1]),
                HoraFinal = TimeSpan.Parse(request.EndTime.Split(' ')[1]),
                Description = request.Description,
                Solicitante = request.Solicitante,
                RecordType = RecordType.Consultivo, // Ou ajuste de acordo com o que voc� precisar
            };

            _context.ProcessRecord.Add(processRecord);
            await _context.SaveChangesAsync();

            return Ok();
        }

        public class TimeEntryRequest
        {
            public int ClientId { get; set; }
            public int DepartmentId { get; set; }
            public string Solicitante { get; set; }
            public string Description { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTime([FromBody] UpdateTimeRequest request)
        {
            if (request == null || request.ProcessRecordId == 0)
                return BadRequest("ID inválido.");

            var record = await _context.ProcessRecord.FindAsync(request.ProcessRecordId);
            if (record == null)
                return NotFound();

            if (!TimeSpan.TryParse(request.HoraInicial, out var horaInicial) ||
                !TimeSpan.TryParse(request.HoraFinal, out var horaFinal))
                return BadRequest("Formato de hora inválido.");

            if (horaInicial >= horaFinal)
                return BadRequest("A hora inicial deve ser menor que a hora final.");

            record.HoraInicial = horaInicial;
            record.HoraFinal = horaFinal;

            if (!string.IsNullOrWhiteSpace(request.Description))
                record.Description = request.Description;
            if (request.ClientId.HasValue && request.ClientId > 0)
                record.ClientId = request.ClientId.Value;
            if (!string.IsNullOrWhiteSpace(request.Solicitante))
                record.Solicitante = request.Solicitante;
            if (request.RecordType.HasValue && Enum.IsDefined(typeof(RecordType), request.RecordType.Value))
                record.RecordType = (RecordType)request.RecordType.Value;

            await _context.SaveChangesAsync();

            var diff = record.HoraFinal - record.HoraInicial;
            var duration = $"{(int)diff.TotalHours:00}:{diff.Minutes:00}:{diff.Seconds:00}";
            return Ok(new { duration });
        }

        public class UpdateTimeRequest
        {
            public int ProcessRecordId { get; set; }
            public string HoraInicial { get; set; }
            public string HoraFinal { get; set; }
            public string Description { get; set; }
            public int? ClientId { get; set; }
            public string Solicitante { get; set; }
            public int? RecordType { get; set; }
        }

        // Action para retornar o solicitante baseado no cliente
        [HttpGet]
        public async Task<IActionResult> GetSolicitanteByClientId(int clientId)
        {
            try
            {
                var solicitante = await _clientService.GetSolicitanteByClientIdAsync(clientId);

                // Retorna como JSON o solicitante encontrado
                return Json(new { solicitante });
            }
            catch (Exception ex)
            {
                // Caso ocorra erro, retorna um erro simples
                return Json(new { solicitante = string.Empty });
            }
        }
        /*
        [HttpGet]
        //[Route("keep-alive")]
        public IActionResult KeepAlive()
        {
            // Resposta 200 OK para manter a aplica��o ativa no Azure
            return Ok();
        }
        */


    }
}
