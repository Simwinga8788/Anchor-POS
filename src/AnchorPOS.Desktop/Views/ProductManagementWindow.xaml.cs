using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;

namespace SurfPOS.Desktop.Views
{
    public partial class ProductManagementWindow : Window
    {
        private readonly IProductService _productService;
        private readonly IExcelService _excelService;
        private readonly IBarcodeService _barcodeService;
        private ObservableCollection<Product> _allProducts;
        private ObservableCollection<Product> _filteredProducts;
        private Product? _selectedProduct;

        public ProductManagementWindow(IProductService productService, IExcelService excelService, IBarcodeService barcodeService)
        {
            InitializeComponent();
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag);
            
            _productService = productService;
            _excelService = excelService;
            _barcodeService = barcodeService;
            _allProducts = new ObservableCollection<Product>();
            _filteredProducts = new ObservableCollection<Product>();
            
            Loaded += ProductManagementWindow_Loaded;
        }

        private async void ProductManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProducts();
        }

        private async Task LoadProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                _allProducts.Clear();
                foreach (var product in products)
                {
                    _allProducts.Add(product);
                }
                UpdateCategoryLists();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCategoryLists()
        {
            if (_allProducts == null) return;

            var categories = _allProducts
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToList();

            var defaults = new[] { "Hair Products", "Wigs", "Perfumes", "Makeup", "Clothes", "General" };
            foreach (var def in defaults)
            {
                if (!categories.Any(c => c.Equals(def, StringComparison.OrdinalIgnoreCase)))
                    categories.Add(def);
            }
            categories.Sort();

            // Update Filter
            // Allow "All Categories" as first item
            var filterItems = new ObservableCollection<string> { "All Categories" };
            foreach (var c in categories) filterItems.Add(c);

            // Save selection (try to preserve)
            var currentSelection = CategoryFilterComboBox.SelectedIndex;

            // Temporarily detach handler to prevent double firing? No, let it fire.
            CategoryFilterComboBox.ItemsSource = filterItems;
            
            if (currentSelection >= 0 && currentSelection < filterItems.Count)
                CategoryFilterComboBox.SelectedIndex = currentSelection;
            else
                CategoryFilterComboBox.SelectedIndex = 0;

            // Update Editor
            CategoryComboBox.ItemsSource = new ObservableCollection<string>(categories);
        }

        private void ApplyFilters()
        {
            if (_filteredProducts == null || _allProducts == null)
                return;

            _filteredProducts.Clear();
            
            var filtered = _allProducts.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchBox?.Text))
            {
                string search = SearchBox.Text.ToLower();
                filtered = filtered.Where(p => 
                    p.Name.ToLower().Contains(search) || 
                    p.Barcode.ToLower().Contains(search));
            }

            // Category filter
            if (CategoryFilterComboBox?.SelectedIndex > 0)
            {
                var selected = CategoryFilterComboBox.SelectedItem;
                string category = selected is ComboBoxItem cbi ? cbi.Content?.ToString()! : selected?.ToString()!;
                filtered = filtered.Where(p => p.Category == category);
            }

            // Low stock filter
            if (LowStockCheckBox?.IsChecked == true)
            {
                filtered = filtered.Where(p => p.StockQuantity <= p.LowStockThreshold);
            }

            foreach (var product in filtered)
            {
                _filteredProducts.Add(product);
            }

            if (ProductsDataGrid != null)
                ProductsDataGrid.ItemsSource = _filteredProducts;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allProducts != null)
                ApplyFilters();
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allProducts != null)
                ApplyFilters();
        }

        private void LowStockCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_allProducts != null)
                ApplyFilters();
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is Product product)
            {
                _selectedProduct = product;
                LoadProductDetails(product);
                EditPanel.IsEnabled = true;
            }
            else
            {
                EditPanel.IsEnabled = false;
                ClearForm();
            }
        }

        private void LoadProductDetails(Product product)
        {
            BarcodeTextBox.Text = product.Barcode;
            NameTextBox.Text = product.Name;
            CategoryComboBox.Text = product.Category;
            PriceTextBox.Text = product.Price.ToString("F2");
            CostPriceTextBox.Text = product.CostPrice.ToString("F2");
            StockQuantityTextBox.Text = product.StockQuantity.ToString();
            LowStockThresholdTextBox.Text = product.LowStockThreshold.ToString();
        }

        private void ClearForm()
        {
            BarcodeTextBox.Clear();
            NameTextBox.Clear();
            CategoryComboBox.SelectedIndex = -1;
            PriceTextBox.Clear();
            CostPriceTextBox.Clear();
            StockQuantityTextBox.Clear();
            LowStockThresholdTextBox.Clear();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var categories = CategoryComboBox.ItemsSource as IEnumerable<string>;
            var addDialog = new AddProductDialog(_productService, categories);
            if (addDialog.ShowDialog() == true)
            {
                await LoadProducts();
                MessageBox.Show("Product added successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
                return;

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Product name is required!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
                {
                    MessageBox.Show("Invalid price!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(CostPriceTextBox.Text, out decimal costPrice) || costPrice < 0)
                {
                    MessageBox.Show("Invalid cost price!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(StockQuantityTextBox.Text, out int stock) || stock < 0)
                {
                    MessageBox.Show("Invalid stock quantity!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(LowStockThresholdTextBox.Text, out int threshold) || threshold < 0)
                {
                    MessageBox.Show("Invalid low stock threshold!", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update product
                _selectedProduct.Name = NameTextBox.Text.Trim();
                _selectedProduct.Category = CategoryComboBox.Text;
                _selectedProduct.Price = price;
                _selectedProduct.CostPrice = costPrice;
                _selectedProduct.StockQuantity = stock;
                _selectedProduct.LowStockThreshold = threshold;

                await _productService.UpdateProductAsync(_selectedProduct);
                await LoadProducts();

                MessageBox.Show("Product updated successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving product: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{_selectedProduct.Name}'?\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _productService.DeleteProductAsync(_selectedProduct.Id);
                    await LoadProducts();
                    ClearForm();
                    EditPanel.IsEnabled = false;

                    MessageBox.Show("Product deleted successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting product: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RestockButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
                return;

            var restockDialog = new RestockDialog(_selectedProduct);
            if (restockDialog.ShowDialog() == true)
            {
                try
                {
                    int additionalStock = restockDialog.AdditionalStock;
                    _selectedProduct.StockQuantity += additionalStock;
                    await _productService.UpdateProductAsync(_selectedProduct);
                    await LoadProducts();

                    MessageBox.Show($"Added {additionalStock} units to stock!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error restocking: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void PrintLabelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
                return;

            try
            {
                var labelBytes = await _barcodeService.GenerateBarcodeLabelAsync(
                    _selectedProduct.Barcode,
                    _selectedProduct.Name,
                    _selectedProduct.Price);

                // Save to Documents\Barcode Labels
                string labelsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Barcode Labels");
                Directory.CreateDirectory(labelsFolder);
                string tempPath = Path.Combine(labelsFolder, $"Label_{_selectedProduct.Barcode}.png");
                await File.WriteAllBytesAsync(tempPath, labelBytes);

                // Open with default image viewer
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                MessageBox.Show("Label generated! You can now print it from your image viewer.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating label: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BatchPrintButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProducts = _filteredProducts.Where(p => p.IsSelected).ToList();

            if (selectedProducts.Count == 0)
            {
                MessageBox.Show("Please check the boxes next to products you want to print labels for.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Generate labels for {selectedProducts.Count} product(s)?",
                "Confirm Batch Print",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Create folder in Documents\Barcode Labels
                string labelsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Barcode Labels");
                string batchFolder = Path.Combine(labelsFolder, $"Batch_{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(batchFolder);

                int count = 0;
                foreach (var product in selectedProducts)
                {
                    var labelBytes = await _barcodeService.GenerateBarcodeLabelAsync(
                        product.Barcode,
                        product.Name,
                        product.Price);

                    string fileName = $"{count + 1:D3}_{product.Barcode}.png";
                    string filePath = Path.Combine(batchFolder, fileName);
                    await File.WriteAllBytesAsync(filePath, labelBytes);
                    count++;
                }

                // Open the folder
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batchFolder,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                MessageBox.Show(
                    $"Generated {count} label(s)!\n\nFolder opened. You can:\n• Select all images (Ctrl+A)\n• Print them together (Ctrl+P)\n• Or print individually",
                    "Batch Labels Generated",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // Uncheck all after printing
                foreach (var product in selectedProducts)
                {
                    product.IsSelected = false;
                }
                ProductsDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating batch labels: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllCheckBox.IsChecked ?? false;
            foreach (var product in _filteredProducts)
            {
                product.IsSelected = isChecked;
            }
            ProductsDataGrid.Items.Refresh();
        }

        private async void DownloadTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Save Product Import Template",
                    FileName = "Product_Import_Template.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var templateBytes = await _excelService.GenerateProductTemplateAsync();
                    await File.WriteAllBytesAsync(saveFileDialog.FileName, templateBytes);

                    var result = MessageBox.Show(
                        "Template downloaded successfully!\n\nWould you like to open it?",
                        "Success",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading template: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    Title = "Select Excel File to Import"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var products = await _excelService.ImportProductsFromExcelAsync(openFileDialog.FileName);

                    if (products.Count == 0)
                    {
                        MessageBox.Show("No valid products found in the Excel file!", "Import Failed", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Found {products.Count} products to import.\n\nBarcodes will be auto-generated.\n\nContinue?",
                        "Confirm Import",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        int imported = 0;
                        foreach (var product in products)
                        {
                            await _productService.AddProductAsync(product);
                            imported++;
                        }

                        await LoadProducts();
                        MessageBox.Show($"Successfully imported {imported} products!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing products: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Export Products to Excel",
                    FileName = $"Products_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await _excelService.ExportProductsToExcelAsync(saveFileDialog.FileName, _allProducts);
                    
                    var result = MessageBox.Show(
                        "Products exported successfully!\n\nWould you like to open the file?",
                        "Success",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting products: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
