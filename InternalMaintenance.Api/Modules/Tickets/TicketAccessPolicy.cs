using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Services;

namespace InternalMaintenance.Api.Modules.Tickets;

public static class TicketAccessPolicy
{
    public static IQueryable<MaintenanceTicket> Apply(
        IQueryable<MaintenanceTicket> query,
        CurrentUserService currentUserService)
    {
        var role = currentUserService.Role;
        var userId = currentUserService.UserId;
        var departmentId = currentUserService.DepartmentId;

        // Admin xem toàn bộ ticket
        if (role == UserRoles.Admin)
        {
            return query;
        }

        // Manager xem ticket khi thỏa 1 trong 3 điều kiện:
        // 1. Thiết bị thuộc phòng ban mà Manager quản lý (Owning Department)
        // 2. Thiết bị được phòng ban mình phụ trách bảo trì (Maintenance Department)
        // 3. Manager tự tay tạo ticket đó (CreatedByUserId)
        if (role == UserRoles.Manager)
        {
            return query.Where(ticket =>
                ticket.Equipment!.DepartmentId == departmentId ||
                ticket.Equipment!.MaintenanceDepartmentId == departmentId ||
                ticket.CreatedByUserId == userId
            );
        }

        // Staff chỉ xem ticket do chính mình tạo
        if (role == UserRoles.Staff)
        {
            return query.Where(ticket => ticket.CreatedByUserId == userId);
        }

        // Technician xem ticket được giao xử lý HOẶC do chính mình tạo
        if (role == UserRoles.Technician)
        {
            return query.Where(ticket =>
                ticket.AssignedTechnicianId == userId ||
                ticket.CreatedByUserId == userId
            );
        }

        // Role không hợp lệ — trả về rỗng
        return query.Where(ticket => false);
    }

    public static bool CanAccess(
        MaintenanceTicket ticket,
        CurrentUserService currentUserService)
    {
        var role = currentUserService.Role;
        var userId = currentUserService.UserId;
        var departmentId = currentUserService.DepartmentId;

        if (role == UserRoles.Admin)
        {
            return true;
        }

        // Manager có quyền nếu: phòng sở hữu, phòng bảo trì, hoặc tự tạo ticket
        if (role == UserRoles.Manager)
        {
            return ticket.Equipment?.DepartmentId == departmentId ||
                   ticket.Equipment?.MaintenanceDepartmentId == departmentId ||
                   ticket.CreatedByUserId == userId;
        }

        if (role == UserRoles.Staff)
        {
            return ticket.CreatedByUserId == userId;
        }

        // Technician: được giao hoặc tự tạo
        if (role == UserRoles.Technician)
        {
            return ticket.AssignedTechnicianId == userId ||
                   ticket.CreatedByUserId == userId;
        }

        return false;
    }
}
