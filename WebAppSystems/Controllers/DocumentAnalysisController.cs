using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using WebAppSystems.Data;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebAppSystems.Controllers
{
    public class DocumentAnalysisController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ISessao _sessao;
        private readonly DocumentTextExtractorService _textExtractor;
        private readonly AIDocumentAnalysisService _aiService;
        private readonly AttorneyRecommendationService _recommendationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocumentAnalysisController> _logger;

        public DocumentAnalysisController(
            WebAppSystemsContext context,
            ISessao sessao,
            DocumentTextExtractorService textExtractor,
            AIDocumentAnalysisService aiService,
            AttorneyRecommendationService recommendationService,
            IServiceProvider serviceProvider,
            ILogger<DocumentAnalysisController> logger)
        {
            _context = context;
            _sessao = sessao;
            _textExtractor = textExtractor;
            _aiService = aiService;
            _recommendationService = recommendationService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("=== INÍCIO Index DocumentAnalysis ===");
                Attorney usuario = _sessao.BuscarSessaoDoUsuario();
                _logger.LogInformation($"Usuário logado: {usuario.Name} (ID: {usuario.Id})");
                
                ViewBag.LoggedUserId = usuario.Id;
                ViewBag.CurrentUserPerfil = usuario.Perfil;

                _logger.LogInformation("Buscando clientes...");
                // Buscar clientes para dropdown
                var clients = await _context.Client
                    .Where(c => !c.ClienteInativo)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation($"Clientes encontrados: {clients.Count}");

                ViewBag.Clients = clients.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

                _logger.LogInformation("=== FIM Index DocumentAnalysis ===");
                return View();
            }
            catch (SessionExpiredException ex)
            {
                _logger.LogWarning($"Sessão expirada: {ex.Message}");
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERRO no Index DocumentAnalysis ===");
                throw;
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, int? clientId)
        {
            try
            {
                _logger.LogInformation("=== INÍCIO DO UPLOAD ===");
                Attorney usuario = _sessao.BuscarSessaoDoUsuario();
                _logger.LogInformation($"Usuário: {usuario.Name} (ID: {usuario.Id})");

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("ERRO: Nenhum arquivo enviado");
                    return Json(new { success = false, message = "Nenhum arquivo foi enviado." });
                }

                _logger.LogInformation($"Arquivo recebido: {file.FileName} ({file.Length} bytes)");

                var fileExtension = Path.GetExtension(file.FileName);
                _logger.LogInformation($"Extensão: {fileExtension}");
                
                if (!_textExtractor.IsValidFileType(fileExtension))
                {
                    _logger.LogWarning("ERRO: Tipo de arquivo inválido");
                    return Json(new { success = false, message = "Tipo de arquivo não suportado. Use PDF, DOCX ou TXT." });
                }

                if (file.Length > _textExtractor.GetMaxFileSizeInBytes())
                {
                    _logger.LogWarning("ERRO: Arquivo muito grande");
                    return Json(new { success = false, message = "Arquivo muito grande. Tamanho máximo: 10 MB." });
                }

                // Salvar arquivo
                _logger.LogInformation("Salvando arquivo...");
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "documents");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                _logger.LogInformation($"Caminho do arquivo: {filePath}");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("Arquivo salvo com sucesso");

                // Criar registro no banco
                _logger.LogInformation("Criando registro no banco...");
                var documentAnalysis = new DocumentAnalysis
                {
                    FileName = file.FileName,
                    FileType = fileExtension.TrimStart('.').ToUpper(),
                    FileSize = file.Length,
                    FilePath = filePath,
                    UploadDate = DateTime.Now,
                    UploadedByAttorneyId = usuario.Id,
                    ClientId = clientId,
                    AnalysisStatus = "Pending"
                };

                _context.DocumentAnalysis.Add(documentAnalysis);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Registro criado com ID: {documentAnalysis.Id}");

                // TESTE: Processar de forma SÍNCRONA para debug
                _logger.LogInformation("=== PROCESSANDO DE FORMA SÍNCRONA (TESTE) ===");
                try
                {
                    await ProcessDocumentAnalysisInBackground(documentAnalysis.Id);
                    _logger.LogInformation("=== PROCESSAMENTO SÍNCRONO CONCLUÍDO ===");
                }
                catch (Exception syncEx)
                {
                    _logger.LogError(syncEx, "=== ERRO NO PROCESSAMENTO SÍNCRONO ===");
                }

                _logger.LogInformation("=== UPLOAD CONCLUÍDO ===");
                return Json(new { success = true, documentId = documentAnalysis.Id, message = "Documento enviado! Iniciando análise..." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERRO NO UPLOAD ===");
                return Json(new { success = false, message = $"Erro ao processar arquivo: {ex.Message}" });
            }
        }

        private async Task ProcessDocumentAnalysisInBackground(int documentId)
        {
            _logger.LogInformation($"[ProcessBackground] INÍCIO - Documento {documentId}");
            
            // Criar um novo contexto para a thread em background usando IServiceProvider
            using (var scope = _serviceProvider.CreateScope())
            {
                _logger.LogInformation($"[ProcessBackground] Scope criado");
                
                var context = scope.ServiceProvider.GetRequiredService<WebAppSystemsContext>();
                var textExtractor = scope.ServiceProvider.GetRequiredService<DocumentTextExtractorService>();
                var aiService = scope.ServiceProvider.GetRequiredService<AIDocumentAnalysisService>();
                var recommendationService = scope.ServiceProvider.GetRequiredService<AttorneyRecommendationService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DocumentAnalysisController>>();

                logger.LogInformation($"[ProcessBackground] Services obtidos");

                var document = await context.DocumentAnalysis.FindAsync(documentId);
                if (document == null)
                {
                    logger.LogError($"[ProcessBackground] Documento {documentId} não encontrado");
                    return;
                }

                logger.LogInformation($"[ProcessBackground] Documento encontrado: {document.FileName}");

                try
                {
                    logger.LogInformation($"[ProcessBackground] Extraindo texto...");
                    var text = await textExtractor.ExtractTextAsync(document.FilePath, Path.GetExtension(document.FileName));

                    if (text.Length > 50000)
                    {
                        text = text.Substring(0, 50000) + "... [texto truncado]";
                    }

                    logger.LogInformation($"[ProcessBackground] Texto extraído: {text.Length} caracteres");
                    logger.LogInformation($"[ProcessBackground] Chamando IA...");
                    
                    var analysis = await aiService.AnalyzeDocumentAsync(text, document.FileName);
                    logger.LogInformation($"[ProcessBackground] Análise recebida");

                    var recommendations = await recommendationService.GetRecommendedAttorneysAsync(
                        analysis.LegalArea,
                        analysis.ActionType,
                        analysis.Complexity,
                        analysis.EstimatedHours
                    );

                    document.Summary = analysis.Summary;
                    document.LegalArea = analysis.LegalArea;
                    document.ActionType = analysis.ActionType;
                    document.Complexity = analysis.Complexity;
                    document.EstimatedHours = analysis.EstimatedHours;
                    document.MainTopics = JsonSerializer.Serialize(analysis.MainTopics);
                    document.LegalBasis = JsonSerializer.Serialize(analysis.LegalBasis);
                    document.Parties = JsonSerializer.Serialize(analysis.Parties);
                    document.CauseValue = analysis.CauseValue;
                    document.Deadlines = JsonSerializer.Serialize(analysis.Deadlines);
                    document.RecommendedAttorneys = JsonSerializer.Serialize(recommendations);
                    document.AnalysisStatus = "Completed";
                    document.AnalysisDate = DateTime.Now;

                    await context.SaveChangesAsync();
                    logger.LogInformation($"[ProcessBackground] Documento {documentId} atualizado com sucesso");
                }
                catch (Exception analysisEx)
                {
                    logger.LogError(analysisEx, $"[ProcessBackground] ERRO na análise do documento {documentId}");
                    document.AnalysisStatus = "Error";
                    document.ErrorMessage = analysisEx.Message;
                    await context.SaveChangesAsync();
                }
            }
            
            _logger.LogInformation($"[ProcessBackground] FIM - Documento {documentId}");
        }

        private async Task ProcessDocumentAnalysisAsync(int documentId)
        {
            try
            {
                var document = await _context.DocumentAnalysis.FindAsync(documentId);
                if (document == null) return;

                // Extrair texto
                var text = await _textExtractor.ExtractTextAsync(document.FilePath, Path.GetExtension(document.FileName));

                // Limitar tamanho do texto (para não exceder limites da API)
                if (text.Length > 50000)
                {
                    text = text.Substring(0, 50000) + "... [texto truncado]";
                }

                // Analisar com IA
                var analysis = await _aiService.AnalyzeDocumentAsync(text, document.FileName);

                // Obter recomendações de advogados
                var recommendations = await _recommendationService.GetRecommendedAttorneysAsync(
                    analysis.LegalArea,
                    analysis.ActionType,
                    analysis.Complexity,
                    analysis.EstimatedHours
                );

                // Atualizar documento
                document.Summary = analysis.Summary;
                document.LegalArea = analysis.LegalArea;
                document.ActionType = analysis.ActionType;
                document.Complexity = analysis.Complexity;
                document.EstimatedHours = analysis.EstimatedHours;
                document.MainTopics = JsonSerializer.Serialize(analysis.MainTopics);
                document.LegalBasis = JsonSerializer.Serialize(analysis.LegalBasis);
                document.Parties = JsonSerializer.Serialize(analysis.Parties);
                document.CauseValue = analysis.CauseValue;
                document.Deadlines = JsonSerializer.Serialize(analysis.Deadlines);
                document.RecommendedAttorneys = JsonSerializer.Serialize(recommendations);
                document.AnalysisStatus = "Completed";
                document.AnalysisDate = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var document = await _context.DocumentAnalysis.FindAsync(documentId);
                if (document != null)
                {
                    document.AnalysisStatus = "Error";
                    document.ErrorMessage = ex.Message;
                    await _context.SaveChangesAsync();
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalysisStatus(int documentId)
        {
            try
            {
                var document = await _context.DocumentAnalysis
                    .Include(d => d.UploadedBy)
                    .Include(d => d.Client)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return Json(new { success = false, message = "Documento não encontrado." });
                }

                var viewModel = new DocumentAnalysisViewModel
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    FileType = document.FileType,
                    FileSize = document.FileSize,
                    UploadDate = document.UploadDate,
                    UploadedByName = document.UploadedBy?.Name,
                    AnalysisStatus = document.AnalysisStatus,
                    AnalysisDate = document.AnalysisDate,
                    ErrorMessage = document.ErrorMessage,
                    ClientId = document.ClientId,
                    ClientName = document.Client?.Name
                };

                if (document.AnalysisStatus == "Completed")
                {
                    viewModel.Summary = document.Summary;
                    viewModel.LegalArea = document.LegalArea;
                    viewModel.ActionType = document.ActionType;
                    viewModel.Complexity = document.Complexity;
                    viewModel.EstimatedHours = document.EstimatedHours;
                    viewModel.CauseValue = document.CauseValue;

                    if (!string.IsNullOrEmpty(document.MainTopics))
                        viewModel.MainTopics = JsonSerializer.Deserialize<List<string>>(document.MainTopics);

                    if (!string.IsNullOrEmpty(document.LegalBasis))
                        viewModel.LegalBasis = JsonSerializer.Deserialize<List<string>>(document.LegalBasis);

                    if (!string.IsNullOrEmpty(document.Parties))
                        viewModel.Parties = JsonSerializer.Deserialize<PartyInfo>(document.Parties);

                    if (!string.IsNullOrEmpty(document.Deadlines))
                        viewModel.Deadlines = JsonSerializer.Deserialize<List<DeadlineInfo>>(document.Deadlines);

                    if (!string.IsNullOrEmpty(document.RecommendedAttorneys))
                        viewModel.RecommendedAttorneys = JsonSerializer.Deserialize<List<AttorneyRecommendation>>(document.RecommendedAttorneys);
                }

                return Json(new { success = true, data = viewModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignToAttorney(int documentId, int attorneyId)
        {
            try
            {
                var document = await _context.DocumentAnalysis.FindAsync(documentId);
                if (document == null)
                {
                    return Json(new { success = false, message = "Documento não encontrado." });
                }

                document.AssignedToAttorneyId = attorneyId;
                document.AssignedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Documento atribuído com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> History()
        {
            try
            {
                Attorney usuario = _sessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                ViewBag.CurrentUserPerfil = usuario.Perfil;

                var documents = await _context.DocumentAnalysis
                    .Include(d => d.UploadedBy)
                    .Include(d => d.AssignedTo)
                    .AsNoTracking()
                    .OrderByDescending(d => d.UploadDate)
                    .ToListAsync();

                return View(documents);
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation($"=== Details chamado para documento {id} ===");
                Attorney usuario = _sessao.BuscarSessaoDoUsuario();
                ViewBag.LoggedUserId = usuario.Id;
                ViewBag.CurrentUserPerfil = usuario.Perfil;

                _logger.LogInformation($"Buscando documento {id}...");
                var document = await _context.DocumentAnalysis
                    .Include(d => d.UploadedBy)
                    .Include(d => d.AssignedTo)
                    .Include(d => d.Client)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (document == null)
                {
                    _logger.LogWarning($"Documento {id} não encontrado");
                    return NotFound();
                }

                _logger.LogInformation($"Documento encontrado: {document.FileName}");
                var viewModel = MapToViewModel(document);

                // Buscar advogados para dropdown de atribuição
                var attorneys = await _context.Attorney
                    .Where(a => a.Perfil != Models.Enums.ProfileEnum.Admin)
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                ViewBag.Attorneys = attorneys.Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Name
                }).ToList();

                _logger.LogInformation($"=== Retornando view Details ===");
                return View(viewModel);
            }
            catch (SessionExpiredException)
            {
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar detalhes do documento {id}");
                throw;
            }
        }

        private DocumentAnalysisViewModel MapToViewModel(DocumentAnalysis document)
        {
            var viewModel = new DocumentAnalysisViewModel
            {
                Id = document.Id,
                FileName = document.FileName,
                FileType = document.FileType,
                FileSize = document.FileSize,
                UploadDate = document.UploadDate,
                UploadedByName = document.UploadedBy?.Name,
                Summary = document.Summary,
                LegalArea = document.LegalArea,
                ActionType = document.ActionType,
                Complexity = document.Complexity,
                EstimatedHours = document.EstimatedHours,
                CauseValue = document.CauseValue,
                AnalysisStatus = document.AnalysisStatus,
                AnalysisDate = document.AnalysisDate,
                ErrorMessage = document.ErrorMessage,
                AssignedToAttorneyId = document.AssignedToAttorneyId,
                AssignedToName = document.AssignedTo?.Name,
                AssignedDate = document.AssignedDate,
                ClientId = document.ClientId,
                ClientName = document.Client?.Name
            };

            if (!string.IsNullOrEmpty(document.MainTopics))
                viewModel.MainTopics = JsonSerializer.Deserialize<List<string>>(document.MainTopics);

            if (!string.IsNullOrEmpty(document.LegalBasis))
                viewModel.LegalBasis = JsonSerializer.Deserialize<List<string>>(document.LegalBasis);

            if (!string.IsNullOrEmpty(document.Parties))
                viewModel.Parties = JsonSerializer.Deserialize<PartyInfo>(document.Parties);

            if (!string.IsNullOrEmpty(document.Deadlines))
                viewModel.Deadlines = JsonSerializer.Deserialize<List<DeadlineInfo>>(document.Deadlines);

            if (!string.IsNullOrEmpty(document.RecommendedAttorneys))
                viewModel.RecommendedAttorneys = JsonSerializer.Deserialize<List<AttorneyRecommendation>>(document.RecommendedAttorneys);

            return viewModel;
        }
    }
}
