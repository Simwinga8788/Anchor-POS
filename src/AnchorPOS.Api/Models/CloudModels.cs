using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnchorPOS.Api.Models
{
    // Tenant-aware entity base
    public abstract class CloudBaseEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string StoreId { get; set; } = string.Empty;
        
        public int LocalId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CloudTransaction : CloudBaseEntity
    {
        [MaxLength(50)]
        public string TransactionRef { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string CashierName { get; set; } = string.Empty;
        
        public int? LocalShiftId { get; set; }
        
        public virtual ICollection<CloudTransactionItem> Items { get; set; } = new List<CloudTransactionItem>();
    }

    public class CloudTransactionItem : CloudBaseEntity
    {
        public int CloudTransactionId { get; set; }
        public virtual CloudTransaction Transaction { get; set; }
        
        public int LocalProductId { get; set; }
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }

    public class CloudShift : CloudBaseEntity
    {
        [MaxLength(100)]
        public string CashierName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CashStart { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CashEnd { get; set; }
    }

    public class CloudProduct : CloudBaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }
        
        public int StockQuantity { get; set; }
        
        [MaxLength(50)]
        public string Category { get; set; } = "General";
        
        public bool IsActive { get; set; } = true;
    }
}
