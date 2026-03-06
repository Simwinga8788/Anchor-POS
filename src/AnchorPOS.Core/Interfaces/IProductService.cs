using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync(bool includeInactive = false);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
        Task<Product> AddProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<string> GenerateNextBarcodeAsync();
        Task<bool> IsLowStockAsync(int productId);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
    }
}
