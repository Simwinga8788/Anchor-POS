using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface ISalesService
    {
        Task<Transaction> ProcessSaleAsync(int userId, List<(int ProductId, int Quantity)> items, PaymentMethod paymentMethod, int? shiftId = null);
        Task<IEnumerable<Transaction>> GetTransactionsByDateAsync(DateTime date);
        Task<IEnumerable<Transaction>> GetTransactionsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate);
    }
}
