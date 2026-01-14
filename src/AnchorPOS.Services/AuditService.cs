using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;

namespace SurfPOS.Services
{
    public class AuditService : IAuditService
    {
        private readonly SurfDbContext _context;

        public AuditService(SurfDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(int? userId, string action, string details)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.Now
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // We should never crash the app just because logging failed, 
                // but for now we write to debug. Ideally write to a text file fallback.
                System.Diagnostics.Debug.WriteLine($"Failed to write audit log: {ex.Message}");
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.AuditLogs
                .Include(l => l.User)
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
