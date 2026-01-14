using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SurfPOS.Core.Entities
{
    public class Shift : BaseEntity
    {
        public int UserId { get; set; }
        public virtual User User { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashStart { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashEnd { get; set; } // Total cash in drawer at end

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; } // Calculated sales during shift

        public int TransactionCount { get; set; } // Number of transactions

        public string? ReportFilePath { get; set; } // Path to generated Excel report

        public bool IsActive { get; set; } // True if shift is still ongoing

        // Calculated property
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    }
}
