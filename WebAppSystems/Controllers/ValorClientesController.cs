using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models;

namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    [PaginaRestritaSomenteAdmin]
    public class ValorClientesController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ISessao _isessao;

        public ValorClientesController(WebAppSystemsContext context, ISessao isessao)
        {
            _context = context;
            _isessao = isessao;
        }

        // GET: ValorClientes
        public async Task<IActionResult> Index()
        {
            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.LoggedUserId = usuario.Id;
            ViewBag.CurrentUserPerfil = usuario.Perfil;
            ViewBag.Clients   = await _context.Client.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Attorneys = await _context.Attorney.OrderBy(a => a.Name).ToListAsync();

            var valores = await _context.ValorCliente
                .Include(v => v.Attorney)
                .Include(v => v.Client)
                .ToListAsync();
            return View(valores);
        }

        // POST: ValorClientes/SaveGrid
        [HttpPost]
        public async Task<IActionResult> SaveGrid([FromBody] List<SaveGridItem> items)
        {
            if (items == null || !items.Any())
                return Ok(new { records = new List<object>() });

            var result = new List<object>();

            foreach (var item in items)
            {
                if (item.Id > 0)
                {
                    // Atualiza existente
                    var existing = await _context.ValorCliente.FindAsync(item.Id);
                    if (existing != null)
                    {
                        if (item.Valor <= 0)
                            _context.ValorCliente.Remove(existing);
                        else
                        {
                            existing.Valor = item.Valor;
                            result.Add(new { id = existing.Id, clientId = existing.ClientId, attorneyId = existing.AttorneyId });
                        }
                    }
                }
                else if (item.Valor > 0)
                {
                    // Cria novo (verifica duplicata)
                    var dup = await _context.ValorCliente
                        .FirstOrDefaultAsync(v => v.ClientId == item.ClientId && v.AttorneyId == item.AttorneyId);
                    if (dup != null)
                    {
                        dup.Valor = item.Valor;
                        result.Add(new { id = dup.Id, clientId = dup.ClientId, attorneyId = dup.AttorneyId });
                    }
                    else
                    {
                        var novo = new ValorCliente { ClientId = item.ClientId, AttorneyId = item.AttorneyId, Valor = item.Valor };
                        _context.ValorCliente.Add(novo);
                        await _context.SaveChangesAsync();
                        result.Add(new { id = novo.Id, clientId = novo.ClientId, attorneyId = novo.AttorneyId });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { records = result });
        }

        public class SaveGridItem
        {
            public int Id { get; set; }
            public int ClientId { get; set; }
            public int AttorneyId { get; set; }
            public double Valor { get; set; }
        }

        // GET: ValorClientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ValorCliente == null)
            {
                return NotFound();
            }

            var valorCliente = await _context.ValorCliente
                .Include(v => v.Attorney)
                .Include(v => v.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (valorCliente == null)
            {
                return NotFound();
            }

            return View(valorCliente);
        }

        // GET: ValorClientes/Create
        public IActionResult Create()
        {

            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name"); // Troque "Document" por "Name" (ou o nome do campo que contém o nome do cliente).
            ViewData["AttorneyId"] = new SelectList(_context.Attorney, "Id", "Name"); // Troque o segundo "Id" por "N
            return View();
        }

        // POST: ValorClientes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClientId,AttorneyId,Valor")] ValorCliente valorCliente)
        {
            // if (ModelState.IsValid)
            // {

            // Verificar se já existe um registro com o mesmo cliente e usuario.
            var existingRecord = await _context.ValorCliente
                .Where(p => p.ClientId == valorCliente.ClientId && p.AttorneyId == valorCliente.AttorneyId)
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um registro com este cliente e usuário cadastrados.");
                ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name"); // Troque "Document" por "Name" (ou o nome do campo que contém o nome do cliente).
                ViewData["AttorneyId"] = new SelectList(_context.Attorney, "Id", "Name"); // Troque o segundo "Id" por "N
                return View(valorCliente);
            }
            _context.Add(valorCliente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
          //  }
         ///   ViewData["AttorneyId"] = new SelectList(_context.Attorney, "Id", "Email", valorCliente.AttorneyId);
         //   ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Document", valorCliente.ClientId);
        //    return View(valorCliente);
        }

        // GET: ValorClientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.ValorCliente == null)
            {
                return NotFound();
            }

            var valorCliente = await _context.ValorCliente.FindAsync(id);
            if (valorCliente == null)
            {
                return NotFound();
            } 
            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name", valorCliente.ClientId);
            ViewData["AttorneyId"] = new SelectList(_context.Department, "Id", "Name", valorCliente.AttorneyId);
            return View(valorCliente);
        }

        // POST: ValorClientes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,AttorneyId,Valor")] ValorCliente valorCliente)
        {
            if (id != valorCliente.Id)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
           //{
                try
                {
                    _context.Update(valorCliente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ValorClienteExists(valorCliente.Id))
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
            ViewData["ClientId"] = new SelectList(_context.Client, "Id", "Name", valorCliente.ClientId);
            ViewData["AttorneyId"] = new SelectList(_context.Department, "Id", "Name", valorCliente.AttorneyId);
            return View(valorCliente);
        }

        // GET: ValorClientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.ValorCliente == null)
            {
                return NotFound();
            }

            var valorCliente = await _context.ValorCliente
                .Include(v => v.Attorney)
                .Include(v => v.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (valorCliente == null)
            {
                return NotFound();
            }

            return View(valorCliente);
        }

        // POST: ValorClientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.ValorCliente == null)
            {
                return Problem("Entity set 'WebAppSystemsContext.ValorCliente'  is null.");
            }
            var valorCliente = await _context.ValorCliente.FindAsync(id);
            if (valorCliente != null)
            {
                _context.ValorCliente.Remove(valorCliente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ValorClienteExists(int id)
        {
            return (_context.ValorCliente?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
