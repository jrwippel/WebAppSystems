using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Data;
using WebAppSystems.Models;
using System.Threading.Tasks;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using WebAppSystems.Models.Dto;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ClientsApiController : ControllerBase
{
    private readonly WebAppSystemsContext _context;
    private readonly ClientService _clientsService;

    public ClientsApiController(WebAppSystemsContext context, ClientService clientService)
    {
        _context = context;
        _clientsService = clientService;
    }

   
    [HttpGet]    
    [Route("GetAllClients")]
    public async Task<ActionResult<List<ClientDTO>>> GetAllClients()
    {
        return await _clientsService.GetAllClientsAsync();
    }

    [HttpGet]
    [Route("GetClientImage/{clientId}")]
    public async Task<IActionResult> GetClientImage(int clientId)
    {
        var client = await _context.Client.FindAsync(clientId);

        if (client == null || client.ImageData == null)
        {
            return NotFound();
        }

        return File(client.ImageData, client.ImageMimeType);
    }


}
