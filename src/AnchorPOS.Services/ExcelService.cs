using ClosedXML.Excel;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;

namespace SurfPOS.Services
{
    public class ExcelService : IExcelService
    {
        public async Task<List<Product>> ImportProductsFromExcelAsync(string filePath)
        {
            var products = new List<Product>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            
            var rows = worksheet.RowsUsed().Skip(1); // Skip header row

            if (!rows.Any())
                return products;

            // Read headers from first row
            var headerRow = worksheet.Row(1);
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            for (int col = 1; col <= headerRow.CellsUsed().Count(); col++)
            {
                var header = headerRow.Cell(col).GetString().Trim().Replace("*", "").Trim();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    columnMap[header] = col;
                }
            }

            // Helper function
            string GetCellValue(IXLRow row, params string[] possibleHeaders)
            {
                foreach (var header in possibleHeaders)
                {
                    if (columnMap.TryGetValue(header, out int col))
                    {
                        return row.Cell(col).GetString().Trim();
                    }
                }
                return "";
            }

            foreach (var row in rows)
            {
                try
                {
                    var barcode = GetCellValue(row, "Barcode", "Code", "Bar Code");
                    var name = GetCellValue(row, "Name", "Product Name", "ProductName", "Item Name");
                    var category = GetCellValue(row, "Category", "Product Category", "Type");
                    var priceStr = GetCellValue(row, "Price", "Selling Price", "Sale Price", "Unit Price");
                    var costPriceStr = GetCellValue(row, "Cost Price", "CostPrice", "Cost", "Purchase Price");
                    var stockStr = GetCellValue(row, "Stock Quantity", "StockQuantity", "Stock", "Quantity", "Qty");
                    var thresholdStr = GetCellValue(row, "Low Stock Threshold", "LowStockThreshold", "Threshold", "Min Stock");

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var product = new Product
                    {
                        Barcode = barcode,
                        Name = name,
                        Category = string.IsNullOrWhiteSpace(category) ? "General" : category,
                        Price = decimal.TryParse(priceStr, out decimal price) ? price : 0,
                        CostPrice = decimal.TryParse(costPriceStr, out decimal costPrice) ? costPrice : 0,
                        StockQuantity = int.TryParse(stockStr, out int stock) ? stock : 0,
                        LowStockThreshold = int.TryParse(thresholdStr, out int threshold) ? threshold : 5,
                        IsActive = true
                    };

                    products.Add(product);
                }
                catch
                {
                    continue;
                }
            }

            return await Task.FromResult(products);
        }

        public async Task ExportProductsToExcelAsync(string filePath, IEnumerable<Product> products)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Products");

            // Headers
            worksheet.Cell(1, 1).Value = "Barcode";
            worksheet.Cell(1, 2).Value = "Name";
            worksheet.Cell(1, 3).Value = "Category";
            worksheet.Cell(1, 4).Value = "Price";
            worksheet.Cell(1, 5).Value = "Cost Price";
            worksheet.Cell(1, 6).Value = "Stock";
            worksheet.Cell(1, 7).Value = "Low Stock Threshold";
            worksheet.Cell(1, 8).Value = "Status";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data
            int row = 2;
            foreach (var product in products)
            {
                worksheet.Cell(row, 1).Value = product.Barcode;
                worksheet.Cell(row, 2).Value = product.Name;
                worksheet.Cell(row, 3).Value = product.Category;
                worksheet.Cell(row, 4).Value = product.Price;
                worksheet.Cell(row, 5).Value = product.CostPrice;
                worksheet.Cell(row, 6).Value = product.StockQuantity;
                worksheet.Cell(row, 7).Value = product.LowStockThreshold;
                worksheet.Cell(row, 8).Value = product.IsActive ? "Active" : "Inactive";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
            await Task.CompletedTask;
        }

        public async Task ExportTransactionsToExcelAsync(string filePath, IEnumerable<Transaction> transactions, DateTime startDate, DateTime endDate)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sales Report");

            // --- STYLING CONSTANTS ---
            var primaryColor = XLColor.FromHtml("#2c3e50"); // Dark Blue-Grey
            var accentColor = XLColor.FromHtml("#3498db");  // Professional Blue
            var headerBg = XLColor.FromHtml("#ecf0f1");     // Light Grey
            var successColor = XLColor.FromHtml("#27ae60"); // Green
            var warningColor = XLColor.FromHtml("#e74c3c"); // Red

            // --- 1. CALCULATIONS ---
            var allItems = transactions.SelectMany(t => t.Items).ToList();
            
            // Calculate Product Performance
            var productPerformance = allItems
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    Name = g.First().Product?.Name ?? "Unknown Product",
                    Category = g.First().Product?.Category ?? "General",
                    Quantity = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(p => p.Quantity) // Sort by Quantity for Most Sold
                .ToList();

            var mostSold = productPerformance.FirstOrDefault();
            var leastSold = productPerformance.LastOrDefault();
            var topRevenue = productPerformance.OrderByDescending(p => p.Revenue).FirstOrDefault();

