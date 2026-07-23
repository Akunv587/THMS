namespace EquipmentIMS.Models.DTOs;

public class StockTransferDto
{
    public long Id { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public long FromWarehouseId { get; set; }
    public string? FromWarehouseName { get; set; }
    public long ToWarehouseId { get; set; }
    public string? ToWarehouseName { get; set; }
    public DateTime TransferDate { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public string? Approver { get; set; }
    public DateTime? ApproveTime { get; set; }
    public string? RejectReason { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public List<StockTransferDetailDto> Details { get; set; } = new();
}

public class StockTransferDetailDto
{
    public long Id { get; set; }
    public long TransferId { get; set; }
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public int Qty { get; set; }
    public int AvailableQty { get; set; }
    public string? Remark { get; set; }
}

public class StockTransferQueryDto
{
    public int? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class StockTransferListDto
{
    public long Id { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public string FromWarehouseName { get; set; } = string.Empty;
    public string ToWarehouseName { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int TotalQty { get; set; }
    public string? Operator { get; set; }
    public DateTime CreateTime { get; set; }
}

public class StockTransferCreateDto
{
    public long FromWarehouseId { get; set; }
    public long ToWarehouseId { get; set; }
    public string? Remark { get; set; }
    public List<StockTransferItemDto> Items { get; set; } = new();
}

public class StockTransferItemDto
{
    public long ProductId { get; set; }
    public int Qty { get; set; }
    public string? Remark { get; set; }
}

public class StockTransferProductDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int AvailableQty { get; set; }
    public decimal UnitPrice { get; set; }
}
