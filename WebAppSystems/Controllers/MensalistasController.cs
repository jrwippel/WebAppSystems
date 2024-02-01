using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Services;

namespace WebAppSystems.Controllers
{
    public class MensalistasController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ClientService _clientService;


        public MensalistasController(WebAppSystemsContext context, ClientService clientService)
        {
            _context = context;
            _clientService = clientService;
        }

        // GET: Mensalistas
        public async Task<IActionResult> Index()
        {
            var webAppSystemsContext = _context.Mensalista.Include(m => m.Client);
            return View(await webAppSystemsContext.ToListAsync());
        }

        // GET: Mensalistas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Mensalista == null)
            {
                return NotFound();
            }

            var mensalista = await _context.Mensalista
                .Include(m => m.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mensalista == null)
            {
                return NotFound();
            }

            return View(mensalista);
        }

        // GET: Mensalistas/Create
        public async Task<IActionResult> Create(int? clientId = null)
        {
            var mensalista = new Mensalista
            {
                ValorMensalBruto = 0, 
                ComissaoParceiro = 0, 
                ComissaoSocio = 0 
            };

            var clients = await _clientService.FindAllAsync();
            var departments = _context.Department.ToList();

            // Criando a lista de SelectListItem com o nome do cliente e o ID
            ViewBag.Clients = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == clientId  // Se o cliente correspondente for passado, marque-o como selecionado
            }).ToList();

            ViewBag.Departments = departments;

            return View(mensalista);
        }



        // POST: Mensalistas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Mensalista mensalista)
        {

            // Verificar se o cliente já está registrado como um Mensalista
            var existsAsMensalista = await _context.Mensalista.AnyAsync(m => m.ClientId == mensalista.ClientId);

            if (existsAsMensalista)
            {
                ModelState.AddModelError("", "Este cliente já está cadastrado como um Mensalista.");
                await ConfigureViewData(mensalista.ClientId);
                return View(mensalista);
            }

            mensalista.ValorMensalBruto = ConvertToDecimalWithDotSeparator(Request.Form["ValorMensalBruto"]);
            mensalista.ComissaoParceiro = ConvertToDecimalWithDotSeparator(Request.Form["ComissaoParceiro"]);
            mensalista.ComissaoSocio = ConvertToDecimalWithDotSeparator(Request.Form["ComissaoSocio"]);

            // Adiciona e salva o Mensalista
            _context.Add(mensalista);
            await _context.SaveChangesAsync();  // Salve o Mensalista primeiro

            await _context.SaveChangesAsync();  // Salve todas as associações MensalistaDepartment

            return RedirectToAction(nameof(Index));


        }


        // Um método helper para fazer a conversão
        private decimal ConvertToDecimalWithDotSeparator(string valueWithCommaSeparator)
        {
            string stringValue = valueWithCommaSeparator.Replace(",", ".");
            if (Decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            throw new FormatException("Unable to convert the provided value to decimal.");
        }


        private async Task ConfigureViewData(int? clientId)
        {
            var clients = await _clientService.FindAllAsync();
            ViewBag.Clients = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == clientId
            }).ToList();

            var departments = _context.Department.ToList();
            ViewBag.Departments = departments;
        }

        // GET: Mensalistas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Mensalista == null)
            {
                return NotFound();
            }

            //var mensalista = await _context.Mensalista.Include(m => m.MensalistaDepartments).FirstOrDefaultAsync(m => m.Id == id);

            var mensalista = await _context.Mensalista.Include(m => m.Client).FirstOrDefaultAsync(m => m.Id == id);



            if (mensalista == null)
            {
                return NotFound();
            }
            ViewBag.Departments = _context.Department.ToList();
            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name", mensalista.ClientId);            
            return View(mensalista);
        }

        // POST: Mensalistas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,ValorMensalBruto,ComissaoParceiro,ComissaoSocio")] Mensalista mensalista)
        {
            if (id != mensalista.Id)
            {
                return NotFound();
            }

           // if (ModelState.IsValid)
           // {
                    _context.Update(mensalista);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
            //}
            //ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Document", mensalista.ClientId);
            //return View(mensalista);
        }

        // GET: Mensalistas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Mensalista == null)
            {
                return NotFound();
            }

            var mensalista = await _context.Mensalista
                .Include(m => m.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mensalista == null)
            {
                return NotFound();
            }

            return View(mensalista);
        }

        // POST: Mensalistas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Mensalista == null)
            {
                return Problem("Entity set 'WebAppSystemsContext.Mensalista'  is null.");
            }
            var mensalista = await _context.Mensalista.FindAsync(id);
            if (mensalista != null)
            {
                _context.Mensalista.Remove(mensalista);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MensalistaExists(int id)
        {
          return (_context.Mensalista?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
