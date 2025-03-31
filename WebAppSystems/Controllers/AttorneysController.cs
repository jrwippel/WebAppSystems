using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using WebAppSystems.Services.Exceptions;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class AttorneysController : Controller
    {
        private readonly AttorneyService _attorneyService;

        private readonly DepartmentService _departmentService;

        private readonly ISessao _isessao;

        public AttorneysController (AttorneyService attorneyService, DepartmentService departmentService, ISessao isessao)
        {
            _attorneyService = attorneyService;
            _departmentService = departmentService;
            _isessao = isessao;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var list = await _attorneyService.FindAllAsync();
                return View(list);
            }
            catch (SessionExpiredException)
            {
                // Redirecione para a página de login se a sessão expirou
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }

       
        public async Task<IActionResult> Create()
        {
            var departments = await _departmentService.FindAllAsync();
            var viewModel = new AttorneyFormViewModel { Departments = departments };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Attorney attorney) 
        {

            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.LoggedUserId = usuario.Id;
            ViewBag.CurrentUserPerfil = usuario.Perfil;

            // Validação: somente Admin pode criar outro usuário com perfil de Admin
            if (usuario.Perfil != ProfileEnum.Admin && attorney.Perfil == ProfileEnum.Admin)
            {
                ModelState.AddModelError(string.Empty, "Você não tem permissão para criar um usuário com perfil de Administrador.");
                var departments = await _departmentService.FindAllAsync();
                var viewModel = new AttorneyFormViewModel { Attorney = attorney, Departments = departments };
                return View(viewModel);
            }

            if (ModelState.IsValid)
            {
   
                var departments = await _departmentService.FindAllAsync();
                var viewModel = new AttorneyFormViewModel { Attorney = attorney, Departments = departments };
                return View(viewModel);
            }

            await _attorneyService.InsertAsync(attorney);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {

            if (id == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not provided"});
            }
            var obj = await _attorneyService.FindByIdAsync(id.Value);
            if (obj == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not found" });
            }
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _attorneyService.RemoveAsync(id);
                return RedirectToAction(nameof(Index));
            }catch (IntegrityException ex)
            {
                return RedirectToAction(nameof(Error), new { message = ex.Message });
            }          
        }

        public async Task<IActionResult> Details(int? id)
        {

            if (id == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not provided" }); ;
            }
            var obj = await _attorneyService.FindByIdAsync(id.Value);
            if (obj == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not found" });
            }
            return View(obj);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not provided" });
            }
            var obj = await _attorneyService.FindByIdAsync(id.Value);
            if (obj == null)
            {
                return RedirectToAction(nameof(Error), new { message = "Id not found" });
            }
            List<Department> departments = await _departmentService.FindAllAsync();
            bool useBorder = obj.UseBorder;
            AttorneyFormViewModel viewModel = new AttorneyFormViewModel { Attorney = obj, Departments = departments };
            return View(viewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Attorney attorney)             
        {
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.LoggedUserId = usuario.Id;
            ViewBag.CurrentUserPerfil = usuario.Perfil;

            // Validação: somente Admin pode criar outro usuário com perfil de Admin
            if (usuario.Perfil != ProfileEnum.Admin && attorney.Perfil == ProfileEnum.Admin)
            {
                ModelState.AddModelError(string.Empty, "Você não tem permissão para alterar um usuário com perfil de Administrador.");
                var departments = await _departmentService.FindAllAsync();
                var viewModel = new AttorneyFormViewModel { Attorney = attorney, Departments = departments };
                return View(viewModel);
            }


            if (ModelState.IsValid)
            {
                var departments = await _departmentService.FindAllAsync();
                var viewModel = new AttorneyFormViewModel { Attorney = attorney, Departments = departments };
                return View(viewModel);
            }

            if (id != attorney.Id) 
            { 
                return RedirectToAction(nameof(Error), new { message = "Id not mismatch" });
            }
            try
            {                
                await _attorneyService.UpdateAsync(attorney);
                return RedirectToAction(nameof(Index));
            }catch (NotFoundException ex)
            {
                return RedirectToAction(nameof(Error), new { message = ex.Message });
            }
        }

        public IActionResult Error (string message)
        {
            var viewModel = new ErrorViewModel
            {
               Message = message,
               RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(viewModel);
        }

    }
}
