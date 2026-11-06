using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models.ViewModels;

namespace WebAppSystems.Services
{
    // Unificação de clientes: move todos os vínculos de um cliente de origem para um destino.
    // Não inativa a origem (fica a cargo do usuário). Mantém trava de faturamento/aprovação.
    public class ClienteDuplicadoService
    {
        private readonly WebAppSystemsContext _context;

        public ClienteDuplicadoService(WebAppSystemsContext context)
        {
            _context = context;
        }

        // Autocomplete de clientes por nome ou documento (para os combos de origem/destino)
        public async Task<List<object>> BuscarClientesAsync(string termo)
        {
            var q = _context.Client!.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                termo = termo.Trim();
                q = q.Where(c => c.Name.Contains(termo) || (c.Document != null && c.Document.Contains(termo)));
            }

            return await q
                .OrderBy(c => c.Name)
                .Take(20)
                .Select(c => (object)new
                {
                    id = c.Id,
                    nome = c.Name,
                    documento = c.Document,
                    inativo = c.ClienteInativo
                })
                .ToListAsync();
        }

        // Resumo sintético de um cliente: total de registros, horas e vínculos; e bloqueio por faturamento.
        public async Task<ResumoClienteViewModel> ResumoClienteAsync(int clienteId)
        {
            var cliente = await _context.Client!.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente == null)
                return new ResumoClienteViewModel { Sucesso = false, Mensagem = "Cliente não encontrado." };

            var lancamentos = _context.ProcessRecord.Where(p => p.ClientId == clienteId);

            var totalLancamentos = await lancamentos.CountAsync();
            var bloqueados = await lancamentos.CountAsync(p => p.IsFaturado || p.EmAprovacao);

            // Soma de horas (HoraFinal - HoraInicial) feita em memória para evitar limitações de tradução TimeSpan
            var tempos = await lancamentos
                .Select(p => new { p.HoraInicial, p.HoraFinal })
                .ToListAsync();
            var totalMinutos = tempos.Sum(t => Math.Max(0, (t.HoraFinal - t.HoraInicial).TotalMinutes));
            var totalHoras = $"{(int)(totalMinutos / 60):00}:{(int)(totalMinutos % 60):00}";

