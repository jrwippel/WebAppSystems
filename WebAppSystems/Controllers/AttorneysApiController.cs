using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Data;
using WebAppSystems.Models;
using System.Threading.Tasks;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using WebAppSystems.Models.Dto;

[Route("api/[controller]")]
[ApiController]
public class AttorneysApiController : ControllerBase
{
    private readonly WebAppSystemsContext _context;
    private readonly AttorneyService _attorneyService;

    public AttorneysApiController(WebAppSystemsContext
        context, AttorneyService attorneyService)
    {
        _context = context;
        _attorneyService = attorneyService;
    }

    [HttpGet]
    [Route("GetAllAttorneys")]
    public async Task<ActionResult<List<AttorneyDTO>>> GetAllAttorneys()
    {
        return await _attorneyService.GetAllAttorneysAsync();
    }


}
