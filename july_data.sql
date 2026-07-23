-- Add July 2026 transactions for dashboard metrics
SET NOCOUNT ON;
GO

-- ===== July Purchase Orders + Purchase In =====
DECLARE @nextPOId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Pur_PurchaseOrder);
DECLARE @nextPODetailId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Pur_PurchaseOrderDetail);
DECLARE @nextPIId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Pur_PurchaseIn);
DECLARE @nextPIDetailId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Pur_PurchaseInDetail);

-- PO #31: July 3, Approved
SET IDENTITY_INSERT Pur_PurchaseOrder ON;
INSERT Pur_PurchaseOrder(Id,OrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,TotalAmount,ReceivedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPOId,'PUR-20260703-0031',2,1,'2026-07-03','2026-07-15',128500.00,128500.00,4,N'wangqiang','2026-07-05 10:00:00',NULL,0,'2026-07-03 09:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseOrder OFF;

SET IDENTITY_INSERT Pur_PurchaseOrderDetail ON;
INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPODetailId,@nextPOId,12,30,30,1850.00,55500.00,NULL,0,'2026-07-03 09:00:00',N'zhangwei');
INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPODetailId+1,@nextPOId,25,20,20,1580.00,31600.00,NULL,0,'2026-07-03 09:00:00',N'zhangwei');
INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPODetailId+2,@nextPOId,38,10,10,4140.00,41400.00,NULL,0,'2026-07-03 09:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseOrderDetail OFF;

SET IDENTITY_INSERT Pur_PurchaseIn ON;
INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIId,'PIN-20260708-0031',@nextPOId,2,1,'2026-07-08',128500.00,1,N'zhangwei',NULL,0,'2026-07-08 10:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseIn OFF;

SET IDENTITY_INSERT Pur_PurchaseInDetail ON;
INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIDetailId,@nextPIId,12,30,0,1850.00,55500.00,0,'2026-07-08 10:00:00',N'zhangwei');
INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIDetailId+1,@nextPIId,25,20,0,1580.00,31600.00,0,'2026-07-08 10:00:00',N'zhangwei');
INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIDetailId+2,@nextPIId,38,10,0,4140.00,41400.00,0,'2026-07-08 10:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseInDetail OFF;

-- PO #32: July 10, partial in
SET @nextPOId = @nextPOId + 1;
SET @nextPODetailId = @nextPODetailId + 3;
SET @nextPIId = @nextPIId + 1;
SET @nextPIDetailId = @nextPIDetailId + 3;

SET IDENTITY_INSERT Pur_PurchaseOrder ON;
INSERT Pur_PurchaseOrder(Id,OrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,TotalAmount,ReceivedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPOId,'PUR-20260710-0032',5,2,'2026-07-10','2026-07-22',95680.00,50360.00,3,N'wangqiang','2026-07-11 14:00:00',NULL,0,'2026-07-10 09:00:00',N'likun');
SET IDENTITY_INSERT Pur_PurchaseOrder OFF;

SET IDENTITY_INSERT Pur_PurchaseOrderDetail ON;
INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPODetailId,@nextPOId,45,25,10,4200.00,105000.00,NULL,0,'2026-07-10 09:00:00',N'likun');
INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPODetailId+1,@nextPOId,55,15,8,58.00,870.00,NULL,0,'2026-07-10 09:00:00',N'likun');
INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPODetailId+2,@nextPOId,62,10,5,320.00,3200.00,NULL,0,'2026-07-10 09:00:00',N'likun');
SET IDENTITY_INSERT Pur_PurchaseOrderDetail OFF;

-- Purchase In #2-1 (partial, July 12)
SET IDENTITY_INSERT Pur_PurchaseIn ON;
INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIId,'PIN-20260712-0032',@nextPOId,5,2,'2026-07-12',50360.00,1,N'zhangwei',NULL,0,'2026-07-12 10:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseIn OFF;

SET IDENTITY_INSERT Pur_PurchaseInDetail ON;
INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIDetailId,@nextPIId,45,10,0,4200.00,42000.00,0,'2026-07-12 10:00:00',N'zhangwei');
INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIDetailId+1,@nextPIId,55,8,0,58.00,464.00,0,'2026-07-12 10:00:00',N'zhangwei');
INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIDetailId+2,@nextPIId,62,5,0,320.00,1600.00,0,'2026-07-12 10:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseInDetail OFF;

-- Purchase In #2-2 (partial, July 15)
SET @nextPIId = @nextPIId + 1;
SET @nextPIDetailId = @nextPIDetailId + 3;

SET IDENTITY_INSERT Pur_PurchaseIn ON;
INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIId,'PIN-20260715-0033',@nextPOId-1,5,2,'2026-07-15',45320.00,1,N'zhangwei',NULL,0,'2026-07-15 14:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseIn OFF;

