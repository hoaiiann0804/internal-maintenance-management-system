using System.Linq.Expressions;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Tickets.Contracts;

namespace InternalMaintenance.Api.Modules.Tickets;

public static class TicketMappingExtensions
{
    /// <summary>
    /// Expression Projection dung trong IQueryable (EF Core LINQ to Entities)
    /// giup toi uu SQL chi query cac cot can thiet tren Database.
    /// </summary>
    public static readonly Expression<Func<MaintenanceTicket, MaintenanceTicketResponse>> ResponseProjection =
        ticket => new MaintenanceTicketResponse
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            Title = ticket.Title,
            Description = ticket.Description,
            EquipmentId = ticket.EquipmentId,
            EquipmentCode = ticket.Equipment != null ? ticket.Equipment.Code : string.Empty,
            EquipmentName = ticket.Equipment != null ? ticket.Equipment.Name : string.Empty,
            EquipmentDepartmentId = ticket.Equipment != null ? ticket.Equipment.DepartmentId : 0,
            EquipmentMaintenanceDepartmentId = ticket.Equipment != null ? ticket.Equipment.MaintenanceDepartmentId : null,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = ticket.CreatedByUser != null ? ticket.CreatedByUser.FullName : string.Empty,
            AssignedTechnicianId = ticket.AssignedTechnicianId,
            AssignedTechnicianName = ticket.AssignedTechnician != null ? ticket.AssignedTechnician.FullName : null,
            SupportTechnicianId = ticket.SupportTechnicianId,
            SupportTechnicianName = ticket.SupportTechnician != null ? ticket.SupportTechnician.FullName : null,
            Priority = ticket.Priority,
            Status = ticket.Status,
            ResolutionNote = ticket.ResolutionNote,
            CancellationReason = ticket.CancellationReason,
            CreatedAt = ticket.CreatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            DueAt = ticket.DueAt,
            SlaStatus = ticket.SlaStatus,
            SlaPausedAt = ticket.SlaPausedAt,
            TotalSlaPausedMinutes = ticket.TotalSlaPausedMinutes,
            VendorLogs = ticket.VendorLogs.Select(vl => new TicketVendorLogResponse
            {
                Id = vl.Id,
                MaintenanceTicketId = vl.MaintenanceTicketId,
                VendorId = vl.VendorId,
                VendorName = vl.Vendor != null ? vl.Vendor.Name : string.Empty,
                DispatchedAt = vl.DispatchedAt,
                EstimatedReturnDate = vl.EstimatedReturnDate,
                ActualReturnDate = vl.ActualReturnDate,
                PausedMinutes = vl.PausedMinutes,
                RepairCost = vl.RepairCost,
                Note = vl.Note,
                CreatedAt = vl.CreatedAt
            }).ToList()
        };

    /// <summary>
    /// Extension method chuyen doi tu MaintenanceTicket Entity sang MaintenanceTicketResponse DTO
    /// </summary>
    public static MaintenanceTicketResponse ToResponse(this MaintenanceTicket ticket)
    {
        return new MaintenanceTicketResponse
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            Title = ticket.Title,
            Description = ticket.Description,
            EquipmentId = ticket.EquipmentId,
            EquipmentCode = ticket.Equipment?.Code ?? string.Empty,
            EquipmentName = ticket.Equipment?.Name ?? string.Empty,
            EquipmentDepartmentId = ticket.Equipment?.DepartmentId ?? 0,
            EquipmentMaintenanceDepartmentId = ticket.Equipment?.MaintenanceDepartmentId,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = ticket.CreatedByUser?.FullName ?? string.Empty,
            AssignedTechnicianId = ticket.AssignedTechnicianId,
            AssignedTechnicianName = ticket.AssignedTechnician?.FullName,
            SupportTechnicianId = ticket.SupportTechnicianId,
            SupportTechnicianName = ticket.SupportTechnician?.FullName,
            Priority = ticket.Priority,
            Status = ticket.Status,
            ResolutionNote = ticket.ResolutionNote,
            CancellationReason = ticket.CancellationReason,
            CreatedAt = ticket.CreatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            DueAt = ticket.DueAt,
            SlaStatus = ticket.SlaStatus,
            SlaPausedAt = ticket.SlaPausedAt,
            TotalSlaPausedMinutes = ticket.TotalSlaPausedMinutes,
            VendorLogs = ticket.VendorLogs.Select(ToResponse).ToList()
        };
    }

    /// <summary>
    /// Extension method chuyen doi tu TicketVendorLog sang TicketVendorLogResponse DTO
    /// </summary>
    public static TicketVendorLogResponse ToResponse(this TicketVendorLog vl)
    {
        return new TicketVendorLogResponse
        {
            Id = vl.Id,
            MaintenanceTicketId = vl.MaintenanceTicketId,
            VendorId = vl.VendorId,
            VendorName = vl.Vendor?.Name ?? string.Empty,
            DispatchedAt = vl.DispatchedAt,
            EstimatedReturnDate = vl.EstimatedReturnDate,
            ActualReturnDate = vl.ActualReturnDate,
            PausedMinutes = vl.PausedMinutes,
            RepairCost = vl.RepairCost,
            Note = vl.Note,
            CreatedAt = vl.CreatedAt
        };
    }

    /// <summary>
    /// Extension method chuyen doi tu TicketComment sang TicketCommentResponse DTO
    /// </summary>
    public static TicketCommentResponse ToResponse(this TicketComment comment)
    {
        return new TicketCommentResponse
        {
            Id = comment.Id,
            UserId = comment.UserId,
            UserName = comment.User?.FullName ?? string.Empty,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }

    /// <summary>
    /// Extension method chuyen doi tu TicketStatusHistory sang TicketStatusHistoryResponse DTO
    /// </summary>
    public static TicketStatusHistoryResponse ToResponse(this TicketStatusHistory history)
    {
        return new TicketStatusHistoryResponse
        {
            Id = history.Id,
            MaintenanceTicketId = history.MaintenanceTicketId,
            OldStatus = history.OldStatus,
            NewStatus = history.NewStatus,
            ChangedByUserId = history.ChangedByUserId,
            ChangedByUserName = history.ChangedByUser?.FullName ?? string.Empty,
            ChangedAt = history.ChangedAt,
            Note = history.Note
        };
    }
}
