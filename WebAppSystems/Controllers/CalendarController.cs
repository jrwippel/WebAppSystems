using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Data;
using WebAppSystems.Helper;
using WebAppSystems.Services;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Models;
using Org.BouncyCastle.Asn1.Ocsp;
using WebAppSystems.Models.Enums;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Migrations;

namespace WebAppSystems.Controllers
{
    public class CalendarController : Controller
    {

       
            private readonly WebAppSystemsContext _context;
            private readonly ProcessRecordsService _processRecordsService;
            private readonly ISessao _isessao;
            private readonly ClientService _clientService;
            private readonly DepartmentService _departmentService;
            
            public CalendarController(WebAppSystemsContext context, ProcessRecordsService processRecordsService, ISessao isessao, ClientService clientService, DepartmentService departmentService)
            {
                _context = context;
                _processRecordsService = processRecordsService;
                _isessao = isessao;
                _clientService = clientService;
                _departmentService = departmentService;
            }
            public async Task<IActionResult> Index()
            {
                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;

                var clients = await _clientService.FindAllAsync();

                var departments = await _departmentService.FindAllAsync();

                var clientsOptions = clients
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    //.Prepend(new SelectListItem { Value = "0", Text = "Selecione o Cliente" })
                    .ToList();

                var departmentsOptions = departments
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    //.Prepend(new SelectListItem { Value = "0", Text = "Selecione a Área" })
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
            public async Task<IActionResult> GetUserRecords(int attorneyId)
            {
                var records = await _context.ProcessRecord
                    .Where(r => r.AttorneyId == attorneyId && r.HoraFinal != null && r.HoraFinal != TimeSpan.Zero)
                    .Include(r => r.Client)
                    .OrderByDescending(r => r.HoraInicial)
                    .ToListAsync();

                // Formatação dos registros no formato JSON esperado pelo FullCalendar
                return Json(records.Select(r => new
                {
                    title = r.Description,
                    start = r.Date.Add(r.HoraInicial),
                    end = r.Date.Add(r.HoraFinal),
                    clientName = r.Client.Name,
                    processId = r.Id,
                    solicitante = r.Solicitante,
                    departmentId = r.DepartmentId,
                    clientId = r.ClientId,
                    tipoRegistro = r.RecordType
                    
                }));
            }

        [HttpPost]
        public async Task<IActionResult> SaveProcessRecord([FromBody] StartTimerRequest record)
        {
            if (record == null)
            {
                return BadRequest(new { success = false, message = "Dados inválidos ou nulos" });
            }

            // Log para verificar o ID recebido
            Console.WriteLine("ID recebido: " + record.Id);

            var recordType = (RecordType)record.RecordType;
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            var attorneyId = usuario.Id;

            // Conversão de HoraInicial e HoraFinal de string para TimeSpan
            TimeSpan horaInicial = TimeSpan.Parse(record.HoraInicial);  // Espera formato "HH:mm"
            TimeSpan horaFinal = TimeSpan.Parse(record.HoraFinal);

            var processRecord = new ProcessRecord
            {
                Date = record.Date,
                HoraInicial = horaInicial,
                HoraFinal = horaFinal,
                Description = record.Description,
                RecordType = recordType,
                AttorneyId = attorneyId,
                ClientId = record.ClientId,                
                DepartmentId = record.DepartmentId,
                Solicitante = record.Solicitante,
                
                
            };

            if (record.Id == 0)
            {
                _context.ProcessRecord.Add(processRecord);
            }
            else
            {
                var existingRecord = await _context.ProcessRecord.FindAsync(record.Id);
                if (existingRecord != null)
                {
                    existingRecord.Description = record.Description;
                    existingRecord.Date = record.Date;
                    existingRecord.HoraInicial = horaInicial;
                    existingRecord.HoraFinal = horaFinal;
                    existingRecord.ClientId = record.ClientId;
                    existingRecord.DepartmentId = record.DepartmentId;
                    existingRecord.Solicitante = record.Solicitante;
                    existingRecord.RecordType = recordType;

                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }




        public class StartTimerRequest
        {
            public int Id { get; set; }
            public int ClientId { get; set; }
            public string Description { get; set; }
            public int DepartmentId { get; set; }
            public string Solicitante { get; set; }
            public int RecordType { get; set; }

            public DateTime Date { get; set; }

            public string HoraInicial { get; set; }
            public string HoraFinal { get; set; }
        }





    }
}
