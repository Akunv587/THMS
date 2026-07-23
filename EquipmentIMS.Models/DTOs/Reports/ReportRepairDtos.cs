namespace EquipmentIMS.Models.DTOs.Reports;

public class RepairStatDto
{
    public string GroupLabel { get; set; } = string.Empty;
    public string StatBy { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRepairCost { get; set; }
    public decimal TotalPartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public double AvgHours { get; set; }
}

public class RepairCostAnalysisDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRepairCost { get; set; }
    public decimal TotalPartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public double TotalWorkHours { get; set; }
    public double AvgWorkHours { get; set; }
    public int WarrantyOrders { get; set; }
    public decimal WarrantyRate { get; set; }
    public List<RepairCostDetailDto> Details { get; set; } = new();
}

public class RepairCostDetailDto
{
    public string OrderNo { get; set; } = string.Empty;
    public string? SerialNo { get; set; }
    public string? ProductName { get; set; }
    public string? CustomerName { get; set; }
    public string? TechnicianName { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal RepairCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal WorkHours { get; set; }
    public bool IsWarranty { get; set; }
    public DateTime CreateTime { get; set; }
}

public class PartsUsageDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public int TotalQty { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AvgUnitPrice { get; set; }
}
