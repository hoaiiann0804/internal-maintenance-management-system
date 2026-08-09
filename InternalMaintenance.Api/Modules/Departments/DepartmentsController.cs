using InternalMaintenance.Api.Common;
using InternalMaintenance.Api.Common.Pagination;
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Departments.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Modules.Departments;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<DepartmentResponse>>> GetDepartments(
        [FromQuery] DepartmentQuery query)
    {
        var departmentQuery = _context.Departments
            .AsNoTracking()
            .AsQueryable();

        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            departmentQuery = departmentQuery.Where(department => department.Name.Contains(keyword));
        }

        if (query.IsMaintenanceTeam.HasValue)
        {
            departmentQuery = departmentQuery.Where(
                department => department.IsMaintenanceTeam == query.IsMaintenanceTeam.Value);
        }

        var totalItems = await departmentQuery.CountAsync();

        departmentQuery = departmentQuery
            .OrderByDescending(department => department.CreatedAt)
            .ThenBy(department => department.Id);

        departmentQuery = departmentQuery.ApplyPaging(query);

        var departments = await departmentQuery
            .Select(department => new DepartmentResponse
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                IsMaintenanceTeam = department.IsMaintenanceTeam,
                CreatedAt = department.CreatedAt
            })
            .ToListAsync();

        return Ok(departments.ToPagedResponse(query, totalItems));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentResponse>> GetDepartmentById(int id)
    {
        // Lay 1 department theo Id, neu khong co thi tra ve 404.
        var department = await _context.Departments
            .Where(department => department.Id == id)
            .Select(department => new DepartmentResponse
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                IsMaintenanceTeam = department.IsMaintenanceTeam,
                CreatedAt = department.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (department is null)
        {
            return NotFound(new
            {
                message = "Department not found"
            });
        }

        return Ok(department);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment(CreateDepartmentRequest request)
    {
        // Kiem tra ten phong ban da ton tai chua.
        var normalizedName = request.Name.Trim();
        var isDuplicate = await _context.Departments.AnyAsync(d => d.Name == normalizedName);

        if (isDuplicate)
        {
            return BadRequest(new
            {
                message = "Department name already exists"
            });
        }

        var department = new Department
        {
            Name = normalizedName,
            Description = request.Description?.Trim(),
            IsMaintenanceTeam = request.IsMaintenanceTeam
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        var response = new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsMaintenanceTeam = department.IsMaintenanceTeam,
            CreatedAt = department.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetDepartmentById),
            new { id = department.Id },
            response);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(int id, UpdateDepartmentRequest request)
    {
        // Lay department can cap nhat, neu khong co thi tra 404.
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department is null)
        {
            return NotFound(new
            {
                message = "Department not found"
            });
        }

        // Kiem tra ten phong ban co bi trung voi phong ban khac hay khong.
        var normalizedName = request.Name.Trim();
        var isDuplicate = await _context.Departments.AnyAsync(d => d.Id != id && d.Name == normalizedName);

        if (isDuplicate)
        {
            return BadRequest(new
            {
                message = "Department name already exists"
            });
        }

        department.Name = normalizedName;
        department.Description = request.Description?.Trim();
        department.IsMaintenanceTeam = request.IsMaintenanceTeam;

        await _context.SaveChangesAsync();

        var response = new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsMaintenanceTeam = department.IsMaintenanceTeam,
            CreatedAt = department.CreatedAt
        };

        return Ok(response);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Department>> DeleteDepartment(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id);

        if (department is null)
        {
            return NotFound(new
            {
                message = "Department not found"
            });
        }

        // Kiem tra phong ban co user dang thuoc ve hay khong.
        var hasUsers = await _context.Users.AnyAsync(user => user.DepartmentId == id);

        // Kiem tra phong ban co thiet bi nao dang thuoc ve hay khong.
        var hasEquipment = await _context.Equipment.AnyAsync(equipment => equipment.DepartmentId == id);

        // Kiem tra phong ban co bi thiet bi nao dung lam team bao tri hay khong.
        var hasMaintenanceEquipment = await _context.Equipment.AnyAsync(
            equipment => equipment.MaintenanceDepartmentId == id);

        // Neu con du lieu lien quan thi khong cho xoa.
        if (hasUsers || hasEquipment || hasMaintenanceEquipment)
        {
            return BadRequest(new
            {
                message = "Cannot delete department because it has related users or equipment"
            });
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
