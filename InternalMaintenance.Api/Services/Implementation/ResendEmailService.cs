using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Services.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InternalMaintenance.Api.Services.Implementation;

public class ResendEmailService : IEmailService
{
    public const string HttpClientName = "ResendClient";
    private const string ResendApiUrl = "https://api.resend.com/emails";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        IHttpClientFactory httpClientFactory,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    private static string FormatVnTime(DateTime? dt)
    {
        if (!dt.HasValue) return "N/A";
        return dt.Value.AddHours(7).ToString("dd/MM/yyyy HH:mm");
    }

    private async Task SendEmailAsync(
        string? toEmail,
        string subject,
        string bodyHtml,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Skipping email send because recipient email address is empty. Subject: {Subject}", subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation(
                "Resend API key is not configured (Resend__ApiKey). Skipping email send to {RecipientMasked} with subject {Subject}",
                MaskEmail(toEmail), subject);
            return;
        }

        var senderName = string.IsNullOrWhiteSpace(_options.FromName) ? "Maintenance System" : _options.FromName.Trim();
        var fromAddress = string.IsNullOrWhiteSpace(_options.FromEmail) ? "no-reply@mail.shopmini.io.vn" : _options.FromEmail.Trim();
        var fromHeader = $"{senderName} <{fromAddress}>";

        var payload = new
        {
            from = fromHeader,
            to = new[] { toEmail.Trim() },
            subject = subject,
            html = bodyHtml
        };

        var jsonBody = JsonSerializer.Serialize(payload);
        const int maxAttempts = 3;
        var delay = TimeSpan.FromMilliseconds(500);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientName);
                client.Timeout = TimeSpan.FromSeconds(10);

                using var request = new HttpRequestMessage(HttpMethod.Post, ResendApiUrl)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());

                using var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Successfully dispatched email to {RecipientMasked} (Subject: {Subject}) via Resend on attempt {Attempt}",
                        MaskEmail(toEmail), subject, attempt);
                    return;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Resend API returned non-success HTTP status {StatusCode} on attempt {Attempt}/{MaxAttempts}. Response: {Response}",
                    (int)response.StatusCode, attempt, maxAttempts, errorContent);

                // Do not retry client errors (4xx) except 429 Rate Limit
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && (int)response.StatusCode != 429)
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Email sending operation canceled for recipient {RecipientMasked}", MaskEmail(toEmail));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Transient error occurred while sending email to {RecipientMasked} on attempt {Attempt}/{MaxAttempts}",
                    MaskEmail(toEmail), attempt, maxAttempts);
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff: 0.5s, 2s
            }
        }

        _logger.LogError("Failed to deliver email to {RecipientMasked} after {MaxAttempts} attempts. Subject: {Subject}",
            MaskEmail(toEmail), maxAttempts, subject);
    }

    public async Task SendNearBreachAlertAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        var recipient = ticket.AssignedTechnician?.Email
            ?? _options.FallbackTechnicianEmail
            ?? _options.FallbackManagerEmail;

        var subject = $"[CẢNH BÁO SLA] Ticket #{ticket.TicketCode} sắp trễ hạn xử lý";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: #ea580c; border-bottom: 2px solid #ea580c; padding-bottom: 8px;'>⚠️ Cảnh Báo Hạn Chót SLA</h3>
                <p>Kính gửi Kỹ thuật viên / Quản lý,</p>
                <p>Ticket <b>#{ticket.TicketCode}</b> - <i>{ticket.Title}</i> đang tiến gần đến hạn chót cam kết SLA.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Độ ưu tiên:</td><td style='padding: 8px;'>{ticket.Priority}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Hạn xử lý (Due At):</td><td style='padding: 8px; color: #ea580c; font-weight: bold;'>{FormatVnTime(ticket.DueAt)}</td></tr>
                </table>
                <p>Vui lòng ưu tiên xử lý ticket này để đảm bảo tiêu chuẩn SLA của hệ thống.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, cancellationToken);
    }

    public async Task SendBreachedAlertAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        var recipient = ticket.AssignedTechnician?.Email
            ?? _options.FallbackManagerEmail
            ?? _options.FallbackAdminEmail;

        var subject = $"[VI PHẠM SLA] Ticket #{ticket.TicketCode} ĐÃ QUÁ HẠN XỬ LÝ";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: #dc2626; border-bottom: 2px solid #dc2626; padding-bottom: 8px;'>🚨 Vi Phạm Cam Kết SLA</h3>
                <p>Kính gửi Quản lý & Kỹ thuật viên,</p>
                <p>Ticket <b>#{ticket.TicketCode}</b> - <i>{ticket.Title}</i> hiện tại <b>ĐÃ VƯỢT QUÁ HẠN XỬ LÝ SLA</b>.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Độ ưu tiên:</td><td style='padding: 8px;'>{ticket.Priority}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Hạn xử lý (Due At):</td><td style='padding: 8px; color: #dc2626; font-weight: bold;'>{FormatVnTime(ticket.DueAt)}</td></tr>
                </table>
                <p>Yêu cầu bộ phận kỹ thuật cập nhật trạng thái hoặc điều phối phương án xử lý ngay lập tức.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, cancellationToken);
    }

    public async Task SendEscalationAlertAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        var recipient = _options.FallbackAdminEmail
            ?? _options.FallbackManagerEmail;

        var subject = $"[LEO THANG KHẨN CẤP] Ticket #{ticket.TicketCode} vi phạm SLA nghiêm trọng";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: #7f1d1d; border-bottom: 2px solid #7f1d1d; padding-bottom: 8px;'>🔥 CẢNH BÁO LEO THANG (ESCALATION)</h3>
                <p>Kính gửi Quản trị viên & Ban Quản lý,</p>
                <p>Ticket <b>#{ticket.TicketCode}</b> đã quá hạn SLA hơn <b>2 giờ</b> và chưa hoàn tất xử lý.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Tiêu đề:</td><td style='padding: 8px;'>{ticket.Title}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Kỹ thuật viên phụ trách:</td><td style='padding: 8px;'>{ticket.AssignedTechnician?.FullName ?? "Chưa phân công"}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Hạn xử lý:</td><td style='padding: 8px; color: #7f1d1d; font-weight: bold;'>{FormatVnTime(ticket.DueAt)}</td></tr>
                </table>
                <p>Vui lòng can thiệp trực tiếp để giải quyết sự cố.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, cancellationToken);
    }

    public async Task SendSlaResultAsync(MaintenanceTicket ticket, bool isMet, CancellationToken cancellationToken = default)
    {
        var recipient = ticket.CreatedByUser?.Email;
        if (string.IsNullOrWhiteSpace(recipient)) return;

        var statusText = isMet ? "ĐẠT CHUẨN SLA (MET)" : "QUÁ HẠN SLA (MISSED)";
        var color = isMet ? "#16a34a" : "#dc2626";
        var subject = $"[KẾT QUẢ XỬ LÝ] Ticket #{ticket.TicketCode} đã hoàn tất - {statusText}";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: {color}; border-bottom: 2px solid {color}; padding-bottom: 8px;'>Thông Báo Hoàn Tất Ticket</h3>
                <p>Kính gửi <b>{ticket.CreatedByUser?.FullName ?? "Người yêu cầu"}</b>,</p>
                <p>Yêu cầu bảo trì <b>#{ticket.TicketCode}</b> - <i>{ticket.Title}</i> đã được kỹ thuật viên xử lý hoàn tất.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Đánh giá SLA:</td><td style='padding: 8px; color: {color}; font-weight: bold;'>{statusText}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Ghi chú xử lý:</td><td style='padding: 8px;'>{ticket.ResolutionNote ?? "Đã hoàn thành"}</td></tr>
                </table>
                <p>Cảm ơn bạn đã sử dụng hệ thống bảo trì thiết bị văn phòng.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, cancellationToken);
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 2) return "***" + email[Math.Max(0, atIndex)..];
        return email[..2] + "***" + email[atIndex..];
    }
}
