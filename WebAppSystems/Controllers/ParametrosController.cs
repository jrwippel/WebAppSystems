using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;
using WebAppSystems.Helper;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    public class ParametrosController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ISessao _isessao;

        public ParametrosController(WebAppSystemsContext context, ISessao isessao)
        {
            _context = context;
            _isessao = isessao;
        }

        // GET: Parametros
        public async Task<IActionResult> Index()
        {
            try
            {
                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                ViewBag.CurrentUserPerfil = usuario.Perfil;
                
                var parametros = await _context.Parametros.ToListAsync();
                bool canCreate = !parametros.Any(); // Permitir criar somente se não houver nenhum parâmetro
                ViewBag.CanCreate = canCreate;
                return View(parametros);
            }
            catch (SessionExpiredException)
            {
                // Redirecione para a página de login se a sessão expirou
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }

        // GET: Parametros/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Parametros == null)
            {
                return NotFound();
            }

            var parametros = await _context.Parametros
                .FirstOrDefaultAsync(m => m.Id == id);
            if (parametros == null)
            {
                return NotFound();
            }

            return View(parametros);
        }

        // GET: Parametros/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Width,Height")] Parametros parametros, IFormFile logo)
        {
           // if (ModelState.IsValid)
           // {
                if (logo != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await logo.CopyToAsync(memoryStream);
                        parametros.LogoData = memoryStream.ToArray();
                        parametros.LogoMimeType = logo.ContentType;
                    }

                    // Salvar as configurações no banco de dados
                    _context.Add(parametros);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("Logo", "The Logo field is required.");
                }
           // }
            return View(parametros);
        }


        // GET: Parametros/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Parametros == null)
            {
                return NotFound();
            }

            var parametros = await _context.Parametros.FindAsync(id);
            if (parametros == null)
            {
                return NotFound();
            }
            return View(parametros);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile logo, int Width, int Height, decimal AliquotaTributos)
        {
            var existing = await _context.Parametros.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Width = Width;
            existing.Height = Height;
            existing.AliquotaTributos = AliquotaTributos;

            if (logo != null)
            {
                if (logo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Logo", "A imagem deve ter no máximo 5MB.");
                    return View(existing);
                }
                using var ms = new MemoryStream();
                await logo.CopyToAsync(ms);
                existing.LogoData = ms.ToArray();
                existing.LogoMimeType = logo.ContentType;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ParametrosExists(id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Parametros/EditLogo/5
        public async Task<IActionResult> EditLogo(int? id)
        {
            if (id == null) return NotFound();
            var p = await _context.Parametros.FindAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLogo(int id, IFormFile logo, int Width, int Height)
        {
            var existing = await _context.Parametros.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Width = Width;
            existing.Height = Height;

            if (logo != null)
            {
                if (logo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Logo", "A imagem deve ter no máximo 5MB.");
                    return View(existing);
                }
                using var ms = new MemoryStream();
                await logo.CopyToAsync(ms);
                existing.LogoData = ms.ToArray();
                existing.LogoMimeType = logo.ContentType;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Parametros/EditFinanceiro/5
        public async Task<IActionResult> EditFinanceiro(int? id)
        {
            if (id == null) return NotFound();
            var p = await _context.Parametros.FindAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinanceiro(int id, string AliquotaTributos)
        {
            var existing = await _context.Parametros.FindAsync(id);
            if (existing == null) return NotFound();

            if (decimal.TryParse(AliquotaTributos?.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal aliquota))
            {
                existing.AliquotaTributos = aliquota;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Parametros/Logo/5
        public async Task<IActionResult> Logo(int id)
        {
            var p = await _context.Parametros.FindAsync(id);
            if (p?.LogoData == null) return NotFound();
            return File(p.LogoData, p.LogoMimeType ?? "image/png");
        }


        // GET: Parametros/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Parametros == null)
            {
                return NotFound();
            }

            var parametros = await _context.Parametros
                .FirstOrDefaultAsync(m => m.Id == id);
            if (parametros == null)
            {
                return NotFound();
            }

            return View(parametros);
        }

        // POST: Parametros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Parametros == null)
            {
                return Problem("Entity set 'WebAppSystemsContext.Parametros'  is null.");
            }
            var parametros = await _context.Parametros.FindAsync(id);
            if (parametros != null)
            {
                _context.Parametros.Remove(parametros);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ParametrosExists(int id)
        {
          return (_context.Parametros?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
