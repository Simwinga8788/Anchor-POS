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
            // Generate the receipt content using the shared service logic
            // This ensures exact match between preview and printed receipt
            var receiptContent = _receiptPrinterService.GenerateReceiptContent(_transaction, _cashier);
            ReceiptTextBlock.Text = receiptContent;
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (InvalidOperationException ex)
            {
                // Handle specific printer-related errors
                MessageBox.Show($"Printer Error:\n\n{ex.Message}\n\nPlease check your Printer Configuration in Settings.", 
                    "Printer Not Available",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (ArgumentNullException ex)
            {
                // Handle data validation errors
                MessageBox.Show($"Data Error:\n\n{ex.Message}", 
                    "Invalid Data",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                MessageBox.Show($"An unexpected error occurred:\n\n{ex.Message}\n\nPlease contact support if this issue persists.", 
                    "Error",
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
