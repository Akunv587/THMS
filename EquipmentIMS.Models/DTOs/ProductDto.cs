namespace EquipmentIMS.Models.DTOs;

public class ProductDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Brand { get; set; }
    public string? Unit { get; set; }
    public string? BarCode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int MinStock { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}

public class ImportResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<ImportFailItem> FailItems { get; set; } = new();
}

public class ImportFailItem
{
    public int Row { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
