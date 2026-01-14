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

        public UserManagementWindow(IUserService userService)
        {
            InitializeComponent();
            _userService = userService;
            _users = new List<User>();
            LoadUsers();
        }

        private async void LoadUsers()
        {
            try
            {
                _users = await _userService.GetAllUsersAsync();
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
                AddButton.Content = "ADD NEW USER";
            }
            else
            {
                ClearForm();
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
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
                var role = (UserRole)int.Parse(((ComboBoxItem)RoleComboBox.SelectedItem).Tag.ToString()!);
                
                await _userService.CreateUserAsync(
                    UsernameTextBox.Text,
                    PasswordBox.Password,
                    role);

                MessageBox.Show("User created successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            try
            {
                var role = (UserRole)int.Parse(((ComboBoxItem)RoleComboBox.SelectedItem).Tag.ToString()!);
                
                await _userService.UpdateUserAsync(
                    _selectedUser.Id,
                    UsernameTextBox.Text,
                    role,
                    IsActiveCheckBox.IsChecked ?? true);

                MessageBox.Show("User updated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (_selectedUser == null) return;

            var result = MessageBox.Show(
                $"Deactivate user '{_selectedUser.Username}'?",
                "Confirm Deactivation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _userService.DeactivateUserAsync(_selectedUser.Id);

                MessageBox.Show("User deactivated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deactivating user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            AddButton.Content = "ADD NEW USER";
            _selectedUser = null;
        }
    }
}
