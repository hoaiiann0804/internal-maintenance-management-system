using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Vendors;

namespace InternalMaintenance.Api.Modules.Tickets;

public class TicketVendorLog
{
    public int Id { get; set; }

    public int MaintenanceTicketId { get; set; }

    [ForeignKey(nameof(MaintenanceTicketId))]
    public MaintenanceTicket? MaintenanceTicket { get; set; }

    public int VendorId { get; set; }

    [ForeignKey(nameof(VendorId))]
    public Vendor? Vendor { get; set; }

    public DateTime DispatchedAt { get; set; }

    public DateTime EstimatedReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    public int PausedMinutes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RepairCost { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
