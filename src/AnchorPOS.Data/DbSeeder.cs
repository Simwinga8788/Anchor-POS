using SurfPOS.Core.Entities;
using System;
using System.Linq;

namespace SurfPOS.Data
{
    public static class DbSeeder
    {
        public static void SeedData(SurfDbContext context)
        {
            var alreadySeeded = context.AppSettings
                .Any(s => s.Key == "DatabaseSeeded" && s.Value == "true");

            if (alreadySeeded)
                return;

            // -------------------------
            // Seed Users
            // -------------------------
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User
                    {
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Role = UserRole.Admin,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new User
                    {
                        Username = "sales",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("sales123"),
                        Role = UserRole.Salesperson,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new User
                    {
                        Username = "developer",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("dev123"),
                        Role = UserRole.Developer,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    }
                );
            }

            // -------------------------
            // Seed App Settings
            // -------------------------
            context.AppSettings.AddRange(
                new AppSetting { Key = "BarcodePrefix", Value = "SURF" },
                new AppSetting { Key = "NextBarcodeNumber", Value = "1" },
                new AppSetting { Key = "StoreName", Value = "Anchor POS" },
                new AppSetting { Key = "StoreAddress", Value = "123 Main Street" },
                new AppSetting { Key = "StorePhone", Value = "+1234567890" },
                new AppSetting { Key = "DatabaseSeeded", Value = "true" }
            );

            context.SaveChanges();
        }
    }
}
