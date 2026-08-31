using InternalMaintenance.Api.Models;

namespace InternalMaintenance.Api.Services.Interface;

public interface IEmailService
{
    Task SendNearBreachAlertAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task SendBreachedAlertAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task SendEscalationAlertAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task SendSlaResultAsync(MaintenanceTicket ticket, bool isMet, CancellationToken cancellationToken = default);
}