SET IDENTITY_INSERT Pur_PurchaseInDetail ON;
INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy)
VALUES(@nextPIDetailId,@nextPIId,45,15,0,4200.00,45320.00,0,'2026-07-15 14:00:00',N'zhangwei');
SET IDENTITY_INSERT Pur_PurchaseInDetail OFF;

-- Update ReceivedQty for partial PO
UPDATE Pur_PurchaseOrderDetail SET ReceivedQty=25 WHERE Id=@nextPODetailId;
UPDATE Pur_PurchaseOrderDetail SET ReceivedQty=15 WHERE Id=@nextPODetailId+2;
GO

-- ===== July Sale Orders + Sale Out =====
DECLARE @nextSOId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Sale_SaleOrder);
DECLARE @nextSODetailId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Sale_SaleOrderDetail);
DECLARE @nextSOutId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Sale_SaleOut);
DECLARE @nextSOutDetailId INT = (SELECT ISNULL(MAX(Id),0)+1 FROM Sale_SaleOutDetail);

-- SO #36: July 5, completed
SET IDENTITY_INSERT Sale_SaleOrder ON;
INSERT Sale_SaleOrder(Id,OrderNo,CustomerId,OrderDate,ExpectedDate,TotalAmount,ShippedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOId,'SAL-20260705-0036',8,'2026-07-05','2026-07-12',364800.00,364800.00,4,N'wangqiang','2026-07-06 10:00:00',N'急单优先发货',0,'2026-07-05 09:30:00',N'liujie');
SET IDENTITY_INSERT Sale_SaleOrder OFF;

SET IDENTITY_INSERT Sale_SaleOrderDetail ON;
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId,@nextSOId,3,1,10,10,4928.00,49280.00,NULL,0,'2026-07-05 09:30:00',N'liujie');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+1,@nextSOId,8,1,5,5,11392.00,56960.00,NULL,0,'2026-07-05 09:30:00',N'liujie');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+2,@nextSOId,15,1,8,8,2368.00,18944.00,NULL,0,'2026-07-05 09:30:00',N'liujie');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+3,@nextSOId,22,1,12,12,1728.00,20736.00,NULL,0,'2026-07-05 09:30:00',N'liujie');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+4,@nextSOId,35,1,15,15,1856.00,27840.00,NULL,0,'2026-07-05 09:30:00',N'liujie');
SET IDENTITY_INSERT Sale_SaleOrderDetail OFF;

-- Sale Out for SO #36
SET IDENTITY_INSERT Sale_SaleOut ON;
INSERT Sale_SaleOut(Id,OutNo,SaleOrderId,CustomerId,WarehouseId,OutTime,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutId,'SHP-20260707-0037',@nextSOId,8,1,'2026-07-07 14:00:00',364800.00,0,N'liujie',NULL,0,'2026-07-07 14:00:00',N'liujie');
SET IDENTITY_INSERT Sale_SaleOut OFF;

SET IDENTITY_INSERT Sale_SaleOutDetail ON;
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId,@nextSOutId,@nextSODetailId,3,1,10,4928.00,49280.00,NULL,0,'2026-07-07 14:00:00',N'liujie');
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId+1,@nextSOutId,@nextSODetailId+1,8,1,5,11392.00,56960.00,NULL,0,'2026-07-07 14:00:00',N'liujie');
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId+2,@nextSOutId,@nextSODetailId+2,15,1,8,2368.00,18944.00,NULL,0,'2026-07-07 14:00:00',N'liujie');
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId+3,@nextSOutId,@nextSODetailId+3,22,1,12,1728.00,20736.00,NULL,0,'2026-07-07 14:00:00',N'liujie');
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId+4,@nextSOutId,@nextSODetailId+4,35,1,15,1856.00,27840.00,NULL,0,'2026-07-07 14:00:00',N'liujie');
SET IDENTITY_INSERT Sale_SaleOutDetail OFF;

-- SO #37: July 8, completed
SET @nextSOId = @nextSOId + 1;
SET @nextSODetailId = @nextSODetailId + 5;
SET @nextSOutId = @nextSOutId + 1;
SET @nextSOutDetailId = @nextSOutDetailId + 5;

SET IDENTITY_INSERT Sale_SaleOrder ON;
INSERT Sale_SaleOrder(Id,OrderNo,CustomerId,OrderDate,ExpectedDate,TotalAmount,ShippedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOId,'SAL-20260708-0037',15,'2026-07-08','2026-07-16',218560.00,218560.00,4,N'wangqiang','2026-07-09 09:00:00',NULL,0,'2026-07-08 10:00:00',N'chenhui');
SET IDENTITY_INSERT Sale_SaleOrder OFF;

