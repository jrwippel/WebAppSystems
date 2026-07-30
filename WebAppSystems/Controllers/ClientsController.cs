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
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;



namespace WebAppSystems.Controllers
{
    [PaginaParaAdminOuControladoria]
    public class ClientsController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISessao _isessao;

        public ClientsController(WebAppSystemsContext context, IWebHostEnvironment webHostEnvironment, ISessao isessao)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _isessao = isessao;
        }

        // GET: Clients
        public async Task<IActionResult> Index(int page = 1, string search = "")
        {
            try
            {
                Attorney usuario = _isessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                ViewBag.CurrentUserPerfil = usuario.Perfil;

                const int pageSize = 10;
                
                // Query base
                var query = _context.Client.AsQueryable();

                // Aplicar filtro de busca se houver
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => 
                        c.Name.Contains(search) || 
                        c.Email.Contains(search) || 
                        c.Document.Contains(search) ||
                        c.Telephone.Contains(search));
                }

                // Contar total de registros
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Buscar apenas a página atual (10 registros)
                var clients = await query
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new Client
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Email = c.Email,
                        Document = c.Document,
                        Telephone = c.Telephone,
                        Solicitante = c.Solicitante,
                        ClienteInterno = c.ClienteInterno,
                        ClienteInativo = c.ClienteInativo,
                    })
                    .AsNoTracking()
                    .ToListAsync();

                // Passar dados de paginação para a view
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.SearchTerm = search;

                return View(clients);
            }
            catch (SessionExpiredException)
            {
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int id, [Bind("Id,Name,Document,Email,Telephone,ImageData,ImageMimeType,Solicitante,ClienteInterno, ClienteInativo")] Client client, IFormFile imageData, bool forcar = false)
        {
            // Normalizar nome para Title Case (primeira letra de cada palavra em maiúscula)
            if (!string.IsNullOrWhiteSpace(client.Name))
                client.Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(client.Name.Trim().ToLower());
            // Verificar duplicidade pelo nome — só bloqueia se não foi confirmado pelo usuário
            if (!forcar)
            {
                var nomeNorm = NormalizarNome(client.Name);
                var todos = await _context.Client.Select(c => new { c.Id, c.Name }).ToListAsync();
                var clienteDuplicado = todos.FirstOrDefault(c => NormalizarNome(c.Name) == nomeNorm);
                if (clienteDuplicado != null)
                {
                    ViewBag.AlertaDuplicado = $"Já existe um cliente cadastrado com o nome \"{clienteDuplicado.Name}\". Deseja cadastrar mesmo assim?";
                    return View(client);
                }

                // Verificar duplicidade pelo Documento (CPF/CNPJ) — BLOQUEIA, não permite prosseguir
                if (!string.IsNullOrWhiteSpace(client.Document))
                {
                    var docLimpo = new string(client.Document.Where(char.IsDigit).ToArray());
                    if (docLimpo.Length >= 11) // CPF ou CNPJ
                    {
                        var clienteDocDuplicado = await _context.Client
                            .FirstOrDefaultAsync(c => c.Document != null && c.Document.Replace(".", "").Replace("-", "").Replace("/", "").Replace(" ", "") == docLimpo);
                        if (clienteDocDuplicado != null)
                        {
                            ViewBag.ErroDuplicado = $"Não é possível cadastrar. Já existe um cliente com o documento informado: \"{clienteDocDuplicado.Name}\" ({clienteDocDuplicado.Document}).";
                            return View(client);
                        }
                    }
                }
            }

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

        // Normaliza string removendo acentos para comparação de duplicidade
        private static string NormalizarNome(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return string.Empty;
            var normalized = nome.Trim().ToLower();
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private bool ClientExists(int id)
        {
          return (_context.Client?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        // POST: Clients/CreateCliente
        [HttpPost]
        public async Task<IActionResult> CreateCliente([Bind("Name,Document,Email,Telephone,ImageData,ImageMimeType,Solicitante,ClienteInterno")] Client client, IFormFile imageData, bool forcar = false)
        {
            // Normalizar nome para Title Case
            if (!string.IsNullOrWhiteSpace(client.Name))
                client.Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(client.Name.Trim().ToLower());
            // Verificar duplicidade — avisa mas permite continuar se forcar=true
            if (!forcar)
            {
                var nomeNorm = NormalizarNome(client.Name);
                var todos = await _context.Client.Select(c => new { c.Id, c.Name }).ToListAsync();
                var clienteDuplicado = todos.FirstOrDefault(c => NormalizarNome(c.Name) == nomeNorm);
                if (clienteDuplicado != null)
                    return Json(new { success = false, duplicado = true, message = $"Já existe um cliente cadastrado com o nome \"{clienteDuplicado.Name}\". Deseja cadastrar mesmo assim?" });

                // Verificar duplicidade pelo Documento (CPF/CNPJ) — BLOQUEIA
                if (!string.IsNullOrWhiteSpace(client.Document))
                {
                    var docLimpo = new string(client.Document.Where(char.IsDigit).ToArray());
                    if (docLimpo.Length >= 11)
                    {
                        var allDocs = await _context.Client.Where(c => c.Document != null).Select(c => new { c.Id, c.Name, c.Document }).ToListAsync();
                        var clienteDocDup = allDocs.FirstOrDefault(c => new string(c.Document.Where(char.IsDigit).ToArray()) == docLimpo);
                        if (clienteDocDup != null)
                            return Json(new { success = false, message = $"Não é possível cadastrar. Já existe um cliente com o documento informado: \"{clienteDocDup.Name}\" ({clienteDocDup.Document})." });
                    }
                }
            }

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
