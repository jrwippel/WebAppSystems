using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Models;

namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    [PaginaRestritaSomenteAdmin]
    public class PercentualAreasController : Controller
    {
        private readonly WebAppSystemsContext _context;

        public PercentualAreasController(WebAppSystemsContext context)
        {
            _context = context;
        }

        // GET: PercentualAreas
        public async Task<IActionResult> Index()
        {
            var data = await _context.PercentualArea
                .Include(p => p.Client).Include(p => p.Department)
                .ToListAsync();
            var clients = await _context.Client.OrderBy(c => c.Name).ToListAsync();
            var departments = await _context.Department.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Clients = clients;
            ViewBag.Departments = departments;
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> SaveGrid([FromBody] List<PercentualAreaGridItem> items)
        {
            if (items == null || !items.Any()) return Ok(new { success = true });
            var clientId = items.First().ClientId;
            var total = items.Where(i => i.Percentual > 0).Sum(i => i.Percentual);
            if (total > 100) return BadRequest($"A soma dos percentuais ({total}%) excede 100%.");
            var existing = await _context.PercentualArea.Where(p => p.ClientId == clientId).ToListAsync();
            foreach (var item in items)
            {
                var rec = existing.FirstOrDefault(e => e.DepartmentId == item.DepartmentId);
                if (item.Percentual > 0)
                {
                    if (rec == null) _context.PercentualArea.Add(new PercentualArea { ClientId = item.ClientId, DepartmentId = item.DepartmentId, Percentual = item.Percentual });
                    else rec.Percentual = item.Percentual;
                }
                else if (rec != null) _context.PercentualArea.Remove(rec);
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public class PercentualAreaGridItem
        {
            public int ClientId { get; set; }
            public int DepartmentId { get; set; }
            public decimal Percentual { get; set; }
        }

        // GET: PercentualAreas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.PercentualArea == null)
            {
                return NotFound();
            }

            var percentualArea = await _context.PercentualArea
                .Include(p => p.Client)
                .Include(p => p.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (percentualArea == null)
            {
                return NotFound();
            }

            return View(percentualArea);
        }

        // GET: PercentualAreas/Create
        public IActionResult Create()
        {
            ViewData["ClientName"] = new SelectList(_context.Client, "Id", "Name");
            ViewData["DepartmentName"] = new SelectList(_context.Department, "Id", "Name");
            return View();
        }

        // POST: PercentualAreas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PercentualArea percentualArea)
        {
            ModelState.Remove("Client");
            ModelState.Remove("Department");

            // Verificar se já existe um registro com o mesmo cliente e departamento.
            var existingRecord = await _context.PercentualArea
                .Where(p => p.ClientId == percentualArea.ClientId && p.DepartmentId == percentualArea.DepartmentId)
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um registro com este cliente e departamento.");
                ViewData["ClientName"] = new SelectList(_context.Client, "Id", "Name");
                ViewData["DepartmentName"] = new SelectList(_context.Department, "Id", "Name");
                return View(percentualArea);
            }

            // Verificar se a soma dos percentuais existentes e o novo percentual não excedem 100% para o cliente.
            var totalPercentual = await _context.PercentualArea
                .Where(p => p.ClientId == percentualArea.ClientId)
                .SumAsync(p => p.Percentual);

            if (totalPercentual + percentualArea.Percentual > 100)
            {
                ModelState.AddModelError(string.Empty, "A soma dos percentuais não pode exceder 100% para este cliente e departamento.");
                ViewData["ClientName"] = new SelectList(_context.Client, "Id", "Name");
                ViewData["DepartmentName"] = new SelectList(_context.Department, "Id", "Name");
                return View(percentualArea);
            }

            if (ModelState.IsValid)
            {
                _context.Add(percentualArea);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Document", percentualArea.ClientId);
            ViewData["DepartmentId"] = new SelectList(_context.Department, "Id", "Id", percentualArea.DepartmentId);
            return View(percentualArea);
        }


        // GET: PercentualAreas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.PercentualArea == null)
            {
                return NotFound();
            }

            var percentualArea = await _context.PercentualArea.FindAsync(id);
            if (percentualArea == null)
            {
                return NotFound();
            }
            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name", percentualArea.ClientId);
            ViewData["DepartmentId"] = new SelectList(_context.Department, "Id", "Name", percentualArea.DepartmentId);
            return View(percentualArea);
        }

        // POST: PercentualAreas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DepartmentId,ClientId,Percentual")] PercentualArea percentualArea)
        {

            ModelState.Remove("Client");
            ModelState.Remove("Department");

            if (id != percentualArea.Id)
            {
                return NotFound();
            }

            // Verificar se já existe um registro com o mesmo cliente e departamento.
            var existingRecord = await _context.PercentualArea
                .Where(p => p.ClientId == percentualArea.ClientId && p.DepartmentId == percentualArea.DepartmentId)
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um registro com este cliente e departamento.");
                ViewData["ClientName"] = new SelectList(_context.Client, "Id", "Name");
                ViewData["DepartmentName"] = new SelectList(_context.Department, "Id", "Name");
                return View(percentualArea);
            }

            // Verificar se a soma dos percentuais existentes e o novo percentual não excedem 100% para o cliente.
            var totalPercentual = await _context.PercentualArea
                .Where(p => p.ClientId == percentualArea.ClientId)
                .SumAsync(p => p.Percentual);

            if (totalPercentual + percentualArea.Percentual > 100)
            {
                ModelState.AddModelError(string.Empty, "A soma dos percentuais não pode exceder 100% para este cliente e departamento.");
                ViewData["ClientName"] = new SelectList(_context.Client, "Id", "Name");
                ViewData["DepartmentName"] = new SelectList(_context.Department, "Id", "Name");
                return View(percentualArea);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(percentualArea);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PercentualAreaExists(percentualArea.Id))
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
            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Document", percentualArea.ClientId);
            ViewData["DepartmentId"] = new SelectList(_context.Department, "Id", "Id", percentualArea.DepartmentId);
            return View(percentualArea);
        }

        // GET: PercentualAreas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.PercentualArea == null)
            {
                return NotFound();
            }

            var percentualArea = await _context.PercentualArea
                .Include(p => p.Client)
                .Include(p => p.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (percentualArea == null)
            {
                return NotFound();
            }

            return View(percentualArea);
        }

        // POST: PercentualAreas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.PercentualArea == null)
            {
                return Problem("Entity set 'WebAppSystemsContext.PercentualArea'  is null.");
            }
            var percentualArea = await _context.PercentualArea.FindAsync(id);
            if (percentualArea != null)
            {
                _context.PercentualArea.Remove(percentualArea);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PercentualAreaExists(int id)
        {
            return (_context.PercentualArea?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByClient(int clientId)
        {
            var departments = await _context.PercentualArea
                .Where(p => p.ClientId == clientId && p.Percentual > 0)
                .Include(p => p.Department)
                .Select(p => new { id = p.DepartmentId, name = p.Department.Name })
                .OrderBy(p => p.name)
                .ToListAsync();
            return Json(departments);
        }
    }
}
