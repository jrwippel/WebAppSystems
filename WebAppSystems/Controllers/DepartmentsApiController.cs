using Microsoft.AspNetCore.Mvc;
using WebAppSystems.Data;
using WebAppSystems.Models;
using System.Threading.Tasks;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using WebAppSystems.Models.Dto;

[Route("api/[controller]")]
[ApiController]
public class DepartmentsApiController : ControllerBase
{
    private readonly WebAppSystemsContext _context;
    private readonly DepartmentService _departmentsService;

    public DepartmentsApiController(WebAppSystemsContext context, DepartmentService departmentService)
    {
        _context = context;
        _departmentsService = departmentService;
    }

    [HttpGet]
    [Route("GetAllDepartments")]
    public async Task<ActionResult<List<DepartmentDto>>> GetAllDepartments()
    {
        return await _departmentsService.GetAllDepartmentsAsync();
    }


}
