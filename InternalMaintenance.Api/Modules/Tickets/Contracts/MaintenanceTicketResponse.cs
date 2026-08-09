namespace InternalMaintenance.Api.Modules.Tickets.Contracts;

public class MaintenanceTicketResponse
{
    public int Id { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public int EquipmentDepartmentId { get; set; }
    public int? EquipmentMaintenanceDepartmentId { get; set; }

    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    public int? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }

    public int? SupportTechnicianId { get; set; }
    public string? SupportTechnicianName { get; set; }

    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
