using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface IExcelService
    {
        Task<List<Product>> ImportProductsFromExcelAsync(string filePath);
        Task ExportProductsToExcelAsync(string filePath, IEnumerable<Product> products);
        Task ExportTransactionsToExcelAsync(string filePath, IEnumerable<Transaction> transactions, DateTime startDate, DateTime endDate, string storeName);
        Task<byte[]> GenerateProductTemplateAsync();
    }
}
