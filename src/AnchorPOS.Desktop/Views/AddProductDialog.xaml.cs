using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;

namespace SurfPOS.Desktop.Views
{
    public partial class AddProductDialog : Window
    {
        private readonly IProductService _productService;
        private string? _scannedBarcode;

        public AddProductDialog(IProductService productService, IEnumerable<string>? categories = null)
        {
            InitializeComponent();
            _productService = productService;
            BarcodeTextBox.Focus();

            if (categories != null)
            {
                CategoryComboBox.ItemsSource = categories;
            }
            else
            {
                CategoryComboBox.ItemsSource = new List<string> { "Hair Products", "Wigs", "Perfumes", "Makeup", "Clothes", "General" };
            }
        }

        private async void BarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await CheckBarcodeAsync();
            }
        }

        private async Task CheckBarcodeAsync()
        {
            var barcode = BarcodeTextBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(barcode))
            {
                BarcodeStatusText.Text = "";
                BarcodeStatusText.Foreground = Brushes.Gray;
                return;
            }

            // Check if barcode already exists
            var existingProduct = await _productService.GetProductByBarcodeAsync(barcode);
            
            if (existingProduct != null)
            {
                BarcodeStatusText.Text = $"⚠️ Barcode already exists: {existingProduct.Name}";
                BarcodeStatusText.Foreground = Brushes.Red;
                
                var result = MessageBox.Show(
                    $"This barcode already exists for:\n\n{existingProduct.Name}\n\nWould you like to edit that product instead?",
                    "Barcode Already Exists",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = false;
                    Close();
                }
            }
            else
            {
                _scannedBarcode = barcode;
                BarcodeStatusText.Text = $"✓ Barcode ready: {barcode}";
                BarcodeStatusText.Foreground = Brushes.Green;
                NameTextBox.Focus();
            }
        }

        private async void GenerateBarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var barcode = await _productService.GenerateNextBarcodeAsync();
                BarcodeTextBox.Text = barcode;
                _scannedBarcode = barcode;
                BarcodeStatusText.Text = $"✓ Generated: {barcode}";
                BarcodeStatusText.Foreground = Brushes.Blue;
                NameTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating barcode: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate barcode
                if (string.IsNullOrWhiteSpace(BarcodeTextBox.Text))
                {
                    MessageBox.Show("Please scan a barcode or click GENERATE!", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BarcodeTextBox.Focus();
                    return;
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Product name is required!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
                {
                    MessageBox.Show("Please enter a valid selling price!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(CostPriceTextBox.Text, out decimal costPrice) || costPrice < 0)
                {
                    MessageBox.Show("Please enter a valid cost price!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(StockQuantityTextBox.Text, out int stock) || stock < 0)
                {
                    MessageBox.Show("Please enter a valid stock quantity!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(LowStockThresholdTextBox.Text, out int threshold) || threshold < 0)
                {
                    MessageBox.Show("Please enter a valid low stock threshold!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create product with scanned/generated barcode
                var product = new Product
                {
                    Barcode = BarcodeTextBox.Text.Trim(),
                    Name = NameTextBox.Text.Trim(),
                    Category = CategoryComboBox.Text,
                    Price = price,
                    CostPrice = costPrice,
                    StockQuantity = stock,
                    LowStockThreshold = threshold,
                    IsActive = true
                };

                await _productService.AddProductAsync(product);
                
                MessageBox.Show($"Product added successfully!\n\nBarcode: {product.Barcode}\nName: {product.Name}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding product: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
