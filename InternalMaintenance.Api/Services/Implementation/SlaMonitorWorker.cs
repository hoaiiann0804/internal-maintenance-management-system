using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Models;
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
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public SlaMonitorWorker(IServiceScopeFactory scopeFactory, ILogger<SlaMonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitor Worker initialized. Polling interval: {IntervalMinutes} minute(s)", CheckInterval.TotalMinutes);

        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSlaChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred during SLA monitoring cycle");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("SLA Monitor Worker has stopped gracefully.");
    }

    private async Task ProcessSlaChecksAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;

        // Query open tickets with eager loaded navigation properties required for notifications
        var openTickets = await db.MaintenanceTickets
            .Include(t => t.AssignedTechnician)
            .Include(t => t.CreatedByUser)
            .Where(t => t.DueAt != null)
            .Where(t => TicketWorkflowRules.OpenStatuses.Contains(t.Status))
            .Where(t => t.Status != TicketStatuses.WaitingForVendor)
            .ToListAsync(stoppingToken);

        if (openTickets.Count == 0)
        {
            return;
        }

        foreach (var ticket in openTickets)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await EvaluateTicketSlaAsync(ticket, db, emailService, now, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SLA evaluation for ticket #{TicketCode} (ID: {TicketId})",
                    ticket.TicketCode, ticket.Id);
            }
        }
    }

    private async Task EvaluateTicketSlaAsync(
        MaintenanceTicket ticket,
        AppDbContext db,
        IEmailService emailService,
        DateTime now,
        CancellationToken stoppingToken)
    {
        if (!ticket.DueAt.HasValue) return;

        var dueAt = ticket.DueAt.Value;

        // 1. Breached Check (Quá hạn)
        if (now > dueAt && ticket.BreachedNotifiedAt == null)
        {
            ticket.SlaStatus = SlaPolicy.Breached;
            ticket.BreachedNotifiedAt = now;

            await db.SaveChangesAsync(stoppingToken);
            _logger.LogWarning("Ticket #{TicketCode} breached SLA (Due: {DueAt}, Now: {Now}). State persisted.",
                ticket.TicketCode, dueAt, now);

            // Gửi email sau khi DB đã lưu state thành công để đảm bảo Idempotency (không bị lặp spam nếu DB lỗi)
            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendBreachedAlertAsync(ticket, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send breach alert email for ticket #{TicketCode}", ticket.TicketCode);
                }
            }, CancellationToken.None);
        }
        // 2. Near Breach Check (Trong vòng 1 giờ trước hạn chót)
        else if (dueAt - now <= TimeSpan.FromHours(1) && now <= dueAt && ticket.NearBreachNotifiedAt == null)
        {
            ticket.SlaStatus = SlaPolicy.NearBreach;
            ticket.NearBreachNotifiedAt = now;

            await db.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Ticket #{TicketCode} is near SLA breach (Due: {DueAt}). State persisted.",
                ticket.TicketCode, dueAt);

            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendNearBreachAlertAsync(ticket, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send near-breach alert email for ticket #{TicketCode}", ticket.TicketCode);
                }
            }, CancellationToken.None);
        }

        // 3. Escalation Check (Quá hạn > 2 giờ)
        if (now > dueAt.AddHours(2) && ticket.EscalatedNotifiedAt == null)
        {
            ticket.EscalatedNotifiedAt = now;
            await db.SaveChangesAsync(stoppingToken);
            _logger.LogWarning("Ticket #{TicketCode} escalated due to severe breach (>2h overdue). State persisted.",
                ticket.TicketCode);

            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendEscalationAlertAsync(ticket, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send escalation alert email for ticket #{TicketCode}", ticket.TicketCode);
                }
            }, CancellationToken.None);
        }
    }
}
