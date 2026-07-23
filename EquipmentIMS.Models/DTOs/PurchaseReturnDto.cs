using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.DTOs;

public class PurchaseReturnListDto
{
    public long Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public long PurchaseInId { get; set; }
    public string InNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseReturnStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Operator { get; set; }
    public DateTime CreateTime { get; set; }
}

public class PurchaseReturnRequestDto
{
    public long PurchaseInId { get; set; }
    public DateTime ReturnDate { get; set; }
    public string? Reason { get; set; }
    public string? Remark { get; set; }
    public List<PurchaseReturnItemRequestDto> Details { get; set; } = new();
}

public class PurchaseReturnItemRequestDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int InQty { get; set; }
    public int ReturnedQty { get; set; }
    public int RemainingQty { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class PurchaseReturnQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
    public long? SupplierId { get; set; }
    public long? WarehouseId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
