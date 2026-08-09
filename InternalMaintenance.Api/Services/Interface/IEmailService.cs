using InternalMaintenance.Api.Models;

namespace InternalMaintenance.Api.Services.Interface;

public interface IEmailService
{
    Task SendNearBreachAlertAsync(MaintenanceTicket ticket);
    Task SendBreachedAlertAsync(MaintenanceTicket ticket);
    Task SendEscalationAlertAsync(MaintenanceTicket ticket);
    Task SendSlaResultAsync(MaintenanceTicket ticket, bool isMet);
}
