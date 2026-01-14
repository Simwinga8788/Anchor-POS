using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string username, string password);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<User?> GetUserByIdAsync(int userId);
    }
}
