using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface IShiftService
    {
        Task<Shift> StartShiftAsync(int userId, decimal cashStart);
        Task<Shift> EndShiftAsync(int shiftId, decimal cashEnd);
        Task<Shift?> GetActiveShiftAsync(int userId);
        Task<List<Shift>> GetShiftsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Shift>> GetAllShiftsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<string> GenerateShiftReportAsync(int shiftId);
    }
}
