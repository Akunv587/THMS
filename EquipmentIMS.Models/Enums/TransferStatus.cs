namespace EquipmentIMS.Models.Enums;

public enum TransferStatus : byte
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Completed = 3,
    Cancelled = 4,
    Rejected = 5
}
