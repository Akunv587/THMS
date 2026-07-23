using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

class Program
{
    static StringBuilder sb = new StringBuilder();
    static void L(string s) { sb.AppendLine(s); }
    static Random rnd = new Random(42);
    static string today = "2026-07-17";
    static DateTime Today = DateTime.Parse("2026-07-17");

    // 90 Products: ID 1-90
    static int[] pIds = Enumerable.Range(1, 90).ToArray();
    // Purchase prices (from base_data.sql)
    static decimal[] pPP = new decimal[]{1850,2450,3850,5680,1850,2450,4280,8900,6200,7800,3500,185,350,420,680,6800,9200,185,1250,85,165,1350,1850,4200,1580,2250,5800,4500,6200,185,128,520,35,55,680,48,72,950,1450,3850,1550,2250,5500,2850,4200,6800,1850,3800,380,520,380,820,1350,1250,1850,2800,3800,58,95,880,1450,1350,1950,3200,4200,320,38,1080,2150,1680,2480,2950,3850,155,178,280,450,1050,1450,2350,2580,3950,165,580,220,2850,5200,2680,6800,5800};
    // Sale prices = purchase * 1.28 rounded
    static decimal[] pSP = pPP.Select(p => Math.Round(p * 1.28m, 0)).ToArray();

    // === Configs ===
    // Purchase Orders: 30 (same as existing)
    static int[] poStatus = new int[]{0,0,1,1,2,2,2, 3,3,3,3, 4,4,4,4,4,4,4,4,4,4,4,4,4,4,4, 5,5, 6,6};
    static int[] poSup = new int[]{1,2,3,1,2,3,4,5,1,6,7,1,2,3,4,5,6,7,8,1,2,3,4,5,6,7,8,1,2,3};
    static int[] poWh = new int[]{1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,3,1,1};
    static int[] poDays = new int[]{-350,-340,-335,-330,-320,-310,-300,-280,-270,-260,-250,-240,-230,-220,-210,-200,-190,-180,-170,-160,-150,-140,-130,-120,-110,-100,-90,-80,-70,-60};
    static int[] poDetailCt = new int[]{4,3,5,4,6,5,3,5,4,6,3,5,4,6,5,3,5,7,4,6,5,4,3,8,5,4,3,4,5,4};
    static HashSet<int> partialInOrders = new HashSet<int>{8,9,10,11,12,13};
    static int[] returnOrders = new int[]{14,19,22,24};

    // Sale Orders: 35 (various statuses, 6-month span)
    static int[] soStatus = new int[]{0,0,1,1,1,1,1, 2,2,2, 3,3,3,3, 4,4,4,4,4,4,4,4,4,4,4,4,4,4,4, 5,5,5, 6,6,6};
    static int[] soCust = new int[]{1,3,5,7,8,12,15, 2,6,10, 4,9,14,18, 1,3,5,7,8,11,13,16,19,20,22,24,2,6,10, 12,17,21, 4,15,9};
    static int[] soDays = new int[]{-3,-5,-7,-10,-14,-18,-21,-25,-30,-35,-40,-45,-50,-55,-60,-65,-70,-75,-80,-85,-90,-95,-100,-105,-110,-115,-120,-130,-140,-150,-160,-170,-175,-180,-185};
    static int[] soDetailCt = new int[]{4,5,3,6,4,5,3,5,4,6,3,5,4,5,6,3,5,4,6,4,5,3,7,4,5,6,5,4,3,5,4,6,4,3,5};
    // Which sale orders have partial out
    static HashSet<int> partialOutOrders = new HashSet<int>{11,12,13,14};
    // Which sale orders have returns
    static int[] saleReturnOrders = new int[]{17,21,25};

    // Stock Transfers: 10
    static int[] stStatus = new int[]{0,1,2,3,3,3,3,3,4,5};
    static int[] stFromWh = new int[]{1,1,1,1,2,1,3,1,2,1};
    static int[] stToWh = new int[]{2,3,2,2,1,2,1,3,3,2};
    static int[] stDaysOffset = new int[]{-2,-5,-10,-15,-20,-25,-30,-40,-45,-50};

    // Stock Checks: 10
    static int[] scStatus = new int[]{0,1,1,2,3,3,3,3,3,4};
    static int[] scWh = new int[]{1,1,2,3,1,2,1,3,1,2};
    static int[] scDaysOffset = new int[]{-1,-3,-8,-12,-18,-22,-28,-35,-42,-48};

    // Repair Orders: 30
    static int[] roStatus = new int[]{0,0,0,0, 1,1,1, 2,2,2,2,2, 3,3,3,3, 4,4,4,4,4,4,4,4,4,4, 5,5, 6,6};
    static int[] roDevices; // filled in Main
    // Maps (SaleOutId, ProductId) -> SaleOutDetailId for return references
    static Dictionary<(int outId, int productId), int> sOutDetailMap = new();

    static void Main(string[] args)
    {
        string file = @"c:\Users\bey\Desktop\THMS\demo_data_all.sql";
        string baseFile = @"c:\Users\bey\Desktop\THMS\base_data.sql";
        roDevices = Enumerable.Range(1, 30).Select(i => rnd.Next(1, 41)).ToArray();

        L("-- Clean existing transactional data");
        L("DELETE FROM Inv_StockTransaction;");
        L("DELETE FROM Sale_ReturnDetail;");
        L("DELETE FROM Sale_Return;");
        L("DELETE FROM Sale_SaleOutDetail;");
        L("DELETE FROM Sale_SaleOut;");
        L("DELETE FROM Sale_SaleOrderDetail;");
        L("DELETE FROM Maint_RepairPart;");
        L("DELETE FROM Maint_RepairOrder;");
        L("DELETE FROM Maint_MaintenancePlan;");
        L("DELETE FROM Maint_DeviceArchive;");
        L("DELETE FROM Sale_SaleOrder;");
        L("DELETE FROM Pur_PurchaseReturnDetail;");
        L("DELETE FROM Pur_PurchaseReturn;");
        L("DELETE FROM Pur_PurchaseInDetail;");
        L("DELETE FROM Pur_PurchaseIn;");
        L("DELETE FROM Pur_PurchaseOrderDetail;");
        L("DELETE FROM Pur_PurchaseOrder;");
        L("DELETE FROM Inv_StockCheckDetail;");
        L("DELETE FROM Inv_StockCheck;");
        L("DELETE FROM Inv_StockTransferDetail;");
        L("DELETE FROM Inv_StockTransfer;");
        L("DELETE FROM Inv_StockAlert;");
        L("DELETE FROM Sys_OperationLog;");
        L("DELETE FROM Base_ProductStock;");
        L("");
        L("-- Disable FK checks for bulk import");
        L("ALTER TABLE Pur_PurchaseOrderDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseInDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseReturnDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_SaleOrderDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_SaleOutDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_ReturnDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockTransferDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockCheckDetail NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Maint_RepairPart NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseIn NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseReturn NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_SaleOut NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_Return NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockTransfer NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockCheck NOCHECK CONSTRAINT ALL;");
        L("ALTER TABLE Maint_RepairOrder NOCHECK CONSTRAINT ALL;");
        L("GO");
        L("");

        GenPurchaseOrders();
        GenSaleOrders();
        GenStockTransfers();
        GenStockChecks();
        GenStockAlerts();
        GenDeviceArchives();
        GenRepairOrders();
        GenMaintenancePlans();
        GenOperationLogs();
        GenJulyData();
        GenStockTransactions();

        L("");
        GenSelfChecks();

        File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"Demo SQL generated. Lines: {sb.ToString().Split('\n').Length}");

