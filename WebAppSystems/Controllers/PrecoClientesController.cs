using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;

namespace WebAppSystems.Controllers
{
    public class PrecoClientesController : Controller
    {
        private readonly WebAppSystemsContext _context;

        public PrecoClientesController(WebAppSystemsContext context)
        {
            _context = context;
        }

        // GET: PrecoClientes
        public async Task<IActionResult> Index()
        {
            var webAppSystemsContext = _context.PrecoCliente.Include(p => p.Client).Include(p => p.Department);
            return View(await webAppSystemsContext.ToListAsync());
        }

        // GET: PrecoClientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.PrecoCliente == null)
            {
                return NotFound();
            }

            var precoCliente = await _context.PrecoCliente
                .Include(p => p.Client)
                .Include(p => p.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (precoCliente == null)
            {
                return NotFound();
            }

            return View(precoCliente);
        }

        // GET: PrecoClientes/Create
        // No método Create GET
        public IActionResult Create()
        {
            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name"); // Troque "Document" por "Name" (ou o nome do campo que contém o nome do cliente).
            ViewData["DepartmentId"] = new SelectList(_context.Department, "Id", "Name"); // Troque o segundo "Id" por "Name" (ou o nome do campo que contém o nome do departamento).
            return View();
        }

        // POST: PrecoClientes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClientId,DepartmentId,Valor")] PrecoCliente precoCliente)
        {
            // Verificar se já existe um registro com o mesmo cliente e departamento.
            var existingRecord = await _context.PrecoCliente
                .Where(p => p.ClientId == precoCliente.ClientId && p.DepartmentId == precoCliente.DepartmentId)
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um registro com este cliente e departamento.");
                ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name", precoCliente.ClientId);
                ViewData["DepartmentId"] = new SelectList(_context.Department, "Id", "Name", precoCliente.DepartmentId);
                return View(precoCliente);
            }
            _context.Add(precoCliente);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: PrecoClientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.PrecoCliente == null)
            {
                return NotFound();
            }

            var precoCliente = await _context.PrecoCliente.FindAsync(id);
            if (precoCliente == null)
            {
                return NotFound();
            }

            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name", precoCliente.ClientId);
            ViewData["DepartmentId"] = new SelectList(_context.Department, "Id", "Name", precoCliente.DepartmentId);
            return View(precoCliente);
        }

        // POST: PrecoClientes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,DepartmentId,Valor")] PrecoCliente precoCliente)
        {
            if (id != precoCliente.Id)
            {
                return NotFound();
            }

           // if (ModelState.IsValid)
           // {
                try
                {
                    _context.Update(precoCliente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrecoClienteExists(precoCliente.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            //}
            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name", precoCliente.ClientId);
            ViewData["DepartmentId"] = new SelectList(_context.Department, "Id", "Name", precoCliente.DepartmentId);
            return View(precoCliente);
        }

        // GET: PrecoClientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.PrecoCliente == null)
            {
                return NotFound();
            }

            var precoCliente = await _context.PrecoCliente
                .Include(p => p.Client)
                .Include(p => p.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (precoCliente == null)
            {
                return NotFound();
            }

            return View(precoCliente);
        }

        // POST: PrecoClientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.PrecoCliente == null)
            {
                return Problem("Entity set 'WebAppSystemsContext.PrecoCliente'  is null.");
            }
            var precoCliente = await _context.PrecoCliente.FindAsync(id);
            if (precoCliente != null)
            {
                _context.PrecoCliente.Remove(precoCliente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PrecoClienteExists(int id)
        {
            return (_context.PrecoCliente?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
