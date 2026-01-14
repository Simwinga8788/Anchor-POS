using System.Windows;
using SurfPOS.Core.Entities;

namespace SurfPOS.Desktop.Views
{
    public partial class RestockDialog : Window
    {
        private readonly Product _product;
        public int AdditionalStock { get; private set; }

        public RestockDialog(Product product)
        {
            InitializeComponent();
            _product = product;

            ProductNameTextBlock.Text = product.Name;
            CurrentStockTextBlock.Text = $"Current Stock: {product.StockQuantity} units";
            
            AdditionalStockTextBox.TextChanged += (s, e) => UpdateNewStock();
        }

        private void UpdateNewStock()
        {
            if (int.TryParse(AdditionalStockTextBox.Text, out int additional) && additional > 0)
            {
                int newStock = _product.StockQuantity + additional;
                NewStockTextBlock.Text = $"New Stock: {newStock} units";
            }
            else
            {
                NewStockTextBlock.Text = "";
            }
        }

        private void RestockButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(AdditionalStockTextBox.Text, out int additional) || additional <= 0)
            {
                MessageBox.Show("Please enter a valid quantity!", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AdditionalStock = additional;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
