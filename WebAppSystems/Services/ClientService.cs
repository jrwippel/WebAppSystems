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
            }).ToList();
        }
    }
}
