namespace EquipmentIMS.Models.Enums;

public enum PurchaseOrderStatus : byte
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    PartialReceived = 3,
    Received = 4,
    Closed = 5,
    Cancelled = 6
}
