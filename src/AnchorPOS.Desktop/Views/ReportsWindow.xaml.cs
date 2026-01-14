using System.Windows;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;

namespace SurfPOS.Desktop.Views
{
    public partial class ReportsWindow : Window
    {
        private readonly ISalesService _salesService;
        private readonly IExcelService _excelService;
        private readonly IAuditService _auditService;
        private List<Transaction> _transactions;

        public User? CurrentUser { get; set; }

        public ReportsWindow(ISalesService salesService, IExcelService excelService, IAuditService auditService)
        {
            InitializeComponent();
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            
            _salesService = salesService;
            _excelService = excelService;
            _auditService = auditService;
            _transactions = new List<Transaction>();

            // Set default dates
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;

            Loaded += ReportsWindow_Loaded;
        }

        private async void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await GenerateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reports window: {ex.Message}", "Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GenerateReport()
        {
            try
            {
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Please select both start and end dates!", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
                DateTime endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

                if (startDate > endDate)
                {
                    MessageBox.Show("Start date cannot be after end date!", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get transactions
                var allTransactions = await _salesService.GetTransactionsByDateAsync(startDate);
                
                // Debug info
                System.Diagnostics.Debug.WriteLine($"Query: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                System.Diagnostics.Debug.WriteLine($"Total transactions from DB: {allTransactions.Count()}");
                
                _transactions = allTransactions
                    .Where(t => t.Date >= startDate && t.Date <= endDate)
                    .OrderByDescending(t => t.Date)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Filtered transactions: {_transactions.Count}");

                // Show message if no transactions found
                if (_transactions.Count == 0)
                {
                    var allCount = allTransactions.Count();
                    MessageBox.Show(
                        $"No sales transactions found between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.\n\n" +
                        $"Total transactions in database: {allCount}\n\n" +
                        (allCount > 0 ? "Try selecting a different date range." : 
                        "To see sales data:\n" +
                        "1. Go to the POS screen\n" +
                        "2. Add products to cart\n" +
                        "3. Complete a sale\n" +
                        "4. Return here to view reports"),
                        "No Data",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                // Calculate summary
                decimal totalSales = _transactions.Sum(t => t.TotalAmount);
                int transactionCount = _transactions.Count;
                decimal averageSale = transactionCount > 0 ? totalSales / transactionCount : 0;
                int itemsSold = _transactions.Where(t => t.Items != null).SelectMany(t => t.Items).Sum(i => i.Quantity);

                // Update UI
                TotalSalesTextBlock.Text = totalSales.ToString("C2");
                TransactionsCountTextBlock.Text = transactionCount.ToString();
                AverageSaleTextBlock.Text = averageSale.ToString("C2");
                ItemsSoldTextBlock.Text = itemsSold.ToString();

                TransactionsDataGrid.ItemsSource = _transactions;
                
                // Group by product to get "Products Sold" breakdown
                var productBreakdown = _transactions
                    .SelectMany(t => t.Items)
                    .GroupBy(i => i.ProductId)
                    .Select(g => new ProductReportItem
                    {
                        ProductName = g.First().Product?.Name ?? "Unknown Product",
                        Category = g.First().Product?.Category ?? "General",
                        QuantitySold = g.Sum(i => i.Quantity),
                        UnitPrice = g.First().UnitPrice,
                        TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
                    })
                    .OrderByDescending(p => p.TotalRevenue)
                    .ToList();

                ProductsSoldDataGrid.ItemsSource = productBreakdown;

                // Load Audit Logs
                var logs = await _auditService.GetAuditLogsAsync(startDate, endDate);
                AuditLogsDataGrid.ItemsSource = logs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}\n\nDetails:\n{ex.InnerException?.Message ?? "No additional details"}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class ProductReportItem
        {
            public string ProductName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int QuantitySold { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalRevenue { get; set; }
        }

        private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GenerateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"CRITICAL ERROR in Generate Report Button:\n\n" +
                    $"Message: {ex.Message}\n\n" +
                    $"Type: {ex.GetType().Name}\n\n" +
                    $"Stack Trace:\n{ex.StackTrace}\n\n" +
                    $"Inner Exception: {ex.InnerException?.Message ?? "None"}",
                    "Button Click Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartDatePicker.SelectedDate = DateTime.Today;
                EndDatePicker.SelectedDate = DateTime.Today;
                await GenerateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Today button: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ThisWeekButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;
            int daysToMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            DateTime monday = today.AddDays(-daysToMonday);

            StartDatePicker.SelectedDate = monday;
            EndDatePicker.SelectedDate = today;
            await GenerateReport();
        }

        private async void ThisMonthButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = today;
            await GenerateReport();
        }

        private async void MonthToDateButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = today;
            await GenerateReport();
        }

        private async void YearToDateButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, 1, 1);
            EndDatePicker.SelectedDate = today;
            await GenerateReport();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_transactions == null || !_transactions.Any())
            {
                MessageBox.Show("No transactions to export. Please generate a report first.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"SalesReport_{StartDatePicker.SelectedDate:yyyyMMdd}_{EndDatePicker.SelectedDate:yyyyMMdd}.xlsx",
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await _excelService.ExportTransactionsToExcelAsync(
                        saveFileDialog.FileName, 
                        _transactions, 
                        StartDatePicker.SelectedDate.Value, 
                        EndDatePicker.SelectedDate.Value);

                    // Log export action
                    if (CurrentUser != null)
                    {
                        await _auditService.LogActionAsync(CurrentUser.Id, "Export Report", 
                            $"Sales Report exported for {StartDatePicker.SelectedDate:yyyy-MM-dd} to {EndDatePicker.SelectedDate:yyyy-MM-dd}");
                    }

                    MessageBox.Show("Report exported successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Open the file
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
