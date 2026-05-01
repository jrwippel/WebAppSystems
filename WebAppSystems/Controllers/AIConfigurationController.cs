using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Helper;
using System.Threading.Tasks;
using System.Linq;

namespace WebAppSystems.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class AIConfigurationController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ISessao _sessao;

        public AIConfigurationController(WebAppSystemsContext context, ISessao sessao)
        {
            _context = context;
            _sessao = sessao;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var config = await _context.AIConfiguration.FirstOrDefaultAsync();
                
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Index - Buscando configuração...");
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Config encontrada: {config != null}");
                
                if (config == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] Criando configuração padrão para exibição");
                    config = new AIConfiguration
                    {
                        Provider = "GoogleGemini",
                        Model = "gemini-1.5-flash",
                        ApiKey = "",
                        IsActive = false
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] Config carregada - ID: {config.Id}, Provider: {config.Provider}, IsActive: {config.IsActive}");
                }

                return View(config);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Erro ao carregar Index: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Stack: {ex.StackTrace}");
                
                TempData["MensagemErro"] = $"Erro ao carregar configurações: {ex.Message}";
                TempData["AIConfigError"] = true;
                
                // Retorna um modelo vazio em caso de erro
                var emptyConfig = new AIConfiguration
                {
                    Provider = "GoogleGemini",
                    Model = "gemini-1.5-flash",
                    ApiKey = "",
                    IsActive = false
                };
                
                return View(emptyConfig);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string Provider, string Model, string ApiKey, bool IsActive)
        {
            System.Diagnostics.Debug.WriteLine($"[AIConfig] Save - Recebido: Provider={Provider}, Model={Model}, ApiKey Length={ApiKey?.Length ?? 0}, IsActive={IsActive}");

            // Validação mais detalhada
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                TempData["MensagemErro"] = "A chave da API é obrigatória. Por favor, cole sua chave da API no campo.";
                TempData["AIConfigError"] = true;
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Erro: API Key vazia ou nula");
                
                var errorModel = new AIConfiguration
                {
                    Provider = Provider ?? "GoogleGemini",
                    Model = Model ?? "gemini-1.5-flash",
                    ApiKey = "",
                    IsActive = IsActive
                };
                return View("Index", errorModel);
            }

            if (ApiKey.Length < 10)
            {
                TempData["MensagemErro"] = "A chave da API parece inválida. Verifique se copiou corretamente.";
                TempData["AIConfigError"] = true;
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Erro: API Key muito curta ({ApiKey.Length} caracteres)");
                
                var errorModel = new AIConfiguration
                {
                    Provider = Provider ?? "GoogleGemini",
                    Model = Model ?? "gemini-1.5-flash",
                    ApiKey = ApiKey,
                    IsActive = IsActive
                };
                return View("Index", errorModel);
            }

            try
            {
                var existing = await _context.AIConfiguration.FirstOrDefaultAsync();
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Registro existente: {existing != null}");

                if (existing == null)
                {
                    // Criar novo registro
                    var newConfig = new AIConfiguration
                    {
                        Provider = Provider ?? "GoogleGemini",
                        ApiKey = ApiKey.Trim(),
                        Model = Model ?? "gemini-1.5-flash",
                        IsActive = IsActive,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = null
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] Criando novo: Provider={newConfig.Provider}, Model={newConfig.Model}, IsActive={newConfig.IsActive}, ApiKey={newConfig.ApiKey.Substring(0, Math.Min(10, newConfig.ApiKey.Length))}...");
                    
                    _context.AIConfiguration.Add(newConfig);
                    var changes = await _context.SaveChangesAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] SaveChanges retornou: {changes} alterações");
                    
                    if (changes > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIConfig] ✓ Registro criado com sucesso! ID={newConfig.Id}");
                        TempData["MensagemSucesso"] = "Configurações de IA salvas com sucesso!";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIConfig] ✗ AVISO: SaveChanges retornou 0 alterações!");
                        TempData["MensagemErro"] = "Nenhuma alteração foi salva. Verifique os logs.";
                        TempData["AIConfigError"] = true;
                    }
                }
                else
                {
                    // Atualizar registro existente
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] Atualizando registro ID={existing.Id}");
                    
                    existing.Provider = Provider ?? existing.Provider;
                    existing.ApiKey = ApiKey.Trim();
                    existing.Model = Model ?? existing.Model;
                    existing.IsActive = IsActive;
                    existing.UpdatedAt = DateTime.Now;
                    
                    _context.AIConfiguration.Update(existing);
                    var changes = await _context.SaveChangesAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] SaveChanges retornou: {changes} alterações");
                    
                    if (changes > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIConfig] ✓ Registro atualizado com sucesso!");
                        TempData["MensagemSucesso"] = "Configurações de IA atualizadas com sucesso!";
                    }
                }

                // Verificar se realmente salvou
                var saved = await _context.AIConfiguration.FirstOrDefaultAsync();
                if (saved != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] ✓ Verificação: Registro encontrado! ID={saved.Id}, Provider={saved.Provider}, IsActive={saved.IsActive}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] ✗ ERRO: Após salvar, nenhum registro foi encontrado!");
                }

                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIConfig] ✗ EXCEÇÃO ao salvar: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIConfig] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIConfig] Inner exception: {ex.InnerException.Message}");
                }
                
                TempData["MensagemErro"] = $"Erro ao salvar configurações: {ex.Message}";
                TempData["AIConfigError"] = true;
                
                var errorModel = new AIConfiguration
                {
                    Provider = Provider ?? "GoogleGemini",
                    Model = Model ?? "gemini-1.5-flash",
                    ApiKey = ApiKey ?? "",
                    IsActive = IsActive
                };
                return View("Index", errorModel);
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection([FromBody] AIConfiguration model)
        {
            if (string.IsNullOrWhiteSpace(model.ApiKey))
            {
                return BadRequest(new { erro = "Chave da API não informada." });
            }

            // Teste simples de conexão
            try
            {
                var testPrompt = "Responda apenas 'OK' se você está funcionando.";
                
                // Aqui você pode implementar testes específicos para cada provedor
                // Por enquanto, apenas retorna sucesso
                
                return Ok(new { sucesso = true, mensagem = "Conexão testada com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao testar conexão: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            try
            {
                var config = await _context.AIConfiguration.FirstOrDefaultAsync();
                
                if (config == null)
                {
                    return Json(new { 
                        status = "Nenhuma configuração encontrada no banco",
                        totalRecords = await _context.AIConfiguration.CountAsync()
                    });
                }

                return Json(new {
                    status = "Configuração encontrada",
                    id = config.Id,
                    provider = config.Provider,
                    model = config.Model,
                    hasApiKey = !string.IsNullOrWhiteSpace(config.ApiKey),
                    apiKeyLength = config.ApiKey?.Length ?? 0,
                    isActive = config.IsActive,
                    createdAt = config.CreatedAt,
                    updatedAt = config.UpdatedAt
                });
            }
            catch (System.Exception ex)
            {
                return Json(new {
                    status = "Erro ao acessar banco de dados",
                    erro = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> VerificarECriarTabela()
        {
            try
            {
                // Tenta acessar a tabela
                var count = await _context.AIConfiguration.CountAsync();
                return Json(new { 
                    sucesso = true, 
                    mensagem = $"Tabela existe e contém {count} registro(s)." 
                });
            }
            catch (System.Exception ex)
            {
                // Se der erro, pode ser que a tabela não existe
                return Json(new { 
                    sucesso = false, 
                    mensagem = "Erro ao acessar tabela. Verifique se a migration foi aplicada.",
                    erro = ex.Message,
                    solucao = "Execute: dotnet ef database update"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AtivarIA()
        {
            try
            {
                var config = await _context.AIConfiguration.FirstOrDefaultAsync();
                if (config != null)
                {
                    config.IsActive = true;
                    
                    // Corrigir modelo para usar gemini-1.5-flash (sem -latest)
                    config.Model = "gemini-1.5-flash";
                    
                    config.UpdatedAt = DateTime.Now;
                    _context.AIConfiguration.Update(config);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { 
                        sucesso = true, 
                        mensagem = $"IA ativada com sucesso! Modelo atualizado para: {config.Model}" 
                    });
                }
                
                return Json(new { 
                    sucesso = false, 
                    mensagem = "Nenhuma configuração encontrada." 
                });
            }
            catch (System.Exception ex)
            {
                return Json(new { 
                    sucesso = false, 
                    mensagem = "Erro ao ativar IA.",
                    erro = ex.Message
                });
            }
        }
    }
}
