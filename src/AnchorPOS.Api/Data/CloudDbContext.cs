using AnchorPOS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnchorPOS.Api.Data
{
    public class CloudDbContext : DbContext
    {
        public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options)
        {
        }

        public DbSet<CloudTransaction> Transactions { get; set; }
        public DbSet<CloudTransactionItem> TransactionItems { get; set; }
        public DbSet<CloudShift> Shifts { get; set; }
        public DbSet<CloudProduct> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // For multitenancy, it's often useful to index StoreId + LocalId
            modelBuilder.Entity<CloudProduct>()
                .HasIndex(p => new { p.StoreId, p.LocalId })
                .IsUnique();

            modelBuilder.Entity<CloudTransaction>()
                .HasIndex(t => new { t.StoreId, t.LocalId })
                .IsUnique();

            modelBuilder.Entity<CloudShift>()
                .HasIndex(s => new { s.StoreId, s.LocalId })
                .IsUnique();
        }
    }
}
