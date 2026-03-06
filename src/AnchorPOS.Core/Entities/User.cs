using System.ComponentModel.DataAnnotations;

namespace SurfPOS.Core.Entities
{
    public enum UserRole
    {
        Admin,
        Salesperson,
        Developer
    }

    public class User : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Salesperson;

        public DateTime? LastLogin { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
