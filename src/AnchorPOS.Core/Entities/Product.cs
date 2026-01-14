using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SurfPOS.Core.Entities
{
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 5;

        [MaxLength(50)]
        public string Category { get; set; } = "General";

        public bool IsActive { get; set; } = true;

        // UI-only property for checkbox selection (not stored in database)
        [NotMapped]
        public bool IsSelected { get; set; }
    }
}
