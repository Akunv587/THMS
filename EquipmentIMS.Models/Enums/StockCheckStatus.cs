namespace EquipmentIMS.Models.Enums;

public enum StockCheckStatus : byte
{
    New = 0,
    Checking = 1,
    PendingApproval = 2,
    Approved = 3,
    Cancelled = 4
}
