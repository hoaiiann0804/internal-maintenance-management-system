namespace InternalMaintenance.Api.Modules.Reports.Contracts;

public class ReportFilterQuery
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? DepartmentId { get; set; }
    public int? TechnicianId { get; set; }
}
