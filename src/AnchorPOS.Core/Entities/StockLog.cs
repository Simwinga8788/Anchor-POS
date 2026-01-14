using System;

namespace SurfPOS.Core.Entities
{
    public class StockLog : BaseEntity
    {
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int ChangeAmount { get; set; } // +ve for restock, -ve for sale/damage
        public int NewQuantity { get; set; } // Snapshot of stock after change

        public string Reason { get; set; } = string.Empty; // "Sale", "Restock", "Damage", "Correction"

        public int UserId { get; set; }
        public virtual User User { get; set; } // Who made the change
    }
}
