using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Program
{
    static StringBuilder sb = new StringBuilder();
    static void L(string s) { sb.AppendLine(s); }
    static Random rnd = new Random(42);

    // (ProductId, PurchasePrice) - from base_data.sql
    static int[] prodIds = new int[]{
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,
        31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,
        61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90
    };
    static decimal[] prodPP = new decimal[]{
        1850,2450,3850,5680,1850,2450,4280,8900,6200,7800,3500,185,350,420,680,6800,9200,185,1250,85,165,
        1350,1850,4200,1580,2250,5800,4500,6200,185,128,520,35,55,680,48,72,
        950,1450,3850,1550,2250,5500,2850,4200,6800,1850,3800,380,520,380,
        820,1350,1250,1850,2800,3800,58,95,
        880,1450,1350,1950,3200,4200,320,38,
        1080,2150,1680,2480,2950,3850,155,178,280,450,
        1050,1450,2350,2580,3950,165,580,220,
        2850,5200,2680,6800,5800
    };

    // Order definitions: (monthOffset, supplierId, warehouseId, status, creator, detailCount, approveOffsetDays)
    // status: 0草稿 1待审批 2已审批 3部分入库 4已入库 5已关闭 6已取消
    static int[] orderStatus = new int[]{0,0,1,1,2,2,2,
        3,3,3,3,
        4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
        5,5,
        6,6};
    static int[] orderSup = new int[]{1,2,3,1,2,3,4, 5,1,6,7, 1,2,3,4,5,6,7,8,1,2,3,4,5,6,7, 8,1, 2,3};
    static int[] orderWh = new int[]{1,1,1,1,1,1,1, 1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 2,3, 1,1};
    static int[] orderDays = new int[]{
        -350,-340,-335,-330,-320,-310,-300,
        -280,-270,-260,-250,
        -240,-230,-220,-210,-200,-190,-180,-170,-160,-150,-140,-130,-120,-110,-100,
        -90,-80,
        -70,-60};
    static int[] orderDetailCt = new int[]{4,3,5,4,6,5,3, 5,4,6,3, 5,4,6,5,3,5,7,4,6,5,4,3,8,5,4, 3,4, 5,4};

    // which orders have partial/multiple PurchaseIn
    static HashSet<int> partialInOrders = new HashSet<int>{8,9,10,11,12,13}; // 1-based (6 orders x2 = 12 records; others x1 = 16 records; total 28)
    // which orders have PurchaseReturn
    static int[] returnOrders = new int[]{14,19,22,24}; // 1-based (all status 4 已入库)
    // 3 partial in: order8->2 in, order9->2 in, order10->2 in, order11->2 in (total 28 In records)

    static void Main(string[] args)
    {
        string file = @"c:\Users\bey\Desktop\THMS\purchase_data.sql";
        string today = "2026-07-17";

        // Header
        L("SET NOCOUNT ON;");
        L("BEGIN TRY");
        L("BEGIN TRAN;");
        L("");

        int orderCount = 30;
        int nextDetailId = 1;
        int nextInId = 1;
        int nextInDetailId = 1;
        int nextReturnId = 1;
        int nextReturnDetailId = 1;

        // Pre-compute order data
        var orders = new List<OrderData>();
        for (int i = 0; i < orderCount; i++)
        {
            int orderId = i + 1;
            DateTime baseDate = DateTime.Parse(today).AddDays(orderDays[i]);
            string orderNo = $"PUR-{baseDate:yyyyMMdd}-{orderId:0000}";
            string orderDate = baseDate.ToString("yyyy-MM-dd");
            int status = orderStatus[i];
            int supId = orderSup[i];
            int whId = orderWh[i];

            string creator = "zhangwei"; // warehouse manager creates purchase orders
            string approver = "NULL";
            string approveTime = "NULL";
            if (status >= 2) // approved
            {
                approver = "N'wangqiang'";
                DateTime appDate = baseDate.AddDays(1 + rnd.Next(2));
                approveTime = "'" + appDate.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }
            string expectedDate = baseDate.AddDays(5 + rnd.Next(10)).ToString("yyyy-MM-dd");

            int detailCt = orderDetailCt[i];
            decimal totalAmount = 0;
            var details = new List<DetailData>();
            var usedProds = new HashSet<int>();
            for (int d = 0; d < detailCt; d++)
            {
                int pi;
                do { pi = rnd.Next(90); } while (usedProds.Contains(prodIds[pi]));
                usedProds.Add(prodIds[pi]);
                int pid = prodIds[pi];
                decimal pp = prodPP[pi] * (0.95m + (decimal)rnd.NextDouble() * 0.10m); // +/-5%
                pp = Math.Round(pp, 2);
                int qty = 10 + rnd.Next(191); // 10-200
                decimal amt = Math.Round(pp * qty, 2);
                totalAmount += amt;
                details.Add(new DetailData{ ProductId=pid, Qty=qty, UnitPrice=pp, Amount=amt });
            }
            totalAmount = Math.Round(totalAmount, 2);

            orders.Add(new OrderData{
                Id=orderId, OrderNo=orderNo, SupId=supId, WhId=whId,
                OrderDate=orderDate, ExpectedDate=expectedDate,
                TotalAmount=totalAmount, Status=status,
                Approver=approver, ApproveTime=approveTime,
                Creator=creator, Details=details
            });
        }

        // ===== 1. Pur_PurchaseOrder =====
        L("-- ===== 1. Pur_PurchaseOrder (30) =====");
        L("SET IDENTITY_INSERT Pur_PurchaseOrder ON;");
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            string status = o.Status.ToString();
            string remark = "NULL";
            if (o.Status == 5) remark = "N'手动关闭，无需入库'";
            else if (o.Status == 6) remark = "N'采购计划变更，已取消'";

            L(string.Format("INSERT Pur_PurchaseOrder(Id,OrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,TotalAmount,ReceivedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES({0},'{1}',{2},{3},'{4}','{5}',{6},0,{7},{8},{9},{10},0,'{11}',N'{12}');",
                o.Id, o.OrderNo, o.SupId, o.WhId, o.OrderDate, o.ExpectedDate, o.TotalAmount,
                status, o.Approver, o.ApproveTime, remark,
                o.OrderDate + " 09:00:00", o.Creator));
        }
        L("SET IDENTITY_INSERT Pur_PurchaseOrder OFF;");
        L("GO");
        L("");

        // ===== 2. Pur_PurchaseOrderDetail =====
        L("-- ===== 2. Pur_PurchaseOrderDetail =====");
        L("SET IDENTITY_INSERT Pur_PurchaseOrderDetail ON;");
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            for (int d = 0; d < o.Details.Count; d++)
            {
                var det = o.Details[d];
                L(string.Format("INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark) VALUES({0},{1},{2},{3},0,{4},{5},NULL);",
                    nextDetailId++, o.Id, det.ProductId, det.Qty, det.UnitPrice, det.Amount));
            }
        }
        L("SET IDENTITY_INSERT Pur_PurchaseOrderDetail OFF;");
        L("GO");
        L("");

        // ===== 3 & 4. Pur_PurchaseIn + Detail =====
        L("-- ===== 3. Pur_PurchaseIn (28) =====");
        L("SET IDENTITY_INSERT Pur_PurchaseIn ON;");
        L("-- ===== 4. Pur_PurchaseInDetail =====");
        L("SET IDENTITY_INSERT Pur_PurchaseInDetail ON;");

        // Track ReceivedQty for each order detail: map<(orderId,productId), receivedQty>
        var receivedMap = new Dictionary<string, int>();
        // Track In dates for return validation
        var inDates = new Dictionary<int, DateTime>();

        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            if (o.Status == 0 || o.Status == 1 || o.Status == 5 || o.Status == 6) continue; // no In for draft/pending/cancelled

            DateTime orderDate = DateTime.Parse(o.OrderDate);
            DateTime inDate;
            int inCount = 1;
            if (partialInOrders.Contains(o.Id)) inCount = 2;

            for (int part = 0; part < inCount; part++)
            {
                int inId = nextInId++;
                if (inCount == 1)
                    inDate = orderDate.AddDays(3 + rnd.Next(10));
                else if (part == 0)
                    inDate = orderDate.AddDays(3 + rnd.Next(5));
                else
                    inDate = orderDate.AddDays(12 + rnd.Next(8));

                inDates[inId] = inDate;
                string inNo = $"PIN-{inDate:yyyyMMdd}-{inId:0000}";

                decimal inTotal = 0;
                var inDetails = new List<DetailData>();
                for (int d = 0; d < o.Details.Count; d++)
                {
                    var det = o.Details[d];
                    int recvQty;
                    if (inCount == 1)
                        recvQty = det.Qty; // full receipt
                    else if (part == 0)
                        recvQty = rnd.Next(det.Qty / 3, det.Qty * 2 / 3);
                    else
                    {
                        string key = o.Id + "_" + det.ProductId;
                        int alreadyRecv = receivedMap.ContainsKey(key) ? receivedMap[key] : 0;
                        recvQty = det.Qty - alreadyRecv; // remaining
                    }

                    if (recvQty <= 0) continue; // skip empty lines

                    string key2 = o.Id + "_" + det.ProductId;
                    if (!receivedMap.ContainsKey(key2)) receivedMap[key2] = 0;
                    receivedMap[key2] += recvQty;

                    decimal dp = det.UnitPrice;
                    decimal damt = Math.Round(dp * recvQty, 2);
                    inTotal += damt;

                    L(string.Format("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,UnitPrice,Amount) VALUES({0},{1},{2},{3},{4},{5});",
                        nextInDetailId++, inId, det.ProductId, recvQty, dp, damt));
                }
                inTotal = Math.Round(inTotal, 2);
                int inStatus = 1; // 已入库 (may be changed to 2 later for returns)

                L(string.Format("INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({0},'{1}',{2},{3},{4},'{5}',{6},1,N'zhangwei',NULL,0,'{7} 10:00:00',N'zhangwei');",
                    inId, inNo, o.Id, o.SupId, o.WhId, inDate.ToString("yyyy-MM-dd"), inTotal, inDate.ToString("yyyy-MM-dd")));
            }
        }
        L("SET IDENTITY_INSERT Pur_PurchaseInDetail OFF;");
        L("SET IDENTITY_INSERT Pur_PurchaseIn OFF;");
        L("GO");
        L("");

        // Update ReceivedQty on order details
        L("-- ===== Update ReceivedQty on OrderDetails =====");
        foreach (var kv in receivedMap)
        {
            string[] parts = kv.Key.Split('_');
            int oid = int.Parse(parts[0]);
            int pid = int.Parse(parts[1]);
            L(string.Format("UPDATE Pur_PurchaseOrderDetail SET ReceivedQty={0} WHERE PurchaseOrderId={1} AND ProductId={2};",
                kv.Value, oid, pid));
        }
        L("");

        // Update ReceivedAmount on orders
        L("-- ===== Update ReceivedAmount on Orders =====");
        for (int i = 0; i < orderCount; i++)
        {
            L(string.Format("UPDATE Pur_PurchaseOrder SET ReceivedAmount=(SELECT ISNULL(SUM(pi.TotalAmount),0) FROM Pur_PurchaseIn pi WHERE pi.PurchaseOrderId={0} AND pi.IsDeleted=0) WHERE Id={0};",
                i + 1));
        }
        L("");

        // ===== 5 & 6. Pur_PurchaseReturn + Detail =====
        L("-- ===== 5. Pur_PurchaseReturn (4) =====");
        L("SET IDENTITY_INSERT Pur_PurchaseReturn ON;");
        L("-- ===== 6. Pur_PurchaseReturnDetail =====");
        L("SET IDENTITY_INSERT Pur_PurchaseReturnDetail ON;");

        string[] returnReasons = new string[]{"N'规格不符，需退换'", "N'外观有划痕损伤'", "N'参数实测异常'", "N'质检验收不合格'"};

        // Map: which InId for each return order
        // We need to pick an existing InId for each return order
        var orderInIds = new Dictionary<int, List<int>>();
        for (int i = 0; i < orderCount; i++)
        {
            int oid = i + 1;
            // Find all InIds for this order by checking which In records we created
            // Our inIds were sequential starting at 1, let's pre-compute
        }
        // Let me rebuild the In tracking
        // InIds by order
        var inCountTracker = 0;
        var orderToInIds = new Dictionary<int, List<int>>();
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            if (o.Status == 0 || o.Status == 1 || o.Status == 5 || o.Status == 6) continue;
            int inCt = partialInOrders.Contains(o.Id) ? 2 : 1;
            var list = new List<int>();
            for (int p = 0; p < inCt; p++)
            {
                inCountTracker++;
                list.Add(inCountTracker);
            }
            orderToInIds[o.Id] = list;
        }

        for (int r = 0; r < returnOrders.Length; r++)
        {
            int orderId = returnOrders[r];
            int retId = nextReturnId++;
            var inIds = orderToInIds[orderId];
            int targetInId = inIds[rnd.Next(inIds.Count)];
            DateTime inDate = inDates[targetInId];
            DateTime retDate = inDate.AddDays(2 + rnd.Next(8));
            string retNo = $"PRT-{retDate:yyyyMMdd}-{retId:0000}";

            var o = orders[orderId - 1];
            decimal retTotal = 0;

            // Return 1-2 items
            int retItemCount = 1 + rnd.Next(2);
            var usedDetIdx = new HashSet<int>();
            for (int d = 0; d < retItemCount; d++)
            {
                int di;
                do { di = rnd.Next(o.Details.Count); } while (usedDetIdx.Contains(di));
                usedDetIdx.Add(di);

                var det = o.Details[di];
                string key = orderId + "_" + det.ProductId;
                int totalRecv = receivedMap.ContainsKey(key) ? receivedMap[key] : 0;
                int retQty = rnd.Next(1, Math.Max(2, totalRecv / 5)); // return a portion
                if (retQty > totalRecv) retQty = totalRecv;
                if (retQty <= 0) retQty = 1;

                decimal rp = det.UnitPrice;
                decimal ramt = Math.Round(rp * retQty, 2);
                retTotal += ramt;

                L(string.Format("INSERT Pur_PurchaseReturnDetail(Id,PurchaseReturnId,ProductId,Qty,UnitPrice,Amount) VALUES({0},{1},{2},{3},{4},{5});",
                    nextReturnDetailId++, retId, det.ProductId, retQty, rp, ramt));

                // Reduce received amount for this detail
                receivedMap[key] = totalRecv - retQty;
            }
            retTotal = Math.Round(retTotal, 2);

            L(string.Format("INSERT Pur_PurchaseReturn(Id,ReturnNo,PurchaseInId,SupplierId,WarehouseId,ReturnDate,TotalAmount,Reason,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({0},'{1}',{2},{3},{4},'{5}',{6},{7},1,N'zhangwei',NULL,0,'{8} 14:00:00',N'zhangwei');",
                retId, retNo, targetInId, o.SupId, o.WhId,
                retDate.ToString("yyyy-MM-dd"), retTotal,
                returnReasons[r], retDate.ToString("yyyy-MM-dd")));

            // Update In status to 2 (已退货)
            L(string.Format("UPDATE Pur_PurchaseIn SET Status=2 WHERE Id={0};", targetInId));
        }
        L("SET IDENTITY_INSERT Pur_PurchaseReturnDetail OFF;");
        L("SET IDENTITY_INSERT Pur_PurchaseReturn OFF;");
        L("GO");
        L("");

        // Re-update ReceivedQty after returns
        L("-- ===== Update ReceivedQty after returns =====");
        foreach (var kv in receivedMap)
        {
            string[] parts = kv.Key.Split('_');
            int oid = int.Parse(parts[0]);
            int pid = int.Parse(parts[1]);
            L(string.Format("UPDATE Pur_PurchaseOrderDetail SET ReceivedQty={0} WHERE PurchaseOrderId={1} AND ProductId={2};",
                kv.Value, oid, pid));
        }
        L("");

        // Re-update ReceivedAmount
        L("-- ===== Update ReceivedAmount after returns =====");
        for (int i = 0; i < orderCount; i++)
        {
            L(string.Format("UPDATE Pur_PurchaseOrder SET ReceivedAmount=(SELECT ISNULL(SUM(pi.TotalAmount),0)-ISNULL((SELECT SUM(pr.TotalAmount) FROM Pur_PurchaseReturn pr WHERE pr.PurchaseInId IN (SELECT pi2.Id FROM Pur_PurchaseIn pi2 WHERE pi2.PurchaseOrderId={0}) AND pr.IsDeleted=0),0) FROM Pur_PurchaseIn pi WHERE pi.PurchaseOrderId={0} AND pi.IsDeleted=0) WHERE Id={0};",
                i + 1));
        }
        L("");

        // Update order status based on ReceivedQty ratio
        L("-- ===== Update Order Status by receipt ratio =====");
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            if (o.Status <= 1 || o.Status >= 5) continue; // skip draft/pending/cancelled/closed

            L(string.Format("UPDATE Pur_PurchaseOrder SET Status=CASE WHEN (SELECT ISNULL(SUM(ReceivedQty),0) FROM Pur_PurchaseOrderDetail WHERE PurchaseOrderId={0})=0 THEN 2 WHEN (SELECT COUNT(*) FROM Pur_PurchaseOrderDetail WHERE PurchaseOrderId={0} AND ReceivedQty<Qty)>0 THEN 3 ELSE 4 END WHERE Id={0};",
                i + 1));
        }
        L("");

        // ===== 7. Self-check SQL =====
        L("-- ===== 7. 自检SQL =====");
        L("-- 1. ReceivedQty与入库一致: 入库Qty-退货Qty=ReceivedQty");
        L("WITH InQty AS (SELECT pid.PurchaseOrderId,pid.ProductId,SUM(pid.Qty) AS InSum FROM Pur_PurchaseInDetail pid JOIN Pur_PurchaseIn pi ON pid.PurchaseInId=pi.Id WHERE pi.IsDeleted=0 GROUP BY pid.PurchaseOrderId,pid.ProductId),");
        L("RetQty AS (SELECT pi2.PurchaseOrderId,prd.ProductId,SUM(prd.Qty) AS RetSum FROM Pur_PurchaseReturnDetail prd JOIN Pur_PurchaseReturn pr ON prd.PurchaseReturnId=pr.Id JOIN Pur_PurchaseIn pi2 ON pr.PurchaseInId=pi2.Id WHERE pr.IsDeleted=0 GROUP BY pi2.PurchaseOrderId,prd.ProductId)");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: ReceivedQty matches In/Return' ELSE 'FAIL' END AS Check_ReceivedQty FROM Pur_PurchaseOrderDetail pod LEFT JOIN InQty i ON pod.PurchaseOrderId=i.PurchaseOrderId AND pod.ProductId=i.ProductId LEFT JOIN RetQty r ON pod.PurchaseOrderId=r.PurchaseOrderId AND pod.ProductId=r.ProductId WHERE pod.ReceivedQty != ISNULL(i.InSum,0)-ISNULL(r.RetSum,0);");
        L("");
        L("-- 2. 无负Qty");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: No negative Qty' ELSE 'FAIL' END FROM Pur_PurchaseOrderDetail WHERE Qty<=0 OR ReceivedQty<0;");
        L("");
        L("-- 3. 金额闭环: Amount=Qty*UnitPrice");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: Detail Amount=Qty*UnitPrice' ELSE 'FAIL' END FROM Pur_PurchaseOrderDetail WHERE Amount!=ROUND(Qty*UnitPrice,2);");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: InDetail Amount=Qty*UnitPrice' ELSE 'FAIL' END FROM Pur_PurchaseInDetail WHERE Amount!=ROUND(Qty*UnitPrice,2);");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: RetDetail Amount=Qty*UnitPrice' ELSE 'FAIL' END FROM Pur_PurchaseReturnDetail WHERE Amount!=ROUND(Qty*UnitPrice,2);");
        L("");
        L("-- 4. TotalAmount与明细一致");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: Order TotalAmount=SUM(Detail)' ELSE 'FAIL' END FROM Pur_PurchaseOrder po WHERE po.TotalAmount!=(SELECT ISNULL(SUM(pod.Amount),0) FROM Pur_PurchaseOrderDetail pod WHERE pod.PurchaseOrderId=po.Id);");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: In TotalAmount=SUM(Detail)' ELSE 'FAIL' END FROM Pur_PurchaseIn pi WHERE pi.TotalAmount!=(SELECT ISNULL(SUM(pid.Amount),0) FROM Pur_PurchaseInDetail pid WHERE pid.PurchaseInId=pi.Id);");
        L("");
        L("-- 5. 时间顺序");
        L("SELECT CASE WHEN COUNT(*)=0 THEN 'PASS: OrderDate<InDate' ELSE 'FAIL' END FROM Pur_PurchaseIn pi JOIN Pur_PurchaseOrder po ON pi.PurchaseOrderId=po.Id WHERE pi.InDate<po.OrderDate;");
        L("");
        L("-- 6. 状态统计");
        L("SELECT 'INFO' AS Check6, Status, COUNT(*) AS Cnt FROM Pur_PurchaseOrder WHERE IsDeleted=0 GROUP BY Status ORDER BY Status;");
        L("");
        L("-- 7. 入库单状态");
        L("SELECT 'INFO' AS Check7, Status, COUNT(*) AS Cnt FROM Pur_PurchaseIn WHERE IsDeleted=0 GROUP BY Status ORDER BY Status;");
        L("");

        L("COMMIT TRANSACTION;");
        L("END TRY");
        L("BEGIN CATCH");
        L("ROLLBACK TRANSACTION;");
        L("THROW;");
        L("END CATCH;");

        File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        Console.WriteLine("Done. Lines: " + sb.ToString().Split('\n').Length);
    }

    class OrderData
    {
        public int Id;
        public string OrderNo;
        public int SupId;
        public int WhId;
        public string OrderDate;
        public string ExpectedDate;
        public decimal TotalAmount;
        public int Status;
        public string Approver;
        public string ApproveTime;
        public string Creator;
        public List<DetailData> Details;
    }

    class DetailData
    {
        public int ProductId;
        public int Qty;
        public decimal UnitPrice;
        public decimal Amount;
    }
}
