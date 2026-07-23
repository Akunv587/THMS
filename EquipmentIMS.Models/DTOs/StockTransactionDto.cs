namespace EquipmentIMS.Models.DTOs;

public class StockTransactionDto
{
    public long Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public int TransactionType { get; set; }
    public string TransactionTypeName { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int Qty { get; set; }
    public int BalanceQty { get; set; }
    public string? RefType { get; set; }
    public long? RefId { get; set; }
    public string? RefNo { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}