            decimal totalRevenue = transactions.Sum(t => t.TotalAmount);
            int totalTransactions = transactions.Count();
            int totalMainItems = allItems.Sum(i => i.Quantity);
            decimal avgTicket = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

            // --- 2. REPORT HEADER ---
            worksheet.Cell(1, 1).Value = "KENJI'S BEAUTY SPACE";
            worksheet.Cell(1, 1).Style.Font.FontSize = 24;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontColor = primaryColor;

            worksheet.Cell(2, 1).Value = "SALES PERFORMANCE REPORT";
            worksheet.Cell(2, 1).Style.Font.FontSize = 14;
            worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            worksheet.Cell(3, 1).Value = $"Period: {startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}";
            worksheet.Cell(4, 1).Value = $"Generated: {DateTime.Now:MMM dd, yyyy HH:mm}";

            // --- 3. EXECUTIVE SUMMARY DASHBOARD ---
            int dashStart = 6;
            
            // Box 1: Revenue
            DrawDashboardCard(worksheet, dashStart, 1, "TOTAL REVENUE", totalRevenue.ToString("C2"), successColor);
            // Box 2: Transactions
            DrawDashboardCard(worksheet, dashStart, 3, "TRANSACTIONS", totalTransactions.ToString(), primaryColor);
            // Box 3: Items Sold
            DrawDashboardCard(worksheet, dashStart, 5, "ITEMS SOLD", totalMainItems.ToString(), accentColor);
            // Box 4: Avg Ticket
            DrawDashboardCard(worksheet, dashStart, 7, "AVG TICKET", avgTicket.ToString("C2"), XLColor.Gray);

