using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Tickets;
using InternalMaintenance.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InternalMaintenance.Api.Services.Implementation;

public class SlaMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitorWorker> _logger;

    public SlaMonitorWorker(IServiceScopeFactory scopeFactory, ILogger<SlaMonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitor Worker started.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var now = DateTime.UtcNow;
                var openTickets = await db.MaintenanceTickets
                    .Where(t => t.DueAt != null)
                    .Where(t => TicketWorkflowRules.OpenStatuses.Contains(t.Status))
                    .Where(t => t.Status != TicketStatuses.WaitingForVendor)
                    .ToListAsync(stoppingToken);

                foreach (var ticket in openTickets)
                {
                    // 1. Breached?
                    if (now > ticket.DueAt && ticket.BreachedNotifiedAt == null)
                    {
                        ticket.SlaStatus = SlaPolicy.Breached;
                        ticket.BreachedNotifiedAt = now;
                        await emailService.SendBreachedAlertAsync(ticket);
                        _logger.LogWarning("Ticket {TicketCode} has breached SLA.", ticket.TicketCode);
                    }
                    // 2. Near Breach? (Dưới 1 tiếng)
                    else if (ticket.DueAt.HasValue && ticket.DueAt.Value - now <= TimeSpan.FromHours(1) && now <= ticket.DueAt && ticket.NearBreachNotifiedAt == null)
                    {
                        ticket.SlaStatus = SlaPolicy.NearBreach;
                        ticket.NearBreachNotifiedAt = now;
                        await emailService.SendNearBreachAlertAsync(ticket);
                        _logger.LogInformation("Ticket {TicketCode} is near SLA breach.", ticket.TicketCode);
                    }
                    
                    // 3. Escalation (Quá hạn > 2 tiếng)?
                    if (ticket.DueAt.HasValue && now > ticket.DueAt.Value.AddHours(2) && ticket.EscalatedNotifiedAt == null)
                    {
                        ticket.EscalatedNotifiedAt = now;
                        await emailService.SendEscalationAlertAsync(ticket);
                        _logger.LogWarning("Ticket {TicketCode} escalated due to severe SLA breach.", ticket.TicketCode);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while monitoring SLAs.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        
        _logger.LogInformation("SLA Monitor Worker is stopping.");
    }
}
