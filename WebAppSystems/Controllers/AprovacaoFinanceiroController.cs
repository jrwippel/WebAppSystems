using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Filters;
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;

namespace WebAppSystems.Controllers
{
    [PaginaParaAdminFinanceiro]
    public class AprovacaoFinanceiroController : Controller
    {
        private readonly WebAppSystemsContext _context;
        private readonly ISessao _sessao;

        public AprovacaoFinanceiroController(WebAppSystemsContext context, ISessao sessao)
        {
            _context = context;
            _sessao = sessao;
        }

        // GET: AprovacaoFinanceiro
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
            
            if (usuarioAtualizado.Perfil != ProfileEnum.Admin || !usuarioAtualizado.IsFinanceiro)
            {
                TempData["MensagemErro"] = "Acesso negado: apenas administradores com perfil financeiro podem acessar esta funcionalidade.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar todos os lotes (todos os financeiros veem todos os lotes)
            // Otimizado: não carrega ProcessRecord completo na listagem
            var lotes = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.CriadoPor)
                .Include(l => l.AprovadoPor)
                .Include(l => l.FaturadoPor)
                .Include(l => l.Itens)
                .AsNoTracking()
                .OrderByDescending(l => l.DataCriacao)
                .ToListAsync();

            return View(lotes);
        }

