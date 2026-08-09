using InternalMaintenance.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;

namespace InternalMaintenance.Api.Services.Implementation;

public class SmtpEmailService : Interface.IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // Hàm tiện ích chuyển UTC sang giờ Việt Nam (GMT+7) và format đẹp
    private string FormatVnTime(DateTime? dt)
    {
        if (!dt.HasValue) return "N/A";
        return dt.Value.AddHours(7).ToString("dd/MM/yyyy HH:mm");
    }

    private async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        try
        {
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromAddress = _configuration["Smtp:FromAddress"] ?? "no-reply@internalmaintenance.local";
            
            // Nếu không cấu hình Host thì skip (dùng cho môi trường dev không cần gửi email thật)
            if (string.IsNullOrEmpty(host))
            {
                _logger.LogInformation("SMTP Host is not configured. Skipping email send to {ToEmail}", toEmail);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Maintenance System", fromAddress));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Successfully sent email to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
        }
    }

    public async Task SendNearBreachAlertAsync(MaintenanceTicket ticket)
    {
        // Trong thực tế sẽ lấy email từ ticket.AssignedTechnician.Email
        var subject = $"[CẢNH BÁO SLA] Ticket {ticket.TicketCode} sắp trễ hạn xử lý";
        var body = $@"
            <h3 style='color:orange;'>Cảnh báo hạn chót SLA</h3>
            <p>Ticket <b>{ticket.TicketCode}</b> - {ticket.Title}</p>
            <p>Thời hạn xử lý (Due At): <b>{FormatVnTime(ticket.DueAt)}</b>.</p>
            <p>Vui lòng ưu tiên xử lý ticket này càng sớm càng tốt để tránh vi phạm SLA.</p>";
            
        await SendEmailAsync("technician@example.com", subject, body);
    }

    public async Task SendBreachedAlertAsync(MaintenanceTicket ticket)
    {
        var subject = $"[VI PHẠM SLA] Ticket {ticket.TicketCode} đã quá hạn xử lý";
        var body = $@"
            <h3 style='color:red;'>Vi phạm SLA!</h3>
            <p>Ticket <b>{ticket.TicketCode}</b> - {ticket.Title}</p>
            <p>Thời hạn xử lý (Due At): <b>{FormatVnTime(ticket.DueAt)}</b>.</p>
            <p>Ticket này hiện tại đã <b>QUÁ HẠN</b>! Đề nghị Kỹ thuật viên xử lý ngay lập tức.</p>";
            
        await SendEmailAsync("manager@example.com", subject, body);
    }

    public async Task SendEscalationAlertAsync(MaintenanceTicket ticket)
    {
        var subject = $"[LEO THANG] Ticket {ticket.TicketCode} vi phạm SLA nghiêm trọng";
        var body = $@"
            <h3 style='color:red;'>CẢNH BÁO LEO THANG (ESCALATION)</h3>
            <p>Ticket <b>{ticket.TicketCode}</b> - {ticket.Title}</p>
            <p>Hạn chót: <b>{FormatVnTime(ticket.DueAt)}</b>.</p>
            <p>Ticket này đã quá hạn nghiêm trọng và vẫn chưa được xử lý. Yêu cầu Quản trị viên/Quản lý can thiệp khẩn cấp.</p>";
            
        await SendEmailAsync("admin@example.com", subject, body);
    }

    public async Task SendSlaResultAsync(MaintenanceTicket ticket, bool isMet)
    {
        var status = isMet ? "ĐẠT (MET)" : "TRỄ HẠN (MISSED)";
        var color = isMet ? "green" : "red";
        var subject = $"[KẾT QUẢ SLA] Ticket {ticket.TicketCode} đã hoàn thành - {status}";
        var body = $@"
            <h3 style='color:{color};'>Thông báo hoàn thành Ticket</h3>
            <p>Ticket <b>{ticket.TicketCode}</b> - {ticket.Title} đã được xử lý xong.</p>
            <p>Kết quả đánh giá SLA: <strong style='color:{color};'>{status}</strong></p>";
            
        await SendEmailAsync("requester@example.com", subject, body);
    }
}
