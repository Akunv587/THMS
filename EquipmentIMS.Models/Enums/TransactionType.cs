namespace EquipmentIMS.Models.Enums;

public enum TransactionType : byte
{
    PurchaseIn = 1,
    PurchaseReturnOut = 2,
    SaleOut = 3,
    SaleReturnIn = 4,
    TransferIn = 5,
    TransferOut = 6,
    StockGain = 7,
    StockLoss = 8,
    InitialImport = 9,
    RepairUse = 10
}
