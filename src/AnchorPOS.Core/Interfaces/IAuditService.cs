using SurfPOS.Core.Entities;

namespace SurfPOS.Core.Interfaces
{
    public interface IAuditService
    {
        Task LogActionAsync(int? userId, string action, string details);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime startDate, DateTime endDate);
    }
}
