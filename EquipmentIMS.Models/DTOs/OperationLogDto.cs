namespace EquipmentIMS.Models.DTOs;

public class OperationLogDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IP { get; set; }
    public DateTime CreateTime { get; set; }
}

public class OperationLogQueryDto
{
    public string? Keyword { get; set; }
    public string? Module { get; set; }
    public long? UserId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
