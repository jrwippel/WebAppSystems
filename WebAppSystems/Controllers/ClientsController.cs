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
                        c.Document.Contains(search));
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
                        Document = c.Document,
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

            ViewBag.Solicitantes = await _context.ClienteSolicitante
                .Where(cs => cs.ClientId == client.Id)
                .OrderByDescending(cs => cs.Padrao)
                .ThenBy(cs => cs.Nome)
                .Select(cs => cs.Padrao ? cs.Nome + " (padrão)" : cs.Nome)
                .ToListAsync();

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
        public async Task<IActionResult> Create(int id, [Bind("Id,Name,Document,ImageData,ImageMimeType,ClienteInterno, ClienteInativo")] Client client, IFormFile imageData, List<string>? solicitantes = null, string? solicitantePadrao = null, bool forcar = false)
        {
            // Normaliza a lista de solicitantes informada (sem vazios e sem duplicados ignorando caixa)
            var solicitantesLimpos = (solicitantes ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .GroupBy(s => s.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            // Padrão só vale se for um dos nomes da lista
            var padraoInformado = solicitantesLimpos
                .FirstOrDefault(s => string.Equals(s, solicitantePadrao?.Trim(), StringComparison.OrdinalIgnoreCase));

            // Devolve a lista para a tela caso o formulário seja reexibido (aviso de duplicidade, erro etc.)
            ViewBag.SolicitantesIniciais = solicitantesLimpos;
            ViewBag.SolicitantePadraoInicial = padraoInformado ?? "";

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

                // Cadastra os solicitantes informados na criação do cliente (cliente interno não usa a lista)
                if (!client.ClienteInterno && solicitantesLimpos.Any())
                {
                    foreach (var nome in solicitantesLimpos)
                    {
                        _context.ClienteSolicitante.Add(new ClienteSolicitante(client.Id, nome)
                        {
                            Padrao = padraoInformado != null && nome == padraoInformado
                        });
                    }

                    // Campo legado acompanha o padrão escolhido
                    if (padraoInformado != null)
                        client.Solicitante = padraoInformado;

                    await _context.SaveChangesAsync();
                }

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Document,ImageData,ImageMimeType,ClienteInterno, ClienteInativo")] Client client, IFormFile imageData)
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

                // O campo Solicitante padrão não é mais editado pela tela; preserva o valor existente
                client.Solicitante = existingClient.Solicitante;

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

            ViewBag.Solicitantes = await _context.ClienteSolicitante
                .Where(cs => cs.ClientId == client.Id)
                .OrderByDescending(cs => cs.Padrao)
                .ThenBy(cs => cs.Nome)
                .Select(cs => cs.Padrao ? cs.Nome + " (padrão)" : cs.Nome)
                .ToListAsync();

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

        // ── Solicitantes do cliente ───────────────────────────────────────

        [HttpGet]
        public async Task<JsonResult> GetSolicitantes(int clientId)
        {
            var solicitantes = await _context.ClienteSolicitante
                .Where(cs => cs.ClientId == clientId)
                .OrderByDescending(cs => cs.Padrao)
                .ThenBy(cs => cs.Nome)
                .Select(cs => new { cs.Id, cs.Nome, cs.Padrao })
                .ToListAsync();

            return Json(new { success = true, solicitantes });
        }

        // Marca (padrao = true) ou desmarca o solicitante padrão do cliente. Marcar um desmarca os demais.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DefinirSolicitantePadrao(int id, bool padrao = true)
        {
            var solicitante = await _context.ClienteSolicitante.FindAsync(id);
            if (solicitante == null)
                return Json(new { success = false, message = "Solicitante não encontrado." });

            var doCliente = await _context.ClienteSolicitante
                .Where(cs => cs.ClientId == solicitante.ClientId)
                .ToListAsync();

            foreach (var cs in doCliente)
                cs.Padrao = padrao && cs.Id == solicitante.Id;

            await SincronizarSolicitanteLegadoAsync(solicitante.ClientId, padrao ? solicitante.Nome : null);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Mantém o campo antigo Client.Solicitante igual ao solicitante padrão (ainda usado na busca de lançamentos)
        private async Task SincronizarSolicitanteLegadoAsync(int clientId, string? nomePadrao)
        {
            var client = await _context.Client!.FindAsync(clientId);
            if (client != null)
                client.Solicitante = nomePadrao;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddSolicitante(int clientId, string nome)
        {
            if (clientId <= 0 || string.IsNullOrWhiteSpace(nome))
                return Json(new { success = false, message = "Informe um nome válido." });

            nome = nome.Trim();

            var jaExiste = await _context.ClienteSolicitante
                .AnyAsync(cs => cs.ClientId == clientId && cs.Nome.ToLower() == nome.ToLower());

            if (jaExiste)
                return Json(new { success = false, message = "Este solicitante já está cadastrado para o cliente." });

            // O primeiro solicitante do cliente já entra como padrão
            var primeiro = !await _context.ClienteSolicitante.AnyAsync(cs => cs.ClientId == clientId);

            var novo = new ClienteSolicitante(clientId, nome) { Padrao = primeiro };
            _context.ClienteSolicitante.Add(novo);

            if (primeiro)
                await SincronizarSolicitanteLegadoAsync(clientId, nome);

            await _context.SaveChangesAsync();

            return Json(new { success = true, id = novo.Id, nome = novo.Nome });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoveSolicitante(int id)
        {
            var solicitante = await _context.ClienteSolicitante.FindAsync(id);
            if (solicitante == null)
                return Json(new { success = false, message = "Solicitante não encontrado." });

            // Removendo o padrão, o cliente fica sem solicitante padrão
            if (solicitante.Padrao)
                await SincronizarSolicitanteLegadoAsync(solicitante.ClientId, null);

            _context.ClienteSolicitante.Remove(solicitante);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Clients/CreateCliente
        [HttpPost]
        public async Task<IActionResult> CreateCliente([Bind("Name,Document,ImageData,ImageMimeType,ClienteInterno")] Client client, IFormFile imageData, bool forcar = false)
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
