using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Services;

namespace InternalMaintenance.Api.Modules.Tickets;

public static class TicketAccessPolicy
{
    private static bool IsManagerInScope(
        MaintenanceTicket ticket,
        int? departmentId)
    {
        if (departmentId is null)
        {
            return false;
        }

        return ticket.Equipment?.DepartmentId == departmentId ||
               ticket.CreatedByUser?.DepartmentId == departmentId ||
               ticket.Equipment?.MaintenanceDepartmentId == departmentId;
    }

    public static IQueryable<MaintenanceTicket> Apply(
        IQueryable<MaintenanceTicket> query,
        CurrentUserService currentUserService)
    {
        var role = currentUserService.Role;
        var userId = currentUserService.UserId;
        var departmentId = currentUserService.DepartmentId;

        // Admin xem toan bo ticket.
        if (role == UserRoles.Admin)
        {
            return query;
        }

        // Manager xem ticket khi thoa mot trong ba truong hop:
        // 1. Manager tu tao ticket do.
        // 2. Staff thuoc phong ban cua Manager tao ticket do.
        // 3. Ticket nam trong queue bao tri cua phong ban Manager.
        if (role == UserRoles.Manager)
        {
            return query.Where(ticket =>
                ticket.Equipment!.DepartmentId == departmentId ||
                ticket.CreatedByUserId == userId ||
                ticket.CreatedByUser!.DepartmentId == departmentId ||
                ticket.Equipment!.MaintenanceDepartmentId == departmentId
            );
        }

        // Staff chi xem ticket do chinh minh tao.
        if (role == UserRoles.Staff)
        {
            return query.Where(ticket => ticket.CreatedByUserId == userId);
        }

        // Technician chi xem ticket duoc giao xu ly hoac ticket do chinh minh tao.
        if (role == UserRoles.Technician)
        {
            return query.Where(ticket =>
                ticket.AssignedTechnicianId == userId ||
                ticket.SupportTechnicianId == userId ||
                ticket.CreatedByUserId == userId
            );
        }

        // Role khong hop le -> tra ve tap rong.
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

        // Manager co quyen neu: phong so huu, phong bao tri, hoac tu tao ticket.
        if (role == UserRoles.Manager)
        {
            return ticket.CreatedByUserId == userId ||
                   IsManagerInScope(ticket, departmentId);
        }

        if (role == UserRoles.Staff)
        {
            return ticket.CreatedByUserId == userId;
        }

        // Technician: duoc giao hoac tu tao.
        if (role == UserRoles.Technician)
        {
            return ticket.AssignedTechnicianId == userId ||
                   ticket.SupportTechnicianId == userId ||
                   ticket.CreatedByUserId == userId;
        }

        return false;
    }

    public static bool CanEdit(
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

        if (ticket.CreatedByUserId == userId)
        {
            return ticket.Status == TicketStatuses.Pending;
        }

        if (role != UserRoles.Manager)
        {
            return false;
        }

        return IsManagerInScope(ticket, departmentId);
    }

    public static bool CanAssign(
        MaintenanceTicket ticket,
        CurrentUserService currentUserService)
    {
        var role = currentUserService.Role;
        var departmentId = currentUserService.DepartmentId;

        if (role == UserRoles.Admin)
        {
            return true;
        }

        if (role != UserRoles.Manager || departmentId is null)
        {
            return false;
        }

        return ticket.Equipment?.MaintenanceDepartmentId == departmentId;
    }

    public static bool CanCloseOrCancel(
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

        if (ticket.CreatedByUserId == userId)
        {
            return true;
        }

        if (role != UserRoles.Manager)
        {
            return false;
        }

        return IsManagerInScope(ticket, departmentId);
    }
}
