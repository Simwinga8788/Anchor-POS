using System.ComponentModel.DataAnnotations;

namespace SurfPOS.Core.Entities
{
    public class AppSetting : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
