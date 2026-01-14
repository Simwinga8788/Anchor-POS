using System.Windows;

namespace SurfPOS.Desktop.Views
{
    public partial class CashEndDialog : Window
    {
        public decimal CashAmount { get; private set; }

        public CashEndDialog()
        {
            InitializeComponent();
            CashAmountTextBox.Focus();
        }

        private void EndButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(CashAmountTextBox.Text, out decimal amount) && amount >= 0)
            {
                CashAmount = amount;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid cash amount (0 or greater)", "Invalid Amount",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CashAmountTextBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
