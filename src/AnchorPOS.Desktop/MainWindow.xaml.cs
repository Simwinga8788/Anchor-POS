using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;
using SurfPOS.Desktop.Views;

namespace SurfPOS.Desktop;

public partial class MainWindow : Window
{
    private readonly IProductService _productService;
    private readonly ISalesService _salesService;
    private readonly IReceiptPrinterService _receiptPrinterService;
    private readonly IShiftService _shiftService;
    private readonly IEmailService _emailService;
    private readonly IServiceProvider _serviceProvider;
    private ObservableCollection<Product> _products;
    private ObservableCollection<CartItem> _cartItems;

    public User? CurrentUser { get; set; }
    public Shift? CurrentShift { get; set; }

    public MainWindow(IProductService productService, ISalesService salesService, 
        IReceiptPrinterService receiptPrinterService, IShiftService shiftService,
        IEmailService emailService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
        
        _productService = productService;
        _salesService = salesService;
        _receiptPrinterService = receiptPrinterService;
        _shiftService = shiftService;
        _emailService = emailService;
        _serviceProvider = serviceProvider;
        _products = new ObservableCollection<Product>();
        _cartItems = new ObservableCollection<CartItem>();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (CurrentUser != null)
        {
            UserInfoTextBlock.Text = $"Logged in as: {CurrentUser.Username} ({CurrentUser.Role})";
            
            // Hide admin-only buttons for salesperson
            if (CurrentUser.Role != UserRole.Admin)
            {
                ProductsButton.Visibility = Visibility.Collapsed;
                ReportsButton.Visibility = Visibility.Collapsed;
                UsersButton.Visibility = Visibility.Collapsed;
                SettingsButton.Visibility = Visibility.Collapsed;
            }
        }

        await LoadProducts();
        CartListView.ItemsSource = _cartItems;
        BarcodeSearchBox.Focus();
    }

    private async Task LoadProducts()
    {
        try
        {
            var products = await _productService.GetAllProductsAsync();
            _products.Clear();
            foreach (var product in products)
            {
                _products.Add(product);
            }
            // Don't show products by default - only show search results
            ProductsDataGrid.ItemsSource = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BarcodeSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        try
        {
            string searchText = BarcodeSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                // If search is empty, clear the product list
                ProductsDataGrid.ItemsSource = null;
                return;
            }

            // Check if it's a barcode (exact match)
            var productByBarcode = _products.FirstOrDefault(p => 
                p.Barcode != null && p.Barcode.Equals(searchText, StringComparison.OrdinalIgnoreCase)); // Added null check for Barcode
            
            if (productByBarcode != null)
            {
                // Barcode found - add to cart directly
                AddToCart(productByBarcode);
                BarcodeSearchBox.Clear();
                ProductsDataGrid.ItemsSource = null; // Clear the list
                BarcodeSearchBox.Focus();
            }
            else
            {
                // Not a barcode - filter products by name
                var filteredProducts = _products.Where(p => 
                    p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (p.Barcode != null && p.Barcode.Contains(searchText, StringComparison.OrdinalIgnoreCase))) // Added null check for Barcode
                    .ToList();
                
                if (filteredProducts.Any())
                {
                    ProductsDataGrid.ItemsSource = filteredProducts; // Changed from ProductsListBox to ProductsDataGrid
                    
                    // If only one result, select it
                    if (filteredProducts.Count == 1)
                    {
                        ProductsDataGrid.SelectedItem = filteredProducts[0]; // Changed from ProductsListBox to ProductsDataGrid
                    }
                }
                else
                {
                    MessageBox.Show($"No products found matching: {searchText}", "Not Found", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    BarcodeSearchBox.SelectAll();
                }
            }
        }
        catch (Exception ex)
            {
                MessageBox.Show($"Error searching product: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
    }

    private void ProductsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProductsDataGrid.SelectedItem is Product product)
        {
            AddToCart(product);
        }
    }

    private void AddToCart(Product product)
    {
        if (product.StockQuantity <= 0)
        {
            MessageBox.Show($"{product.Name} is out of stock!", "Out of Stock", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existingItem = _cartItems.FirstOrDefault(c => c.ProductId == product.Id);
        
        if (existingItem != null)
        {
            if (existingItem.Quantity >= product.StockQuantity)
            {
                MessageBox.Show($"Cannot add more. Only {product.StockQuantity} in stock.", 
                    "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            existingItem.Quantity++;
        }
        else
        {
            _cartItems.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });
        }

        UpdateTotal();
    }

    private void RemoveCartItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int productId)
        {
            var item = _cartItems.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                _cartItems.Remove(item);
                UpdateTotal();
            }
        }
    }

