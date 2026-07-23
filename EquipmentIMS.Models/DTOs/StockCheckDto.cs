namespace EquipmentIMS.Models.DTOs;

public class StockCheckDto
{
    public long Id { get; set; }
    public string CheckNo { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateTime CheckDate { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public bool IsLocked { get; set; }
    public decimal TotalDiffAmount { get; set; }
    public string? Approver { get; set; }
    public DateTime? ApproveTime { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public List<StockCheckDetailDto> Details { get; set; } = new();
}

public class StockCheckDetailDto
{
    public long Id { get; set; }
    public long StockCheckId { get; set; }
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public int SystemQty { get; set; }
    public int? ActualQty { get; set; }
    public int DiffQty { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Remark { get; set; }
}

public class StockCheckQueryDto
{
    public int? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class StockCheckListDto
{
    public long Id { get; set; }
    public string CheckNo { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime CheckDate { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public int TotalProducts { get; set; }
    public int CheckedCount { get; set; }
    public decimal TotalDiffAmount { get; set; }
    public string? Operator { get; set; }
    public DateTime CreateTime { get; set; }
}

public class StockCheckCreateDto
{
    public long WarehouseId { get; set; }
    public string? Remark { get; set; }
}

public class StockCheckRecordDto
{
    public long DetailId { get; set; }
    public int ActualQty { get; set; }
    public string? Remark { get; set; }
}
