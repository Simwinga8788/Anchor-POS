using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace SurfPOS.Services
{
    [SupportedOSPlatform("windows")]
    public class ReceiptPrinterService : IReceiptPrinterService
    {
        private readonly SurfPOS.Data.SurfDbContext? _context;
        private string? _receiptContent;
        private const int RECEIPT_WIDTH = 42; 
        private string _printDensity = "Normal";

        public ReceiptPrinterService(SurfPOS.Data.SurfDbContext? context = null)
        {
            _context = context;
        }

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
                // Check if printing is enabled in config
                bool printEnabled = true; // Default
                try
                {
                    var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS", "store_config.json");
                    if (File.Exists(configPath))
                    {
                         using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                         {
                             if (doc.RootElement.TryGetProperty("PrintEnabled", out var enabledProp))
                                 printEnabled = enabledProp.GetBoolean();
                         }
                    }
                    else if (_context != null)
                    {
                        var setting = _context.AppSettings.AsNoTracking().FirstOrDefault(s => s.Key == "PrintEnabled")?.Value;
                        if (setting != null && bool.TryParse(setting, out bool isEnabled)) printEnabled = isEnabled;
                    }
                }
                catch { }

                if (!printEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("Printing is disabled in settings. Skipping print.");
                    return true; // Return success immediately
                }

                // Check if any printer is available
                if (!IsPrinterAvailable())
                {
                    // Instead of throwing, just return false (fail gracefully)
                    // This allows the system to work without a printer
                    System.Diagnostics.Debug.WriteLine("No printer installed. Receipt skipped.");
                    return false; 
                }

                // Validate transaction data
                if (transaction == null)
                {
                    throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null.");
                }

                if (cashier == null)
                {
                    throw new ArgumentNullException(nameof(cashier), "Cashier cannot be null.");
                }

                if (transaction.Items == null || !transaction.Items.Any())
                {
                    throw new InvalidOperationException("Transaction must have at least one item.");
                }

                _receiptContent = GenerateReceiptContent(transaction, cashier);
                
                var printDocument = new PrintDocument();
                printDocument.PrintPage += PrintDocument_PrintPage;
                
                // Try to use saved printer preference (File > DB)
                string? savedPrinter = null;
                
                // 1. Check local file (most reliable)
                try
                {
                    var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS", "printer_config.txt");
                    if (File.Exists(configPath))
                    {
                        savedPrinter = File.ReadAllText(configPath).Trim();
                        System.Diagnostics.Debug.WriteLine($"File printer config found: '{savedPrinter}'");
                    }
                }
                catch (Exception fileEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading printer config file: {fileEx.Message}");
                }

                // 2. Fallback to DB if file empty/missing
                if (string.IsNullOrEmpty(savedPrinter) && _context != null)
                {
                    try 
                    {
                        savedPrinter = _context.AppSettings
                            .AsNoTracking()
                            .FirstOrDefault(s => s.Key == "ReceiptPrinter")?.Value;
                        System.Diagnostics.Debug.WriteLine($"DB ReceiptSettings Value: '{savedPrinter}'");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading settings: {ex.Message}");
                    }
                }

                // Set printer name
                if (!string.IsNullOrEmpty(savedPrinter))
                {
                    printDocument.PrinterSettings.PrinterName = savedPrinter;
                    System.Diagnostics.Debug.WriteLine($"Using saved printer: {savedPrinter}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Using default printer: {printDocument.PrinterSettings.PrinterName}");
                }
                
                // Verify printer settings
                if (string.IsNullOrEmpty(printDocument.PrinterSettings.PrinterName))
                {
                    throw new InvalidOperationException("No printer is configured. Please select a printer in Settings.");
                }

                if (!printDocument.PrinterSettings.IsValid)
                {
                    throw new InvalidOperationException($"Printer '{printDocument.PrinterSettings.PrinterName}' is not valid or not accessible. Please check Settings.");
                }

                // Log printer details
                System.Diagnostics.Debug.WriteLine($"Printer: {printDocument.PrinterSettings.PrinterName}");
                System.Diagnostics.Debug.WriteLine($"Is Valid: {printDocument.PrinterSettings.IsValid}");
                System.Diagnostics.Debug.WriteLine($"Can Duplex: {printDocument.PrinterSettings.CanDuplex}");
                System.Diagnostics.Debug.WriteLine($"Default Page Settings - Width: {printDocument.DefaultPageSettings.PaperSize.Width}, Height: {printDocument.DefaultPageSettings.PaperSize.Height}");

                // Don't set custom paper size - use printer's default
                // Some drivers don't support custom paper sizes
                // Configure margins for thermal printer
                printDocument.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(5, 5, 5, 5);
                
                // Use default printer
                printDocument.Print();
                
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                System.Diagnostics.Debug.WriteLine($"Print Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Re-throw to allow caller to handle
                throw;
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
                
                // Try to use saved printer preference (File > DB)
                string? savedPrinter = null;
                
                try
                {
                    var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS", "printer_config.txt");
                    if (File.Exists(configPath))
                    {
                        savedPrinter = File.ReadAllText(configPath).Trim();
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(savedPrinter) && _context != null)
                {
                    try
                    {
                        savedPrinter = _context.AppSettings
                            .AsNoTracking()
                            .FirstOrDefault(s => s.Key == "ReceiptPrinter")?.Value;
                    }
                    catch (Exception) { /* Ignore error */ }
                }

                if (!string.IsNullOrEmpty(savedPrinter))
                {
                    printDocument.PrinterSettings.PrinterName = savedPrinter;
                }

                if (!printDocument.PrinterSettings.IsValid)
                {
                    // If printer is invalid, just return false, don't crash
                    return false;
                }

                printDocument.PrintPage += (sender, e) =>
                {
                    // Send raw bytes to printer to open drawer
                    // Note: This relies on the driver supporting raw text/commands
                    // If the driver interprets this as text, it will execute or print garbage
                    // But it should not crash the app
                    e.Graphics?.DrawString(Encoding.ASCII.GetString(drawerCommand), 
                        new Font("Courier New", 8), Brushes.Black, 0, 0);
                };
                
                printDocument.Print();
                
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                // Log exception but return false to avoid crashing the flow
                System.Diagnostics.Debug.WriteLine($"Error opening cash drawer: {ex.Message}");
                return false;
            }
        }

        public string GenerateReceiptContent(Transaction transaction, User cashier)
        {
            var sb = new StringBuilder();
            
            // Get Store Info & Configuration
            string storeName = "Anchor POS";
            string storeAddress = "Point of Sale";
            string storePhone = "";
            int receiptWidth = 42; // Default to 80mm

            // Try File Config First
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS", "store_config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("StoreName", out var nameProp)) storeName = nameProp.GetString() ?? storeName;
                        if (root.TryGetProperty("StoreAddress", out var addrProp)) storeAddress = addrProp.GetString() ?? storeAddress;
                        if (root.TryGetProperty("StorePhone", out var phoneProp)) storePhone = phoneProp.GetString() ?? "";
                        if (root.TryGetProperty("PaperSize", out var sizeProp))
                        {
                             if (sizeProp.GetString() == "58mm") receiptWidth = 32;
                        }
                        if (root.TryGetProperty("PrintDensity", out var densityProp))
                        {
                            _printDensity = densityProp.GetString() ?? "Normal";
                        }
                    }
                }
                else if (_context != null) // Fallback to DB
                {
                     var settings = _context.AppSettings.AsNoTracking().ToList();
                     storeName = settings.FirstOrDefault(s => s.Key == "StoreName")?.Value ?? storeName;
                     storeAddress = settings.FirstOrDefault(s => s.Key == "StoreAddress")?.Value ?? storeAddress;
                     storePhone = settings.FirstOrDefault(s => s.Key == "StorePhone")?.Value ?? "";
                     
                     var paperSize = settings.FirstOrDefault(s => s.Key == "PaperSize")?.Value;
                     if (paperSize == "58mm") receiptWidth = 32;

                     _printDensity = settings.FirstOrDefault(s => s.Key == "PrintDensity")?.Value ?? "Normal";
                }
            }
            catch { /* Ignore errors, use defaults */ }

            // Header
            sb.AppendLine(Center(storeName, receiptWidth));
            sb.AppendLine(Center("Point of Sale", receiptWidth));
                
            sb.AppendLine(new string('-', receiptWidth));
            sb.AppendLine();
            
            // Transaction Info
            sb.AppendLine($"Receipt #: {transaction.Id}");
            sb.AppendLine($"Date: {transaction.Date:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cashier: {cashier.Username}");
            sb.AppendLine($"Payment: {transaction.PaymentMethod}");
            sb.AppendLine(new string('-', receiptWidth));
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
                
                // Ensure spacing is non-negative
                var spacingCount = receiptWidth - qtyLine.Length - totalStr.Length;
                if (spacingCount < 1) spacingCount = 1;

                sb.AppendLine($"{qtyLine}{new string(' ', spacingCount)}{totalStr}");
                sb.AppendLine();
            }
            
            sb.AppendLine(new string('-', receiptWidth));
            
            // Total
            var totalLine = "TOTAL:";
            var totalAmount = $"K{transaction.TotalAmount:N2}";
            var totalSpacing = receiptWidth - totalLine.Length - totalAmount.Length;
            if (totalSpacing < 1) totalSpacing = 1;
            
            sb.AppendLine($"{totalLine}{new string(' ', totalSpacing)}{totalAmount}");
            
            sb.AppendLine(new string('=', receiptWidth));
            sb.AppendLine();
            
            // Footer
            sb.AppendLine(Center("Thank you for shopping!", receiptWidth));
            sb.AppendLine(Center("Please come again", receiptWidth));
            sb.AppendLine();
            
            // Address and Phone at the bottom
            if (!string.IsNullOrEmpty(storeAddress))
                sb.AppendLine(Center(storeAddress, receiptWidth));
            if (!string.IsNullOrEmpty(storePhone))
                sb.AppendLine(Center(storePhone, receiptWidth));

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            
            return sb.ToString();
        }

        private string Center(string text, int width)
        {
            if (text.Length >= width)
                return text;
            
            var padding = (width - text.Length) / 2;
            return new string(' ', padding) + text;
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PrintDocument_PrintPage called");
            
            if (_receiptContent == null || e.Graphics == null)
            {
                return;
            }
            
            // Determine font style based on density
            var fontStyle = FontStyle.Regular;
            if (_printDensity == "Bold") fontStyle = FontStyle.Bold;

            var font = new Font("Courier New", 7, fontStyle);
            var brush = Brushes.Black;
            
            // Print logic
            e.Graphics.DrawString(_receiptContent, font, brush, 5, 5); 

            // Double Strike for "Medium" density
            if (_printDensity == "Medium")
            {
                // Print again with tiny offset to thicken lines without full bold
                e.Graphics.DrawString(_receiptContent, font, brush, 5.1f, 5); 
            }
            
            e.HasMorePages = false;
        }
    }
}
