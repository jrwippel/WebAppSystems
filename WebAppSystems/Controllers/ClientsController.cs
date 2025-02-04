using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Models;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;

namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    [PaginaRestritaSomenteAdmin]
    public class ClientsController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ClientsController(WebAppSystemsContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            try
            {
                return _context.Client != null ?
                            View(await _context.Client.ToListAsync()) :
                            Problem("Entity set 'WebAppSystemsContext.Client'  is null.");
            }
            catch (SessionExpiredException)
            {
                // Redirecione para a página de login se a sessão expirou
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Client == null)
            {
                return NotFound();
            }

            var client = await _context.Client
                .FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        
        // POST: Clients/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Client client, IFormFile image)
        public async Task<IActionResult> Create(int id, [Bind("Id,Name,Document,Email,Telephone,ImageData,ImageMimeType,Solicitante,ClienteInterno, ClienteInativo")] Client client, IFormFile imageData)
        {
            //if (ModelState.IsValid)
            //{

            if (client.ImageData == null && imageData == null)
            {
                // Caminho da imagem padrão no sistema de arquivos
                string defaultImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "default-image.jpg");

                // Lê a imagem padrão como bytes
                byte[] defaultImageData = System.IO.File.ReadAllBytes(defaultImagePath);

                // Atribui a imagem padrão ao cliente
                client.ImageData = defaultImageData;
                client.ImageMimeType = "image/jpeg"; // Ajuste conforme o tipo de imagem padrão que você tem
            }

            if (imageData != null && imageData.Length > 0)
                {
                    // Verificar se é um tipo de arquivo de imagem válido
                    if (!imageData.ContentType.StartsWith("image"))
                    {
                        ModelState.AddModelError("Image", "O arquivo enviado não é uma imagem válida.");
                        return View(client);
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await imageData.CopyToAsync(memoryStream);
                        client.ImageData = memoryStream.ToArray();
                        client.ImageMimeType = imageData.ContentType;
                    }
                }               

                _context.Add(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            //}

            //return View(client);
        }   

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Client == null)
            {
                return NotFound();
            }

            var client = await _context.Client.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            ViewData["ImageData"] = client.ImageData != null ? Convert.ToBase64String(client.ImageData) : null;

            return View(client);
        }

        // POST: Clients/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Document,Email,Telephone")] Client client, IFormFile imageData)
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Document,Email,Telephone,ImageData,ImageMimeType,Solicitante,ClienteInterno, ClienteInativo")] Client client, IFormFile imageData)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            var existingClient = await _context.Client.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existingClient == null)
            {
                return NotFound();
            }

            try
            {
                if (imageData != null && imageData.Length > 0)
                {
                    // Verificar se é um tipo de arquivo de imagem válido
                    if (!imageData.ContentType.StartsWith("image"))
                    {
                        ModelState.AddModelError("Image", "O arquivo enviado não é uma imagem válida.");
                        return View(client);
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await imageData.CopyToAsync(memoryStream);
                        client.ImageData = memoryStream.ToArray();
                        client.ImageMimeType = imageData.ContentType;
                    }
                }
                else
                {
                    // Preserva a imagem existente no banco de dados
                    client.ImageData = existingClient.ImageData;
                    client.ImageMimeType = existingClient.ImageMimeType;
                }

                _context.Update(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(client.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }


        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Client == null)
            {
                return NotFound();
            }

            var client = await _context.Client
                  .Include(c => c.ProcessRecords) // Inclui os registros de processo relacionados
                  .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            // Verifica se o cliente tem registros relacionados
            if (client.ProcessRecords.Any())
            {
                ModelState.AddModelError(string.Empty, "Não é possível excluir o cliente porque existem registros relacionados.");
                return View(client); // Retorna a view com a mensagem de erro
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Client == null)
            {
                return Problem("Entity set 'WebAppSystemsContext.Client' is null.");
            }

            var client = await _context.Client
                .Include(c => c.ProcessRecords)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client != null)
            {
                if (client.ProcessRecords.Any())
                {
                    ModelState.AddModelError(string.Empty, "Não é possível excluir o cliente porque existem registros relacionados.");
                    return View(client); // Retorna a view com a mensagem de erro
                }

                _context.Client.Remove(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Redireciona após a exclusão
            }

            return NotFound(); // Retorna NotFound se o cliente não for encontrado
        }

        private bool ClientExists(int id)
        {
          return (_context.Client?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        // POST: Clients/CreateCliente
        [HttpPost]
        public async Task<IActionResult> CreateCliente([Bind("Name,Document,Email,Telephone,ImageData,ImageMimeType,Solicitante,ClienteInterno")] Client client, IFormFile imageData)
        {

            //if (ModelState.IsValid)
            //{
                if (client.ImageData == null && imageData == null)
                {
                    // Caminho da imagem padrão no sistema de arquivos
                    string defaultImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "default-image.jpg");

                    // Lê a imagem padrão como bytes
                    byte[] defaultImageData = System.IO.File.ReadAllBytes(defaultImagePath);

                    // Atribui a imagem padrão ao cliente
                    client.ImageData = defaultImageData;
                    client.ImageMimeType = "image/jpeg"; // Ajuste conforme o tipo de imagem padrão que você tem
                }

                if (imageData != null && imageData.Length > 0)
                {
                    // Verificar se é um tipo de arquivo de imagem válido
                    if (!imageData.ContentType.StartsWith("image"))
                    {
                        return Json(new { success = false, message = "O arquivo enviado não é uma imagem válida." });
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await imageData.CopyToAsync(memoryStream);
                        client.ImageData = memoryStream.ToArray();
                        client.ImageMimeType = imageData.ContentType;
                    }
                }

                _context.Add(client);
                await _context.SaveChangesAsync();

                return Json(new { success = true, clienteId = client.Id, clienteNome = client.Name });
            //}

            //return Json(new { success = false, message = "Erro ao cadastrar cliente." });
        }



    }
}
