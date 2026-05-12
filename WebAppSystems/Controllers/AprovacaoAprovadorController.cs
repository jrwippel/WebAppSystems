using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;

namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    public class AprovacaoAprovadorController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ISessao _sessao;

        public AprovacaoAprovadorController(WebAppSystemsContext context, ISessao sessao)
        {
            _context = context;
            _sessao = sessao;
        }

        // GET: AprovacaoAprovador
        public async Task<IActionResult> Index()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                TempData["MensagemErro"] = "Usuário não autenticado.";
                return RedirectToAction("Index", "Home");
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == usuarioLogado.Id);
            
            if (usuarioAtualizado == null)
            {
                TempData["MensagemErro"] = "Usuário não encontrado no banco de dados.";
                return RedirectToAction("Index", "Home");
            }
            
            if (!usuarioAtualizado.IsAprovador)
            {
                TempData["MensagemErro"] = "Acesso negado: você não possui permissão para esta funcionalidade.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar TODOS os lotes (pendentes e revisados) dos últimos 90 dias
            var dataLimite = DateTime.Now.AddDays(-90);
            var todosLotes = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.CriadoPor)
                .Include(l => l.Itens)
                .Where(l => l.DataCriacao >= dataLimite)
                .OrderByDescending(l => l.DataCriacao)
                .ToListAsync();

            // Buscar notificações não lidas
            var notificacoesNaoLidas = await _context.NotificacaoAprovacao
                .Where(n => n.UsuarioId == usuarioLogado.Id && !n.Lida)
                .CountAsync();

            ViewBag.NotificacoesNaoLidas = notificacoesNaoLidas;

            return View(todosLotes);
        }

        // GET: AprovacaoAprovador/Notificacoes
        public async Task<IActionResult> Notificacoes()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                TempData["MensagemErro"] = "Usuário não autenticado.";
                return RedirectToAction("Index", "Home");
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney.FindAsync(usuarioLogado.Id);
            
            if (usuarioAtualizado == null || !usuarioAtualizado.IsAprovador)
            {
                TempData["MensagemErro"] = "Acesso negado: você não possui permissão para esta funcionalidade.";
                return RedirectToAction("Index", "Home");
            }

            var notificacoes = await _context.NotificacaoAprovacao
                .Include(n => n.LoteAprovacao)
                    .ThenInclude(l => l.Cliente)
                .Where(n => n.UsuarioId == usuarioLogado.Id)
                .OrderByDescending(n => n.DataCriacao)
                .ToListAsync();

            return View(notificacoes);
        }

        // GET: AprovacaoAprovador/DetalhesLote/5
        public async Task<IActionResult> DetalhesLote(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();

            if (usuarioLogado == null)
            {
                TempData["MensagemErro"] = "Usuário não autenticado.";
                return RedirectToAction("Index", "Home");
            }

            var usuarioAtualizado = await _context.Attorney
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == usuarioLogado.Id);

            if (usuarioAtualizado == null || !usuarioAtualizado.IsAprovador)
            {
                TempData["MensagemErro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            var lote = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.CriadoPor)
                .Include(l => l.AprovadoPor)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                        .ThenInclude(p => p.Attorney)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                        .ThenInclude(p => p.Department)
                .Include(l => l.Historico)
                    .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lote == null)
            {
                TempData["MensagemErro"] = "Lote não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            return View(lote);
        }

        // POST: AprovacaoAprovador/MarcarNotificacaoLida
        [HttpPost]
        public async Task<IActionResult> MarcarNotificacaoLida(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }

            var notificacao = await _context.NotificacaoAprovacao
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioLogado.Id);

            if (notificacao == null)
            {
                return Json(new { success = false, message = "Notificação não encontrada" });
            }

            notificacao.Lida = true;
            notificacao.DataLeitura = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: AprovacaoAprovador/TesteSimples
        public IActionResult TesteSimples()
        {
            return View();
        }

        // GET: AprovacaoAprovador/Revisar/5
        public async Task<IActionResult> Revisar(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                TempData["MensagemErro"] = "Usuário não autenticado.";
                return RedirectToAction("Index", "Home");
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == usuarioLogado.Id);
            
            if (usuarioAtualizado == null)
            {
                TempData["MensagemErro"] = "Usuário não encontrado no banco de dados.";
                return RedirectToAction("Index", "Home");
            }
            
            if (!usuarioAtualizado.IsAprovador)
            {
                TempData["MensagemErro"] = "Acesso negado: você não possui permissão para esta funcionalidade.";
                return RedirectToAction("Index", "Home");
            }

            var lote = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.CriadoPor)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                        .ThenInclude(p => p.Attorney)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                        .ThenInclude(p => p.Department)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lote == null)
            {
                TempData["MensagemErro"] = "Lote não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            if (lote.Status != StatusLoteAprovacao.Pendente)
            {
                TempData["MensagemErro"] = "Este lote não está mais pendente de aprovação.";
                return RedirectToAction(nameof(Index));
            }

            // Buscar valores do cliente para exibir (simplificado para evitar ciclo de referência JSON)
            var valoresCliente = await _context.ValorCliente
                .Include(v => v.Attorney)
                .Where(v => v.ClientId == lote.ClienteId)
                .Select(v => new
                {
                    id = v.Id,
                    clientId = v.ClientId,
                    attorneyId = v.AttorneyId,
                    valor = v.Valor,
                    attorneyName = v.Attorney != null ? v.Attorney.Name : null
                })
                .ToListAsync();

            ViewBag.ValoresCliente = valoresCliente;

            return View(lote);
        }

        // POST: AprovacaoAprovador/AbonarItem
        [HttpPost]
        public async Task<IActionResult> AbonarItem(int itemId, string? observacao)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == usuarioLogado.Id);
            
            if (usuarioAtualizado == null || !usuarioAtualizado.IsAprovador)
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            var item = await _context.LoteAprovacaoItem
                .Include(i => i.LoteAprovacao)
                .Include(i => i.ProcessRecord)
                    .ThenInclude(p => p.Attorney)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                return Json(new { success = false, message = "Item não encontrado" });
            }

            if (item.LoteAprovacao.Status != StatusLoteAprovacao.Pendente)
            {
                return Json(new { success = false, message = "Lote não está mais pendente" });
            }

            try
            {
                // Marcar item como abonado
                item.Status = StatusItemAprovacao.Abonado;
                item.Abonado = true;
                item.DataRevisao = DateTime.Now;
                item.ObservacaoRevisao = observacao;

                // Liberar lançamento (remover flag EmAprovacao)
                item.ProcessRecord.EmAprovacao = false;

                await _context.SaveChangesAsync();

                // Registrar no histórico
                var historico = new HistoricoAprovacao
                {
                    LoteAprovacaoId = item.LoteAprovacaoId,
                    DataHora = DateTime.Now,
                    UsuarioId = usuarioLogado.Id,
                    TipoAcao = "Abono",
                    Detalhes = $"Lançamento abonado: {item.ProcessRecord.Date:dd/MM/yyyy} - {item.ProcessRecord.Attorney.Name} - {item.ProcessRecord.CalculoHorasDecimal():F2}h. Observação: {observacao}",
                    ProcessRecordId = item.ProcessRecordId
                };

                _context.HistoricoAprovacao.Add(historico);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Item abonado com sucesso" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR AbonarItem] Erro ao abonar item {itemId}: {ex.Message}");
                Console.WriteLine($"[ERROR AbonarItem] StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Erro ao abonar item: {ex.Message}" });
            }
        }

        // POST: AprovacaoAprovador/AprovarItem
        [HttpPost]
        public async Task<IActionResult> AprovarItem(int itemId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == usuarioLogado.Id);
            
            if (usuarioAtualizado == null || !usuarioAtualizado.IsAprovador)
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            try
            {
                var item = await _context.LoteAprovacaoItem
                    .Include(i => i.LoteAprovacao)
                    .Include(i => i.ProcessRecord)
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item == null)
                {
                    return Json(new { success = false, message = "Item não encontrado" });
                }

                if (item.LoteAprovacao.Status != StatusLoteAprovacao.Pendente)
                {
                    return Json(new { success = false, message = "Lote não está mais pendente" });
                }

                // Não permitir aprovar item já abonado
                if (item.Status == StatusItemAprovacao.Abonado)
                {
                    return Json(new { success = false, message = "Item já foi abonado e não pode ser aprovado" });
                }

                // Marcar item como aprovado
                item.Status = StatusItemAprovacao.Aprovado;
                item.DataRevisao = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Item aprovado com sucesso" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR AprovarItem] Erro ao aprovar item {itemId}: {ex.Message}");
                Console.WriteLine($"[ERROR AprovarItem] StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Erro ao aprovar item: {ex.Message}" });
            }
        }

        // POST: AprovacaoAprovador/EditarItem
        [HttpPost]
        public async Task<IActionResult> EditarItem(int itemId, string descricao, string horaInicial, string horaFinal)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == usuarioLogado.Id);
            
            if (usuarioAtualizado == null || !usuarioAtualizado.IsAprovador)
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            var item = await _context.LoteAprovacaoItem
                .Include(i => i.LoteAprovacao)
                .Include(i => i.ProcessRecord)
                    .ThenInclude(p => p.Attorney)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                return Json(new { success = false, message = "Item não encontrado" });
            }

            if (item.LoteAprovacao.Status != StatusLoteAprovacao.Pendente)
            {
                return Json(new { success = false, message = "Lote não está mais pendente" });
            }

            try
            {
                // Salvar valores originais se ainda não foram salvos
                if (!item.FoiEditado)
                {
                    item.DescricaoOriginal = item.ProcessRecord.Description;
                    item.HoraInicialOriginal = item.ProcessRecord.HoraInicial;
                    item.HoraFinalOriginal = item.ProcessRecord.HoraFinal;
                    item.FoiEditado = true;
                }

                var detalhesAlteracoes = new List<string>();

                // Atualizar descrição
                if (descricao != item.ProcessRecord.Description)
                {
                    detalhesAlteracoes.Add($"Descrição: '{item.ProcessRecord.Description}' → '{descricao}'");
                    item.ProcessRecord.Description = descricao;
                }

                // Atualizar horas
                var novaHoraInicial = TimeSpan.Parse(horaInicial);
                var novaHoraFinal = TimeSpan.Parse(horaFinal);

                if (novaHoraInicial != item.ProcessRecord.HoraInicial)
                {
                    detalhesAlteracoes.Add($"Hora Inicial: {item.ProcessRecord.HoraInicial:hh\\:mm} → {novaHoraInicial:hh\\:mm}");
                    item.ProcessRecord.HoraInicial = novaHoraInicial;
                }

                if (novaHoraFinal != item.ProcessRecord.HoraFinal)
                {
                    detalhesAlteracoes.Add($"Hora Final: {item.ProcessRecord.HoraFinal:hh\\:mm} → {novaHoraFinal:hh\\:mm}");
                    item.ProcessRecord.HoraFinal = novaHoraFinal;
                }

                await _context.SaveChangesAsync();

                // Registrar no histórico
                if (detalhesAlteracoes.Any())
                {
                    var historico = new HistoricoAprovacao
                    {
                        LoteAprovacaoId = item.LoteAprovacaoId,
                        DataHora = DateTime.Now,
                        UsuarioId = usuarioLogado.Id,
                        TipoAcao = "Edicao",
                        Detalhes = $"Lançamento editado: {item.ProcessRecord.Date:dd/MM/yyyy} - {item.ProcessRecord.Attorney.Name}. Alterações: {string.Join("; ", detalhesAlteracoes)}",
                        ProcessRecordId = item.ProcessRecordId
                    };

                    _context.HistoricoAprovacao.Add(historico);
                    await _context.SaveChangesAsync();
                }

                return Json(new { 
                    success = true, 
                    message = "Item editado com sucesso",
                    novasHoras = item.ProcessRecord.CalculoHorasDecimal()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao editar item: {ex.Message}" });
            }
        }

        // POST: AprovacaoAprovador/AprovarLote
        [HttpPost]
        public async Task<IActionResult> AprovarLote(int loteId, string? comentario)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney.FindAsync(usuarioLogado.Id);
            
            if (usuarioAtualizado == null || !usuarioAtualizado.IsAprovador)
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            var lote = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.CriadoPor)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                .FirstOrDefaultAsync(l => l.Id == loteId);

            if (lote == null)
            {
                return Json(new { success = false, message = "Lote não encontrado" });
            }

            if (lote.Status != StatusLoteAprovacao.Pendente)
            {
                return Json(new { success = false, message = "Lote não está mais pendente" });
            }

            // Verificar se todos os itens foram revisados
            var itensNaoRevisados = lote.Itens.Where(i => i.Status == StatusItemAprovacao.Pendente).ToList();
            if (itensNaoRevisados.Any())
            {
                return Json(new { 
                    success = false, 
                    message = $"Existem {itensNaoRevisados.Count} item(ns) não revisado(s). Revise todos os itens antes de aprovar o lote.",
                    itensNaoRevisados = itensNaoRevisados.Select(i => i.Id).ToList()
                });
            }

            // Recalcular totais considerando apenas itens aprovados
            var itensAprovados = lote.Itens.Where(i => i.Status == StatusItemAprovacao.Aprovado).ToList();
            
            double totalHoras = 0;
            double valorEstimado = 0;

            // Buscar valores do cliente
            var valoresCliente = await _context.ValorCliente
                .Where(v => v.ClientId == lote.ClienteId)
                .ToListAsync();

            foreach (var item in itensAprovados)
            {
                var horas = item.ProcessRecord.CalculoHorasDecimal();
                totalHoras += horas;

                var valorHora = valoresCliente
                    .FirstOrDefault(v => v.AttorneyId == item.ProcessRecord.AttorneyId)?.Valor
                    ?? valoresCliente.FirstOrDefault(v => v.AttorneyId == null)?.Valor
                    ?? 0;

                valorEstimado += horas * valorHora;
            }

            // Atualizar lote
            lote.Status = StatusLoteAprovacao.Aprovado;
            lote.DataAprovacao = DateTime.Now;
            lote.AprovadoPorId = usuarioLogado.Id;
            lote.ComentarioAprovador = comentario;
            lote.TotalHoras = totalHoras;
            lote.ValorEstimado = valorEstimado;

            await _context.SaveChangesAsync();

            // Registrar no histórico
            var historico = new HistoricoAprovacao
            {
                LoteAprovacaoId = lote.Id,
                DataHora = DateTime.Now,
                UsuarioId = usuarioLogado.Id,
                TipoAcao = "Aprovacao",
                Detalhes = $"Lote aprovado com {itensAprovados.Count} lançamentos. Total: {totalHoras:F2}h - R$ {valorEstimado:F2}. Comentário: {comentario}"
            };

            _context.HistoricoAprovacao.Add(historico);
            await _context.SaveChangesAsync();

            // Criar notificação para o usuário financeiro que criou o lote
            var notificacao = new NotificacaoAprovacao
            {
                UsuarioId = lote.CriadoPorId,
                LoteAprovacaoId = lote.Id,
                TipoNotificacao = "LoteAprovado",
                Mensagem = $"Lote aprovado: {lote.Cliente.Name} - {totalHoras:F2}h - R$ {valorEstimado:F2}",
                DataCriacao = DateTime.Now,
                Lida = false
            };

            _context.NotificacaoAprovacao.Add(notificacao);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Lote aprovado com sucesso",
                totalHoras = totalHoras,
                valorEstimado = valorEstimado
            });
        }

        // GET: AprovacaoAprovador/ProximoLote
        public async Task<IActionResult> ProximoLote(int? loteAtualId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                TempData["MensagemErro"] = "Usuário não autenticado.";
                return RedirectToAction("Index", "Home");
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney.FindAsync(usuarioLogado.Id);
            
            if (usuarioAtualizado == null || !usuarioAtualizado.IsAprovador)
            {
                TempData["MensagemErro"] = "Acesso negado: você não possui permissão para esta funcionalidade.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar próximo lote pendente
            var query = _context.LoteAprovacao
                .Where(l => l.Status == StatusLoteAprovacao.Pendente);

            if (loteAtualId.HasValue)
            {
                query = query.Where(l => l.Id != loteAtualId.Value);
            }

            var proximoLote = await query
                .OrderBy(l => l.DataCriacao)
                .FirstOrDefaultAsync();

            if (proximoLote == null)
            {
                TempData["MensagemSucesso"] = "Todos os lotes foram revisados!";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Revisar), new { id = proximoLote.Id });
        }
    }
}
