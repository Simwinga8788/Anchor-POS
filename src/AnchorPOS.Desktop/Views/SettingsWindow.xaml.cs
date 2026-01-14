using System.Windows;
using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Entities;
using SurfPOS.Data;

namespace SurfPOS.Desktop.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SurfDbContext _context;

        public SettingsWindow(SurfDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += SettingsWindow_Loaded;
        }

        private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var settings = await _context.AppSettings.ToListAsync();

                AdminEmailTextBox.Text = GetSetting(settings, "AdminEmail");
                SmtpHostTextBox.Text = GetSetting(settings, "SmtpHost");
                SmtpPortTextBox.Text = GetSetting(settings, "SmtpPort");
                SmtpUsernameTextBox.Text = GetSetting(settings, "SmtpUsername");
                SmtpPasswordBox.Password = GetSetting(settings, "SmtpPassword");
                FromEmailTextBox.Text = GetSetting(settings, "FromEmail");
                AdminPhoneTextBox.Text = GetSetting(settings, "AdminPhone");
                StoreNameTextBox.Text = GetSetting(settings, "StoreName");
                StoreAddressTextBox.Text = GetSetting(settings, "StoreAddress");
                StorePhoneTextBox.Text = GetSetting(settings, "StorePhone");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetSetting(List<AppSetting> settings, string key)
        {
            return settings.FirstOrDefault(s => s.Key == key)?.Value ?? "";
        }

        private async void TestEmailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(AdminEmailTextBox.Text))
                {
                    MessageBox.Show("Please enter an Admin Email address to send the test email to.", "Missing Information",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SmtpHostTextBox.Text) || 
                    string.IsNullOrWhiteSpace(SmtpUsernameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SmtpPasswordBox.Password) ||
                    string.IsNullOrWhiteSpace(FromEmailTextBox.Text))
                {
                    MessageBox.Show("Please fill in all SMTP settings before testing.", "Missing Information",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Temporarily save settings to test
                await SaveSettingAsync("AdminEmail", AdminEmailTextBox.Text);
                await SaveSettingAsync("SmtpHost", SmtpHostTextBox.Text);
                await SaveSettingAsync("SmtpPort", SmtpPortTextBox.Text);
                await SaveSettingAsync("SmtpUsername", SmtpUsernameTextBox.Text);
                await SaveSettingAsync("SmtpPassword", SmtpPasswordBox.Password);
                await SaveSettingAsync("FromEmail", FromEmailTextBox.Text);
                await _context.SaveChangesAsync();

                // Send test email
                var emailService = new Services.EmailService(_context);
                var subject = "Surf POS - Test Email";
                var body = $"This is a test email from Surf POS.\\n\\n" +
                          $"Your email configuration is working correctly!\\n\\n" +
                          $"Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\\n\\n" +
                          $"Kenji's Beauty Space POS System";

                var success = await emailService.SendEmailAsync(AdminEmailTextBox.Text, subject, body);

                if (success)
                {
                    MessageBox.Show($"Test email sent successfully to {AdminEmailTextBox.Text}!\\n\\nPlease check your inbox.", 
                        "Email Test Successful",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to send test email. Please check your SMTP settings:\\n\\n" +
                                  "• Verify SMTP host and port\\n" +
                                  "• Check username and password\\n" +
                                  "• Ensure 'From Email' is valid\\n" +
                                  "• For Gmail, you may need an App Password", 
                        "Email Test Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing email: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveSettingAsync("AdminEmail", AdminEmailTextBox.Text);
                await SaveSettingAsync("SmtpHost", SmtpHostTextBox.Text);
                await SaveSettingAsync("SmtpPort", SmtpPortTextBox.Text);
                await SaveSettingAsync("SmtpUsername", SmtpUsernameTextBox.Text);
                await SaveSettingAsync("SmtpPassword", SmtpPasswordBox.Password);
                await SaveSettingAsync("FromEmail", FromEmailTextBox.Text);
                await SaveSettingAsync("AdminPhone", AdminPhoneTextBox.Text);
                await SaveSettingAsync("StoreName", StoreNameTextBox.Text);
                await SaveSettingAsync("StoreAddress", StoreAddressTextBox.Text);
                await SaveSettingAsync("StorePhone", StorePhoneTextBox.Text);

                await _context.SaveChangesAsync();

                MessageBox.Show("Settings saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveSettingAsync(string key, string value)
        {
            var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            
            if (setting == null)
            {
                setting = new AppSetting { Key = key, Value = value };
                _context.AppSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