            return new ResumoClienteViewModel
            {
                Sucesso = true,
                ClienteId = cliente.Id,
                Nome = cliente.Name,
                Documento = cliente.Document,
                Inativo = cliente.ClienteInativo,
                TotalLancamentos = totalLancamentos,
                LancamentosBloqueados = bloqueados,
                TotalHoras = totalHoras,
                Valores = await _context.ValorCliente!.CountAsync(v => v.ClientId == clienteId),
                PercentuaisArea = await _context.PercentualArea!.CountAsync(p => p.ClientId == clienteId),
                Mensalistas = await _context.Mensalista.CountAsync(m => m.ClientId == clienteId),
                Lotes = await _context.LoteAprovacao.CountAsync(l => l.ClienteId == clienteId),
                Documentos = await _context.DocumentAnalysis.CountAsync(d => d.ClientId == clienteId),
                Solicitantes = await _context.ClienteSolicitante.CountAsync(s => s.ClientId == clienteId)
            };
        }

        // Detalhes resumidos dos lançamentos (carregados sob demanda ao expandir o resumo)
        public async Task<List<object>> DetalhesLancamentosAsync(int clienteId)
        {
            // Projeta só as colunas necessárias (sem Include): o EF faz um JOIN leve, trazendo apenas o nome do advogado
            var itens = await _context.ProcessRecord
                .AsNoTracking()
                .Where(p => p.ClientId == clienteId)
                .OrderByDescending(p => p.Date)
                .Take(200)
                .Select(p => new
                {
                    p.Date,
                    p.HoraInicial,
                    p.HoraFinal,
                    Advogado = p.Attorney.Name,
                    p.Description,
                    p.IsFaturado,
                    p.EmAprovacao
                })
                .ToListAsync();

            return itens.Select(p =>
            {
                var min = Math.Max(0, (p.HoraFinal - p.HoraInicial).TotalMinutes);
                var desc = p.Description ?? string.Empty;
                if (desc.Length > 80) desc = desc.Substring(0, 80) + "…";
                return (object)new
                {
                    data = p.Date.ToString("dd/MM/yyyy"),
                    horas = $"{(int)(min / 60):00}:{(int)(min % 60):00}",
                    advogado = p.Advogado,
                    descricao = desc,
                    bloqueado = p.IsFaturado || p.EmAprovacao
                };
            }).ToList();
        }

        // Prévia da movimentação (origem -> destino)
        public async Task<PreviaUnificacaoViewModel> ObterPreviaAsync(int origemId, int destinoId)
        {
            var origem = await _context.Client!.FindAsync(origemId);
            var destino = await _context.Client!.FindAsync(destinoId);

            if (origem == null || destino == null)
                return new PreviaUnificacaoViewModel { Sucesso = false, Mensagem = "Cliente não encontrado." };

            if (origemId == destinoId)
                return new PreviaUnificacaoViewModel { Sucesso = false, Mensagem = "A origem e o destino devem ser clientes diferentes." };

            var bloqueados = await _context.ProcessRecord
                .CountAsync(p => p.ClientId == origemId && (p.IsFaturado || p.EmAprovacao));

            return new PreviaUnificacaoViewModel
            {
                Sucesso = true,
                ClienteDuplicadoNome = origem.Name,
                ClienteCorretoNome = destino.Name,
                Bloqueado = bloqueados > 0,
                Mensagem = bloqueados > 0
                    ? $"Não é possível mover: o cliente de origem possui {bloqueados} lançamento(s) em faturamento ou aprovação."
                    : null,
                Lancamentos = await _context.ProcessRecord.CountAsync(p => p.ClientId == origemId),
                Valores = await _context.ValorCliente!.CountAsync(v => v.ClientId == origemId),
                PercentuaisArea = await _context.PercentualArea!.CountAsync(p => p.ClientId == origemId),
                Mensalistas = await _context.Mensalista.CountAsync(m => m.ClientId == origemId),
                Lotes = await _context.LoteAprovacao.CountAsync(l => l.ClienteId == origemId),
                Documentos = await _context.DocumentAnalysis.CountAsync(d => d.ClientId == origemId),
                Solicitantes = await _context.ClienteSolicitante.CountAsync(s => s.ClientId == origemId)
            };
        }

        // Move todos os vínculos da origem para o destino, em transação. NÃO inativa a origem.
        public async Task<(bool Sucesso, string Mensagem)> MoverAsync(int origemId, int destinoId)
        {
            if (origemId == destinoId)
                return (false, "A origem e o destino devem ser clientes diferentes.");

            var origem = await _context.Client!.FindAsync(origemId);
            var destino = await _context.Client!.FindAsync(destinoId);
            if (origem == null || destino == null)
                return (false, "Cliente não encontrado.");

            var bloqueados = await _context.ProcessRecord
                .CountAsync(p => p.ClientId == origemId && (p.IsFaturado || p.EmAprovacao));
            if (bloqueados > 0)
                return (false, $"Não é possível mover: o cliente de origem possui {bloqueados} lançamento(s) em faturamento ou aprovação.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.ProcessRecord
                    .Where(p => p.ClientId == origemId)
                    .ForEachAsync(p => p.ClientId = destinoId);

                await _context.ValorCliente!
                    .Where(v => v.ClientId == origemId)
                    .ForEachAsync(v => v.ClientId = destinoId);

                await _context.PercentualArea!
                    .Where(p => p.ClientId == origemId)
                    .ForEachAsync(p => p.ClientId = destinoId);

                await _context.Mensalista
                    .Where(m => m.ClientId == origemId)
                    .ForEachAsync(m => m.ClientId = destinoId);

                await _context.LoteAprovacao
                    .Where(l => l.ClienteId == origemId)
                    .ForEachAsync(l => l.ClienteId = destinoId);

                await _context.DocumentAnalysis
                    .Where(d => d.ClientId == origemId)
                    .ForEachAsync(d => d.ClientId = destinoId);

                // Solicitantes: move evitando duplicar nomes já existentes no destino
                var nomesDestino = await _context.ClienteSolicitante
                    .Where(s => s.ClientId == destinoId)
                    .Select(s => s.Nome.ToLower())
                    .ToListAsync();

                var solicitantesOrigem = await _context.ClienteSolicitante
                    .Where(s => s.ClientId == origemId)
                    .ToListAsync();

                foreach (var s in solicitantesOrigem)
                {
                    if (nomesDestino.Contains(s.Nome.ToLower()))
                        _context.ClienteSolicitante.Remove(s);
                    else
                    {
                        s.ClientId = destinoId;
                        s.Padrao = false; // preserva o padrão do destino
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Registros movidos de \"{origem.Name}\" para \"{destino.Name}\" com sucesso. " +
                              $"Lembre-se de inativar o cliente de origem \"{origem.Name}\" se ele não for mais utilizado.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erro ao mover: {ex.Message}");
            }
        }
    }
}
