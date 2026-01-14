using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Entities;

namespace SurfPOS.Data
{
    public class SurfDbContext : DbContext
    {
        public SurfDbContext(DbContextOptions<SurfDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<StockLog> StockLogs { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique Barcode Constraint
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique();
            
            // Unique Username Constraint
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Default Values & Precision config (if needed beyond attributes)
        }
    }
}