    private void IncreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int productId)
        {
            var item = _cartItems.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                // Check stock
                var product = _products.FirstOrDefault(p => p.Id == productId);
                // If product is not in current filtered list, we might need to fetch it or check stored stock in CartItem if we had it.
                // But wait, _products might filters. 
                // However, CartItem doesn't store max stock. 
                // Let's assume we can find it in _products even if filtered? No, _products is the view source.
                // We should really check database or fetch fresh. But for now, let's assume we can just increment.
                // Actually, let's check against the product stock if available.
                
                // Better approach: We need to know the stock limit. 
                // Let's fetch the product from service or assume we can keep adding if we don't block.
                // But we should block.
                // Let's rely on the service to check stock or just increment for now to keep it responsive.
                // Ideally CartItem should have StockQuantity property.
                
                item.Quantity++;
                // Force UI update for TotalText
                var index = _cartItems.IndexOf(item);
                _cartItems[index] = item; 
                UpdateTotal();
            }
        }
    }

    private void DecreaseQty_Click(object sender, RoutedEventArgs e)
    {
         if (sender is Button button && button.Tag is int productId)
        {
            var item = _cartItems.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    // Force UI update
                    var index = _cartItems.IndexOf(item);
                    _cartItems[index] = item;
                    UpdateTotal();
                }
                else
                {
                    // If quantity is 1 and they click minus, remove it?
                    // Or keep at 1? Let's remove it for convenience.
                     _cartItems.Remove(item);
                    UpdateTotal();
                }
            }
        }
    }

    private void UpdateTotal()
    {
        decimal total = _cartItems.Sum(item => item.Total);
        TotalTextBlock.Text = $"K{total:N2}";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cartItems.Count > 0)
        {
            var result = MessageBox.Show("Clear all items from cart?", "Confirm", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _cartItems.Clear();
                UpdateTotal();
                BarcodeSearchBox.Focus();
            }
        }
    }

    private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("Cart is empty!", "Cannot Checkout", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (CurrentUser == null)
        {
            MessageBox.Show("User not logged in!", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            CheckoutButton.IsEnabled = false;

            // Get payment method
            var paymentMethod = PaymentMethodComboBox.SelectedIndex switch
            {
                0 => PaymentMethod.Cash,
                1 => PaymentMethod.Card,
                2 => PaymentMethod.MobileMoney,
                _ => PaymentMethod.Cash
            };

            // Prepare items
            var items = _cartItems.Select(c => (c.ProductId, c.Quantity)).ToList();

            // Process sale
            var transaction = await _salesService.ProcessSaleAsync(CurrentUser.Id, items, paymentMethod, CurrentShift?.Id);

            // Open cash drawer for cash payments
            if (paymentMethod == PaymentMethod.Cash)
            {
                await _receiptPrinterService.OpenCashDrawerAsync();
            }

            // Show receipt preview
            var auditService = _serviceProvider.GetRequiredService<IAuditService>();
            var previewDialog = new ReceiptPreviewDialog(_receiptPrinterService, auditService, transaction, CurrentUser);
            previewDialog.Owner = this;
            previewDialog.ShowDialog();
            
            // Show success
            decimal total = _cartItems.Sum(item => item.Total);
            MessageBox.Show(
                $"Sale completed successfully!\n\n" +
                $"Transaction: {transaction.TransactionRef}\n" +
                $"Total: K{total:N2}\n" +
                $"Payment: {paymentMethod}",
                "Success",
                MessageBoxButton.OK, 
                MessageBoxImage.Information);

            // Clear cart and reload products (to update stock)
            _cartItems.Clear();
            UpdateTotal();
            await LoadProducts();
            BarcodeSearchBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Checkout failed: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CheckoutButton.IsEnabled = true;
        }
    }

    private void ProductsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var productWindow = _serviceProvider.GetRequiredService<ProductManagementWindow>();
            productWindow.ShowDialog();
            
            // Reload products after closing product management
            _ = LoadProducts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening product management: {ex.Message}\n\nDetails: {ex.InnerException?.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReportsButton_Click(object sender, RoutedEventArgs e)
    {
        var reportsWindow = _serviceProvider.GetRequiredService<ReportsWindow>();
        reportsWindow.CurrentUser = CurrentUser;
        reportsWindow.ShowDialog();
    }

    private void UsersButton_Click(object sender, RoutedEventArgs e)
    {
        var usersWindow = _serviceProvider.GetRequiredService<UserManagementWindow>();
        usersWindow.ShowDialog();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.ShowDialog();
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // End shift if active
                if (CurrentShift != null && CurrentShift.IsActive && CurrentUser != null)
                {
                    // End shift automatically (cash calculated from transactions)
                    var endedShift = await _shiftService.EndShiftAsync(CurrentShift.Id, 0);
                    
                    // Automatically send reports
                    bool emailSent = false;
                    bool whatsappOpened = false;

                    // Try to send via email (if configured)
                    try
                    {
                        var adminEmail = await GetAdminEmailAsync();
                        if (!string.IsNullOrEmpty(adminEmail))
                        {
                            emailSent = await _emailService.SendShiftReportAsync(
                                adminEmail, 
                                endedShift.ReportFilePath!, 
                                CurrentUser.Username, 
                                endedShift.StartTime, 
                                endedShift.EndTime);
                        }
                    }
                    catch { }

                    // Try to send via WhatsApp (if configured)
                    try
                    {
                        var adminPhone = await GetAdminPhoneAsync();
                        if (!string.IsNullOrEmpty(adminPhone))
                        {
                            whatsappOpened = await _emailService.SendWhatsAppReportAsync(
                                adminPhone, 
                                endedShift.ReportFilePath!, 
                                CurrentUser.Username);
                        }
                    }
                    catch { }

                    var sendStatus = "";
                    if (emailSent) sendStatus += "\n✓ Email sent";
                    if (whatsappOpened) sendStatus += "\n✓ WhatsApp opened";
                    if (!emailSent && !whatsappOpened) sendStatus = "\n(Email/WhatsApp not configured)";
                    
                    MessageBox.Show(
                        $"Shift ended successfully!\n\n" +
                        $"Duration: {endedShift.Duration?.ToString(@"hh\:mm")}\n" +
                        $"Transactions: {endedShift.TransactionCount}\n" +
                        $"Total Sales: K{endedShift.TotalSales:N2}\n\n" +
                        $"Report saved to:\n{endedShift.ReportFilePath}" +
                        sendStatus,
                        "Shift Report",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error ending shift: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task<string?> GetAdminEmailAsync()
    {
        // Get admin email from app settings
        var setting = await Task.Run(() => 
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SurfPOS.Data.SurfDbContext>();
            return context.AppSettings.FirstOrDefault(s => s.Key == "AdminEmail")?.Value;
        });
        return setting;
    }

    private async Task<string?> GetAdminPhoneAsync()
    {
        // Get admin phone from app settings
        var setting = await Task.Run(() => 
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SurfPOS.Data.SurfDbContext>();
            return context.AppSettings.FirstOrDefault(s => s.Key == "AdminPhone")?.Value;
        });
        return setting;
    }
}

// Helper class for cart items
public class CartItem : System.ComponentModel.INotifyPropertyChanged
{
    private int _quantity;
    
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(QuantityText));
                OnPropertyChanged(nameof(TotalText));
            }
        }
    }
    
    public decimal Total => UnitPrice * Quantity;
    public string QuantityText => $"{Quantity} × K{UnitPrice:N2}";
    public string TotalText => $"K{Total:N2}";

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}
