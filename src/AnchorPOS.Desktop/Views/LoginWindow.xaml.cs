using System.Windows;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.Json;

namespace SurfPOS.Desktop.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _authService;
        private readonly IShiftService _shiftService;
        private readonly IServiceProvider _serviceProvider;

        public LoginWindow(IAuthService authService, IShiftService shiftService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _authService = authService;
            _shiftService = shiftService;
            _serviceProvider = serviceProvider;

            // Set focus to username
            Loaded += (s, e) => 
            {
                UsernameTextBox.Focus();
                LoadStoreConfig();
            };

            // Handle Enter key on password box
            PasswordBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    LoginButton_Click(s, e);
            };
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            LoginButton.IsEnabled = false;

            try
            {
                string username = UsernameTextBox.Text.Trim();
                string password = PasswordBox.Password;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ShowError("Please enter both username and password");
                    return;
                }

                var user = await _authService.LoginAsync(username, password);

                if (user == null)
                {
                    // Check if user exists but is inactive
                    var inactiveUser = await Task.Run(() =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<SurfPOS.Data.SurfDbContext>();
                        return context.Users.FirstOrDefault(u => u.Username == username);
                    });

                    if (inactiveUser != null && !inactiveUser.IsActive)
                    {
                        ShowError("This user account is deactivated.\nPlease contact an administrator.");
                    }
                    else
                    {
                        ShowError("Invalid username or password.\n\nPlease check:\n• Username is correct\n• Password is correct\n• User is active");
                    }
                    return;
                }

                // Check for existing active shift or start new one
                var shift = await _shiftService.GetActiveShiftAsync(user.Id);
                if (shift == null)
                {
                    shift = await _shiftService.StartShiftAsync(user.Id, 0);
                }

                // Login successful - open main window
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.CurrentUser = user;
                mainWindow.CurrentShift = shift;
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
        private void LoadStoreConfig()
        {
            try
            {
                // Try to load store name from file config
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS", "store_config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("StoreName", out var nameProp))
                        {
                            var storeName = nameProp.GetString();
                            if (!string.IsNullOrEmpty(storeName))
                            {
                                StoreNameTextBlock.Text = storeName.ToUpper();
                                // StoreSubtitleTextBlock.Text = ... // Optional: Load address/phone if desired
                            }
                        }
                    }
                }
                else
                {
                    // Fallback to database
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<SurfDbContext>();
                        var storeName = dbContext.AppSettings
                            .AsNoTracking()
                            .FirstOrDefault(s => s.Key == "StoreName")?.Value;
                        
                        if (!string.IsNullOrEmpty(storeName))
                        {
                            StoreNameTextBlock.Text = storeName.ToUpper();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading store config for Login: {ex.Message}");
            }
        }
    }
}
