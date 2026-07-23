namespace EquipmentIMS.Models.DTOs.Reports;

/// <summary>异步导出任务状态</summary>
public enum ExportTaskStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>异步导出任务跟踪 DTO（供前端轮询状态）</summary>
public class ExportTaskDto
{
    public string TaskId { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public ExportTaskStatus Status { get; set; }
    public int Progress { get; set; }
    public string? FileName { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
