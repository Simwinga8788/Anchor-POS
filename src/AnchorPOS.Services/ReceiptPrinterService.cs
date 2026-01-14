using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;
using System.Text;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;

namespace SurfPOS.Services
{
    [SupportedOSPlatform("windows")]
    public class ReceiptPrinterService : IReceiptPrinterService
    {
        private string? _receiptContent;
        private const int RECEIPT_WIDTH = 32; // Characters for 80mm thermal printer

        public bool IsPrinterAvailable()
        {
            try
            {
                return PrinterSettings.InstalledPrinters.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public string GetDefaultPrinterName()
        {
            try
            {
                var settings = new PrinterSettings();
                return settings.PrinterName;
            }
            catch
            {
                return "No printer found";
            }
        }

        public async Task<bool> PrintReceiptAsync(Transaction transaction, User cashier)
        {
            try
            {
                _receiptContent = GenerateReceiptContent(transaction, cashier);
                
                var printDocument = new PrintDocument();
                printDocument.PrintPage += PrintDocument_PrintPage;
                
                // Use default printer
                printDocument.Print();
                
                await Task.CompletedTask;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> OpenCashDrawerAsync()
        {
            try
            {
                // ESC/POS command to open cash drawer
                // ESC p m t1 t2 (0x1B 0x70 0x00 0x19 0xFA)
                var drawerCommand = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
                
                var printDocument = new PrintDocument();
                printDocument.PrintPage += (sender, e) =>
                {
                    // Send raw bytes to printer to open drawer
                    e.Graphics?.DrawString(Encoding.ASCII.GetString(drawerCommand), 
                        new Font("Courier New", 8), Brushes.Black, 0, 0);
                };
                
                printDocument.Print();
                
                await Task.CompletedTask;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GenerateReceiptContent(Transaction transaction, User cashier)
        {
            var sb = new StringBuilder();
            
            // Header
            sb.AppendLine(Center("KENJI'S BEAUTY SPACE"));
            sb.AppendLine(Center("Point of Sale"));
            sb.AppendLine(new string('-', RECEIPT_WIDTH));
            sb.AppendLine();
            
            // Transaction Info
            sb.AppendLine($"Receipt #: {transaction.Id}");
            sb.AppendLine($"Date: {transaction.Date:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cashier: {cashier.Username}");
            sb.AppendLine($"Payment: {transaction.PaymentMethod}");
            sb.AppendLine(new string('-', RECEIPT_WIDTH));
            sb.AppendLine();
            
            // Items
            sb.AppendLine("ITEMS:");
            sb.AppendLine();
            
            foreach (var item in transaction.Items)
            {
                var productName = item.Product?.Name ?? "Unknown";
                var qty = item.Quantity;
                var price = item.UnitPrice;
                var total = qty * price; // Calculate total
                
                // Product name (may wrap)
                sb.AppendLine(productName);
                
                // Quantity x Price = Total
                var qtyLine = $"  {qty} x K{price:N2}";
                var totalStr = $"K{total:N2}";
                var spacing = RECEIPT_WIDTH - qtyLine.Length - totalStr.Length;
                sb.AppendLine($"{qtyLine}{new string(' ', spacing)}{totalStr}");
                sb.AppendLine();
            }
            
            sb.AppendLine(new string('-', RECEIPT_WIDTH));
            
            // Total
            var totalLine = "TOTAL:";
            var totalAmount = $"K{transaction.TotalAmount:N2}";
            var totalSpacing = RECEIPT_WIDTH - totalLine.Length - totalAmount.Length;
            sb.AppendLine($"{totalLine}{new string(' ', totalSpacing)}{totalAmount}");
            
            sb.AppendLine(new string('=', RECEIPT_WIDTH));
            sb.AppendLine();
            
            // Footer
            sb.AppendLine(Center("Thank you for shopping!"));
            sb.AppendLine(Center("Please come again"));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            
            return sb.ToString();
        }

        private string Center(string text)
        {
            if (text.Length >= RECEIPT_WIDTH)
                return text;
            
            var padding = (RECEIPT_WIDTH - text.Length) / 2;
            return new string(' ', padding) + text;
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_receiptContent == null || e.Graphics == null)
                return;
            
            // Use monospace font for proper alignment
            var font = new Font("Courier New", 9);
            var brush = Brushes.Black;
            
            // Print the receipt content
            e.Graphics.DrawString(_receiptContent, font, brush, 0, 0);
            
            e.HasMorePages = false;
        }
    }
}
