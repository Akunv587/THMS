namespace EquipmentIMS.Models.DTOs.Reports;

public class ReportExportTaskItem
{
    public Guid TaskId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Param1 { get; set; } // rankBy / statBy / groupBy
    public long? Param2 { get; set; } // warehouseId / categoryId
    public long UserId { get; set; }
}
