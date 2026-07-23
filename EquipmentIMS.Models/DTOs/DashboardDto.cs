namespace EquipmentIMS.Models.DTOs;

public class DashboardData
{
    public decimal StockTotalValue { get; set; }
    public decimal MonthlySalesAmount { get; set; }
    public decimal MonthlyPurchaseAmount { get; set; }
    public int PendingWorkOrders { get; set; }
    public int LowStockAlertCount { get; set; }
    public int PendingApprovalCount { get; set; }
}

public class SaleTrendData
{
    public List<string> Months { get; set; } = new();
    public List<decimal> Amounts { get; set; } = new();
}

public class StockDistData
{
    public List<string> Categories { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
}

public class PendingApprovalItem
{
    public string? OrderNo { get; set; }
    public string? TypeName { get; set; }
    public decimal Amount { get; set; }
    public string? Creator { get; set; }
    public long OrderId { get; set; }
    public string? OrderType { get; set; }
}
