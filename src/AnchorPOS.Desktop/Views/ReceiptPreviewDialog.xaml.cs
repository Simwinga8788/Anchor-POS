using System.Windows;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;

namespace SurfPOS.Desktop.Views
{
    public partial class ReceiptPreviewDialog : Window
    {
        private readonly IReceiptPrinterService _receiptPrinterService;
        private readonly IAuditService _auditService;
        private readonly Transaction _transaction;
        private readonly User _cashier;

        public ReceiptPreviewDialog(IReceiptPrinterService receiptPrinterService, 
            IAuditService auditService,
            Transaction transaction, User cashier)
        {
            InitializeComponent();
            _receiptPrinterService = receiptPrinterService;
            _auditService = auditService;
            _transaction = transaction;
            _cashier = cashier;
            
            LoadReceiptPreview();
        }

        private void LoadReceiptPreview()
        {
            // Generate the receipt content
            var receiptContent = GenerateReceiptPreview();
            ReceiptTextBlock.Text = receiptContent;
        }

        private string GenerateReceiptPreview()
        {
            const int RECEIPT_WIDTH = 32;
            var sb = new System.Text.StringBuilder();
            
            // Header
            sb.AppendLine(Center("KENJI'S BEAUTY SPACE", RECEIPT_WIDTH));
            sb.AppendLine(Center("Point of Sale", RECEIPT_WIDTH));
            sb.AppendLine(new string('-', RECEIPT_WIDTH));
            sb.AppendLine();
            
            // Transaction Info
            sb.AppendLine($"Receipt #: {_transaction.Id}");
            sb.AppendLine($"Date: {_transaction.Date:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cashier: {_cashier.Username}");
            sb.AppendLine($"Payment: {_transaction.PaymentMethod}");
            sb.AppendLine(new string('-', RECEIPT_WIDTH));
            sb.AppendLine();
            
            // Items
            sb.AppendLine("ITEMS:");
            sb.AppendLine();
            
            foreach (var item in _transaction.Items)
            {
                var productName = item.Product?.Name ?? "Unknown";
                var qty = item.Quantity;
                var price = item.UnitPrice;
                var total = qty * price;
                
                // Product name
                sb.AppendLine(productName);
                
                // Quantity x Price = Total
                var qtyLine = $"  {qty} x {price:C2}";
                var totalStr = $"{total:C2}";
                var spacing = RECEIPT_WIDTH - qtyLine.Length - totalStr.Length;
                sb.AppendLine($"{qtyLine}{new string(' ', spacing)}{totalStr}");
                sb.AppendLine();
            }
            
            sb.AppendLine(new string('-', RECEIPT_WIDTH));
            
            // Total
            var totalLine = "TOTAL:";
            var totalAmount = $"{_transaction.TotalAmount:C2}";
            var totalSpacing = RECEIPT_WIDTH - totalLine.Length - totalAmount.Length;
            sb.AppendLine($"{totalLine}{new string(' ', totalSpacing)}{totalAmount}");
            
            sb.AppendLine(new string('=', RECEIPT_WIDTH));
            sb.AppendLine();
            
            // Footer
            sb.AppendLine(Center("Thank you for shopping!", RECEIPT_WIDTH));
            sb.AppendLine(Center("Please come again", RECEIPT_WIDTH));
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

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var printed = await _receiptPrinterService.PrintReceiptAsync(_transaction, _cashier);
            
            if (printed)
            {
                // Log the print action
                await _auditService.LogActionAsync(_cashier.Id, "Print Receipt", $"Receipt #{_transaction.Id} printed");

                MessageBox.Show("Receipt sent to printer!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Failed to print receipt.\nCheck printer connection.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
