using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(string username, string password, UserRole role);
        Task<User> UpdateUserAsync(int id, string username, UserRole role, bool isActive);
        Task<bool> ChangePasswordAsync(int id, string newPassword);
        Task<bool> DeactivateUserAsync(int id);
        Task<bool> DeleteUserAsync(int id);
    }
}
