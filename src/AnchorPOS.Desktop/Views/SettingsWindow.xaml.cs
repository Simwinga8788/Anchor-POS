using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Entities;
using SurfPOS.Data;
using SurfPOS.Services;
using SurfPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

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
            LoadAvailablePrinters();
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

                string paperSize = GetSetting(settings, "PaperSize");
                if (paperSize.Contains("58mm"))
                {
                    PaperSizeComboBox.SelectedIndex = 1;
                }
                else
                {
                    PaperSizeComboBox.SelectedIndex = 0;
                }

                string printEnabled = GetSetting(settings, "PrintEnabled");
                if (bool.TryParse(printEnabled, out bool isPrintEnabled))
                {
                    PrintingEnabledToggle.IsChecked = isPrintEnabled;
                }
                else
                {
                    PrintingEnabledToggle.IsChecked = true; // Default
                }

                string density = GetSetting(settings, "PrintDensity");
                if (string.IsNullOrEmpty(density))
                {
                    // Check legacy setting
                    string darker = GetSetting(settings, "PrintDarkness");
                    if (bool.TryParse(darker, out bool isDark) && isDark) density = "Bold";
                    else density = "Normal";
                }

                foreach (ComboBoxItem item in DensityComboBox.Items)
                {
                    if (item.Tag?.ToString() == density)
                    {
                        DensityComboBox.SelectedItem = item;
                        break;
                    }
                }

                string themeDark = GetSetting(settings, "ThemeDark");
                if (bool.TryParse(themeDark, out bool isThemeDark))
                {
                    DarkModeToggle.IsChecked = isThemeDark;
                }

                string themeColor = GetSetting(settings, "ThemeColor");
                if (!string.IsNullOrEmpty(themeColor))
                {
                    foreach (ComboBoxItem item in ColorComboBox.Items)
                    {
                        if (item.Tag?.ToString() == themeColor)
                        {
                            ColorComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
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
                var subject = "Anchor POS - Test Email";
                var body = $"This is a test email from Anchor POS.\\n\\n" +
                          $"Your email configuration is working correctly!\\n\\n" +
                          $"Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\\n\\n" +
                          $"Anchor POS System";

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

                string selectedSize = "80mm";
                if (PaperSizeComboBox.SelectedIndex == 1) selectedSize = "58mm";
                await SaveSettingAsync("PaperSize", selectedSize);

                string density = (DensityComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Normal";
                await SaveSettingAsync("PrintDensity", density);
                
                // Save Store Config to File (Robust backup)
                try
                {
                    var storeConfig = new 
                    {
                        StoreName = StoreNameTextBox.Text,
                        StoreAddress = StoreAddressTextBox.Text,
                        StorePhone = StorePhoneTextBox.Text,
                        PaperSize = selectedSize,
                        PrintDensity = density,
                        PrintEnabled = PrintingEnabledToggle.IsChecked ?? true,
                        ThemeDark = DarkModeToggle.IsChecked ?? false,
                        ThemeColor = (ColorComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "DeepPurple"
                    };
                    
                    var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS");
                    if (!Directory.Exists(configDir))
                        Directory.CreateDirectory(configDir);
                        
                    var configPath = Path.Combine(configDir, "store_config.json");
                    var json = JsonSerializer.Serialize(storeConfig);
                    File.WriteAllText(configPath, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving store config: {ex.Message}");
                }

                // Save printer selection
                if (PrinterComboBox.SelectedItem != null)
                {
                    string printerName = PrinterComboBox.SelectedItem.ToString() ?? "";
                    await SaveSettingAsync("ReceiptPrinter", printerName);
                    
                    // Also save to local file as backup (bypasses DB issues)
                    try
                    {
                        var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS");
                        if (!Directory.Exists(configDir))
                            Directory.CreateDirectory(configDir);
                            
                        var configPath = Path.Combine(configDir, "printer_config.txt");
                        File.WriteAllText(configPath, printerName);
                    }
                    catch (Exception fileEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving printer config file: {fileEx.Message}");
                    }
                }

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

        private void LoadAvailablePrinters()
        {
            try
            {
                PrinterComboBox.Items.Clear();
                
                // Get all installed printers
                var printers = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
                
                if (printers.Count == 0)
                {
                    PrinterStatusTextBlock.Text = "⚠️ No printers found. Please install a printer driver.";
                    return;
                }

                // Add each printer to the combo box
                foreach (string printer in printers)
                {
                    PrinterComboBox.Items.Add(printer);
                }

                // Load saved printer preference
                var savedPrinter = GetSetting(_context.AppSettings.ToList(), "ReceiptPrinter");
                
                if (!string.IsNullOrEmpty(savedPrinter))
                {
                    // Select the saved printer if it exists
                    var index = PrinterComboBox.Items.IndexOf(savedPrinter);
                    if (index >= 0)
                    {
                        PrinterComboBox.SelectedIndex = index;
                    }
                    else
                    {
                        // Saved printer not found, select first one
                        PrinterComboBox.SelectedIndex = 0;
                        PrinterStatusTextBlock.Text = $"⚠️ Previously selected printer '{savedPrinter}' not found. Please select a new printer.";
                    }
                }
                else
                {
                    // No saved printer, select the first one
                    PrinterComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                PrinterStatusTextBlock.Text = $"❌ Error loading printers: {ex.Message}";
            }
        }

        private void PrinterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem != null)
            {
                var printerName = PrinterComboBox.SelectedItem.ToString();
                PrinterStatusTextBlock.Text = $"✓ Selected: {printerName}";
            }
        }

        private async void TestPrinterButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a printer first.", "No Printer Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var printerName = PrinterComboBox.SelectedItem.ToString();
                PrinterStatusTextBlock.Text = $"🖨️ Testing printer: {printerName}...";

                // Create a test print document
                var printDoc = new System.Drawing.Printing.PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerName;

                // Check if printer is valid
                if (!printDoc.PrinterSettings.IsValid)
                {
                    PrinterStatusTextBlock.Text = $"❌ Printer '{printerName}' is not accessible.";
                    MessageBox.Show($"The printer '{printerName}' is not accessible or offline.\n\nPlease check:\n• Printer is powered on\n• Printer is connected\n• Printer is not paused",
                        "Printer Not Accessible", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Set up the test page content
                printDoc.PrintPage += (s, ev) =>
                {
                    if (ev.Graphics == null) return;

                    var font = new System.Drawing.Font("Courier New", 10);
                    var y = 10;
                    var lineHeight = 20;

                    ev.Graphics.DrawString("================================", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight;
                    ev.Graphics.DrawString("   ANCHOR POS - TEST RECEIPT", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight;
                    ev.Graphics.DrawString("================================", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight * 2;
                    ev.Graphics.DrawString($"Printer: {printerName}", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight;
                    ev.Graphics.DrawString($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight * 2;
                    ev.Graphics.DrawString("This is a test print.", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight;
                    ev.Graphics.DrawString("If you can read this, your", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight;
                    ev.Graphics.DrawString("printer is working correctly!", font, System.Drawing.Brushes.Black, 10, y);
                    y += lineHeight * 2;
                    ev.Graphics.DrawString("================================", font, System.Drawing.Brushes.Black, 10, y);

                    ev.HasMorePages = false;
                };

                // Print the test page
                printDoc.Print();

                await Task.Delay(500); // Small delay to ensure print job is sent

                PrinterStatusTextBlock.Text = $"✓ Test print sent to {printerName}";
                MessageBox.Show($"Test print sent to '{printerName}'!\n\nPlease check your printer for the test receipt.",
                    "Test Print Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                PrinterStatusTextBlock.Text = $"❌ Test print failed: {ex.Message}";
                MessageBox.Show($"Failed to print test page:\n\n{ex.Message}\n\nPlease check your printer connection and driver.",
                    "Print Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshPrintersButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAvailablePrinters();
            MessageBox.Show("Printer list refreshed!", "Printers Refreshed",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void DarkModeToggle_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void ColorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            try
            {
                bool isDark = DarkModeToggle.IsChecked ?? false;
                string colorName = (ColorComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Blue";

                var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
                var theme = paletteHelper.GetTheme();

                var baseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;
                theme.SetBaseTheme(baseTheme);

                string hexColor = colorName switch
                {
                    "Blue" => "#2196F3",
                    "DeepPurple" => "#673AB7",
                    "Indigo" => "#3F51B5",
                    "Teal" => "#009688",
                    "Green" => "#4CAF50",
                    "Amber" => "#FFC107",
                    "Red" => "#F44336",
                    "Pink" => "#E91E63",
                    "Cyan" => "#00BCD4",
                    "LightBlue" => "#03A9F4",
                    "Orange" => "#FF9800",
                    "DeepOrange" => "#FF5722",
                    "Lime" => "#CDDC39",
                    "Yellow" => "#FFEB3B",
                    _ => "#2196F3"
                };

                var mediaColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
                theme.SetPrimaryColor(mediaColor);
                paletteHelper.SetTheme(theme);
            }
            catch { }
        }
    }
}
