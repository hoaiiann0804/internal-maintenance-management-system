namespace InternalMaintenance.Api.Constants;

public static class SlaPolicy
{
    // Cấu hình thời gian SLA tính bằng giờ tùy theo mức độ ưu tiên
    public static readonly Dictionary<string, int> SlaHoursMap = new()
    {
        { TicketPriorities.Critical, 2 },
        { TicketPriorities.High, 8 },
        { TicketPriorities.Medium, 24 },
        { TicketPriorities.Low, 48 },
    };

    // Các trạng thái SLA
    public const string InSLA = "InSLA";
    public const string NearBreach = "NearBreach";
    public const string Breached = "Breached";
    public const string MetSLA = "MetSLA";
    public const string MissedSLA = "MissedSLA";

    /// <summary>
    /// Tính thời hạn xử lý (DueAt) dựa trên thời gian tạo và mức độ ưu tiên.
    /// </summary>
    public static DateTime CalculateDueAt(DateTime createdAt, string priority)
    {
        var hours = SlaHoursMap.GetValueOrDefault(priority, 24); // Mặc định 24h nếu không tìm thấy
        return createdAt.AddHours(hours);
    }
}
