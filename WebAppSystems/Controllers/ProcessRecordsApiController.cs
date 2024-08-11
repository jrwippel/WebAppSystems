using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Data;
using WebAppSystems.Models;
using System.Threading.Tasks;
using WebAppSystems.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WebAppSystems.Helper;
using WebAppSystems.Services;
using WebAppSystems.Models.Enums;

[Route("api/[controller]")]
[ApiController]
[Authorize]
//[BasicAuthenticationFilter]
public class ProcessRecordsApiController : ControllerBase
{
    private readonly WebAppSystemsContext _context;

    private readonly AttorneyService _attorneyService;

    public ProcessRecordsApiController(WebAppSystemsContext context, AttorneyService attorneyService)
    {
        _context = context;
        _attorneyService = attorneyService;
    }

    [HttpPost("register")]    
    public async Task<IActionResult> RegisterRecord([FromBody] ProcessRecordInputModel inputModel)
    {
        if (inputModel == null)
        {
            return BadRequest("ProcessRecord cannot be null");
        }
        try
        {
            var processRecord = new ProcessRecord
            {
                Id = inputModel.Id,
                Date = inputModel.Date,
                AttorneyId = inputModel.AttorneyId,
                ClientId = inputModel.ClientId,
                HoraInicial = inputModel.HoraInicial,
                HoraFinal = inputModel.HoraFinal,
                Description = inputModel.Description,
                DepartmentId = inputModel.DepartmentId,
                Solicitante = inputModel.Solicitante,
                RecordType = inputModel.RecordType

            };

            // Carregue as entidades associadas com base nos IDs
            processRecord.Client = await _context.Client.FindAsync(inputModel.ClientId);
            processRecord.Attorney = await _context.Attorney.FindAsync(inputModel.AttorneyId);
            processRecord.Department = await _context.Department.FindAsync(inputModel.DepartmentId);

            // Verifique se as entidades foram carregadas corretamente
            if (processRecord.Client == null || processRecord.Attorney == null || processRecord.Department == null)
            {
                return BadRequest("One or more related entities could not be found.");
            }

            _context.ProcessRecord.Add(processRecord);
            await _context.SaveChangesAsync();

            return Ok(new { status = "Success", message = "Record saved successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "Error", message = ex.Message });
        }
    }

    [HttpGet("lastActivity/{clientId}/{departmentId}")]
    public IActionResult GetUltimaAtividade(int clientId, int departmentId)
    {
        try
        {
            // Encontre o último registro de atividade para o cliente e departamento especificados
            var ultimoRegistro = _context.ProcessRecord
                .Where(x => x.ClientId == clientId && x.DepartmentId == departmentId)
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            if (ultimoRegistro != null)
            {
                // Retorne apenas a descrição da atividade
                return Ok(new { descricaoAtividade = ultimoRegistro.Description, solicitante = ultimoRegistro.Solicitante });
            }
            else
            {
                return NotFound("Nenhum registro encontrado para o cliente e departamento especificados.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "Error", message = ex.Message });
        }
    }
}