        // GET: AprovacaoFinanceiro/ContarNotificacoes (chamado pelo layout via JS)
        public async Task<IActionResult> ContarNotificacoes()
        {
            try
            {
                var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
                if (usuarioLogado == null)
                    return Json(new { naoLidas = 0, lotesParaFaturar = 0 });

                // Só retorna dados se for financeiro
                var usuario = await _context.Attorney.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == usuarioLogado.Id);

                if (usuario == null || !usuario.IsFinanceiro || usuario.Perfil != ProfileEnum.Admin)
                    return Json(new { naoLidas = 0, lotesParaFaturar = 0 });

                var naoLidas = await _context.NotificacaoAprovacao
                    .CountAsync(n => n.UsuarioId == usuarioLogado.Id && !n.Lida);

                var lotesParaFaturar = await _context.LoteAprovacao
                    .CountAsync(l => l.CriadoPorId == usuarioLogado.Id
                        && (l.Status == WebAppSystems.Models.StatusLoteAprovacao.Aprovado
                         || l.Status == WebAppSystems.Models.StatusLoteAprovacao.ParcialmenteAprovado));

                return Json(new { naoLidas, lotesParaFaturar });
            }
            catch
            {
                return Json(new { naoLidas = 0, lotesParaFaturar = 0 });
            }
        }

        // GET: AprovacaoFinanceiro/Notificacoes
        public async Task<IActionResult> Notificacoes()
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

            if (usuarioAtualizado == null || !usuarioAtualizado.IsFinanceiro || usuarioAtualizado.Perfil != ProfileEnum.Admin)
            {
                TempData["MensagemErro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            var notificacoes = await _context.NotificacaoAprovacao
                .Include(n => n.LoteAprovacao)
                    .ThenInclude(l => l.Cliente)
                .Where(n => n.UsuarioId == usuarioLogado.Id)
                .OrderByDescending(n => n.DataCriacao)
                .ToListAsync();

            // Contar não lidas para o ViewBag
            ViewBag.NaoLidas = notificacoes.Count(n => !n.Lida);

            return View(notificacoes);
        }

        // POST: AprovacaoFinanceiro/MarcarNotificacaoLida
        [HttpPost]
        public async Task<IActionResult> MarcarNotificacaoLida(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null)
                return Json(new { success = false, message = "Usuário não autenticado" });

            var notificacao = await _context.NotificacaoAprovacao
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioLogado.Id);

            if (notificacao == null)
                return Json(new { success = false, message = "Notificação não encontrada" });

            notificacao.Lida = true;
            notificacao.DataLeitura = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: AprovacaoFinanceiro/MarcarTodasLidas
        [HttpPost]
        public async Task<IActionResult> MarcarTodasLidas()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null)
                return Json(new { success = false, message = "Usuário não autenticado" });

            var naoLidas = await _context.NotificacaoAprovacao
                .Where(n => n.UsuarioId == usuarioLogado.Id && !n.Lida)
                .ToListAsync();

            foreach (var n in naoLidas)
            {
                n.Lida = true;
                n.DataLeitura = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true, count = naoLidas.Count });
        }

        // GET: AprovacaoFinanceiro/CriarLote
        public async Task<IActionResult> CriarLote()
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
            
            if (usuarioAtualizado.Perfil != ProfileEnum.Admin || !usuarioAtualizado.IsFinanceiro)
            {
                TempData["MensagemErro"] = "Acesso negado: apenas administradores com perfil financeiro podem acessar esta funcionalidade.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar clientes ativos
            ViewBag.Clientes = await _context.Client
                .Where(c => !c.ClienteInativo)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View();
        }
        
        // GET: AprovacaoFinanceiro/DiagnosticoUsuario - Endpoint para debug
        public async Task<IActionResult> DiagnosticoUsuario()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { erro = "Usuário não autenticado" });
            }
            
            var usuarioAtualizado = await _context.Attorney.FindAsync(usuarioLogado.Id);
            
            return Json(new
            {
                usuarioSessao = new
                {
                    id = usuarioLogado.Id,
                    nome = usuarioLogado.Name,
                    perfil = usuarioLogado.Perfil.ToString(),
                    isFinanceiro = usuarioLogado.IsFinanceiro,
                    isAprovador = usuarioLogado.IsAprovador
                },
                usuarioBanco = usuarioAtualizado == null ? null : new
                {
                    id = usuarioAtualizado.Id,
                    nome = usuarioAtualizado.Name,
                    perfil = usuarioAtualizado.Perfil.ToString(),
                    isFinanceiro = usuarioAtualizado.IsFinanceiro,
                    isAprovador = usuarioAtualizado.IsAprovador
                }
            });
        }

        // POST: AprovacaoFinanceiro/BuscarLancamentos
        [HttpPost]
        public async Task<IActionResult> BuscarLancamentos(DateTime dataInicio, DateTime dataFim, int[] clienteIds)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney.FindAsync(usuarioLogado.Id);
            
            if (usuarioAtualizado == null || usuarioAtualizado.Perfil != ProfileEnum.Admin || !usuarioAtualizado.IsFinanceiro)
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            if (clienteIds == null || clienteIds.Length == 0)
            {
                // Se nenhum cliente foi selecionado, buscar de todos os clientes ativos
                clienteIds = await _context.Client
                    .Where(c => !c.ClienteInativo)
                    .Select(c => c.Id)
                    .ToArrayAsync();
                
                if (clienteIds.Length == 0)
                {
                    return Json(new { success = false, message = "Nenhum cliente ativo encontrado" });
                }
            }

            // Buscar lançamentos não faturados e não em aprovação para os clientes selecionados
            var lancamentos = await _context.ProcessRecord
                .Include(p => p.Attorney)
                .Include(p => p.Client)
                .Include(p => p.Department)
                .Where(p => clienteIds.Contains(p.ClientId) 
                    && p.Date >= dataInicio 
                    && p.Date <= dataFim
                    && !p.IsFaturado
                    && !p.EmAprovacao)
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Attorney.Name)
                .Select(p => new
                {
                    id = p.Id,
                    data = p.Date.ToString("dd/MM/yyyy"),
                    advogado = p.Attorney.Name,
                    cliente = p.Client.Name,
                    clienteId = p.ClientId,
                    descricao = p.Description,
                    horaInicial = p.HoraInicial.ToString(@"hh\:mm"),
                    horaFinal = p.HoraFinal.ToString(@"hh\:mm"),
                    horas = p.CalculoHorasDecimal(),
                    departamento = p.Department.Name
                })
                .ToListAsync();

            // Agrupar por cliente
            var lancamentosPorCliente = lancamentos.GroupBy(l => new { l.clienteId, l.cliente })
                .Select(g => new
                {
                    clienteId = g.Key.clienteId,
                    clienteNome = g.Key.cliente,
                    lancamentos = g.ToList(),
                    totalHoras = g.Sum(l => l.horas),
                    quantidadeLancamentos = g.Count()
                })
                .ToList();

            return Json(new { success = true, data = lancamentosPorCliente });
        }

        // POST: AprovacaoFinanceiro/CriarLotes
        [HttpPost]
        public async Task<IActionResult> CriarLotes(DateTime dataInicio, DateTime dataFim, int[] clienteIds, int[] lancamentoIds)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            
            if (usuarioLogado == null)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }
            
            // Buscar dados atualizados do banco para verificar permissão
            var usuarioAtualizado = await _context.Attorney.FindAsync(usuarioLogado.Id);
            
            if (usuarioAtualizado == null || usuarioAtualizado.Perfil != ProfileEnum.Admin || !usuarioAtualizado.IsFinanceiro)
            {
                return Json(new { success = false, message = "Acesso negado: você não possui permissão para esta funcionalidade" });
            }

            if (clienteIds == null || clienteIds.Length == 0)
            {
                return Json(new { success = false, message = "Selecione pelo menos um cliente" });
            }

            if (lancamentoIds == null || lancamentoIds.Length == 0)
            {
                return Json(new { success = false, message = "Selecione pelo menos um lançamento" });
            }

            try
            {
                // Buscar lançamentos selecionados
                var lancamentos = await _context.ProcessRecord
                    .Include(p => p.Client)
                    .Where(p => lancamentoIds.Contains(p.Id))
                    .ToListAsync();

                // Verificar se algum lançamento já está em aprovação ou faturado
                var lancamentosInvalidos = lancamentos.Where(p => p.IsFaturado || p.EmAprovacao).ToList();
                if (lancamentosInvalidos.Any())
                {
                    return Json(new { success = false, message = "Alguns lançamentos já estão faturados ou em aprovação" });
                }

                // Agrupar lançamentos por cliente
                var lancamentosPorCliente = lancamentos.GroupBy(p => p.ClientId);

                var lotesCriados = new List<LoteAprovacao>();

                foreach (var grupo in lancamentosPorCliente)
                {
                    var clienteId = grupo.Key;
                    var lancamentosCliente = grupo.ToList();

                    // Buscar valores do cliente para calcular valor estimado
                    var valoresCliente = await _context.ValorCliente
                        .Where(v => v.ClientId == clienteId)
                        .ToListAsync();

                    double valorEstimado = 0;
                    double totalHoras = 0;

                    foreach (var lancamento in lancamentosCliente)
                    {
                        var horas = lancamento.CalculoHorasDecimal();
                        totalHoras += horas;

                        // Buscar valor específico do advogado ou valor padrão do cliente
                        var valorHora = valoresCliente
                            .FirstOrDefault(v => v.AttorneyId == lancamento.AttorneyId)?.Valor
                            ?? valoresCliente.FirstOrDefault(v => v.AttorneyId == null)?.Valor
                            ?? 0;

                        valorEstimado += horas * valorHora;
                    }

                    // Criar lote de aprovação
                    var lote = new LoteAprovacao
                    {
                        DataCriacao = DateTime.Now,
                        CriadoPorId = usuarioLogado.Id,
                        ClienteId = clienteId,
                        PeriodoInicio = dataInicio,
                        PeriodoFim = dataFim,
                        Status = StatusLoteAprovacao.Pendente,
                        TotalHoras = totalHoras,
                        ValorEstimado = valorEstimado
                    };

                    _context.LoteAprovacao.Add(lote);
                    await _context.SaveChangesAsync();

                    // Criar itens do lote
                    foreach (var lancamento in lancamentosCliente)
                    {
                        var item = new LoteAprovacaoItem
                        {
                            LoteAprovacaoId = lote.Id,
                            ProcessRecordId = lancamento.Id,
                            Status = StatusItemAprovacao.Pendente,
                            Abonado = false,
                            FoiEditado = false,
                            DescricaoOriginal = lancamento.Description,
                            HoraInicialOriginal = lancamento.HoraInicial,
                            HoraFinalOriginal = lancamento.HoraFinal
                        };

                        _context.LoteAprovacaoItem.Add(item);

                        // Marcar lançamento como em aprovação
                        lancamento.EmAprovacao = true;
                    }

                    await _context.SaveChangesAsync();

                    // Registrar no histórico
                    var historico = new HistoricoAprovacao
                    {
                        LoteAprovacaoId = lote.Id,
                        DataHora = DateTime.Now,
                        UsuarioId = usuarioLogado.Id,
                        TipoAcao = "Criacao",
                        Detalhes = $"Lote criado com {lancamentosCliente.Count} lançamentos. Total: {totalHoras:F2}h - R$ {valorEstimado:F2}"
                    };

                    _context.HistoricoAprovacao.Add(historico);
                    await _context.SaveChangesAsync();

                    // Criar notificações para todos os aprovadores
                    var aprovadores = await _context.Attorney
                        .Where(a => a.IsAprovador && !a.Inativo)
                        .ToListAsync();

                    foreach (var aprovador in aprovadores)
                    {
                        var notificacao = new NotificacaoAprovacao
                        {
                            UsuarioId = aprovador.Id,
                            LoteAprovacaoId = lote.Id,
                            TipoNotificacao = "NovoLote",
                            Mensagem = $"Novo lote de aprovação: {lancamentosCliente.First().Client.Name} - {totalHoras:F2}h - R$ {valorEstimado:F2}",
                            DataCriacao = DateTime.Now,
                            Lida = false
                        };

                        _context.NotificacaoAprovacao.Add(notificacao);
                    }

                    await _context.SaveChangesAsync();

                    lotesCriados.Add(lote);
                }

                return Json(new { 
                    success = true, 
                    message = $"{lotesCriados.Count} lote(s) criado(s) com sucesso",
                    lotesIds = lotesCriados.Select(l => l.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao criar lotes: {ex.Message}" });
            }
        }

        // GET: AprovacaoFinanceiro/DetalhesLote/5
        public async Task<IActionResult> DetalhesLote(int id)
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
            
            if (!usuarioAtualizado.IsFinanceiro || usuarioAtualizado.Perfil != ProfileEnum.Admin)
            {
                TempData["MensagemErro"] = "Acesso negado: apenas administradores com perfil financeiro podem acessar esta funcionalidade.";
                return RedirectToAction("Index", "Home");
            }

            var lote = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.CriadoPor)
                .Include(l => l.AprovadoPor)
                .Include(l => l.FaturadoPor)
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

        // GET: AprovacaoFinanceiro/GerarExcel/5
        public async Task<IActionResult> GerarExcel(int id)
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
            
            if (usuarioAtualizado == null || usuarioAtualizado.Perfil != ProfileEnum.Admin || !usuarioAtualizado.IsFinanceiro)
            {
                TempData["MensagemErro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar o lote com seus itens (sem AsNoTracking para poder salvar)
            var lote = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lote == null)
            {
                TempData["MensagemErro"] = "Lote não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Filtrar apenas itens aprovados
            var itensAprovados = lote.Itens
                .Where(i => i.Status == StatusItemAprovacao.Aprovado)
                .ToList();

            if (!itensAprovados.Any())
            {
                // Verificar se todos foram abonados
                var totalItens = lote.Itens.Count;
                var itensAbonados = lote.Itens.Count(i => i.Abonado || i.Status == StatusItemAprovacao.Abonado);
                
                if (totalItens > 0 && itensAbonados == totalItens)
                    TempData["MensagemErro"] = "Todos os lançamentos deste lote foram abonados. Não há itens para faturar.";
                else
                    TempData["MensagemErro"] = "Não há itens aprovados neste lote para gerar o Excel.";
                
                return RedirectToAction(nameof(Index));
            }

            // Marcar lote como Faturado ao gerar o Excel (apenas na primeira vez)
            if (lote.Status == StatusLoteAprovacao.Aprovado)
            {
                lote.Status = StatusLoteAprovacao.Faturado;
                lote.DataFaturamento = DateTime.Now;
                lote.FaturadoPorId = usuarioLogado.Id;

                // Marcar os ProcessRecords aprovados como faturados
                foreach (var item in itensAprovados)
                {
                    if (item.ProcessRecord != null)
                    {
                        item.ProcessRecord.IsFaturado = true;
                        item.ProcessRecord.DataFaturamento = DateTime.Now;
                        item.ProcessRecord.FaturadoPorId = usuarioLogado.Id;
                        item.ProcessRecord.EmAprovacao = false;
                    }
                }

                // Registrar no histórico
                var historico = new HistoricoAprovacao
                {
                    LoteAprovacaoId = lote.Id,
                    DataHora = DateTime.Now,
                    UsuarioId = usuarioLogado.Id,
                    TipoAcao = "Faturamento",
                    Detalhes = $"Excel gerado e lote marcado como faturado por {usuarioAtualizado.Name}."
                };
                _context.HistoricoAprovacao.Add(historico);

                await _context.SaveChangesAsync();
            }

            // Obter IDs dos ProcessRecords aprovados
            var processRecordIds = itensAprovados.Select(i => i.ProcessRecordId).ToList();

            // Buscar todos os ProcessRecords do período para calcular os ignorados
            var todosProcessRecords = await _context.ProcessRecord
                .Where(p => p.ClientId == lote.ClienteId 
                    && p.Date >= lote.PeriodoInicio 
                    && p.Date <= lote.PeriodoFim)
                .Select(p => p.Id)
                .ToListAsync();

            // IDs a ignorar = todos do período EXCETO os aprovados
            var ignoredIds = todosProcessRecords.Except(processRecordIds).ToList();
            var ignoredIdsString = string.Join(",", ignoredIds);

            // Copiar o cookie de download token se existir
            var downloadToken = Request.Cookies["fileDownloadToken"];
            if (!string.IsNullOrEmpty(downloadToken))
            {
                Response.Cookies.Append("fileDownloadToken", downloadToken, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Path = "/",
                    HttpOnly = false
                });
            }

            // Redirecionar para o método de geração de Excel do ProcessRecordController
            return RedirectToAction("DownloadReport", "ProcessRecord", new
            {
                minDate = lote.PeriodoInicio,
                maxDate = lote.PeriodoFim,
                clientIds = lote.ClienteId.ToString(),
                format = "xlsx",
                ignoredIds = ignoredIdsString
            });
        }

        // GET: AprovacaoFinanceiro/GerarPreFatura/5
        public async Task<IActionResult> GerarPreFatura(int id)
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
            
            if (usuarioAtualizado == null || usuarioAtualizado.Perfil != ProfileEnum.Admin || !usuarioAtualizado.IsFinanceiro)
            {
                TempData["MensagemErro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar o lote com seus itens
            var lote = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lote == null)
            {
                TempData["MensagemErro"] = "Lote não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Filtrar apenas itens aprovados
            var itensAprovados = lote.Itens
                .Where(i => i.Status == StatusItemAprovacao.Aprovado)
                .ToList();

            if (!itensAprovados.Any())
            {
                TempData["MensagemErro"] = "Não há itens aprovados neste lote para gerar a Pré-Fatura.";
                return RedirectToAction("DetalhesLote", new { id });
            }

            // Obter IDs dos ProcessRecords aprovados
            var processRecordIds = itensAprovados.Select(i => i.ProcessRecordId).ToList();

            // Buscar todos os ProcessRecords do período para calcular os ignorados
            var todosProcessRecords = await _context.ProcessRecord
                .Where(p => p.ClientId == lote.ClienteId 
                    && p.Date >= lote.PeriodoInicio 
                    && p.Date <= lote.PeriodoFim)
                .Select(p => p.Id)
                .ToListAsync();

            // IDs a ignorar = todos do período EXCETO os aprovados
            var ignoredIds = todosProcessRecords.Except(processRecordIds).ToList();
            var ignoredIdsString = string.Join(",", ignoredIds);

            // Copiar o cookie de download token se existir
            var downloadToken = Request.Cookies["fileDownloadToken"];
            if (!string.IsNullOrEmpty(downloadToken))
            {
                Response.Cookies.Append("fileDownloadToken", downloadToken, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Path = "/",
                    HttpOnly = false
                });
            }

            // Redirecionar para o método de geração de Pré-Fatura do ProcessRecordController
            return RedirectToAction("PreFatura", "ProcessRecord", new
            {
                minDate = lote.PeriodoInicio,
                maxDate = lote.PeriodoFim,
                clientIds = lote.ClienteId.ToString(),
                ignoredIds = ignoredIdsString
            });
        }

        // POST: AprovacaoFinanceiro/FecharLote
        [HttpPost]
        public async Task<IActionResult> FecharLote(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null)
                return Json(new { success = false, message = "Usuário não autenticado" });

            var lote = await _context.LoteAprovacao
                .Include(l => l.Itens)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lote == null)
                return Json(new { success = false, message = "Lote não encontrado" });

            if (lote.Status == StatusLoteAprovacao.Faturado)
                return Json(new { success = false, message = "Lote já está faturado" });

            // Verifica se todos os itens estão abonados
            var todosAbonados = lote.Itens.Any() && lote.Itens.All(i => i.Status == StatusItemAprovacao.Abonado);
            if (!todosAbonados)
                return Json(new { success = false, message = "Nem todos os itens estão abonados" });

            lote.Status = StatusLoteAprovacao.Faturado;
            lote.DataFaturamento = DateTime.Now;
            lote.FaturadoPorId = usuarioLogado.Id;

            var historico = new HistoricoAprovacao
            {
                LoteAprovacaoId = lote.Id,
                DataHora = DateTime.Now,
                UsuarioId = usuarioLogado.Id,
                TipoAcao = "Faturamento",
                Detalhes = "Lote fechado sem faturamento — todos os lançamentos foram abonados."
            };
            _context.HistoricoAprovacao.Add(historico);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: AprovacaoFinanceiro/ExcluirLote/5
        [HttpPost]
        public async Task<IActionResult> ExcluirLote(int id)
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
            
            if (usuarioAtualizado == null || usuarioAtualizado.Perfil != ProfileEnum.Admin || !usuarioAtualizado.IsFinanceiro)
            {
                TempData["MensagemErro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar o lote com seus itens e relacionamentos
            var lote = await _context.LoteAprovacao
                .Include(l => l.Cliente)
                .Include(l => l.Itens)
                    .ThenInclude(i => i.ProcessRecord)
                .Include(l => l.Historico)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lote == null)
            {
                TempData["MensagemErro"] = "Lote não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Não permitir exclusão de lotes faturados
            if (lote.Status == StatusLoteAprovacao.Faturado)
            {
                TempData["MensagemErro"] = "Não é possível excluir lotes que já foram faturados.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Desmarcar os ProcessRecords como "EmAprovacao"
                foreach (var item in lote.Itens)
                {
                    var processRecord = item.ProcessRecord;
                    if (processRecord != null)
                    {
                        processRecord.EmAprovacao = false;
                    }
                }

                // Excluir notificações relacionadas ao lote
                var notificacoes = await _context.NotificacaoAprovacao
                    .Where(n => n.LoteAprovacaoId == id)
                    .ToListAsync();
                
                if (notificacoes.Any())
                {
                    _context.NotificacaoAprovacao.RemoveRange(notificacoes);
                }

                // Excluir histórico do lote
                if (lote.Historico.Any())
                {
                    _context.HistoricoAprovacao.RemoveRange(lote.Historico);
                }

                // Excluir itens do lote
                if (lote.Itens.Any())
                {
                    _context.LoteAprovacaoItem.RemoveRange(lote.Itens);
                }

                // Excluir o lote
                _context.LoteAprovacao.Remove(lote);

                // Salvar todas as alterações
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"Lote do cliente {lote.Cliente.Name} excluído com sucesso. Os lançamentos voltaram a ficar disponíveis.";
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao excluir lote: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}