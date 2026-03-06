using System.Windows;
using System.Windows.Controls;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;

namespace SurfPOS.Desktop.Views
{
    public partial class UserManagementWindow : Window
    {
        private readonly IUserService _userService;
        private List<User> _users;
        private User? _selectedUser;

        private bool _isBusy;

        public UserManagementWindow(IUserService userService)
        {
            InitializeComponent();
            _userService = userService;
            _users = new List<User>();
            
            Loaded += async (s, e) => await LoadUsers();
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            AddButton.IsEnabled = !busy;
            UpdateButton.IsEnabled = !busy && _selectedUser != null;
            ChangePasswordButton.IsEnabled = !busy && _selectedUser != null;
            DeactivateButton.IsEnabled = !busy && (_selectedUser?.IsActive ?? false);
            DeleteButton.IsEnabled = !busy && _selectedUser != null;
            UsersDataGrid.IsEnabled = !busy;
        }

        private async Task LoadUsers()
        {
            if (_isBusy) return;
            try
            {
                _users = await _userService.GetAllUsersAsync();
                UsersDataGrid.ItemsSource = null; // Reset binding
                UsersDataGrid.ItemsSource = _users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = UsersDataGrid.SelectedItem as User;

            if (_selectedUser != null)
            {
                UsernameTextBox.Text = _selectedUser.Username;
                RoleComboBox.SelectedIndex = (int)_selectedUser.Role;
                IsActiveCheckBox.IsChecked = _selectedUser.IsActive;
                PasswordBox.Password = "";

                UpdateButton.IsEnabled = true;
                ChangePasswordButton.IsEnabled = true;
                DeactivateButton.IsEnabled = _selectedUser.IsActive;
                DeleteButton.IsEnabled = true;
                AddButton.Content = "CLEAR SELECTION";
            }
            else
            {
                ClearForm();
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;

            if (AddButton.Content.ToString() == "CLEAR SELECTION")
            {
                ClearForm();
                return;
            }

            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Please enter a username", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please enter a password", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SetBusy(true);
                var role = (UserRole)int.Parse(((ComboBoxItem)RoleComboBox.SelectedItem).Tag.ToString()!);
                
                await _userService.CreateUserAsync(
                    UsernameTextBox.Text,
                    PasswordBox.Password,
                    role);

                MessageBox.Show("User created successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                await LoadUsers();
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("duplicate") || msg.Contains("unique"))
                {
                    MessageBox.Show($"The username '{UsernameTextBox.Text}' is already in use.", 
                        "Duplicate Username", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Error creating user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null || _isBusy) return;

            try
            {
                SetBusy(true);
                var role = (UserRole)int.Parse(((ComboBoxItem)RoleComboBox.SelectedItem).Tag.ToString()!);
                
                await _userService.UpdateUserAsync(
                    _selectedUser.Id,
                    UsernameTextBox.Text,
                    role,
                    IsActiveCheckBox.IsChecked ?? true);

                // Also update password if provided
                if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    await _userService.ChangePasswordAsync(_selectedUser.Id, PasswordBox.Password);
                    PasswordBox.Password = "";
                }

                MessageBox.Show("User updated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please enter a new password", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Change password for user '{_selectedUser.Username}'?",
                "Confirm Password Change",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _userService.ChangePasswordAsync(_selectedUser.Id, PasswordBox.Password);

                MessageBox.Show("Password changed successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                PasswordBox.Password = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing password: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null || _isBusy) return;

            var result = MessageBox.Show(
                $"Deactivate user '{_selectedUser.Username}'?",
                "Confirm Deactivation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                SetBusy(true);
                await _userService.DeactivateUserAsync(_selectedUser.Id);

                MessageBox.Show("User deactivated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deactivating user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null || _isBusy) return;

            var result = MessageBox.Show(
                $"Permanently delete user '{_selectedUser.Username}'?\n\nThis cannot be undone and will fail if the user has sales history.",
                "Confirm Permanent Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                SetBusy(true);
                await _userService.DeleteUserAsync(_selectedUser.Id);

                MessageBox.Show("User deleted permanently!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                await LoadUsers();
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("REFERENCE constraint") || msg.Contains("foreign key"))
                {
                    MessageBox.Show($"Cannot delete user '{_selectedUser.Username}' because they have transaction history. Deactivate them instead.", 
                        "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearForm()
        {
            UsernameTextBox.Text = "";
            PasswordBox.Password = "";
            RoleComboBox.SelectedIndex = 1;
            IsActiveCheckBox.IsChecked = true;
            UpdateButton.IsEnabled = false;
            ChangePasswordButton.IsEnabled = false;
            DeactivateButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            AddButton.Content = "ADD NEW USER";
            _selectedUser = null;
        }
    }
}