        // Execute SQL against database
        Console.Write("Execute SQL against database? (y/n): ");
        bool auto = args.Length > 0 && args[0] == "-y";
        if (auto || Console.ReadKey().KeyChar == 'y')
        {
            if (!auto) Console.WriteLine();
            // Reference data already imported via run_base_data.ps1, skip base_data.sql
            Console.WriteLine("Executing demo data...");
            ExecuteSql(file);
        }
        else Console.WriteLine("Skipped execution. Run manually with: dotnet run -- -y");
    }

    // ===== Helpers =====
    static string DateStr(int dayOffset) => Today.AddDays(dayOffset).ToString("yyyy-MM-dd");
    static string DateTimeStr(int dayOffset, string time) => Today.AddDays(dayOffset).ToString("yyyy-MM-dd") + " " + time;
    static string DTS(DateTime dt, string time) => dt.ToString("yyyy-MM-dd") + " " + time;
    static decimal RndPrice(decimal basePrice) => Math.Round(basePrice * (0.95m + (decimal)rnd.NextDouble() * 0.10m), 2);
    static int RndQty(int min, int max) => rnd.Next(min, max + 1);
    static HashSet<int> PickN(int n, int max, HashSet<int> exclude = null)
    {
        var h = new HashSet<int>();
        while (h.Count < n) { int v = rnd.Next(max); if (exclude == null || !exclude.Contains(v)) h.Add(v); }
        return h;
    }

    static void ExecuteBaseSql(string filePath)
    {
        string dbConn = "Server=.;Database=EquipmentIMS_Dev;Trusted_Connection=True;TrustServerCertificate=True;";
        Console.WriteLine($"Executing base data from: {filePath}");
        try
        {
            using var conn = new SqlConnection(dbConn);
            conn.Open();
            string sql = File.ReadAllText(filePath);
            // base_data.sql uses BEGIN TRAN/COMMIT TRAN with GO batch separators
            // GO boundaries break TRY/CATCH scope, so strip the outer wrapper and run each section
            // Strip outer TRY/CATCH wrapper: find all content between first GO and last GO
            var batches = sql.Split(new[] { "GO" }, StringSplitOptions.None);
            int ok = 0, fail = 0;
            bool inTransaction = false;
            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.StartsWith("SET NOCOUNT") || trimmed.StartsWith("BEGIN TRY") || trimmed.StartsWith("BEGIN TRAN"))
                {
                    // Start: begin our own transaction
                    if (trimmed.StartsWith("BEGIN TRAN")) { using var cmd = new SqlCommand("BEGIN TRAN", conn); cmd.ExecuteNonQuery(); inTransaction = true; }
                    continue;
                }
                if (trimmed.StartsWith("COMMIT TRANSACTION") || trimmed.StartsWith("END TRY") || trimmed.StartsWith("BEGIN CATCH") || trimmed.StartsWith("ROLLBACK") || trimmed.StartsWith("THROW") || trimmed.StartsWith("END CATCH"))
                    continue;
                using var batchCmd = new SqlCommand(trimmed, conn);
                batchCmd.CommandTimeout = 120;
                try { batchCmd.ExecuteNonQuery(); ok++; if (ok % 10 == 0) Console.Write($"{ok}.."); }
                catch (Exception ex) { fail++; Console.WriteLine($"\n  Batch error: {ex.Message.Substring(0, Math.Min(120, ex.Message.Length))}"); }
            }
            if (inTransaction) { using var cmd = new SqlCommand("COMMIT TRAN", conn); try { cmd.ExecuteNonQuery(); Console.WriteLine("Committed."); } catch { using var rc = new SqlCommand("ROLLBACK TRAN", conn); rc.ExecuteNonQuery(); } }
            Console.WriteLine($"\nBase data complete! {ok} batches OK, {fail} errors.");
        }
        catch (Exception ex) { Console.WriteLine($"Base data error: {ex.Message}"); }
    }

    static void ExecuteSql(string filePath)
    {
        string masterConn = "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
        string dbConn = "Server=.;Database=EquipmentIMS_Dev;Trusted_Connection=True;TrustServerCertificate=True;";
        Console.WriteLine($"Checking database EquipmentIMS_Dev...");
        try
        {
            using var mconn = new SqlConnection(masterConn);
            mconn.Open();
            using var cmd = new SqlCommand("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name='EquipmentIMS_Dev') BEGIN CREATE DATABASE EquipmentIMS_Dev; END SELECT 1;", mconn);
            cmd.ExecuteNonQuery();
            mconn.Close();
            Console.WriteLine("Database ready.");
        }
        catch (Exception ex) { Console.WriteLine($"Create DB error: {ex.Message}"); return; }

        Console.WriteLine($"Connecting to EquipmentIMS_Dev...");
        try
        {
            using var conn = new SqlConnection(dbConn);
            conn.Open();
            Console.WriteLine("Connected. Executing SQL in transaction...");
            string sql = File.ReadAllText(filePath);

            // Split by GO markers into sections
            var sections = System.Text.RegularExpressions.Regex.Split(sql, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline);

            using var tran = conn.BeginTransaction();
            int ok = 0, fail = 0;

            foreach (var section in sections)
            {
                var trimmed = section.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // Skip outer wrapper statements
                var checkLine = trimmed.Split('\n')[0].Trim();
                if (checkLine == "SET NOCOUNT ON;" || checkLine == "BEGIN TRY" || checkLine.StartsWith("BEGIN TRAN") ||
                    checkLine == "COMMIT TRANSACTION;" || checkLine == "END TRY" || checkLine == "BEGIN CATCH" ||
                    checkLine.StartsWith("ROLLBACK") || checkLine == "THROW;" || checkLine == "END CATCH") continue;

                // Fix IDENTITY_INSERT conflicts: auto-manage per INSERT target table
                List<string> sectionLines = [];
                string currentIdTable = null;
                foreach (var line in trimmed.Split('\n'))
                {
                    var t = line.Trim();
                    // Track explicit SET IDENTITY_INSERT statements
                    if (t.StartsWith("SET IDENTITY_INSERT "))
                    {
                        if (t.EndsWith(" ON;"))
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(t, @"SET IDENTITY_INSERT (\w+) ON;");
                            if (m.Success)
                            {
                                if (currentIdTable != null && currentIdTable != m.Groups[1].Value)
                                    sectionLines.Add($"SET IDENTITY_INSERT {currentIdTable} OFF;");
                                currentIdTable = m.Groups[1].Value;
                            }
                        }
                        else if (t.EndsWith(" OFF;"))
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(t, @"SET IDENTITY_INSERT (\w+) OFF;");
                            if (m.Success && m.Groups[1].Value == currentIdTable)
                                currentIdTable = null;
                        }
                        sectionLines.Add(line);
                        continue;
                    }
                    // For INSERT statements with explicit Id, ensure correct IDENTITY_INSERT is ON
                    if (t.StartsWith("INSERT "))
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(t, @"INSERT\s+(\w+)\s*\(([^)]*)\)");
                        if (m.Success)
                        {
                            var tbl = m.Groups[1].Value;
                            var cols = m.Groups[2].Value;
                            if (cols.Split(',').Any(c => c.Trim() == "Id"))
                            {
                                if (currentIdTable != tbl)
                                {
                                    if (currentIdTable != null)
                                        sectionLines.Add($"SET IDENTITY_INSERT {currentIdTable} OFF;");
                                    sectionLines.Add($"SET IDENTITY_INSERT {tbl} ON;");
                                    currentIdTable = tbl;
                                }
                            }
                        }
                    }
                    sectionLines.Add(line);
                }
                if (currentIdTable != null)
                    sectionLines.Add($"SET IDENTITY_INSERT {currentIdTable} OFF;");

                var fixedSection = string.Join("\n", sectionLines);

                using var batchCmd = new SqlCommand(fixedSection, conn, tran);
                batchCmd.CommandTimeout = 120;
                try
                {
                    batchCmd.ExecuteNonQuery();
                    ok++;
                    if (ok % 5 == 0) Console.Write($"{ok}..");
                }
                catch (Exception ex)
                {
                    fail++;
                    Console.WriteLine($"\n  Section error: {ex.Message.Substring(0, Math.Min(150, ex.Message.Length))}");
                }
            }

            if (fail == 0)
            {
                tran.Commit();
                Console.WriteLine($"\nSQL execution complete! {ok} sections OK, committed.");
            }
            else
            {
                tran.Rollback();
                Console.WriteLine($"\nSQL execution FAILED! {ok} OK, {fail} errors. Rolled back.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
        }
    }

    // ===== 1. Purchase Orders (30) + In (28) + Returns (4) =====
    static void GenPurchaseOrders()
    {
        int orderCount = 30;
        int nextDetailId = 1, nextInId = 1, nextInDetailId = 1;
        int nextReturnId = 1, nextReturnDetailId = 1;

        List<OrderData> orders = [];
        for (int i = 0; i < orderCount; i++)
        {
            int orderId = i + 1;
            DateTime baseDate = Today.AddDays(poDays[i]);
            string orderNo = $"PUR-{baseDate:yyyyMMdd}-{orderId:0000}";
            string orderDate = baseDate.ToString("yyyy-MM-dd");
            int status = poStatus[i];
            int supId = poSup[i], whId = poWh[i];
            string creator = "zhangwei";
            string approver = "NULL", approveTime = "NULL";
            if (status >= 2) { approver = "N'wangqiang'"; approveTime = "'" + baseDate.AddDays(1 + rnd.Next(3)).ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
            string expectedDate = baseDate.AddDays(5 + rnd.Next(10)).ToString("yyyy-MM-dd");

            int detailCt = poDetailCt[i];
            decimal totalAmount = 0;
            List<DetailData> details = [];
            var usedProds = new HashSet<int>();
            for (int d = 0; d < detailCt; d++)
            {
                int pi; do { pi = rnd.Next(90); } while (usedProds.Contains(pi));
                usedProds.Add(pi);
                int pid = pIds[pi];
                decimal pp = RndPrice(pPP[pi]);
                int qty = RndQty(10, 200);
                decimal amt = Math.Round(pp * qty, 2);
                totalAmount += amt;
                details.Add(new DetailData { ProductId = pid, Qty = qty, UnitPrice = pp, Amount = amt });
            }
            totalAmount = Math.Round(totalAmount, 2);
            orders.Add(new OrderData { Id = orderId, OrderNo = orderNo, SupId = supId, WhId = whId, OrderDate = orderDate, ExpectedDate = expectedDate, TotalAmount = totalAmount, Status = status, Approver = approver, ApproveTime = approveTime, Creator = creator, Details = details });
        }

        // Pur_PurchaseOrder
        L("-- ===== 1. Pur_PurchaseOrder (30) =====");
        L("SET IDENTITY_INSERT Pur_PurchaseOrder ON;");
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            string remark = "NULL";
            if (o.Status == 5) remark = "N'手动关闭'";
            else if (o.Status == 6) remark = "N'采购计划变更已取消'";
            L($"INSERT Pur_PurchaseOrder(Id,OrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,TotalAmount,ReceivedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES({o.Id},'{o.OrderNo}',{o.SupId},{o.WhId},'{o.OrderDate}','{o.ExpectedDate}',{o.TotalAmount},0,{o.Status},{o.Approver},{o.ApproveTime},{remark},0,'{o.OrderDate} 09:00:00',N'{o.Creator}');");
        }
        L("SET IDENTITY_INSERT Pur_PurchaseOrder OFF;");
        L("GO\n");

        // Pur_PurchaseOrderDetail
        L("-- ===== 2. Pur_PurchaseOrderDetail =====");
        L("SET IDENTITY_INSERT Pur_PurchaseOrderDetail ON;");
        for (int i = 0; i < orderCount; i++)
            foreach (var det in orders[i].Details)
                L($"INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES({nextDetailId++},{orders[i].Id},{det.ProductId},{det.Qty},0,{det.UnitPrice},{det.Amount},NULL,0,'{orders[i].OrderDate} 09:00:00',N'{orders[i].Creator}');");
        L("SET IDENTITY_INSERT Pur_PurchaseOrderDetail OFF;");
        L("GO\n");

        // Pur_PurchaseIn + Detail
        L("-- ===== 3. Pur_PurchaseIn + Detail =====");
        L("SET IDENTITY_INSERT Pur_PurchaseIn ON;");
        L("SET IDENTITY_INSERT Pur_PurchaseInDetail ON;");
        var receivedMap = new Dictionary<string, int>();
        var inDates = new Dictionary<int, DateTime>();
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            if (o.Status == 0 || o.Status == 1 || o.Status >= 5) continue;
            DateTime orderDate = DateTime.Parse(o.OrderDate);
            int inCount = partialInOrders.Contains(o.Id) ? 2 : 1;
            for (int part = 0; part < inCount; part++)
            {
                int inId = nextInId++;
                DateTime inDate = inCount == 1 ? orderDate.AddDays(3 + rnd.Next(10)) : (part == 0 ? orderDate.AddDays(3 + rnd.Next(5)) : orderDate.AddDays(12 + rnd.Next(8)));
                inDates[inId] = inDate;
                string inNo = $"PIN-{inDate:yyyyMMdd}-{inId:0000}";
                decimal inTotal = 0;
                for (int d = 0; d < o.Details.Count; d++)
                {
                    var det = o.Details[d];
                    int recvQty;
                    if (inCount == 1) recvQty = det.Qty;
                    else if (part == 0) recvQty = rnd.Next(det.Qty / 3, det.Qty * 2 / 3);
                    else { string key = o.Id + "_" + det.ProductId; int alreadyRecv = receivedMap.ContainsKey(key) ? receivedMap[key] : 0; recvQty = det.Qty - alreadyRecv; }
                    if (recvQty <= 0) continue;
                    string key2 = o.Id + "_" + det.ProductId;
                    if (!receivedMap.ContainsKey(key2)) receivedMap[key2] = 0;
                    receivedMap[key2] += recvQty;
                    decimal dp = det.UnitPrice; decimal damt = Math.Round(dp * recvQty, 2);
                    inTotal += damt;
                    L($"INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES({nextInDetailId++},{inId},{det.ProductId},{recvQty},0,{dp},{damt},0,'{DTS(inDate, "10:00:00")}',N'zhangwei');");
                }
                inTotal = Math.Round(inTotal, 2);
                L($"INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({inId},'{inNo}',{o.Id},{o.SupId},{o.WhId},'{inDate:yyyy-MM-dd}',{inTotal},1,N'zhangwei',NULL,0,'{DTS(inDate, "10:00:00")}',N'zhangwei');");
            }
        }
        L("SET IDENTITY_INSERT Pur_PurchaseInDetail OFF;");
        L("SET IDENTITY_INSERT Pur_PurchaseIn OFF;");
        L("GO\n");

        // Update ReceivedQty
        L("-- Update ReceivedQty =====");
        foreach (var kv in receivedMap) { var p = kv.Key.Split('_'); L($"UPDATE Pur_PurchaseOrderDetail SET ReceivedQty={kv.Value} WHERE PurchaseOrderId={p[0]} AND ProductId={p[1]};"); }
        L("");
        L("-- Update ReceivedAmount =====");
        for (int i = 0; i < orderCount; i++) L($"UPDATE Pur_PurchaseOrder SET ReceivedAmount=(SELECT ISNULL(SUM(pi.TotalAmount),0) FROM Pur_PurchaseIn pi WHERE pi.PurchaseOrderId={i + 1} AND pi.IsDeleted=0) WHERE Id={i + 1};");
        L("");

        // Pur_PurchaseReturn
        L("-- ===== 4. Pur_PurchaseReturn (4) =====");
        L("SET IDENTITY_INSERT Pur_PurchaseReturn ON;");
        L("SET IDENTITY_INSERT Pur_PurchaseReturnDetail ON;");
        string[] returnReasons = new string[] { "N'规格不符需退换'", "N'外观有划痕损伤'", "N'参数实测异常'", "N'质检验收不合格'" };
        // Rebuild In tracking
        var inCountTracker = 0;
        var orderToInIds = new Dictionary<int, List<int>>();
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            if (o.Status == 0 || o.Status == 1 || o.Status >= 5) continue;
            int inCt = partialInOrders.Contains(o.Id) ? 2 : 1;
            List<int> list = [];
            for (int p = 0; p < inCt; p++) { inCountTracker++; list.Add(inCountTracker); }
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
            int retItemCount = 1 + rnd.Next(2);
            var usedDetIdx = new HashSet<int>();
            for (int d = 0; d < retItemCount; d++)
            {
                int di; do { di = rnd.Next(o.Details.Count); } while (usedDetIdx.Contains(di));
                usedDetIdx.Add(di);
                var det = o.Details[di];
                string key = orderId + "_" + det.ProductId;
                int totalRecv = receivedMap.ContainsKey(key) ? receivedMap[key] : 0;
                int retQty = rnd.Next(1, Math.Max(2, totalRecv / 5));
                if (retQty > totalRecv) retQty = totalRecv;
                if (retQty <= 0) retQty = 1;
                decimal rp = det.UnitPrice; decimal ramt = Math.Round(rp * retQty, 2);
                retTotal += ramt;
                L($"INSERT Pur_PurchaseReturnDetail(Id,PurchaseReturnId,ProductId,Qty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES({nextReturnDetailId++},{retId},{det.ProductId},{retQty},{rp},{ramt},0,'{DTS(retDate, "14:00:00")}',N'zhangwei');");
                receivedMap[key] = totalRecv - retQty;
            }
            retTotal = Math.Round(retTotal, 2);
            L($"INSERT Pur_PurchaseReturn(Id,ReturnNo,PurchaseInId,SupplierId,WarehouseId,ReturnDate,TotalAmount,Reason,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({retId},'{retNo}',{targetInId},{o.SupId},{o.WhId},'{retDate:yyyy-MM-dd}',{retTotal},{returnReasons[r]},1,N'zhangwei',NULL,0,'{DTS(retDate, "14:00:00")}',N'zhangwei');");
            L($"UPDATE Pur_PurchaseIn SET Status=2 WHERE Id={targetInId};");
        }
        L("SET IDENTITY_INSERT Pur_PurchaseReturnDetail OFF;");
        L("SET IDENTITY_INSERT Pur_PurchaseReturn OFF;");
        L("GO\n");

        // Final ReceivedQty update
        L("-- Final ReceivedQty =====");
        foreach (var kv in receivedMap) { var p = kv.Key.Split('_'); L($"UPDATE Pur_PurchaseOrderDetail SET ReceivedQty={kv.Value} WHERE PurchaseOrderId={p[0]} AND ProductId={p[1]};"); }
        L("");
        L("-- Final ReceivedAmount =====");
        for (int i = 0; i < orderCount; i++) L($"UPDATE Pur_PurchaseOrder SET ReceivedAmount=(SELECT ISNULL(SUM(pi.TotalAmount),0)-ISNULL((SELECT SUM(pr.TotalAmount) FROM Pur_PurchaseReturn pr WHERE pr.PurchaseInId IN (SELECT pi2.Id FROM Pur_PurchaseIn pi2 WHERE pi2.PurchaseOrderId={i + 1}) AND pr.IsDeleted=0),0) FROM Pur_PurchaseIn pi WHERE pi.PurchaseOrderId={i + 1} AND pi.IsDeleted=0) WHERE Id={i + 1};");
        L("");
        L("-- Update Order Status by receipt =====");
        for (int i = 0; i < orderCount; i++)
        {
            var o = orders[i];
            if (o.Status <= 1 || o.Status >= 5) continue;
            L($"UPDATE Pur_PurchaseOrder SET Status=CASE WHEN (SELECT ISNULL(SUM(ReceivedQty),0) FROM Pur_PurchaseOrderDetail WHERE PurchaseOrderId={i + 1})=0 THEN 2 WHEN (SELECT COUNT(*) FROM Pur_PurchaseOrderDetail WHERE PurchaseOrderId={i + 1} AND ReceivedQty<Qty)>0 THEN 3 ELSE 4 END WHERE Id={i + 1};");
        }
        L("");
    }

    // ===== 3. Stock Transfers (10) =====
    static void GenStockTransfers()
    {
        int count = 10, nextDetailId = 1;
        string[] users = new string[] { "zhangwei", "likun" };
        L("-- ===== 9. Inv_StockTransfer (10) =====");
        L("SET IDENTITY_INSERT Inv_StockTransfer ON;");
        L("SET IDENTITY_INSERT Inv_StockTransferDetail ON;");
        for (int i = 0; i < count; i++)
        {
            int tid = i + 1;
            DateTime tDate = Today.AddDays(stDaysOffset[i]);
            string tNo = $"TRN-{tDate:yyyyMMdd}-{tid:0000}";
            int fromWh = stFromWh[i], toWh = stToWh[i];
            int status = stStatus[i];
            string approver = "NULL", approveTime = "NULL", rejectReason = "NULL";
            if (status >= 2) { approver = "N'wangqiang'"; approveTime = "'" + tDate.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
            if (status == 5) rejectReason = "N'库存不足无法调拨'";
            string op = users[i % 2];
            int detailCt = 2 + rnd.Next(4);
            var used = new HashSet<int>();
            L($"INSERT Inv_StockTransfer(Id,TransferNo,FromWarehouseId,ToWarehouseId,TransferDate,Status,Approver,ApproveTime,RejectReason,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({tid},'{tNo}',{fromWh},{toWh},'{tDate:yyyy-MM-dd}',{status},{approver},{approveTime},{rejectReason},N'{op}',NULL,0,'{DTS(tDate, "09:00:00")}',N'{op}');");
            for (int d = 0; d < detailCt; d++)
            {
                int pi; do { pi = rnd.Next(90); } while (used.Contains(pi)); used.Add(pi);
                int qty = RndQty(5, 50);
                L($"INSERT Inv_StockTransferDetail(Id,TransferId,ProductId,Qty,Remark,IsDeleted,CreateTime,CreateBy) VALUES({nextDetailId++},{tid},{pIds[pi]},{qty},NULL,0,'{DTS(tDate, "09:00:00")}',N'{op}');");
            }
        }
        L("SET IDENTITY_INSERT Inv_StockTransferDetail OFF;");
        L("SET IDENTITY_INSERT Inv_StockTransfer OFF;");
        L("GO\n");
    }

    // ===== 4. Stock Checks (10) =====
    static void GenStockChecks()
    {
        int count = 10, nextDetailId = 1;
        string[] users = new string[] { "zhangwei", "likun", "wangfang" };
        L("-- ===== 10. Inv_StockCheck (10) =====");
        L("SET IDENTITY_INSERT Inv_StockCheck ON;");
        L("SET IDENTITY_INSERT Inv_StockCheckDetail ON;");
        for (int i = 0; i < count; i++)
        {
            int cid = i + 1;
            DateTime cDate = Today.AddDays(scDaysOffset[i]);
            string cNo = $"CHK-{cDate:yyyyMMdd}-{cid:0000}";
            int whId = scWh[i], status = scStatus[i];
            bool isLocked = status >= 1;
            string approver = "NULL", approveTime = "NULL";
            if (status >= 3) { approver = "N'wangqiang'"; approveTime = "'" + cDate.AddDays(2).ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
            string op = users[i % 3];
            int detailCt = 3 + rnd.Next(6);
            var used = new HashSet<int>();
            decimal totalDiff = 0;
            L($"INSERT Inv_StockCheck(Id,CheckNo,WarehouseId,CheckDate,Status,IsLocked,TotalDiffAmount,Approver,ApproveTime,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({cid},'{cNo}',{whId},'{cDate:yyyy-MM-dd}',{status},{(isLocked ? 1 : 0)},0,{approver},{approveTime},N'{op}',NULL,0,'{DTS(cDate, "08:00:00")}',N'{op}');");
            for (int d = 0; d < detailCt; d++)
            {
                int pi; do { pi = rnd.Next(90); } while (used.Contains(pi)); used.Add(pi);
                int sysQty = RndQty(10, 120);
                int? actualQty = status >= 2 ? (int?)(sysQty + rnd.Next(-5, 6)) : null;
                int diffQty = (actualQty ?? sysQty) - sysQty;
                decimal up = pSP[pi];
                totalDiff += Math.Abs(diffQty) * up;
                L($"INSERT Inv_StockCheckDetail(Id,StockCheckId,ProductId,SystemQty,ActualQty,DiffQty,UnitPrice,Remark,IsDeleted,CreateTime,CreateBy) VALUES({nextDetailId++},{cid},{pIds[pi]},{sysQty},{(actualQty.HasValue ? actualQty.Value.ToString() : "NULL")},{diffQty},{up},NULL,0,'{DTS(cDate, "08:00:00")}',N'{op}');");
            }
            L($"UPDATE Inv_StockCheck SET TotalDiffAmount={Math.Round(totalDiff, 2)} WHERE Id={cid};");
        }
        L("SET IDENTITY_INSERT Inv_StockCheckDetail OFF;");
        L("SET IDENTITY_INSERT Inv_StockCheck OFF;");
        L("GO\n");
    }

    // ===== 5. Stock Alerts (20) =====
    static void GenStockAlerts()
    {
        L("-- ===== 11. Inv_StockAlert (20) =====");
        int[] alertProds = new int[] { 4, 8, 10, 16, 17, 24, 7, 9, 11, 25, 28, 30, 38, 41, 43, 47, 54, 60, 74, 83 };
        for (int i = 0; i < alertProds.Length; i++)
        {
            int pid = alertProds[i];
            int whId = (i % 3) + 1;
            int curQty = rnd.Next(0, 6);
            int alertLevel = curQty == 0 ? 1 : 2;
            bool resolved = i < 5;
            string resolvedTime = resolved ? "'" + Today.AddDays(-rnd.Next(1, 10)).ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL";
            L($"INSERT Inv_StockAlert(ProductId,WarehouseId,AlertLevel,CurrentQty,MinStock,IsResolved,ResolvedTime,CreateTime) VALUES({pid},{whId},{alertLevel},{curQty},5,{(resolved ? 1 : 0)},{resolvedTime},'{Today.AddDays(-rnd.Next(5, 30)):yyyy-MM-dd} 08:00:00');");
        }
        L("GO\n");
    }

    // ===== 6. Device Archives (40) =====
    static void GenDeviceArchives()
    {
        L("-- ===== 12. Maint_DeviceArchive (40) =====");
        L("SET IDENTITY_INSERT Maint_DeviceArchive ON;");
        string[] brands = new string[] { "西门子", "施耐德", "三菱", "汇川", "台达", "欧姆龙", "松下", "罗克韦尔" };
        for (int i = 1; i <= 40; i++)
        {
            int prodId = (i % 90) + 1;
            int custId = (i % 25) + 1;
            string serialNo = $"SN-{brands[i % 8]}-{2020000 + i}";
            DateTime installDate = Today.AddDays(-rnd.Next(30, 730));
            DateTime warrantyStart = installDate;
            DateTime warrantyEnd = installDate.AddDays(365);
            int status = rnd.Next(100) < 75 ? 1 : (rnd.Next(100) < 80 ? 2 : 3);
            int? saleOrderId = i <= 20 ? (int?)((i % 15) + 1) : null;
            L($"INSERT Maint_DeviceArchive(Id,SerialNo,ProductId,CustomerId,SaleOrderId,InstallDate,WarrantyStart,WarrantyEnd,Status,Remark,IsDeleted,CreateTime,CreateBy) VALUES({i},'{serialNo}',{prodId},{custId},{saleOrderId?.ToString() ?? "NULL"},'{installDate:yyyy-MM-dd}','{warrantyStart:yyyy-MM-dd}','{warrantyEnd:yyyy-MM-dd}',{status},NULL,0,'{DTS(installDate, "10:00:00")}',N'maxiang');");
        }
        L("SET IDENTITY_INSERT Maint_DeviceArchive OFF;");
        L("GO\n");
    }

    // ===== 7. Repair Orders (30) =====
    static void GenRepairOrders()
    {
        L("-- ===== 13. Maint_RepairOrder (30) =====");
        L("SET IDENTITY_INSERT Maint_RepairOrder ON;");
        L("SET IDENTITY_INSERT Maint_RepairPart ON;");
        int nextPartId = 1;
        string[] faultTypes = new string[] { "电气故障", "机械故障", "参数异常", "通讯故障", "传感器失灵", "电源故障" };
        string[] faultDescs = new string[] {
            "设备无法启动，通电后无任何反应", "运行中频繁跳闸，电流过大", "输出信号不稳定，波动较大",
            "通讯模块指示灯异常闪烁", "触摸屏显示花屏，无法操作", "电机运行有异响，震动过大",
            "传感器读数偏差超过允许范围", "设备间歇性停机，原因不明"
        };
        string[] repairDescs = new string[] {
            "更换电源模块，重新校准参数", "检查线路连接，更换老化接触器", "升级固件版本，重新配置通讯参数",
            "更换通讯模块，测试通讯恢复正常", "更换显示屏，校准触摸功能", "更换轴承，添加润滑脂",
            "重新校准传感器零点，测试精度合格", "排查供电线路，更换稳压电源"
        };
        string[] techNames = new string[] { "maxiang", "guoqiang" };
        for (int i = 1; i <= 30; i++)
        {
            int deviceId = roDevices[i - 1];
            int custId = (deviceId % 25) + 1;
            DateTime createDate = Today.AddDays(-rnd.Next(2, 120));
            string orderNo = $"REP-{createDate:yyyyMMdd}-{i:0000}";
            int status = roStatus[i - 1];
            string faultType = faultTypes[rnd.Next(faultTypes.Length)];
            string faultDesc = faultDescs[rnd.Next(faultDescs.Length)];
            int? techId = status >= 1 ? (int?)(8 + rnd.Next(2)) : null;
            string techName = status >= 1 ? "N'" + techNames[rnd.Next(2)] + "'" : "NULL";
            string assignTime = status >= 1 ? "'" + createDate.AddDays(1 + rnd.Next(3)).ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL";
            string startTime = status >= 2 ? "'" + createDate.AddDays(2 + rnd.Next(5)).ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL";
            string completeTime = status >= 4 ? "'" + createDate.AddDays(3 + rnd.Next(10)).ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL";
            string repairDesc = status >= 3 ? "N'" + repairDescs[rnd.Next(repairDescs.Length)] + "'" : "NULL";
            decimal workHours = status >= 3 ? Math.Round((decimal)(rnd.NextDouble() * 8 + 0.5), 1) : 0;
            decimal partsCost = 0;
            bool isWarranty = rnd.Next(100) < 60;
            decimal repairCost = isWarranty ? 0 : Math.Round(workHours * 200, 2);
            string remark = "NULL";
            if (status == 5) remark = "N'维修完成已关闭'";
            else if (status == 6) remark = "N'客户取消维修'";
            L($"INSERT Maint_RepairOrder(Id,OrderNo,DeviceArchiveId,CustomerId,FaultDesc,FaultType,Status,TechnicianId,TechnicianName,AssignTime,StartTime,CompleteTime,RepairDesc,WorkHours,RepairCost,PartsCost,TotalCost,IsWarranty,Remark,IsDeleted,CreateTime,CreateBy) VALUES({i},'{orderNo}',{deviceId},{custId},N'{faultDesc}',N'{faultType}',{status},{techId?.ToString() ?? "NULL"},{techName},{assignTime},{startTime},{completeTime},{repairDesc},{workHours},{repairCost},0,{repairCost},{(isWarranty ? 1 : 0)},{remark},0,'{DTS(createDate, "08:30:00")}',N'maxiang');");
            if (status >= 2 && rnd.Next(100) < 70)
            {
                int partCount = 1 + rnd.Next(3);
                var usedP = new HashSet<int>();
                for (int p = 0; p < partCount; p++)
                {
                    int pi; do { pi = rnd.Next(90); } while (usedP.Contains(pi)); usedP.Add(pi);
                    int pid = pIds[pi];
                    int qty = RndQty(1, 5);
                    decimal up = pPP[pi];
                    decimal amt = Math.Round(up * qty, 2);
                    partsCost += amt;
                    L($"INSERT Maint_RepairPart(Id,RepairOrderId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES({nextPartId++},{i},{pid},2,{qty},{up},{amt},NULL,0,'{DTS(createDate, "08:30:00")}',N'maxiang');");
                }
                partsCost = Math.Round(partsCost, 2);
            }
            decimal totalCost = Math.Round(repairCost + partsCost, 2);
            L($"UPDATE Maint_RepairOrder SET PartsCost={partsCost},TotalCost={totalCost} WHERE Id={i};");
        }
        L("SET IDENTITY_INSERT Maint_RepairPart OFF;");
        L("SET IDENTITY_INSERT Maint_RepairOrder OFF;");
        L("GO\n");
    }

    // ===== 8. Maintenance Plans (25) =====
    static void GenMaintenancePlans()
    {
        L("-- ===== 14. Maint_MaintenancePlan (25) =====");
        L("SET IDENTITY_INSERT Maint_MaintenancePlan ON;");
        string[] planNames = new string[] { "季度常规保养", "年度大修保养", "月度检查保养", "半年度深度保养", "电气系统检查", "机械部件润滑", "传感器校准", "通讯模块检测" };
        int[] cycleTypes = new int[] { 1, 4, 1, 3, 1, 2, 1, 3 };
        int[] cycleDays = new int[] { 30, 365, 30, 180, 30, 90, 30, 180 };
        for (int i = 1; i <= 25; i++)
        {
            int deviceId = (i % 40) + 1;
            int planIdx = i % planNames.Length;
            string planName = planNames[planIdx] + $" (#{i})";
            int ct = cycleTypes[planIdx];
            int cd = cycleDays[planIdx];
            DateTime nextDate = Today.AddDays(rnd.Next(-30, 60));
            bool isActive = rnd.Next(100) < 85;
            string content = rnd.Next(100) < 60 ? "N'检查设备运行状态、清洁内外部件、紧固接线端子、测试关键参数、更换易损件。'" : "NULL";
            L($"INSERT Maint_MaintenancePlan(Id,DeviceArchiveId,PlanName,CycleType,CycleDays,NextDate,Content,IsActive,Remark,IsDeleted,CreateTime,CreateBy) VALUES({i},{deviceId},N'{planName}',{ct},{cd},'{nextDate:yyyy-MM-dd}',{content},{(isActive ? 1 : 0)},NULL,0,'{Today.AddDays(-rnd.Next(60, 180)):yyyy-MM-dd} 09:00:00',N'maxiang');");
        }
        L("SET IDENTITY_INSERT Maint_MaintenancePlan OFF;");
        L("GO\n");
    }

    // ===== 9. Operation Logs (150) =====
    static void GenOperationLogs()
    {
        L("-- ===== 15. Sys_OperationLog (150) =====");
        string[] modules = new string[] { "采购管理", "销售管理", "库存管理", "设备维保", "系统管理", "基础数据" };
        string[] actions = new string[] { "新增", "编辑", "删除", "审批", "入库", "出库", "盘点", "调拨", "派工", "维修", "确认", "登录", "导出" };
        string[] userNames = new string[] { "admin", "zhangwei", "likun", "wangfang", "liujie", "zhaomin", "chenhui", "maxiang", "guoqiang" };
        string[] targetTypes = new string[] { "采购订单", "销售订单", "入库单", "出库单", "退货单", "调拨单", "盘点单", "维修工单", "设备档案", "保养计划", "用户", "角色" };
        for (int i = 1; i <= 150; i++)
        {
            int userId = rnd.Next(1, 10);
            string userName = userNames[userId - 1];
            string module = modules[rnd.Next(modules.Length)];
            string action = actions[rnd.Next(actions.Length)];
            string targetType = targetTypes[rnd.Next(targetTypes.Length)];
            int? targetId = rnd.Next(100) < 70 ? (int?)rnd.Next(1, 36) : null;
            string desc = rnd.Next(100) < 50 ? $"N'{action}{targetType}记录'" : "NULL";
            string ip = "'192.168.1." + rnd.Next(1, 255) + "'";
            DateTime logTime = Today.AddDays(-rnd.Next(0, 180)).AddHours(rnd.Next(8, 20)).AddMinutes(rnd.Next(0, 60));
            L($"INSERT Sys_OperationLog(UserId,UserName,Module,Action,TargetType,TargetId,Description,OldValue,NewValue,IP,CreateTime) VALUES({userId},N'{userName}',N'{module}',N'{action}',N'{targetType}',{targetId?.ToString() ?? "NULL"},{desc},NULL,NULL,{ip},'{logTime:yyyy-MM-dd HH:mm:ss}');");
        }
        L("GO\n");
    }

    // ===== 10. Stock Transactions =====
    static void GenStockTransactions()
    {
        L("-- ===== 16. Inv_StockTransaction (Initial stock) =====");
        for (int i = 1; i <= 90; i++)
        {
            int whId = (i % 3) + 1;
            int qty = RndQty(15, 80);
            L($"INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) VALUES('INIT-20260717-{i:0000}',9,{i},{whId},{qty},{qty},'InitialImport',NULL,NULL,N'likun',N'期初库存导入','{Today:yyyy-MM-dd} 08:00:00',N'likun');");
        }
        L("");
        L("-- PurchaseIn stock txns =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('PIN-TXN-',pid.Id),1,pid.ProductId,pi.WarehouseId,pid.Qty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=pid.ProductId AND WarehouseId=pi.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)+pid.Qty,'PurchaseIn',pi.Id,pi.InNo,pi.Operator,'采购入库自动流水',DATEADD(day,0,pi.InDate),pi.CreateBy FROM Pur_PurchaseInDetail pid JOIN Pur_PurchaseIn pi ON pid.PurchaseInId=pi.Id WHERE pi.IsDeleted=0;");
        L("");
        L("-- PurchaseReturn stock txns (out) =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('PRT-TXN-',prd.Id),2,prd.ProductId,pr.WarehouseId,-prd.Qty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=prd.ProductId AND WarehouseId=pr.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)-prd.Qty,'PurchaseReturn',pr.Id,pr.ReturnNo,pr.Operator,'采购退货出库自动流水',DATEADD(day,0,pr.ReturnDate),pr.CreateBy FROM Pur_PurchaseReturnDetail prd JOIN Pur_PurchaseReturn pr ON prd.PurchaseReturnId=pr.Id WHERE pr.IsDeleted=0;");
        L("");
        L("-- SaleOut stock txns (out) =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('SHP-TXN-',sod.Id),3,sod.ProductId,sod.WarehouseId,-sod.Qty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=sod.ProductId AND WarehouseId=sod.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)-sod.Qty,'SaleOut',so.Id,so.OutNo,so.Operator,'销售出库自动流水',DATEADD(day,0,so.OutTime),so.CreateBy FROM Sale_SaleOutDetail sod JOIN Sale_SaleOut so ON sod.SaleOutId=so.Id WHERE so.IsDeleted=0;");
        L("");
        L("-- SaleReturn stock txns (in) =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('SLR-TXN-',srd.Id),4,srd.ProductId,srd.WarehouseId,srd.Qty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=srd.ProductId AND WarehouseId=srd.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)+srd.Qty,'SaleReturn',sr.Id,sr.ReturnNo,sr.Operator,'销售退货入库自动流水',DATEADD(day,0,sr.ReturnDate),sr.CreateBy FROM Sale_ReturnDetail srd JOIN Sale_Return sr ON srd.SaleReturnId=sr.Id WHERE sr.IsDeleted=0;");
        L("");
        L("-- StockTransfer out =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('TRN-OUT-',std.Id),6,std.ProductId,st.FromWarehouseId,-std.Qty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=std.ProductId AND WarehouseId=st.FromWarehouseId ORDER BY CreateTime DESC,Id DESC),0)-std.Qty,'StockTransfer',st.Id,st.TransferNo,st.Operator,'调拨出库自动流水',DATEADD(day,0,st.TransferDate),st.CreateBy FROM Inv_StockTransferDetail std JOIN Inv_StockTransfer st ON std.TransferId=st.Id WHERE st.Status=3 AND st.IsDeleted=0;");
        L("");
        L("-- StockTransfer in =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('TRN-IN-',std.Id),5,std.ProductId,st.ToWarehouseId,std.Qty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=std.ProductId AND WarehouseId=st.ToWarehouseId ORDER BY CreateTime DESC,Id DESC),0)+std.Qty,'StockTransfer',st.Id,st.TransferNo,st.Operator,'调拨入库自动流水',DATEADD(day,0,st.TransferDate),st.CreateBy FROM Inv_StockTransferDetail std JOIN Inv_StockTransfer st ON std.TransferId=st.Id WHERE st.Status=3 AND st.IsDeleted=0;");
        L("");
        L("-- StockCheck gain/loss =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('CHK-ADJ-',scd.Id),CASE WHEN scd.DiffQty>=0 THEN 7 ELSE 8 END,scd.ProductId,sc.WarehouseId,scd.DiffQty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=scd.ProductId AND WarehouseId=sc.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)+scd.DiffQty,'StockCheck',sc.Id,sc.CheckNo,sc.Operator,'盘点调整自动流水',DATEADD(day,0,sc.CheckDate),sc.CreateBy FROM Inv_StockCheckDetail scd JOIN Inv_StockCheck sc ON scd.StockCheckId=sc.Id WHERE sc.Status=3 AND scd.DiffQty!=0 AND sc.IsDeleted=0;");
        L("");
        L("-- RepairPart usage =====");
        L("INSERT Inv_StockTransaction(TransactionNo,TransactionType,ProductId,WarehouseId,Qty,BalanceQty,RefType,RefId,RefNo,Operator,Remark,CreateTime,CreateBy) SELECT CONCAT('REP-USE-',rp.Id),10,rp.ProductId,rp.WarehouseId,-rp.Qty,ISNULL((SELECT TOP 1 BalanceQty FROM Inv_StockTransaction WHERE ProductId=rp.ProductId AND WarehouseId=rp.WarehouseId ORDER BY CreateTime DESC,Id DESC),0)-rp.Qty,'RepairOrder',rp.RepairOrderId,ro.OrderNo,N'maxiang','维修领用自动流水',DATEADD(day,0,ro.CreateTime),ro.CreateBy FROM Maint_RepairPart rp JOIN Maint_RepairOrder ro ON rp.RepairOrderId=ro.Id;");
        L("GO\n");
        L("");
        L("-- Re-enable FK checks");
        L("ALTER TABLE Pur_PurchaseOrderDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseInDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseReturnDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_SaleOrderDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_SaleOutDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_ReturnDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockTransferDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockCheckDetail WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Maint_RepairPart WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseIn WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Pur_PurchaseReturn WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_SaleOut WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Sale_Return WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockTransfer WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Inv_StockCheck WITH CHECK CHECK CONSTRAINT ALL;");
        L("ALTER TABLE Maint_RepairOrder WITH CHECK CHECK CONSTRAINT ALL;");
        L("GO");
        L("-- Update Base_ProductStock =====");
        L("DELETE FROM Base_ProductStock;");
        L("INSERT Base_ProductStock(ProductId,WarehouseId,Quantity,LockQty,IsDeleted,CreateTime,CreateBy) SELECT ProductId,WarehouseId,SUM(Qty) AS Quantity,0,0,MAX(CreateTime),N'system' FROM Inv_StockTransaction GROUP BY ProductId,WarehouseId HAVING SUM(Qty)>0;");
        L("");
    }

    // ===== 11. July 2026 Data (for dashboard metrics) =====
    static void GenJulyData()
    {
        L("-- ===== 17. July 2026 Transactions (Dashboard) =====");
        L("SET IDENTITY_INSERT Pur_PurchaseOrder ON;");
        L("SET IDENTITY_INSERT Pur_PurchaseOrderDetail ON;");
        L("SET IDENTITY_INSERT Pur_PurchaseIn ON;");
        L("SET IDENTITY_INSERT Pur_PurchaseInDetail ON;");
        L("SET IDENTITY_INSERT Sale_SaleOrder ON;");
        L("SET IDENTITY_INSERT Sale_SaleOrderDetail ON;");
        L("SET IDENTITY_INSERT Sale_SaleOut ON;");
        L("SET IDENTITY_INSERT Sale_SaleOutDetail ON;");
        L("");

        // PO #31: July 3
        L("INSERT Pur_PurchaseOrder(Id,OrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,TotalAmount,ReceivedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES(31,'PUR-20260703-0031',2,1,'2026-07-03','2026-07-15',128500.00,128500.00,4,N'wangqiang','2026-07-05 10:00:00',NULL,0,'2026-07-03 09:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(151,31,12,30,30,1850.00,55500.00,NULL,0,'2026-07-03 09:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(152,31,25,20,20,1580.00,31600.00,NULL,0,'2026-07-03 09:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(153,31,38,10,10,4140.00,41400.00,NULL,0,'2026-07-03 09:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES(31,'PIN-20260708-0031',31,2,1,'2026-07-08',128500.00,1,N'zhangwei',NULL,0,'2026-07-08 10:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES(151,31,12,30,0,1850.00,55500.00,0,'2026-07-08 10:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES(152,31,25,20,0,1580.00,31600.00,0,'2026-07-08 10:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES(153,31,38,10,0,4140.00,41400.00,0,'2026-07-08 10:00:00',N'zhangwei');");
        L("");

        // PO #32: July 10, partial in
        L("INSERT Pur_PurchaseOrder(Id,OrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,TotalAmount,ReceivedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES(32,'PUR-20260710-0032',5,2,'2026-07-10','2026-07-22',109070.00,48970.00,3,N'wangqiang','2026-07-11 14:00:00',NULL,0,'2026-07-10 09:00:00',N'likun');");
        L("INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(154,32,45,25,10,4200.00,105000.00,NULL,0,'2026-07-10 09:00:00',N'likun');");
        L("INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(155,32,55,15,8,58.00,870.00,NULL,0,'2026-07-10 09:00:00',N'likun');");
        L("INSERT Pur_PurchaseOrderDetail(Id,PurchaseOrderId,ProductId,Qty,ReceivedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(156,32,62,10,5,320.00,3200.00,NULL,0,'2026-07-10 09:00:00',N'likun');");
        L("INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES(32,'PIN-20260712-0032',32,5,2,'2026-07-12',48970.00,1,N'zhangwei',NULL,0,'2026-07-12 10:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES(154,32,45,10,0,4200.00,42000.00,0,'2026-07-12 10:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES(155,32,55,8,0,58.00,464.00,0,'2026-07-12 10:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES(156,32,62,5,0,320.00,1600.00,0,'2026-07-12 10:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseIn(Id,InNo,PurchaseOrderId,SupplierId,WarehouseId,InDate,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES(33,'PIN-20260715-0033',32,5,2,'2026-07-15',60090.00,1,N'zhangwei',NULL,0,'2026-07-15 14:00:00',N'zhangwei');");
        L("INSERT Pur_PurchaseInDetail(Id,PurchaseInId,ProductId,Qty,ReturnedQty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES(157,33,45,15,0,4006.00,60090.00,0,'2026-07-15 14:00:00',N'zhangwei');");
        L("");

        // SO #36: July 5, completed
        L("INSERT Sale_SaleOrder(Id,OrderNo,CustomerId,OrderDate,ExpectedDate,TotalAmount,ShippedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES(36,'SAL-20260705-0036',8,'2026-07-05','2026-07-12',173760.00,173760.00,4,N'wangqiang','2026-07-06 10:00:00',N'急单优先发货',0,'2026-07-05 09:30:00',N'liujie');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(176,36,3,1,10,10,4928.00,49280.00,NULL,0,'2026-07-05 09:30:00',N'liujie');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(177,36,8,1,5,5,11392.00,56960.00,NULL,0,'2026-07-05 09:30:00',N'liujie');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(178,36,15,1,8,8,2368.00,18944.00,NULL,0,'2026-07-05 09:30:00',N'liujie');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(179,36,22,1,12,12,1728.00,20736.00,NULL,0,'2026-07-05 09:30:00',N'liujie');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(180,36,35,1,15,15,1856.00,27840.00,NULL,0,'2026-07-05 09:30:00',N'liujie');");
        L("INSERT Sale_SaleOut(Id,OutNo,SaleOrderId,CustomerId,WarehouseId,OutTime,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES(30,'SHP-20260707-0036',36,8,1,'2026-07-07 14:00:00',173760.00,0,N'liujie',NULL,0,'2026-07-07 14:00:00',N'liujie');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(141,30,176,3,1,10,4928.00,49280.00,NULL,0,'2026-07-07 14:00:00',N'liujie');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(142,30,177,8,1,5,11392.00,56960.00,NULL,0,'2026-07-07 14:00:00',N'liujie');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(143,30,178,15,1,8,2368.00,18944.00,NULL,0,'2026-07-07 14:00:00',N'liujie');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(144,30,179,22,1,12,1728.00,20736.00,NULL,0,'2026-07-07 14:00:00',N'liujie');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(145,30,180,35,1,15,1856.00,27840.00,NULL,0,'2026-07-07 14:00:00',N'liujie');");
        L("");

        // SO #37: July 8, completed
        L("INSERT Sale_SaleOrder(Id,OrderNo,CustomerId,OrderDate,ExpectedDate,TotalAmount,ShippedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES(37,'SAL-20260708-0037',15,'2026-07-08','2026-07-16',146052.00,146052.00,4,N'wangqiang','2026-07-09 09:00:00',NULL,0,'2026-07-08 10:00:00',N'chenhui');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(181,37,7,1,12,12,4224.00,50688.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(182,37,18,1,20,20,237.00,4740.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(183,37,29,1,6,6,7936.00,47616.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(184,37,42,2,8,8,5376.00,43008.00,NULL,0,'2026-07-08 10:00:00',N'chenhui');");
        L("INSERT Sale_SaleOut(Id,OutNo,SaleOrderId,CustomerId,WarehouseId,OutTime,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES(31,'SHP-20260710-0037',37,15,1,'2026-07-10 16:00:00',146052.00,0,N'chenhui',NULL,0,'2026-07-10 16:00:00',N'chenhui');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(146,31,181,7,1,12,4224.00,50688.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(147,31,182,18,1,20,237.00,4740.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(148,31,183,29,1,6,7936.00,47616.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');");
        L("INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(149,31,184,42,2,8,5376.00,43008.00,NULL,0,'2026-07-10 16:00:00',N'chenhui');");
        L("");

        // SO #38: July 12, pending approval
        L("INSERT Sale_SaleOrder(Id,OrderNo,CustomerId,OrderDate,ExpectedDate,TotalAmount,ShippedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES(38,'SAL-20260712-0038',22,'2026-07-12','2026-07-22',223862.00,0.00,1,NULL,NULL,N'新客户大单',0,'2026-07-12 08:30:00',N'zhaomin');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(185,38,1,1,20,0,2368.00,47360.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(186,38,6,1,10,0,8704.00,87040.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(187,38,11,2,15,0,1728.00,25920.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(188,38,33,1,25,0,166.00,4150.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');");
        L("INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES(189,38,57,1,16,0,3648.00,58368.00,NULL,0,'2026-07-12 08:30:00',N'zhaomin');");
        L("");

        L("SET IDENTITY_INSERT Pur_PurchaseOrder OFF;");
        L("SET IDENTITY_INSERT Pur_PurchaseOrderDetail OFF;");
        L("SET IDENTITY_INSERT Pur_PurchaseIn OFF;");
        L("SET IDENTITY_INSERT Pur_PurchaseInDetail OFF;");
        L("SET IDENTITY_INSERT Sale_SaleOrder OFF;");
        L("SET IDENTITY_INSERT Sale_SaleOrderDetail OFF;");
        L("SET IDENTITY_INSERT Sale_SaleOut OFF;");
        L("SET IDENTITY_INSERT Sale_SaleOutDetail OFF;");
        L("GO\n");
    }

    // ===== Self-Check SQL =====
    static void GenSelfChecks()
    {
        L("-- ===== Self-Check SQL =====");
        L("SELECT 'PO Status' AS Info, Status, COUNT(*) AS Cnt FROM Pur_PurchaseOrder WHERE IsDeleted=0 GROUP BY Status ORDER BY Status;");
        L("SELECT 'SO Status' AS Info, Status, COUNT(*) AS Cnt FROM Sale_SaleOrder WHERE IsDeleted=0 GROUP BY Status ORDER BY Status;");
        L("SELECT 'Repair Status' AS Info, Status, COUNT(*) AS Cnt FROM Maint_RepairOrder WHERE IsDeleted=0 GROUP BY Status ORDER BY Status;");
        L("SELECT 'Counts' AS Info, 'PO' AS Module, COUNT(*) AS Cnt FROM Pur_PurchaseOrder WHERE IsDeleted=0 UNION ALL SELECT 'Counts','SO',COUNT(*) FROM Sale_SaleOrder WHERE IsDeleted=0 UNION ALL SELECT 'Counts','Pin',COUNT(*) FROM Pur_PurchaseIn WHERE IsDeleted=0 UNION ALL SELECT 'Counts','PRet',COUNT(*) FROM Pur_PurchaseReturn WHERE IsDeleted=0 UNION ALL SELECT 'Counts','SOut',COUNT(*) FROM Sale_SaleOut WHERE IsDeleted=0 UNION ALL SELECT 'Counts','SRet',COUNT(*) FROM Sale_Return WHERE IsDeleted=0 UNION ALL SELECT 'Counts','STrans',COUNT(*) FROM Inv_StockTransfer WHERE IsDeleted=0 UNION ALL SELECT 'Counts','SCheck',COUNT(*) FROM Inv_StockCheck WHERE IsDeleted=0 UNION ALL SELECT 'Counts','Alert',COUNT(*) FROM Inv_StockAlert WHERE IsResolved=0 UNION ALL SELECT 'Counts','Device',COUNT(*) FROM Maint_DeviceArchive WHERE IsDeleted=0 UNION ALL SELECT 'Counts','Repair',COUNT(*) FROM Maint_RepairOrder WHERE IsDeleted=0 UNION ALL SELECT 'Counts','Plan',COUNT(*) FROM Maint_MaintenancePlan WHERE IsDeleted=0 UNION ALL SELECT 'Counts','Log',COUNT(*) FROM Sys_OperationLog UNION ALL SELECT 'Counts','Txn',COUNT(*) FROM Inv_StockTransaction;");
        L("SELECT 'Pending-PO' AS Info, COUNT(*) AS Cnt FROM Pur_PurchaseOrder WHERE Status=1 AND IsDeleted=0 UNION ALL SELECT 'Pending-SO',COUNT(*) FROM Sale_SaleOrder WHERE Status=1 AND IsDeleted=0 UNION ALL SELECT 'Pending-STrans',COUNT(*) FROM Inv_StockTransfer WHERE Status=1 AND IsDeleted=0 UNION ALL SELECT 'Pending-Repair',COUNT(*) FROM Maint_RepairOrder WHERE Status=0 AND IsDeleted=0;");
        L("");
    }

    // ===== Data Classes =====
    class OrderData
    {
        public int Id; public string OrderNo; public int SupId; public int WhId;
        public string OrderDate; public string ExpectedDate; public decimal TotalAmount;
        public int Status; public string Approver; public string ApproveTime; public string Creator;
        public List<DetailData> Details;
    }
    class DetailData
    {
        public int ProductId; public int Qty; public decimal UnitPrice; public decimal Amount;
    }
    class SOrderData
    {
        public int Id; public string OrderNo; public int CustId;
        public string OrderDate; public string ExpectedDate; public decimal TotalAmount;
        public decimal ShippedAmount; public int Status; public string Approver;
        public string ApproveTime; public string Creator; public string Remark;
        public List<SDetailData> Details;
    }
    class SDetailData
    {
        public int ProductId; public int Qty; public int ShippedQty;
        public decimal UnitPrice; public decimal Amount; public int WarehouseId;
    }

    // ===== 2. Sale Orders (35) + Out + Returns =====
    static void GenSaleOrders()
    {
        int orderCount = 35;
        int nextDetailId = 1, nextOutId = 1, nextOutDetailId = 1;
        int nextReturnId = 1, nextReturnDetailId = 1;
        string[] salesUsers = new string[] { "liujie", "zhaomin", "chenhui" };
        string approverUser = "wangqiang";

        // Pre-compute sale orders
        List<SOrderData> sOrders = [];
        for (int i = 0; i < orderCount; i++)
        {
            int orderId = i + 1;
            DateTime baseDate = Today.AddDays(soDays[i]);
            string orderNo = $"SAL-{baseDate:yyyyMMdd}-{orderId:0000}";
            int status = soStatus[i], custId = soCust[i];
            int whId = 1; // default warehouse
            string creator = salesUsers[i % 3];
            string approver = "NULL", approveTime = "NULL";
            if (status >= 2) { approver = "N'" + approverUser + "'"; approveTime = "'" + baseDate.AddDays(1 + rnd.Next(3)).ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
            string expectedDate = baseDate.AddDays(3 + rnd.Next(7)).ToString("yyyy-MM-dd");
            int detailCt = soDetailCt[i];
            decimal totalAmount = 0;
            List<SDetailData> details = [];
            var used = new HashSet<int>();
            for (int d = 0; d < detailCt; d++)
            {
                int pi; do { pi = rnd.Next(90); } while (used.Contains(pi));
                used.Add(pi);
                int pid = pIds[pi];
                decimal sp = RndPrice(pSP[pi]);
                int qty = RndQty(5, 80);
                decimal amt = Math.Round(sp * qty, 2);
                totalAmount += amt;
                details.Add(new SDetailData { ProductId = pid, Qty = qty, ShippedQty = 0, UnitPrice = sp, Amount = amt, WarehouseId = whId });
            }
            totalAmount = Math.Round(totalAmount, 2);
            string remark = "NULL";
            if (status == 6) remark = "N'客户取消订单'";
            sOrders.Add(new SOrderData { Id = orderId, OrderNo = orderNo, CustId = custId, OrderDate = baseDate.ToString("yyyy-MM-dd"), ExpectedDate = expectedDate, TotalAmount = totalAmount, ShippedAmount = 0, Status = status, Approver = approver, ApproveTime = approveTime, Creator = creator, Remark = remark, Details = details });
        }

        // Sale_SaleOrder
        L("-- ===== 5. Sale_SaleOrder (35) =====");
        L("SET IDENTITY_INSERT Sale_SaleOrder ON;");
        for (int i = 0; i < orderCount; i++)
        {
            var o = sOrders[i];
            L($"INSERT Sale_SaleOrder(Id,OrderNo,CustomerId,OrderDate,ExpectedDate,TotalAmount,ShippedAmount,Status,Approver,ApproveTime,Remark,IsDeleted,CreateTime,CreateBy) VALUES({o.Id},'{o.OrderNo}',{o.CustId},'{o.OrderDate}','{o.ExpectedDate}',{o.TotalAmount},0,{o.Status},{o.Approver},{o.ApproveTime},{o.Remark},0,'{o.OrderDate} 09:30:00',N'{o.Creator}');");
        }
        L("SET IDENTITY_INSERT Sale_SaleOrder OFF;");
        L("GO\n");

        // Sale_SaleOrderDetail
        L("-- ===== 6. Sale_SaleOrderDetail =====");
        L("SET IDENTITY_INSERT Sale_SaleOrderDetail ON;");
        for (int i = 0; i < orderCount; i++)
        {
            var o = sOrders[i];
            foreach (var det in o.Details)
                L($"INSERT Sale_SaleOrderDetail(Id,SaleOrderId,ProductId,WarehouseId,Qty,ShippedQty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES({nextDetailId++},{o.Id},{det.ProductId},{det.WarehouseId},{det.Qty},0,{det.UnitPrice},{det.Amount},NULL,0,'{o.OrderDate} 09:30:00',N'{o.Creator}');");
        }
        L("SET IDENTITY_INSERT Sale_SaleOrderDetail OFF;");
        L("GO\n");

        // Sale_SaleOut + Detail
        L("-- ===== 7. Sale_SaleOut + Detail =====");
        L("SET IDENTITY_INSERT Sale_SaleOut ON;");
        L("SET IDENTITY_INSERT Sale_SaleOutDetail ON;");
        var sShippedMap = new Dictionary<string, int>();
        var outDates = new Dictionary<int, DateTime>();
        for (int i = 0; i < orderCount; i++)
        {
            var o = sOrders[i];
            if (o.Status <= 1 || o.Status >= 6) continue;
            DateTime orderDate = DateTime.Parse(o.OrderDate);
            int outCount = partialOutOrders.Contains(o.Id) ? 2 : 1;
            for (int part = 0; part < outCount; part++)
            {
                int outId = nextOutId++;
                DateTime outDate = outCount == 1 ? orderDate.AddDays(2 + rnd.Next(5)) : (part == 0 ? orderDate.AddDays(2 + rnd.Next(3)) : orderDate.AddDays(8 + rnd.Next(5)));
                outDates[outId] = outDate;
                string outNo = $"SHP-{outDate:yyyyMMdd}-{outId:0000}";
                decimal outTotal = 0;
                for (int d = 0; d < o.Details.Count; d++)
                {
                    var det = o.Details[d];
                    int shipQty;
                    if (outCount == 1) shipQty = det.Qty;
                    else if (part == 0) shipQty = rnd.Next(det.Qty / 3, det.Qty * 2 / 3);
                    else { string key = o.Id + "_" + det.ProductId; int alreadyShip = sShippedMap.ContainsKey(key) ? sShippedMap[key] : 0; shipQty = det.Qty - alreadyShip; }
                    if (shipQty <= 0) continue;
                    string key2 = o.Id + "_" + det.ProductId;
                    if (!sShippedMap.ContainsKey(key2)) sShippedMap[key2] = 0;
                    sShippedMap[key2] += shipQty;
                    decimal dp = det.UnitPrice; decimal damt = Math.Round(dp * shipQty, 2);
                    outTotal += damt;
                    L($"INSERT Sale_SaleOutDetail(Id,SaleOutId,SaleOrderDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,Remark,IsDeleted,CreateTime,CreateBy) VALUES({nextOutDetailId},{outId},{d + 1},{det.ProductId},{det.WarehouseId},{shipQty},{dp},{damt},NULL,0,'{DTS(outDate, "14:00:00")}',N'{o.Creator}');");
                    sOutDetailMap[(outId, det.ProductId)] = nextOutDetailId;
                    nextOutDetailId++;
                }
                outTotal = Math.Round(outTotal, 2);
                L($"INSERT Sale_SaleOut(Id,OutNo,SaleOrderId,CustomerId,WarehouseId,OutTime,TotalAmount,Status,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({outId},'{outNo}',{o.Id},{o.CustId},{o.Details[0].WarehouseId},'{outDate:yyyy-MM-dd} 14:00:00',{outTotal},0,N'{o.Creator}',NULL,0,'{DTS(outDate, "14:00:00")}',N'{o.Creator}');");
            }
        }
        L("SET IDENTITY_INSERT Sale_SaleOutDetail OFF;");
        L("SET IDENTITY_INSERT Sale_SaleOut OFF;");
        L("GO\n");

        // Update ShippedQty
        L("-- Update ShippedQty =====");
        foreach (var kv in sShippedMap) { var p = kv.Key.Split('_'); L($"UPDATE Sale_SaleOrderDetail SET ShippedQty={kv.Value} WHERE SaleOrderId={p[0]} AND ProductId={p[1]};"); }
        L("");
        L("-- Update ShippedAmount =====");
        for (int i = 0; i < orderCount; i++) L($"UPDATE Sale_SaleOrder SET ShippedAmount=(SELECT ISNULL(SUM(so.TotalAmount),0) FROM Sale_SaleOut so WHERE so.SaleOrderId={i + 1} AND so.IsDeleted=0) WHERE Id={i + 1};");
        L("");

        // Sale Return
        L("-- ===== 8. Sale_Return (3) =====");
        L("SET IDENTITY_INSERT Sale_Return ON;");
        L("SET IDENTITY_INSERT Sale_ReturnDetail ON;");
        string[] sReasons = new string[] { "N'客户反馈设备参数不符'", "N'运输过程中外观损坏'", "N'客户要求换货'" };
        // Find OutIds for return orders
        var sOutIdsByOrder = new Dictionary<int, List<int>>();
        int outTracker = 0;
        for (int i = 0; i < orderCount; i++)
        {
            var o = sOrders[i];
            if (o.Status <= 1 || o.Status >= 6) continue;
            int oc = partialOutOrders.Contains(o.Id) ? 2 : 1;
            List<int> list = [];
            for (int p = 0; p < oc; p++) { outTracker++; list.Add(outTracker); }
            sOutIdsByOrder[o.Id] = list;
        }
        for (int r = 0; r < saleReturnOrders.Length; r++)
        {
            int orderId = saleReturnOrders[r];
            int retId = nextReturnId++;
            var outIds = sOutIdsByOrder[orderId];
            int targetOutId = outIds[rnd.Next(outIds.Count)];
            DateTime outDate = outDates[targetOutId];
            DateTime retDate = outDate.AddDays(5 + rnd.Next(15));
            string retNo = $"SLR-{retDate:yyyyMMdd}-{retId:0000}";
            var o = sOrders[orderId - 1];
            decimal retTotal = 0;
            int retItems = 1 + rnd.Next(2);
            var usedDet = new HashSet<int>();
            for (int d = 0; d < retItems; d++)
            {
                int di; do { di = rnd.Next(o.Details.Count); } while (usedDet.Contains(di));
                usedDet.Add(di);
                var det = o.Details[di];
                string key = orderId + "_" + det.ProductId;
                int totalShip = sShippedMap.ContainsKey(key) ? sShippedMap[key] : 0;
                int retQty = rnd.Next(1, Math.Max(2, totalShip / 4));
                if (retQty > totalShip) retQty = totalShip;
                if (retQty <= 0) retQty = 1;
                decimal rp = det.UnitPrice; decimal ramt = Math.Round(rp * retQty, 2);
                retTotal += ramt;
                int sodId = sOutDetailMap.TryGetValue((targetOutId, det.ProductId), out int v) ? v : 1;
                L($"INSERT Sale_ReturnDetail(Id,SaleReturnId,SaleOutDetailId,ProductId,WarehouseId,Qty,UnitPrice,Amount,IsDeleted,CreateTime,CreateBy) VALUES({nextReturnDetailId++},{retId},{sodId},{det.ProductId},{det.WarehouseId},{retQty},{rp},{ramt},0,'{DTS(retDate, "15:00:00")}',N'{o.Creator}');");
            }
            retTotal = Math.Round(retTotal, 2);
            L($"INSERT Sale_Return(Id,ReturnNo,SaleOutId,CustomerId,WarehouseId,ReturnDate,TotalAmount,Reason,Status,Approver,ApproveTime,Operator,Remark,IsDeleted,CreateTime,CreateBy) VALUES({retId},'{retNo}',{targetOutId},{o.CustId},{o.Details[0].WarehouseId},'{retDate:yyyy-MM-dd}',{retTotal},{sReasons[r]},1,N'{approverUser}','{retDate.AddDays(1):yyyy-MM-dd} 10:00:00',N'{o.Creator}',NULL,0,'{DTS(retDate, "15:00:00")}',N'{o.Creator}');");
        }
        L("SET IDENTITY_INSERT Sale_ReturnDetail OFF;");
        L("SET IDENTITY_INSERT Sale_Return OFF;");
        L("GO\n");

        // Update SaleOrder status after out/return
        L("-- Update SaleOrder Status =====");
        for (int i = 0; i < orderCount; i++)
        {
            var o = sOrders[i];
            if (o.Status <= 1 || o.Status >= 6) continue;
            L($"UPDATE Sale_SaleOrder SET Status=CASE WHEN (SELECT ISNULL(SUM(ShippedQty),0) FROM Sale_SaleOrderDetail WHERE SaleOrderId={i + 1})=0 THEN 2 WHEN (SELECT COUNT(*) FROM Sale_SaleOrderDetail WHERE SaleOrderId={i + 1} AND ShippedQty<Qty)>0 THEN 3 ELSE 4 END WHERE Id={i + 1};");
        }
        // Mark returned orders
        foreach (int rid in saleReturnOrders)
            L($"UPDATE Sale_SaleOrder SET Status=5 WHERE Id={rid};");
        L("");
    }
}
