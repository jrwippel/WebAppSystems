using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Data;
using WebAppSystems.Models;
using System.Threading.Tasks;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using WebAppSystems.Models.Dto;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class PrecoClienteApiController : ControllerBase
{
    private readonly WebAppSystemsContext _context;
    private readonly PrecoClienteService _precoClienteService;

    public PrecoClienteApiController(WebAppSystemsContext context, PrecoClienteService precoClienteService)
    {
        _context = context;
        _precoClienteService = precoClienteService;
    }

    [HttpGet]
    [Route("GetPrecoClient/{clientId}/{departmentId}")]
    public async Task<IActionResult> GetPrecoCliente(int clientId, int departmentId)
    {
        var precoCliente = await _precoClienteService.GetPrecoForClienteAndDepartmentIdAsync(clientId, departmentId);

        if (precoCliente != null)
        {
            return Ok(precoCliente); // Retorna 200 OK com o PrecoCliente
        }
        else
        {
            return NotFound(); // Retorna 404 Not Found se o PrecoCliente não for encontrado
        }
    }



}
