namespace EquipmentIMS.Models.DTOs.Reports;

public class PurchaseSummaryDto
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int OrderCount { get; set; }
    public int SupplierCount { get; set; }
}

public class PurchaseReturnStatDto
{
    public int ReturnCount { get; set; }
    public decimal ReturnAmount { get; set; }
    public int PurchaseCount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal ReturnRate { get; set; }
    public List<PurchaseReturnTrendDto> MonthlyTrends { get; set; } = new();
}

public class PurchaseReturnTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal PurchaseAmount { get; set; }
    public decimal ReturnAmount { get; set; }
    public decimal ReturnRate { get; set; }
}
