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
            // Verificação dos dados de entrada
            if (request == null || string.IsNullOrWhiteSpace(request.Description) || request.ClientId <= 0 || request.DepartmentId <= 0)
            {
                return BadRequest("Todos os campos da tela devem ser preenchidos");
            }

            if (!Enum.IsDefined(typeof(RecordType), request.RecordType))
            {
                return BadRequest("Tipo de registro inválido");
            }

            // Verifica se a sessão do usuário está ativa
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            if (usuario == null)
            {
                // Retorna uma mensagem informando que a sessão expirou
                return Unauthorized("Sessão expirada. Por favor, faça login novamente.");
            }

            // Obtém o ID do usuário a partir da sessão
            var attorneyId = usuario.Id;

            // Configura o horário usando o fuso horário de Brasília
            var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var nowInBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaTimeZone);

            // Cria o registro de processo com as informações fornecidas
            var processRecord = new ProcessRecord
            {
                AttorneyId = attorneyId,
                ClientId = request.ClientId,
                DepartmentId = request.DepartmentId,
                Date = DateTime.Now.Date,
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

            var processRecord = await _processRecordsService.FindByIdAsync(request.ProcessRecordId);

            if (processRecord == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                processRecord.Description = request.Description;
            }

            var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var nowInBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaTimeZone);
            //processRecord.HoraFinal = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);            
            processRecord.HoraFinal = new TimeSpan(nowInBrasilia.Hour, nowInBrasilia.Minute, nowInBrasilia.Second);

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clients = await _clientService.FindAllAsync();
            var departments = await _departmentService.FindAllAsync();
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.LoggedUserId = usuario.Id;

            var clientsOptions = clients
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .Prepend(new SelectListItem { Value = "0", Text = "Selecione o Cliente" })
                .ToList();

            var departmentsOptions = departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .Prepend(new SelectListItem { Value = "0", Text = "Selecione a Área" }) 
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
        }

        [HttpGet]
        public async Task<IActionResult> GetRecordsForToday(int attorneyId)
        {
            var today = DateTime.Now.Date;
            var records = await _context.ProcessRecord
                .Where(r => r.AttorneyId == attorneyId && r.HoraFinal != null && r.HoraFinal != TimeSpan.Zero)
                .Include(r => r.Client)
                .OrderByDescending(r => r.Date) // Ordena pela data, do mais recente ao mais antigo
                .ThenByDescending(r => r.HoraInicial) // Dentro da mesma data, ordena pela hora inicial
                .ToListAsync();


            var viewModel = new ProcessRecordViewModel
            {
                ProcessRecords = records,
                Clients = records.Select(r => r.Client).ToList(),
                // Outras propriedades que o ViewModel pode precisar
            };

            return Json(records.Select(r => new
            {
                r.Id,
                r.Description,
                ClienteNome = r.Client.Name, // Inclui o nome do cliente no JSON
                r.HoraInicial,
                r.HoraFinal,
                r.RecordType,
                r.Solicitante,
                r.Date
                
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetRecordById(int recordId)
        {
            var record = await _context.ProcessRecord
                .Include(r => r.Client) // Inclui o cliente no retorno
                .Include(r => r.Department) // Inclui o departamento, se necessário
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
                RecordType = RecordType.Consultivo, // Ou ajuste de acordo com o que você precisar
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


    }
}
