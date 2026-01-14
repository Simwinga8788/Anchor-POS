using System.Windows;

namespace SurfPOS.Desktop.Views
{
    public partial class CashStartDialog : Window
    {
        public decimal CashAmount { get; private set; }

        public CashStartDialog()
        {
            InitializeComponent();
            CashAmountTextBox.Focus();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
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
