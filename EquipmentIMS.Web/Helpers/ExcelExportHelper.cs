using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using EquipmentIMS.Models.DTOs.Reports;

namespace EquipmentIMS.Web.Helpers;

public static class ExcelExportHelper
{
    private const int MaxRowsPerSheet = 50000;

    public static byte[] ExportInventorySummary(List<InventoryReportDto> data)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);
        var styleMoney = CreateMoneyStyle(wb);

        WriteSheet(wb, "库存汇总", new[] {
            ("产品编码", 4000), ("产品名称", 6000), ("规格型号", 4000), ("品牌", 3000),
            ("仓库", 4000), ("库存数量", 3000), ("锁定数量", 3000), ("可用数量", 3000),
            ("安全库存", 3000), ("采购单价", 3500), ("库存价值", 4000), ("是否预警", 3000)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.ProductCode);
            row.CreateCell(1).SetCellValue(item.ProductName);
            row.CreateCell(2).SetCellValue(item.Spec ?? "");
            row.CreateCell(3).SetCellValue(item.Brand ?? "");
            row.CreateCell(4).SetCellValue(item.WarehouseName);
            row.CreateCell(5).SetCellValue(item.Quantity);
            row.CreateCell(6).SetCellValue(item.LockQty);
            row.CreateCell(7).SetCellValue(item.AvailableQty);
            row.CreateCell(8).SetCellValue(item.MinStock);
            var c10 = row.CreateCell(9); c10.SetCellValue((double)item.UnitPrice); c10.CellStyle = styleMoney;
            var c11 = row.CreateCell(10); c11.SetCellValue((double)item.TotalValue); c11.CellStyle = styleMoney;
            row.CreateCell(11).SetCellValue(item.IsWarning ? "是" : "否");
            if (item.IsWarning) ApplyWarningStyle(wb, row);
        }, styleHeader);

        return WriteToBytes(wb);
    }

    public static byte[] ExportStockWarnings(List<StockWarningDto> data)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);

        WriteSheet(wb, "库存预警", new[] {
            ("产品编码", 4000), ("产品名称", 6000), ("规格型号", 4000),
            ("仓库", 4000), ("当前库存", 3000), ("安全库存", 3000),
            ("缺口", 3000), ("预警等级", 3000)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.ProductCode);
            row.CreateCell(1).SetCellValue(item.ProductName);
            row.CreateCell(2).SetCellValue(item.Spec ?? "");
            row.CreateCell(3).SetCellValue(item.WarehouseName);
            row.CreateCell(4).SetCellValue(item.CurrentQty);
            row.CreateCell(5).SetCellValue(item.MinStock);
            row.CreateCell(6).SetCellValue(item.Gaps);
            row.CreateCell(7).SetCellValue(item.AlertLevel);
        }, styleHeader);

        return WriteToBytes(wb);
    }

    public static byte[] ExportStockFund(List<StockFundDto> data)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);
        var styleMoney = CreateMoneyStyle(wb);

        WriteSheet(wb, "资金占用分析", new[] {
            ("仓库名称", 5000), ("库存价值", 4500), ("占比(%)", 3000),
            ("产品数", 3000), ("总数量", 3000)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.GroupName);
            var c1 = row.CreateCell(1); c1.SetCellValue((double)item.StockValue); c1.CellStyle = styleMoney;
            row.CreateCell(2).SetCellValue((double)item.Percent);
            row.CreateCell(3).SetCellValue(item.ProductCount);
            row.CreateCell(4).SetCellValue(item.TotalQty);
        }, styleHeader);

        return WriteToBytes(wb);
    }

    public static byte[] ExportSaleRanking(List<SaleRankingDto> data, string rankBy)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);
        var styleMoney = CreateMoneyStyle(wb);

        var title = rankBy switch { "product" => "产品销售排行", "customer" => "客户销售排行", "salesman" => "销售员业绩排行", _ => "销售排行" };
        var col1 = rankBy switch { "product" => "产品名称", "customer" => "客户名称", "salesman" => "销售员", _ => "名称" };

        WriteSheet(wb, title, new[] {
            ("排名", 2000), (col1, 5000), ("出库数量", 3000),
            ("销售金额", 4500), ("订单数", 3000)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.Ranking);
            row.CreateCell(1).SetCellValue(item.Name);
            row.CreateCell(2).SetCellValue(item.TotalQuantity);
            var c3 = row.CreateCell(3); c3.SetCellValue((double)item.TotalAmount); c3.CellStyle = styleMoney;
            row.CreateCell(4).SetCellValue(item.OrderCount);
        }, styleHeader);

        return WriteToBytes(wb);
    }

    public static byte[] ExportSaleTrend(List<SaleTrendDto> data)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);
        var styleMoney = CreateMoneyStyle(wb);

        WriteSheet(wb, "销售趋势", new[] {
            ("周期", 4000), ("销售金额", 4500), ("订单数", 3000)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.Period);
            var c1 = row.CreateCell(1); c1.SetCellValue((double)item.TotalAmount); c1.CellStyle = styleMoney;
            row.CreateCell(2).SetCellValue(item.OrderCount);
        }, styleHeader);

        return WriteToBytes(wb);
    }

    public static byte[] ExportRepairStat(List<RepairStatDto> data)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);
        var styleMoney = CreateMoneyStyle(wb);

        WriteSheet(wb, "维修统计", new[] {
            ("分组", 5000), ("工单数", 3000), ("维修费用", 4000),
            ("配件费用", 4000), ("总费用", 4000), ("平均工时", 3000)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.GroupLabel);
            row.CreateCell(1).SetCellValue(item.OrderCount);
            var c2 = row.CreateCell(2); c2.SetCellValue((double)item.TotalRepairCost); c2.CellStyle = styleMoney;
            var c3 = row.CreateCell(3); c3.SetCellValue((double)item.TotalPartsCost); c3.CellStyle = styleMoney;
            var c4 = row.CreateCell(4); c4.SetCellValue((double)item.TotalCost); c4.CellStyle = styleMoney;
            row.CreateCell(5).SetCellValue(item.AvgHours);
        }, styleHeader);

        return WriteToBytes(wb);
    }

    public static byte[] ExportPartsUsage(List<PartsUsageDto> data)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);
        var styleMoney = CreateMoneyStyle(wb);

        WriteSheet(wb, "配件使用统计", new[] {
            ("配件编码", 4000), ("配件名称", 5000), ("使用次数", 3000),
            ("使用数量", 3000), ("总金额", 4500), ("平均单价", 3500)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.ProductCode);
            row.CreateCell(1).SetCellValue(item.ProductName);
            row.CreateCell(2).SetCellValue(item.UsageCount);
            row.CreateCell(3).SetCellValue(item.TotalQty);
            var c4 = row.CreateCell(4); c4.SetCellValue((double)item.TotalAmount); c4.CellStyle = styleMoney;
            var c5 = row.CreateCell(5); c5.SetCellValue((double)item.AvgUnitPrice); c5.CellStyle = styleMoney;
        }, styleHeader);

        return WriteToBytes(wb);
    }

    public static byte[] ExportPurchaseSummary(List<PurchaseSummaryDto> data)
    {
        var wb = new XSSFWorkbook();
        var styleHeader = CreateHeaderStyle(wb);
        var styleMoney = CreateMoneyStyle(wb);

        WriteSheet(wb, "采购汇总", new[] {
            ("周期", 4500), ("采购金额", 4500), ("订单数", 3500), ("供应商数", 3500)
        }, data, (row, item, i) =>
        {
            row.CreateCell(0).SetCellValue(item.Period);
            var c1 = row.CreateCell(1); c1.SetCellValue((double)item.TotalAmount); c1.CellStyle = styleMoney;
            row.CreateCell(2).SetCellValue(item.OrderCount);
            row.CreateCell(3).SetCellValue(item.SupplierCount);
        }, styleHeader);

        return WriteToBytes(wb);
    }

    private static void WriteSheet<T>(XSSFWorkbook wb, string sheetName,
        (string name, int width)[] headers, List<T> data,
        Action<IRow, T, int> fillRow, ICellStyle styleHeader)
    {
        int total = data.Count;
        int sheetCount = (total + MaxRowsPerSheet - 1) / MaxRowsPerSheet;

        for (int si = 0; si < sheetCount; si++)
        {
            var name = sheetCount == 1 ? sheetName : $"{sheetName}_{si + 1}";
            var sheet = wb.CreateSheet(name);
            var headerRow = sheet.CreateRow(0);
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = headerRow.CreateCell(c);
                cell.SetCellValue(headers[c].name);
                cell.CellStyle = styleHeader;
                sheet.SetColumnWidth(c, headers[c].width);
            }

            int start = si * MaxRowsPerSheet;
            int end = Math.Min(start + MaxRowsPerSheet, total);
            for (int i = start; i < end; i++)
            {
                var row = sheet.CreateRow(i - start + 1);
                fillRow(row, data[i], i);
            }
        }
    }

    private static ICellStyle CreateHeaderStyle(XSSFWorkbook wb)
    {
        var style = wb.CreateCellStyle();
        var font = wb.CreateFont();
        font.IsBold = true;
        font.FontHeightInPoints = 11;
        style.SetFont(font);
        style.Alignment = HorizontalAlignment.Center;
        style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
        style.FillPattern = FillPattern.SolidForeground;
        style.BorderBottom = BorderStyle.Thin;
        style.BorderTop = BorderStyle.Thin;
        style.BorderLeft = BorderStyle.Thin;
        style.BorderRight = BorderStyle.Thin;
        return style;
    }

    private static ICellStyle CreateMoneyStyle(XSSFWorkbook wb)
    {
        var style = wb.CreateCellStyle();
        style.DataFormat = wb.CreateDataFormat().GetFormat("#,##0.00");
        style.Alignment = HorizontalAlignment.Right;
        return style;
    }

    private static void ApplyWarningStyle(XSSFWorkbook wb, IRow row)
    {
        var style = wb.CreateCellStyle();
        var font = wb.CreateFont();
        font.Color = IndexedColors.Red.Index;
        style.SetFont(font);
        for (int c = 0; c < row.LastCellNum; c++)
            row.GetCell(c).CellStyle = style;
    }

    private static byte[] WriteToBytes(XSSFWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.Write(ms, leaveOpen: false);
        return ms.ToArray();
    }
}
