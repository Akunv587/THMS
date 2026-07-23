namespace EquipmentIMS.Models.DTOs;

public class BackupHistoryDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeStr { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateBy { get; set; } = string.Empty;
}

public class BackupResultDto
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? Message { get; set; }
    public bool Verified { get; set; }
}
