namespace EquipmentIMS.Models.DTOs;

public class InitialImportPreviewItemDto
{
    public int Row { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool Valid { get; set; }
    public string? Error { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
}

public class InitialImportPreviewDto
{
    public List<InitialImportPreviewItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
}

public class InitialImportResultDto
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<string> FailItems { get; set; } = new();
    public string? Message { get; set; }
}
