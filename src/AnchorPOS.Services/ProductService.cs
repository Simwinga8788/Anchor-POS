using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;

namespace SurfPOS.Services
{
    public class ProductService : IProductService
    {
        private readonly SurfDbContext _context;

        public ProductService(SurfDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeInactive = false)
        {
            return await _context.Products
                .Where(p => includeInactive || p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            // Auto-generate barcode if not provided
            if (string.IsNullOrWhiteSpace(product.Barcode))
            {
                product.Barcode = await GenerateNextBarcodeAsync();
            }

            product.CreatedAt = DateTime.Now;
            _context.Products.Add(product);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Detach the entity if save fails so it doesn't pollute the context for future operations
                _context.Entry(product).State = EntityState.Detached;
                throw;
            }

            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            product.UpdatedAt = DateTime.Now;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            // Hard delete
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string> GenerateNextBarcodeAsync()
        {
            var prefixSetting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "BarcodePrefix");
            var numberSetting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "NextBarcodeNumber");

            string prefix = prefixSetting?.Value ?? "SURF";
            int nextNumber = int.Parse(numberSetting?.Value ?? "1");

            string barcode = $"{prefix}{nextNumber:D5}"; // e.g., SURF00001

            // Update the next number
            if (numberSetting != null)
            {
                numberSetting.Value = (nextNumber + 1).ToString();
                await _context.SaveChangesAsync();
            }

            return barcode;
        }

        public async Task<bool> IsLowStockAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return false;

            return product.StockQuantity <= product.LowStockThreshold;
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold)
                .ToListAsync();
        }
    }
}
