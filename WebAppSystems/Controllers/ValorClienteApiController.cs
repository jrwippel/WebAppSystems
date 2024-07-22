using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Data;
using WebAppSystems.Services;

namespace WebAppSystems.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ValorClienteApiController : ControllerBase
    {

        private readonly WebAppSystemsContext _context;
        private readonly ValorClienteService _valorClienteService;

        public ValorClienteApiController(WebAppSystemsContext context, ValorClienteService valorClienteService)
        {
            _context = context;
            _valorClienteService = valorClienteService;
        }

        [HttpGet]
        [Route("GetValorClient/{clientId}/{userId}")]
        public async Task<IActionResult> GetValorCliente(int clientId, int userId)
        {
            var precoCliente = await _valorClienteService.GetValorForClienteAndUserIdAsync(clientId, userId);

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
}
