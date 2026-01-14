using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace SurfPOS.Data
{
    // Factory for creating DbContext at Design Time (for Migrations)
    public class SurfDbContextFactory : IDesignTimeDbContextFactory<SurfDbContext>
    {
        public SurfDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SurfDbContext>();
            
            // Should match connection string in App.config / appsettings.json later
            // For now, hardcode for initial migration generation
            optionsBuilder.UseSqlServer("Server=DESKTOP-U6O1NKU\\SQLEXPRESS;Database=SurfPOS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            return new SurfDbContext(optionsBuilder.Options);
        }
    }
}
