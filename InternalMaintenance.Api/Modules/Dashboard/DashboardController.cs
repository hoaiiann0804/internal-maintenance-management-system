using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Dashboard.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace InternalMaintenance.Api.Modules.Dashboard;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUserService;

    public DashboardController(AppDbContext context, CurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private async Task<bool> IsMaintenanceManagerAsync(string role, int? departmentId)
    {
        if (role != "Manager" || !departmentId.HasValue) return false;
        var dept = await _context.Departments.FindAsync(departmentId.Value);
        return dept?.IsMaintenanceTeam ?? false;
    }

    private IQueryable<MaintenanceTicket> GetTicketQuery(string role, int userId, int? userDeptId, bool isMaintenanceManager)
    {
        var query = _context.MaintenanceTickets.AsQueryable();

        if (role == "Technician")
            return query.Where(t => t.AssignedTechnicianId == userId || t.CreatedByUserId == userId);
        
        if (role == "Staff")
            return query.Where(t => t.CreatedByUserId == userId);

        if (role == "Manager" && !isMaintenanceManager)
            return query.Where(t => t.Equipment != null && t.Equipment.DepartmentId == userDeptId);

        // Admin or MaintenanceManager
        return query;
    }

    private IQueryable<Models.Equipment> GetEquipmentQuery(string role, int? userDeptId, bool isMaintenanceManager)
    {
        var query = _context.Equipment.AsQueryable();

        if (role == "Staff" || (role == "Manager" && !isMaintenanceManager))
        {
            if (userDeptId.HasValue)
                return query.Where(e => e.DepartmentId == userDeptId.Value);
            return query.Where(e => false); // Return empty if no department assigned
        }

        // Admin, Technician, or MaintenanceManager
        return query;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary()
    {
        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var userDeptId = _currentUserService.DepartmentId;

        bool isMaintenanceManager = await IsMaintenanceManagerAsync(role, userDeptId);

        var ticketQuery = GetTicketQuery(role, userId, userDeptId, isMaintenanceManager);
        var equipmentQuery = GetEquipmentQuery(role, userDeptId, isMaintenanceManager);

        var totalTickets = await ticketQuery.CountAsync();
        var openTickets = await ticketQuery.CountAsync(t => t.Status != "Closed" && t.Status != "Cancelled" && t.Status != "Resolved");
        var resolvedTickets = await ticketQuery.CountAsync(t => t.Status == "Resolved");
        var closedTickets = await ticketQuery.CountAsync(t => t.Status == "Closed");

        var totalEquipment = await equipmentQuery.CountAsync();
        var activeEquipment = await equipmentQuery.CountAsync(e => e.Status == "Active");
        var underMaintenanceEquipment = await equipmentQuery.CountAsync(e => e.Status == "UnderMaintenance");

        var totalTechnicians = await _context.Users.CountAsync(u => u.Role != null && u.Role.Name == "Technician");
        var totalDepartments = await _context.Departments.CountAsync();

        var response = new DashboardSummaryResponse
        {
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            ResolvedTickets = resolvedTickets,
            ClosedTickets = closedTickets,
            TotalEquipment = totalEquipment,
            ActiveEquipment = activeEquipment,
            UnderMaintenanceEquipment = underMaintenanceEquipment,
            TotalTechnicians = totalTechnicians,
            TotalDepartments = totalDepartments
        };

        return Ok(response);
    }

    [HttpGet("charts")]
    public async Task<ActionResult<ChartDataResponse>> GetCharts()
    {
        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var userDeptId = _currentUserService.DepartmentId;

        bool isMaintenanceManager = await IsMaintenanceManagerAsync(role, userDeptId);

        var ticketQuery = GetTicketQuery(role, userId, userDeptId, isMaintenanceManager);
        var equipmentQuery = GetEquipmentQuery(role, userDeptId, isMaintenanceManager);

        var ticketsByStatus = await ticketQuery
            .GroupBy(t => t.Status)
            .Select(g => new ChartItem { Name = g.Key ?? "Unknown", Value = g.Count() })
            .ToListAsync();

        var ticketsByPriority = await ticketQuery
            .GroupBy(t => t.Priority)
            .Select(g => new ChartItem { Name = g.Key ?? "Unknown", Value = g.Count() })
            .ToListAsync();

        var equipmentStats = await equipmentQuery
            .GroupBy(e => e.DepartmentId)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToListAsync();
            
        var deptIds = equipmentStats.Select(e => e.DeptId).Distinct().ToList();
        var depts = await _context.Departments.Where(d => deptIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name);

        var equipmentByDepartment = equipmentStats.Select(e => new ChartItem 
        { 
            Name = depts.ContainsKey(e.DeptId) ? depts[e.DeptId] : "No Department", 
            Value = e.Count 
        }).ToList();

        var response = new ChartDataResponse
        {
            TicketsByStatus = ticketsByStatus,
            TicketsByPriority = ticketsByPriority,
            EquipmentByDepartment = equipmentByDepartment
        };

        return Ok(response);
    }
}