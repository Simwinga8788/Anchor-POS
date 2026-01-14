using SurfPOS.Core.Entities;
using System;
using System.Linq;

namespace SurfPOS.Data
{
    public static class DbSeeder
    {
        public static void SeedData(SurfDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Seed Default Admin User
            if (!context.Users.Any())
            {
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Default password
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var salesUser = new User
                {
                    Username = "sales",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("sales123"), // Default password
                    Role = UserRole.Salesperson,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                context.Users.Add(adminUser);
                context.Users.Add(salesUser);
                context.SaveChanges();
            }

            // Seed Sample Products
            if (!context.Products.Any())
            {
                var products = new[]
                {
                    new Product
                    {
                        Name = "Shampoo - Moisturizing",
                        Barcode = "SURF00001",
                        Price = 15.99m,
                        CostPrice = 8.00m,
                        StockQuantity = 50,
                        LowStockThreshold = 10,
                        Category = "Hair Products",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Wig - Long Curly Black",
                        Barcode = "SURF00002",
                        Price = 89.99m,
                        CostPrice = 45.00m,
                        StockQuantity = 15,
                        LowStockThreshold = 5,
                        Category = "Wigs",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Perfume - Floral Essence",
                        Barcode = "SURF00003",
                        Price = 45.50m,
                        CostPrice = 22.00m,
                        StockQuantity = 30,
                        LowStockThreshold = 8,
                        Category = "Perfumes",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Lipstick - Red Velvet",
                        Barcode = "SURF00004",
                        Price = 12.99m,
                        CostPrice = 6.00m,
                        StockQuantity = 100,
                        LowStockThreshold = 20,
                        Category = "Makeup",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "T-Shirt - Cotton White",
                        Barcode = "SURF00005",
                        Price = 25.00m,
                        CostPrice = 12.00m,
                        StockQuantity = 40,
                        LowStockThreshold = 10,
                        Category = "Clothes",
                        IsActive = true
                    }
                };

                context.Products.AddRange(products);
                context.SaveChanges();
            }

            // Seed App Settings
            if (!context.AppSettings.Any())
            {
                var settings = new[]
                {
                    new AppSetting { Key = "BarcodePrefix", Value = "SURF" },
                    new AppSetting { Key = "NextBarcodeNumber", Value = "6" },
                    new AppSetting { Key = "StoreName", Value = "Kenji's Beauty Space" },
                    new AppSetting { Key = "StoreAddress", Value = "123 Main Street" },
                    new AppSetting { Key = "StorePhone", Value = "+1234567890" }
                };

                context.AppSettings.AddRange(settings);
                context.SaveChanges();
            }
        }
    }
}