            // --- 4. HIGHLIGHTS (Most/Least Sold) ---
            int highStart = dashStart + 4;
            worksheet.Cell(highStart, 1).Value = "Performance Highlights";
            worksheet.Cell(highStart, 1).Style.Font.Bold = true;
            worksheet.Cell(highStart, 1).Style.Font.FontSize = 12;
            worksheet.Cell(highStart, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            
            worksheet.Range(highStart, 1, highStart, 8).Merge();

            int hRow = highStart + 2;
            // Most Sold
            worksheet.Cell(hRow, 1).Value = "🏆 Best Selling Item:";
            worksheet.Cell(hRow, 1).Style.Font.Bold = true;
            worksheet.Cell(hRow, 2).Value = mostSold != null ? $"{mostSold.Name}" : "N/A";
            worksheet.Cell(hRow, 3).Value = mostSold != null ? $"{mostSold.Quantity} units" : "";
            worksheet.Cell(hRow, 3).Style.Font.FontColor = successColor;
            worksheet.Cell(hRow, 3).Style.Font.Bold = true;

            // Least Sold
            worksheet.Cell(hRow + 1, 1).Value = "⚠️ Least Selling Item:";
            worksheet.Cell(hRow + 1, 1).Style.Font.Bold = true;
            worksheet.Cell(hRow + 1, 2).Value = leastSold != null ? $"{leastSold.Name}" : "N/A";
            worksheet.Cell(hRow + 1, 3).Value = leastSold != null ? $"{leastSold.Quantity} units" : "";
            worksheet.Cell(hRow + 1, 3).Style.Font.FontColor = warningColor;

            // Top Revenue
            worksheet.Cell(hRow + 2, 1).Value = "💰 Top Revenue Earner:";
            worksheet.Cell(hRow + 2, 1).Style.Font.Bold = true;
            worksheet.Cell(hRow + 2, 2).Value = topRevenue != null ? $"{topRevenue.Name}" : "N/A";
            worksheet.Cell(hRow + 2, 3).Value = topRevenue != null ? $"{topRevenue.Revenue:C2}" : "";

            // --- 5. TRANSACTION HISTORY TABLE ---
            int transRow = hRow + 5;
            worksheet.Cell(transRow, 1).Value = "Transaction Data";
            worksheet.Cell(transRow, 1).Style.Font.Bold = true;
            worksheet.Cell(transRow, 1).Style.Font.FontSize = 12;
            
            // Table Headers
            var headers = new[] { "Ref", "Date", "Salesperson", "Products", "Payment", "Items Qty", "Amount" };
            for(int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(transRow + 1, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = primaryColor;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int currRow = transRow + 2;
            foreach(var t in transactions.OrderByDescending(x => x.Date))
            {
                worksheet.Cell(currRow, 1).Value = t.TransactionRef;
                worksheet.Cell(currRow, 2).Value = t.Date;
                worksheet.Cell(currRow, 2).Style.NumberFormat.Format = "yyyy-MM-dd HH:mm";
                worksheet.Cell(currRow, 3).Value = t.User?.Username ?? "-";
                
                // Format product list: "Product A (x2), Product B (x1)"
                var productList = string.Join(", ", t.Items.Select(i => $"{i.Product?.Name ?? "Unknown"} (x{i.Quantity})"));
                worksheet.Cell(currRow, 4).Value = productList;
                
                worksheet.Cell(currRow, 5).Value = t.PaymentMethod.ToString();
                worksheet.Cell(currRow, 6).Value = t.Items.Sum(i => i.Quantity);
                worksheet.Cell(currRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currRow, 7).Value = t.TotalAmount;
                worksheet.Cell(currRow, 7).Style.NumberFormat.Format = "\"K\"#,##0.00";
                
                // Zebra Striping
                if (currRow % 2 == 0) 
                    worksheet.Range(currRow, 1, currRow, 7).Style.Fill.BackgroundColor = headerBg;
                
                currRow++;
            }
            
            // Borders
            var transTable = worksheet.Range(transRow + 1, 1, currRow - 1, 7);
            transTable.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            transTable.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // --- 6. PRODUCT BREAKDOWN TABLE (Side by Side or Below) ---
            var sheet2 = workbook.Worksheets.Add("Product Analysis");
            
            sheet2.Cell(1,1).Value = "Product Performance Analysis";
            sheet2.Cell(1,1).Style.Font.FontSize = 16;
            sheet2.Cell(1,1).Style.Font.Bold = true;
            sheet2.Cell(1,1).Style.Font.FontColor = primaryColor;

            var pHeaders = new[] { "Product Name", "Category", "Qty Sold", "Total Revenue", "% of Sales" };
            for (int i = 0; i < pHeaders.Length; i++)
            {
                var cell = sheet2.Cell(3, i + 1);
                cell.Value = pHeaders[i];
                cell.Style.Fill.BackgroundColor = accentColor;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
            }

            int pRow = 4;
            foreach(var p in productPerformance)
            {
                sheet2.Cell(pRow, 1).Value = p.Name;
                sheet2.Cell(pRow, 2).Value = p.Category;
                sheet2.Cell(pRow, 3).Value = p.Quantity;
                sheet2.Cell(pRow, 4).Value = p.Revenue;
                sheet2.Cell(pRow, 4).Style.NumberFormat.Format = "\"K\"#,##0.00";
                
                double share = totalRevenue > 0 ? (double)(p.Revenue / totalRevenue) : 0;
                sheet2.Cell(pRow, 5).Value = share;
                sheet2.Cell(pRow, 5).Style.NumberFormat.Format = "0.0%";

                pRow++;
            }

            // Auto-fit columns (no fixed widths)
            sheet2.Columns().AdjustToContents();
            worksheet.Columns().AdjustToContents();
            
            workbook.SaveAs(filePath);
            await Task.CompletedTask;
        }

        private void DrawDashboardCard(IXLWorksheet ws, int row, int col, string title, string value, XLColor color)
        {
            var card = ws.Range(row, col, row + 2, col + 1);
            card.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            card.Style.Border.OutsideBorderColor = color;
            
            ws.Cell(row, col).Value = title;
            var titleRange = ws.Range(row, col, row, col + 1).Merge();
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontColor = XLColor.White;
            titleRange.Style.Fill.BackgroundColor = color;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row + 1, col).Value = value;
            var valueRange = ws.Range(row + 1, col, row + 2, col + 1).Merge();
            valueRange.Style.Font.FontSize = 16;
            valueRange.Style.Font.Bold = true;
            valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            valueRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        public async Task<byte[]> GenerateProductTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Product Import Template");

            // Headers
            worksheet.Cell(1, 1).Value = "Barcode";
            worksheet.Cell(1, 2).Value = "Name *";
            worksheet.Cell(1, 3).Value = "Category";
            worksheet.Cell(1, 4).Value = "Price *";
            worksheet.Cell(1, 5).Value = "Cost Price *";
            worksheet.Cell(1, 6).Value = "Stock Quantity";
            worksheet.Cell(1, 7).Value = "Low Stock Threshold";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // Sample data
            worksheet.Cell(2, 1).Value = "A1000";
            worksheet.Cell(2, 2).Value = "Sample Product";
            worksheet.Cell(2, 3).Value = "General";
            worksheet.Cell(2, 4).Value = 10.99;
            worksheet.Cell(2, 5).Value = 5.00;
            worksheet.Cell(2, 6).Value = 100;
            worksheet.Cell(2, 7).Value = 10;

            // Instructions
            worksheet.Cell(4, 1).Value = "Instructions:";
            worksheet.Cell(4, 1).Style.Font.Bold = true;
            worksheet.Cell(5, 1).Value = "1. Fill in product details starting from row 2";
            worksheet.Cell(6, 1).Value = "2. Fields marked with * are required";
            worksheet.Cell(7, 1).Value = "3. Barcode: Enter valid barcode OR leave blank to auto-generate";
            worksheet.Cell(8, 1).Value = "4. Categories: Hair Products, Wigs, Perfumes, Makeup, Clothes, General";

            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return await Task.FromResult(stream.ToArray());
        }
    }
}
