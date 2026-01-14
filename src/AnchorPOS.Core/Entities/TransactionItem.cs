using System.ComponentModel.DataAnnotations.Schema;

namespace SurfPOS.Core.Entities
{
    public class TransactionItem : BaseEntity
    {
        public int TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // Price at moment of sale
    }
}
