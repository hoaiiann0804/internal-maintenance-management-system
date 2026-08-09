namespace InternalMaintenance.Api.Models;

// MaintenanceTicket = phieu bao loi / yeu cau bao tri.
// Staff tao ticket, sau do Admin/Manager phan cong technician xu ly.
public class MaintenanceTicket
{
    public int Id { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int EquipmentId { get; set; }

    // Thong tin chi tiet cua thiet bi; chi co khi query co Include.
    public Equipment? Equipment { get; set; }

    // Nguoi tao ticket.
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    // Technician chinh phu trach ticket.
    public int? AssignedTechnicianId { get; set; }
    public User? AssignedTechnician { get; set; }

    // Technician ho tro them neu can.
    public int? SupportTechnicianId { get; set; }
    public User? SupportTechnician { get; set; }

    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";
    public string? ResolutionNote { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<TicketStatusHistory> StatusHistories { get; set; } = new List<TicketStatusHistory>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
