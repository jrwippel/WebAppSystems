﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using WebAppSystems.Data;
using WebAppSystems.Extensions;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    public class ProcessRecordsController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ClientService _clientService;
        private readonly AttorneyService _attorneyService;
        private readonly ProcessRecordsService _processRecordsService;
        private readonly ISessao _isessao;
        private readonly DepartmentService _departmentService;

        public ProcessRecordsController(WebAppSystemsContext context, ClientService clientService, ProcessRecordsService processRecordsService, AttorneyService attorneyService, ISessao isessao, DepartmentService departmentService)
        {
            _context = context;
            _clientService = clientService;
            _processRecordsService = processRecordsService;
            _attorneyService = attorneyService;
            _isessao = isessao;
            _departmentService = departmentService;
        }

        // GET: ProcessRecords
        public IActionResult Index()
        {
            try
            {
                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                return View(Enumerable.Empty<ProcessRecord>()); // Retorna um modelo vazio
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }



        // GET: ProcessRecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.LoggedUserId = usuario.Id;
            ViewBag.IsAdmin = usuario.Perfil == ProfileEnum.Admin;

            if (id == null || _context.ProcessRecord == null)
            {
                return NotFound();
            }

            var processRecord = await _processRecordsService.FindByIdAsync(id.Value);

            if (processRecord == null)
            {
                return NotFound();
            }

            return View(processRecord);

        }

        public async Task<IActionResult> Create()
        {
            var clients = await _clientService.FindAllAsync();
            var attorneys = _attorneyService.FindAll();
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            var departments = await _departmentService.FindAllAsync();

            ViewBag.LoggedUserId = usuario.Id;
            ViewBag.IsAdmin = usuario.Perfil == ProfileEnum.Admin;

            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var clientsOptions = clients
                .Where(c => !c.ClienteInativo) // Filtra apenas clientes ativos
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .Prepend(new SelectListItem { Value = "0", Text = "Selecionar" })
                .ToList();

            var recordTypeOptions = Enum.GetValues(typeof(RecordType))
                           .Cast<RecordType>()
                           .Select(rt => new SelectListItem
                           {
                               Value = rt.ToString(),
                               Text = rt.ToString()
                           })
                           .ToList();

            var viewModel = new ProcessRecordViewModel
            {
                ProcessRecord = new ProcessRecord
                {
                    AttorneyId = usuario.Id,
                    Date = DateTime.Now,
                    HoraInicial = TimeSpan.Zero,
                    HoraFinal = TimeSpan.Zero,
                    Description = string.Empty,
                    ClientId = 0
                },
                Attorneys = attorneys,
                Clients = clients,
                ClientsOptions = clientsOptions,
                Departments = departments,
                RecordTypesOptions = recordTypeOptions
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProcessRecord processRecord)
        {
            if (processRecord.ClientId == 0)
            {
                ModelState.AddModelError("ProcessRecord.ClientId", "Por favor, selecione um cliente.");
            }

            if (!processRecord.IsStartTimeLessEndTime())
            {
                ModelState.AddModelError("ProcessRecord.HoraInicial", "A hora inicial deve ser menor que a hora final.");
            }

            // >>> Validação de sobreposição de horários
            bool temConflito = await _context.ProcessRecord
                .AnyAsync(pr =>
                    pr.AttorneyId == processRecord.AttorneyId &&
                    pr.Date.Date == processRecord.Date.Date &&
                    pr.Id != processRecord.Id && // Evita conflito consigo mesmo na edição
                    processRecord.HoraInicial < pr.HoraFinal &&
                    processRecord.HoraFinal > pr.HoraInicial
                );

            if (temConflito)
            {
                ModelState.AddModelError("", "O horário informado conflita com outro registro do mesmo dia.");
            }

            if (!ModelState.IsValid)
            {
                var clients = await _clientService.FindAllAsync();
                var attorneys = await _attorneyService.FindAllAsync();
                var departments = await _departmentService.FindAllAsync();
                var clientsOptions = clients
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .Prepend(new SelectListItem { Value = "0", Text = "Selecionar" })
                    .ToList();

                var recordTypeOptions = Enum.GetValues(typeof(RecordType))
                                   .Cast<RecordType>()
                                   .Select(rt => new SelectListItem
                                   {
                                       Value = rt.ToString(),
                                       Text = rt.ToString()
                                   })
                                   .ToList();

                var viewModel = new ProcessRecordViewModel
                {
                    ProcessRecord = processRecord,
                    Attorneys = attorneys,
                    Clients = clients,
                    ClientsOptions = clientsOptions,
                    Departments = departments,
                    RecordTypesOptions = recordTypeOptions
                };
                return View(viewModel);
            }

            _context.Add(processRecord);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: ProcessRecords/Edit/5

        public async Task<IActionResult> Edit(int? id)
        {
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();

            if (usuario == null)
            {
                // Redirecionar para a página de login se o usuário não estiver logado
                return RedirectToAction("Index", "Login");
            }
            if (id == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not provided" });
            }
            var obj = await _processRecordsService.FindByIdAsync(id.Value);
            if (obj == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not found" });
            }
            List<Attorney> attorneys = await _attorneyService.FindAllAsync();
            List<Client> clients = await _clientService.FindAllAsync();
            List<Department> departments = await _departmentService.FindAllAsync();

            var clientsOptions = clients
                 .OrderBy(c => c.Name)
                 .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                 .ToList();

            var recordTypeOptions = Enum.GetValues(typeof(RecordType))
                           .Cast<RecordType>()
                           .Select(rt => new SelectListItem
                           {
                               Value = rt.ToString(),
                               Text = rt.ToString()
                           })
                           .ToList();



            ProcessRecordViewModel viewModel = new ProcessRecordViewModel
            {
                ProcessRecord = obj,
                Attorneys = attorneys,
                Clients = clients,
                ClientsOptions = clientsOptions,
                Departments = departments,
                RecordTypesOptions = recordTypeOptions
            };
            return View(viewModel);

        }

        // POST: ProcessRecords/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProcessRecord processRecord)
        {
            if (!processRecord.IsStartTimeLessEndTime())
            {
                return RedirectToAction(nameof(Error), new { message = "A hora inicial deve ser menor que a hora final.\"" });
            }

            if (id != processRecord.Id)
            {
                return NotFound();
            }

            try
            {
                _context.Update(processRecord);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProcessRecordExists(processRecord.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: ProcessRecords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.ProcessRecord == null)
            {
                return NotFound();
            }

            var processRecord = await _context.ProcessRecord
                .FirstOrDefaultAsync(m => m.Id == id);
            if (processRecord == null)
            {
                return NotFound();
            }

            return View(processRecord);
        }

        // POST: ProcessRecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.ProcessRecord == null)
            {
                return Problem("Entity set 'WebAppSystemsContext.ProcessRecord'  is null.");
            }
            var processRecord = await _context.ProcessRecord.FindAsync(id);
            if (processRecord != null)
            {
                _context.ProcessRecord.Remove(processRecord);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProcessRecordExists(int id)
        {
            return (_context.ProcessRecord?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        public IActionResult Error(string message)
        {
            var viewModel = new ErrorViewModel
            {
                Message = message,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetClientSolicitante(int clientId)
        {


            var client = await _clientService.FindByIdAsync(clientId);
            if (client == null)
            {
                return Json(new { success = false });
            }

            // Verifica se o cliente é interno
            if (client.ClienteInterno)
            {
                var attorneys = _attorneyService.FindAll();
                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                // Obtém o nome do usuário logado
                var userName = usuario.Name; // Ajuste conforme necessário para obter o nome do usuário logado
                return Json(new { success = true, solicitante = userName });
            }

            return Json(new { success = true, solicitante = client.Solicitante });
        }

        public async Task<JsonResult> GetProcessRecords(int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            try
            {
                int page = (start / length) + 1;
                string searchValue = search?.Trim().ToLower();

                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                bool isAdmin = usuario.Perfil == ProfileEnum.Admin;

                var (records, totalRecords) = await _processRecordsService.FindAllAsync(
                    page,
                    length,
                    searchValue,
                    orderColumn,
                    orderDir,
                    loggedUserId: usuario.Id,
                    isAdmin: isAdmin
                );

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = records.Select(pr => new
                    {
                        Date = pr.Date.ToString("dd/MM/yyyy"),
                        pr.HoraInicial,
                        pr.HoraFinal,
                        Horas = pr.CalculoHoras(),
                        Cliente = pr.Client.Name,
                        Usuario = pr.Attorney.Name,
                        Tipo = ((RecordType)pr.RecordType).GetDisplayName(),
                        EditLink = pr.Attorney.Id == ViewBag.LoggedUserId
                            ? Url.Action("Edit", new { id = pr.Id })
                            : null,
                        DetailsLink = Url.Action("Details", new { id = pr.Id }),
                        DeleteLink = pr.Attorney.Id == ViewBag.LoggedUserId
                            ? Url.Action("Delete", new { id = pr.Id })
                            : null
                    })
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }



    }
}
