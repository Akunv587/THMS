namespace EquipmentIMS.Models.DTOs.Reports;

public class SaleRankingDto
{
    public string Name { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public int OrderCount { get; set; }
    public int Ranking { get; set; }
}

public class SaleTrendDto
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int OrderCount { get; set; }
}

public class SaleReturnStatDto
{
    public int ReturnCount { get; set; }
    public decimal ReturnAmount { get; set; }
    public int SaleCount { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal ReturnRate { get; set; } // ReturnAmount / SaleAmount
    public List<SaleReturnTrendDto> MonthlyTrends { get; set; } = new();
}

public class SaleReturnTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal SaleAmount { get; set; }
    public decimal ReturnAmount { get; set; }
    public decimal ReturnRate { get; set; }
}
