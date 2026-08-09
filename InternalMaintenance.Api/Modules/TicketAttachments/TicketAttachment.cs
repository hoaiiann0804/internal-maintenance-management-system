namespace InternalMaintenance.Api.Models;

// Chỉ lưu metadata của file đính kèm; file thật nằm ở R2 storage.
public class TicketAttachment
{
    public int Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageProvider { get; set; } = "R2";
    public string StorageKey { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? FileHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
