using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;
using WebAppSystems.Models.Dto;

namespace WebAppSystems.Services
{
    public class ClientService
    {
        private readonly WebAppSystemsContext _context;

        public ClientService(WebAppSystemsContext context)
        {
            _context = context;
        }

        public async Task<Client> FindByIdAsync(int id)
        {
            return await _context.Client.FirstOrDefaultAsync(client => client.Id == id);
        }


        public async Task<List<Client>> FindAllAsync()
        {         
            var clients = await _context.Client.ToListAsync(); 
            return clients;
        }

        public async Task<List<ClientDTO>> GetAllClientsAsync()
        {
            var clients = await _context.Client.ToListAsync();
            return clients.Select(a => new ClientDTO
            {
                Id = a.Id,
                Name = a.Name,
                Solicitante = a.Solicitante,
            }).ToList();
        }

        // Lista de solicitantes do cliente (padrão primeiro) e o nome a sugerir ao selecionar o cliente.
        // Sugestão: o marcado como padrão; sem padrão, só sugere quando há um único solicitante.
        public async Task<(List<string> Nomes, string Padrao)> GetSolicitantesComPadraoAsync(Client client)
        {
            var lista = await _context.ClienteSolicitante
                .Where(cs => cs.ClientId == client.Id)
                .OrderByDescending(cs => cs.Padrao)
                .ThenBy(cs => cs.Nome)
                .Select(cs => new { cs.Nome, cs.Padrao })
                .ToListAsync();

            var nomes = lista.Select(x => x.Nome).ToList();

            // Fallback legado: cliente ainda sem lista, mas com o solicitante antigo do cadastro
            if (!nomes.Any() && !string.IsNullOrWhiteSpace(client.Solicitante))
                nomes.Add(client.Solicitante.Trim());

            var padrao = lista.FirstOrDefault(x => x.Padrao)?.Nome
                         ?? (nomes.Count == 1 ? nomes[0] : string.Empty);

            return (nomes, padrao);
        }

        // Cadastra o solicitante na lista do cliente caso ainda não exista (comparação ignorando maiúsculas/espaços).
        // O primeiro solicitante do cliente já entra como padrão. Cliente interno não usa a lista.
        public async Task RegistrarSolicitanteSeNovoAsync(int clientId, string? solicitante)
        {
            if (clientId <= 0 || string.IsNullOrWhiteSpace(solicitante))
                return;

            var nome = solicitante.Trim();

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Id == clientId);
            if (client == null || client.ClienteInterno)
                return;

            var existentes = await _context.ClienteSolicitante
                .Where(cs => cs.ClientId == clientId)
                .ToListAsync();

            if (existentes.Any(cs => string.Equals(cs.Nome.Trim(), nome, StringComparison.OrdinalIgnoreCase)))
                return;

            var primeiro = !existentes.Any();
            _context.ClienteSolicitante.Add(new ClienteSolicitante(clientId, nome) { Padrao = primeiro });

            // Campo legado acompanha o padrão
            if (primeiro)
                client.Solicitante = nome;

            await _context.SaveChangesAsync();
        }

        public async Task<string> GetSolicitanteByClientIdAsync(int clientId)
        {
            var client = await _context.Client
                .Where(c => c.Id == clientId)
                .FirstOrDefaultAsync();

            if (client != null)
            {
                // Retorne o solicitante (ajuste conforme seu modelo de dados)
                return client.Solicitante;
            }

            return string.Empty;
        }


    }
}
