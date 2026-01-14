using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface IReceiptPrinterService
    {
        Task<bool> PrintReceiptAsync(Transaction transaction, User cashier);
        Task<bool> OpenCashDrawerAsync();
        bool IsPrinterAvailable();
        string GetDefaultPrinterName();
    }
}
