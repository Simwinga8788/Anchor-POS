using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;

namespace SurfPOS.Services
{
    public class ShiftService : IShiftService
    {
        private readonly SurfDbContext _context;
        private readonly IExcelService _excelService;

        public ShiftService(SurfDbContext context, IExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        public async Task<Shift> StartShiftAsync(int userId, decimal cashStart)
        {
            // Check if user already has an active shift
            var activeShift = await GetActiveShiftAsync(userId);
            if (activeShift != null)
            {
                throw new InvalidOperationException("User already has an active shift");
            }

            var shift = new Shift
            {
                UserId = userId,
                StartTime = DateTime.Now,
                CashStart = cashStart,
                IsActive = true,
                TransactionCount = 0,
                TotalSales = 0
            };

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
            return shift;
        }

        public async Task<Shift> EndShiftAsync(int shiftId, decimal cashEnd)
        {
            var shift = await _context.Shifts
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == shiftId);

            if (shift == null)
                throw new Exception("Shift not found");

            if (!shift.IsActive)
                throw new InvalidOperationException("Shift is already ended");

            // Calculate shift statistics
            var shiftTransactions = await _context.Transactions
                .Where(t => t.ShiftId == shift.Id)
                .ToListAsync();

            shift.EndTime = DateTime.Now;
            shift.CashEnd = cashEnd;
            shift.TransactionCount = shiftTransactions.Count;
            shift.TotalSales = shiftTransactions.Sum(t => t.TotalAmount);
            shift.IsActive = false;

            // Generate shift report
            shift.ReportFilePath = await GenerateShiftReportAsync(shiftId);

            await _context.SaveChangesAsync();
            return shift;
        }

        public async Task<Shift?> GetActiveShiftAsync(int userId)
        {
            return await _context.Shifts
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
        }

        public async Task<List<Shift>> GetShiftsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Shifts
                .Include(s => s.User)
                .Where(s => s.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(s => s.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.StartTime <= endDate.Value);

            return await query.OrderByDescending(s => s.StartTime).ToListAsync();
        }

        public async Task<List<Shift>> GetAllShiftsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Shifts.Include(s => s.User).AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.StartTime <= endDate.Value);

            return await query.OrderByDescending(s => s.StartTime).ToListAsync();
        }

        public async Task<string> GenerateShiftReportAsync(int shiftId)
        {
            var shift = await _context.Shifts
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == shiftId);

            if (shift == null)
                throw new Exception("Shift not found");

            // Get transactions for this shift
            var transactions = await _context.Transactions
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .Where(t => t.ShiftId == shiftId)
                .ToListAsync();

            // Create folder for shift reports
            string reportsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Kenji's Beauty Space",
                "Shift Reports");
            Directory.CreateDirectory(reportsFolder);

            // Generate filename
            string filename = $"Shift_{shift.User.Username}_{shift.StartTime:yyyy-MM-dd_HH-mm}.xlsx";
            string filePath = Path.Combine(reportsFolder, filename);

            // Generate Excel report
            await _excelService.ExportTransactionsToExcelAsync(
                filePath, 
                transactions, 
                shift.StartTime, 
                shift.EndTime ?? DateTime.Now);

            return filePath;
        }
    }
}
