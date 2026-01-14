using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SurfPOS.Core.Entities
{
    public enum PaymentMethod
    {
        Cash,
        Card,
        MobileMoney
    }

    public class Transaction : BaseEntity
    {
        public string TransactionRef { get; set; } = string.Empty; // e.g. "TXN-20251224-001"
        
        public DateTime Date { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        // Optional: Link to a Shift if we implement shift tracking
        public int? ShiftId { get; set; }

        public virtual ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();

        [NotMapped]
        public int TotalQuantity => Items?.Sum(i => i.Quantity) ?? 0;
    }
}
