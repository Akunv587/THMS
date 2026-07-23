namespace EquipmentIMS.Models.Enums;

public enum RepairOrderStatus : byte
{
    PendingDispatch = 0,
    Dispatched = 1,
    InRepair = 2,
    PendingConfirm = 3,
    Completed = 4,
    Closed = 5,
    Cancelled = 6
}
