using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;
using WebAppSystems.Models.Dto;
using WebAppSystems.Services.Exceptions;

namespace WebAppSystems.Services
{
    public class DepartmentService
    {

        private readonly WebAppSystemsContext _context;

        public DepartmentService(WebAppSystemsContext context)
        {
            _context = context;
        }

        public async Task <List<Department>> FindAllAsync()
        {
            return await _context.Department.OrderBy(x => x.Name).ToListAsync();
        }
        public async Task<string> GetDepartmentNameByIdAsync(int? departmentId)
        {
            if (!departmentId.HasValue)
                return null;

            var department = await _context.Department.FindAsync(departmentId.Value);
            return department?.Name.ToUpper(); // Retorne apenas o nome
        }

        public async Task<List<DepartmentDto>> GetAllDepartmentsAsync()
        {
            var departments = await _context.Department.ToListAsync();
            return departments.Select(a => new DepartmentDto
            {
                Id = a.Id,
                Name = a.Name,
            }).ToList();
        }


        public async Task RemoveAsync(int id)
        {
            try
            {
                var obj = await _context.Department.FindAsync(id);
                _context.Remove(obj);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new IntegrityException("Não é possível excluir essa Area, pois possui Usuarios!");
            }

        }
    }
}
