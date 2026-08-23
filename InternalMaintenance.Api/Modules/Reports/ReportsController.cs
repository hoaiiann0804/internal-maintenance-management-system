using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Reports.Contracts;
using InternalMaintenance.Api.Modules.Reports.Services;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace InternalMaintenance.Api.Modules.Reports;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUserService;
    private readonly ReportExportService _exportService;

    public ReportsController(
        AppDbContext context,
        CurrentUserService currentUserService,
        ReportExportService exportService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _exportService = exportService;
    }

    private IQueryable<MaintenanceTicket> ApplyReportScope(IQueryable<MaintenanceTicket> query)
    {
        var role = _currentUserService.Role;
        var deptId = _currentUserService.DepartmentId;

        if (role == UserRoles.Admin)
            return query;

        if (role == UserRoles.Manager && deptId.HasValue)
        {
            return query.Where(t => 
                t.Equipment!.DepartmentId == deptId.Value || 
                t.Equipment!.MaintenanceDepartmentId == deptId.Value);
        }

        return query.Where(_ => false);
    }

    private IQueryable<User> ApplyTechnicianScope(IQueryable<User> query)
    {
        var role = _currentUserService.Role;
        var deptId = _currentUserService.DepartmentId;

        if (role == UserRoles.Admin)
            return query;

        if (role == UserRoles.Manager && deptId.HasValue)
        {
            return query.Where(u => u.DepartmentId == deptId.Value);
        }

        return query.Where(_ => false);
    }

    private string? ValidateDateRange(ReportFilterQuery filter)
    {
        if (filter.FromDate.HasValue && filter.FromDate.Value.Date > DateTime.UtcNow.Date)
            return "'Từ ngày' không thể nằm trong tương lai.";

        if (filter.ToDate.HasValue && filter.ToDate.Value.Date > DateTime.UtcNow.Date)
            return "'Đến ngày' không thể nằm trong tương lai.";
        return null;
    }

    [HttpGet("technician-performance")]
    public async Task<ActionResult<List<TechnicianPerformanceReportResponse>>> GetTechnicianPerformance([FromQuery] ReportFilterQuery filter)
    {
        var validation = ValidateDateRange(filter);
        if (validation != null) return BadRequest(new { message = validation });

        var query = _context.MaintenanceTickets
            .Include(t => t.AssignedTechnician)
                .ThenInclude(u => u!.Department)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyReportScope(query);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(t => t.Equipment!.DepartmentId == filter.DepartmentId.Value || t.Equipment!.MaintenanceDepartmentId == filter.DepartmentId.Value);

        if (filter.TechnicianId.HasValue)
            query = query.Where(t => t.AssignedTechnicianId == filter.TechnicianId.Value);

        var tickets = await query
            .Where(t => t.AssignedTechnicianId.HasValue)
            .ToListAsync();

        var techQuery = _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Where(u => u.Role != null && u.Role.Name == UserRoles.Technician)
            .AsNoTracking()
            .AsQueryable();

        techQuery = ApplyTechnicianScope(techQuery);

        if (filter.DepartmentId.HasValue)
            techQuery = techQuery.Where(u => u.DepartmentId == filter.DepartmentId.Value);

        if (filter.TechnicianId.HasValue)
            techQuery = techQuery.Where(u => u.Id == filter.TechnicianId.Value);

        var techUsers = await techQuery.ToListAsync();

        var result = techUsers.Select(tech =>
        {
            var userTickets = tickets.Where(t => t.AssignedTechnicianId == tech.Id).ToList();
            var totalAssigned = userTickets.Count;
            var inProgressCount = userTickets.Count(t => t.Status == TicketStatuses.InProgress);
            var waitingVendorCount = userTickets.Count(t => t.Status == TicketStatuses.WaitingForVendor);
            var resolvedCount = userTickets.Count(t => t.Status == TicketStatuses.Resolved);
            var closedCount = userTickets.Count(t => t.Status == TicketStatuses.Closed);
            var metSlaCount = userTickets.Count(t => t.SlaStatus == SlaPolicy.MetSLA);
            var missedSlaCount = userTickets.Count(t => t.SlaStatus == SlaPolicy.MissedSLA);

            var finishedCount = metSlaCount + missedSlaCount;
            var complianceRate = finishedCount > 0 ? Math.Round((double)metSlaCount / finishedCount * 100, 1) : 100.0;

            var resolvedTicketsWithTime = userTickets
                .Where(t => t.ResolvedAt.HasValue)
                .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
                .ToList();

            var avgHours = resolvedTicketsWithTime.Count > 0 ? Math.Round(resolvedTicketsWithTime.Average(), 1) : 0.0;

            return new TechnicianPerformanceReportResponse
            {
                TechnicianId = tech.Id,
                FullName = tech.FullName,
                Email = tech.Email,
                DepartmentName = tech.Department?.Name ?? "N/A",
                TotalAssigned = totalAssigned,
                InProgressCount = inProgressCount,
                WaitingForVendorCount = waitingVendorCount,
                TotalResolved = resolvedCount,
                TotalClosed = closedCount,
                MetSlaCount = metSlaCount,
                MissedSlaCount = missedSlaCount,
                SlaComplianceRate = complianceRate,
                AvgResolutionHours = avgHours
            };
        })
        .OrderByDescending(r => r.TotalAssigned)
        .ToList();

        return Ok(result);
    }

    [HttpGet("sla-compliance")]
    public async Task<ActionResult<SlaComplianceReportResponse>> GetSlaCompliance([FromQuery] ReportFilterQuery filter)
    {
        var validation = ValidateDateRange(filter);
        if (validation != null) return BadRequest(new { message = validation });

        var query = _context.MaintenanceTickets
            .AsNoTracking()
            .AsQueryable();

        query = ApplyReportScope(query);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(t => t.Equipment!.DepartmentId == filter.DepartmentId.Value || t.Equipment!.MaintenanceDepartmentId == filter.DepartmentId.Value);

        if (filter.TechnicianId.HasValue)
            query = query.Where(t => t.AssignedTechnicianId == filter.TechnicianId.Value);

        var tickets = await query.ToListAsync();

        var total = tickets.Count;
        var inSlaActive = tickets.Count(t => t.SlaStatus == SlaPolicy.InSLA);
        var nearBreach = tickets.Count(t => t.SlaStatus == SlaPolicy.NearBreach);
        var breachedActive = tickets.Count(t => t.SlaStatus == SlaPolicy.Breached);
        var paused = tickets.Count(t => t.SlaStatus == SlaPolicy.Paused);
        var metSla = tickets.Count(t => t.SlaStatus == SlaPolicy.MetSLA);
        var missedSla = tickets.Count(t => t.SlaStatus == SlaPolicy.MissedSLA);

        var finished = metSla + missedSla;
        var overallCompliance = finished > 0 ? Math.Round((double)metSla / finished * 100, 1) : 100.0;

        // Breakdown by Priority
        var priorities = new[] { TicketPriorities.Critical, TicketPriorities.High, TicketPriorities.Medium, TicketPriorities.Low };
        var byPriority = priorities.Select(p =>
        {
            var pTickets = tickets.Where(t => t.Priority == p).ToList();
            var pMet = pTickets.Count(t => t.SlaStatus == SlaPolicy.MetSLA);
            var pMissed = pTickets.Count(t => t.SlaStatus == SlaPolicy.MissedSLA);
            var pFin = pMet + pMissed;
            return new SlaPriorityBreakdown
            {
                Priority = p,
                TotalTickets = pTickets.Count,
                MetSlaCount = pMet,
                MissedSlaCount = pMissed,
                ComplianceRate = pFin > 0 ? Math.Round((double)pMet / pFin * 100, 1) : 100.0
            };
        }).ToList();

        // Monthly trends for the past 6 months
        var now = DateTime.UtcNow;
        var monthlyTrends = new List<MonthlySlaTrend>();
        for (int i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var monthStr = monthDate.ToString("MM/yyyy", CultureInfo.InvariantCulture);
            var mTickets = tickets.Where(t => t.CreatedAt.Month == monthDate.Month && t.CreatedAt.Year == monthDate.Year).ToList();
            var mMet = mTickets.Count(t => t.SlaStatus == SlaPolicy.MetSLA);
            var mMissed = mTickets.Count(t => t.SlaStatus == SlaPolicy.MissedSLA);
            var mFin = mMet + mMissed;

            monthlyTrends.Add(new MonthlySlaTrend
            {
                Month = monthStr,
                TotalResolved = mFin,
                MetSlaCount = mMet,
                MissedSlaCount = mMissed,
                ComplianceRate = mFin > 0 ? Math.Round((double)mMet / mFin * 100, 1) : 100.0
            });
        }

        return Ok(new SlaComplianceReportResponse
        {
            TotalTickets = total,
            InSlaActiveCount = inSlaActive,
            NearBreachCount = nearBreach,
            BreachedActiveCount = breachedActive,
            PausedCount = paused,
            MetSlaCount = metSla,
            MissedSlaCount = missedSla,
            OverallComplianceRate = overallCompliance,
            ByPriority = byPriority,
            MonthlyTrends = monthlyTrends
        });
    }

    [HttpGet("maintenance-cost")]
    public async Task<ActionResult<MaintenanceCostReportResponse>> GetMaintenanceCost([FromQuery] ReportFilterQuery filter)
    {
        var validation = ValidateDateRange(filter);
        if (validation != null) return BadRequest(new { message = validation });

        var logQuery = _context.TicketVendorLogs
            .Include(l => l.Vendor)
            .Include(l => l.MaintenanceTicket)
                .ThenInclude(t => t!.Equipment)
                    .ThenInclude(e => e!.Department)
            .AsNoTracking()
            .AsQueryable();

        var role = _currentUserService.Role;
        var deptId = _currentUserService.DepartmentId;

        if (role == UserRoles.Manager && deptId.HasValue)
        {
            logQuery = logQuery.Where(l => 
                l.MaintenanceTicket!.Equipment!.DepartmentId == deptId.Value || 
                l.MaintenanceTicket!.Equipment!.MaintenanceDepartmentId == deptId.Value);
        }

        if (filter.FromDate.HasValue)
            logQuery = logQuery.Where(l => l.DispatchedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            logQuery = logQuery.Where(l => l.DispatchedAt <= filter.ToDate.Value);

        if (filter.DepartmentId.HasValue)
            logQuery = logQuery.Where(l => l.MaintenanceTicket!.Equipment!.DepartmentId == filter.DepartmentId.Value || l.MaintenanceTicket!.Equipment!.MaintenanceDepartmentId == filter.DepartmentId.Value);

        var logs = await logQuery.ToListAsync();

        var totalCost = logs.Sum(l => l.RepairCost ?? 0);
        var totalDispatches = logs.Count;
        var avgCost = totalDispatches > 0 ? totalCost / totalDispatches : 0;

        // Monthly breakdown
        var now = DateTime.UtcNow;
        var monthlyCosts = new List<MonthlyCostItem>();
        for (int i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var monthStr = monthDate.ToString("MM/yyyy", CultureInfo.InvariantCulture);
            var mLogs = logs.Where(l => l.DispatchedAt.Month == monthDate.Month && l.DispatchedAt.Year == monthDate.Year).ToList();

            monthlyCosts.Add(new MonthlyCostItem
            {
                Month = monthStr,
                TotalCost = mLogs.Sum(l => l.RepairCost ?? 0),
                TicketCount = mLogs.Count
            });
        }

        // Department breakdown
        var deptQuery = _context.Departments.AsNoTracking().AsQueryable();
        if (role == UserRoles.Manager && deptId.HasValue)
        {
            deptQuery = deptQuery.Where(d => d.Id == deptId.Value);
        }
        var depts = await deptQuery.ToListAsync();

        var deptCosts = depts.Select(d =>
        {
            var dLogs = logs.Where(l => l.MaintenanceTicket?.Equipment?.DepartmentId == d.Id).ToList();
            return new DepartmentCostItem
            {
                DepartmentId = d.Id,
                DepartmentName = d.Name,
                TotalCost = dLogs.Sum(l => l.RepairCost ?? 0),
                TicketCount = dLogs.Count
            };
        })
        .Where(d => d.TotalCost > 0 || d.TicketCount > 0)
        .OrderByDescending(d => d.TotalCost)
        .ToList();

        // Vendor breakdown
        var vendors = await _context.Vendors.AsNoTracking().ToListAsync();
        var vendorCosts = vendors.Select(v =>
        {
            var vLogs = logs.Where(l => l.VendorId == v.Id).ToList();
            return new VendorCostItem
            {
                VendorId = v.Id,
                VendorName = v.Name,
                TotalCost = vLogs.Sum(l => l.RepairCost ?? 0),
                DispatchCount = vLogs.Count
            };
        })
        .Where(v => v.TotalCost > 0 || v.DispatchCount > 0)
        .OrderByDescending(v => v.TotalCost)
        .ToList();

        // Top Costly Equipment
        var equipQuery = _context.Equipment
            .Include(e => e.Department)
            .AsNoTracking()
            .AsQueryable();

        if (role == UserRoles.Manager && deptId.HasValue)
        {
            equipQuery = equipQuery.Where(e => e.DepartmentId == deptId.Value || e.MaintenanceDepartmentId == deptId.Value);
        }

        var equipmentList = await equipQuery.ToListAsync();

        var costlyEquipment = equipmentList.Select(e =>
        {
            var eLogs = logs.Where(l => l.MaintenanceTicket?.EquipmentId == e.Id).ToList();
            return new CostlyEquipmentItem
            {
                EquipmentId = e.Id,
                EquipmentCode = e.Code,
                EquipmentName = e.Name,
                DepartmentName = e.Department?.Name ?? "N/A",
                TotalRepairCost = eLogs.Sum(l => l.RepairCost ?? 0),
                MaintenanceCount = eLogs.Count
            };
        })
        .Where(e => e.TotalRepairCost > 0 || e.MaintenanceCount > 0)
        .OrderByDescending(e => e.TotalRepairCost)
        .Take(5)
        .ToList();

        return Ok(new MaintenanceCostReportResponse
        {
            TotalMaintenanceCost = totalCost,
            TotalVendorDispatches = totalDispatches,
            AvgCostPerTicket = avgCost,
            MonthlyCosts = monthlyCosts,
            DepartmentCosts = deptCosts,
            VendorCosts = vendorCosts,
            TopCostlyEquipment = costlyEquipment
        });
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] ReportFilterQuery filter)
    {
        var techResp = await GetTechnicianPerformance(filter);
        var techData = (techResp.Result as OkObjectResult)?.Value as List<TechnicianPerformanceReportResponse> ?? new();

        var slaResp = await GetSlaCompliance(filter);
        var slaData = (slaResp.Result as OkObjectResult)?.Value as SlaComplianceReportResponse ?? new();

        var costResp = await GetMaintenanceCost(filter);
        var costData = (costResp.Result as OkObjectResult)?.Value as MaintenanceCostReportResponse ?? new();

        var fileBytes = _exportService.ExportAllReportsToExcel(techData, slaData, costData, filter);
        var fileName = $"BaoCao_BaoTri_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
