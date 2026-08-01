using System.Text;

namespace ShopPro.Core.Services
{
    public class PdfExcelExporter
    {
        public static string ExportToPdfText(ProfitLossSummary pl, GstTaxLiabilitySummary gst, DateTime startDate, DateTime endDate)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine("         SHOPPRO RETAIL FINANCIAL REPORT          ");
            sb.AppendLine("=================================================");
            sb.AppendLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("-------------------------------------------------");
            sb.AppendLine("1. PROFIT & LOSS SUMMARY");
            sb.AppendLine($"   Gross Revenue:        ₹{pl.GrossRevenue:N2}");
            sb.AppendLine($"   Cost of Goods (COGS): ₹{pl.TotalCogs:N2}");
            sb.AppendLine($"   Gross Profit:         ₹{pl.GrossProfit:N2}");
            sb.AppendLine($"   Operating Expenses:   ₹{pl.OperatingExpenses:N2}");
            sb.AppendLine($"   NET PROFIT:           ₹{pl.NetProfit:N2} ({pl.NetProfitMarginPercent}%)");
            sb.AppendLine("-------------------------------------------------");
            sb.AppendLine("2. GST TAX LIABILITY SUMMARY");
            sb.AppendLine($"   Total Tax Collected:  ₹{gst.TotalTaxCollected:N2}");
            sb.AppendLine($"   CGST (Central Tax):   ₹{gst.TotalCgstCollected:N2}");
            sb.AppendLine($"   SGST (State Tax):     ₹{gst.TotalSgstCollected:N2}");
            sb.AppendLine("=================================================");
            return sb.ToString();
        }

        public static string ExportToExcelXml(List<ProductVelocity> velocities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.AppendLine(" <Worksheet ss:Name=\"Product Velocity\">");
            sb.AppendLine("  <Table>");
            sb.AppendLine("   <Row>");
            sb.AppendLine("    <Cell><Data ss:Type=\"String\">SKU</Data></Cell>");
            sb.AppendLine("    <Cell><Data ss:Type=\"String\">Product Name</Data></Cell>");
            sb.AppendLine("    <Cell><Data ss:Type=\"String\">Units Sold</Data></Cell>");
            sb.AppendLine("    <Cell><Data ss:Type=\"String\">Revenue (INR)</Data></Cell>");
            sb.AppendLine("    <Cell><Data ss:Type=\"String\">Status</Data></Cell>");
            sb.AppendLine("   </Row>");

            foreach (var v in velocities)
            {
                var status = v.IsFastMoving ? "Fast Moving" : (v.IsDeadStock ? "Dead Stock" : "Regular");
                sb.AppendLine("   <Row>");
                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{v.Sku}</Data></Cell>");
                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{v.ProductName}</Data></Cell>");
                sb.AppendLine($"    <Cell><Data ss:Type=\"Number\">{v.UnitsSold}</Data></Cell>");
                sb.AppendLine($"    <Cell><Data ss:Type=\"Number\">{v.RevenueGenerated}</Data></Cell>");
                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{status}</Data></Cell>");
                sb.AppendLine("   </Row>");
            }

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");
            sb.AppendLine("</Workbook>");
            return sb.ToString();
        }
    }
}
