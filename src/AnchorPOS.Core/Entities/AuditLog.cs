using System;
using System.ComponentModel.DataAnnotations;

namespace SurfPOS.Core.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g., "Print Receipt", "Export Report"

        [Required]
        public string Details { get; set; } = string.Empty; // e.g., "Receipt #501 printed", "Sales Report 2025-01-01 to 2025-01-31"

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string IpAddress { get; set; } = "Local"; // Useful if you ever go networked
    }
}
