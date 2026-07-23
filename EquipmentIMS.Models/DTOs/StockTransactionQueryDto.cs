namespace EquipmentIMS.Models.DTOs;

public class StockTransactionQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long? ProductId { get; set; }
    public long? WarehouseId { get; set; }
    public int? TransactionType { get; set; }
    public string? Keyword { get; set; }
}