SET IDENTITY_INSERT Sale_SaleOrderDetail ON;
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId,@nextSOId,7,1,12,12,4224.00,50688.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+1,@nextSOId,18,1,20,20,237.00,4740.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+2,@nextSOId,29,1,6,6,7936.00,47616.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+3,@nextSOId,42,2,8,8,5376.00,43008.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');
SET IDENTITY_INSERT Sale_SaleOrderDetail OFF;

SET IDENTITY_INSERT Sale_SaleOut ON;
INSERT Sale_SaleOut(Id,OutNo,SaleOrderId,CustomerId,WarehouseId,OutTime,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutId,'SHP-20260710-0038',@nextSOId,15,1,'2026-07-10 16:00:00',146052.00,0,N'chenhui',NULL,0,'2026-07-10 16:00:00',N'chenhui');
SET IDENTITY_INSERT Sale_SaleOut OFF;

SET IDENTITY_INSERT Sale_SaleOutDetail ON;
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId,@nextSOutId,@nextSODetailId,7,1,12,4224.00,50688.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId+1,@nextSOutId,@nextSODetailId+1,18,1,20,237.00,4740.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId+2,@nextSOutId,@nextSODetailId+2,29,1,6,7936.00,47616.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');
INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOutDetailId+3,@nextSOutId,@nextSODetailId+3,42,2,8,5376.00,43008.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');
SET IDENTITY_INSERT Sale_SaleOutDetail OFF;

-- SO #38: July 12 (SALE with PENDING approval)
SET @nextSOId = @nextSOId + 1;
SET @nextSODetailId = @nextSODetailId + 4;
-- Insert into SaleOrder (no out yet, pending approval)
SET IDENTITY_INSERT Sale_SaleOrder ON;
INSERT Sale_SaleOrder(Id,OrderNo,CustomerId,OrderDate,ExpectedDate,TotalAmount,ShippedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSOId,'SAL-20260712-0038',22,'2026-07-12','2026-07-22',287360.00,0.00,1,'NULL','NULL',N'新客户大单',0,'2026-07-12 08:30:00',N'zhaomin');
SET IDENTITY_INSERT Sale_SaleOrder OFF;

SET IDENTITY_INSERT Sale_SaleOrderDetail ON;
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId,@nextSOId,1,1,20,0,2368.00,47360.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+1,@nextSOId,6,1,10,0,8704.00,87040.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+2,@nextSOId,11,2,15,0,1728.00,25920.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+3,@nextSOId,27,1,8,0,7424.00,59392.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');
INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy)
VALUES(@nextSODetailId+4,@nextSOId,33,1,25,0,166.00,4150.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');
SET IDENTITY_INSERT Sale_SaleOrderDetail OFF;
GO

-- ===== Add Stock Transactions for July data =====
-- Purchase In stock transactions
INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy)
SELECT CONCAT('PIN-TXN-JUL-',pid.Id),1,pid.ProductId,pi.WarehouseId,pid.Qty,
  ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=pid.ProductId AND WarehouseId=pi.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)+pid.Qty,
  'PurchaseIn',pi.Id,pi.InNo,pi.Operator,N'采购入库自动流水',pi.InDate,pi.CreateBy
FROM Pur_PurchaseInDetail pid 
JOIN Pur_PurchaseIn pi ON pid.PurchaseInId=pi.Id 
WHERE pi.InDate>='2026-07-01' AND pi.IsDeleted=0;

-- Sale Out stock transactions  
INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy)
SELECT CONCAT('SHP-TXN-JUL-',sod.Id),3,sod.ProductId,sod.WarehouseId,-sod.Qty,
  ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=sod.ProductId AND WarehouseId=sod.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)-sod.Qty,
  'SaleOut',so.Id,so.OutNo,so.Operator,N'销售出库自动流水',so.OutTime,so.CreateBy
FROM Sale_SaleOutDetail sod 
JOIN Sale_SaleOut so ON sod.SaleOutId=so.Id 
WHERE so.OutTime>='2026-07-01' AND so.IsDeleted=0;
GO

-- ===== Rebuild Base_ProductStock =====
DELETE FROM Base_ProductStock;
INSERT Base_ProductStock(ProductId,WarehouseId,Quantity,LockQty,IsDeleted,CreateTime,CreateBy)
SELECT ProductId,WarehouseId,SUM(Qty) AS Quantity,0,0,MAX(CreateTime),N'system'
FROM Inv_StockTransaction 
GROUP BY ProductId,WarehouseId 
HAVING SUM(Qty)>0;
GO

-- ===== Verify =====
SELECT 'July Summary' AS Info,
  (SELECT ISNULL(SUM(TotalAmount),0) FROM Pur_PurchaseIn WHERE InDate>='2026-07-01' AND IsDeleted=0) AS JulyPurchase,
  (SELECT ISNULL(SUM(TotalAmount),0) FROM Sale_SaleOut WHERE OutTime>='2026-07-01' AND IsDeleted=0) AS JulySales;
GO
