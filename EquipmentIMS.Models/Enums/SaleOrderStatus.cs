namespace EquipmentIMS.Models.Enums;

public enum SaleOrderStatus : byte
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    PartialShipped = 3,
    Shipped = 4,
    Returned = 5,
    Cancelled = 6
}
