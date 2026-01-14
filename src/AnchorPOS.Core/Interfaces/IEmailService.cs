namespace SurfPOS.Core.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath = null);
        Task<bool> SendShiftReportAsync(string toEmail, string reportPath, string employeeName, DateTime shiftStart, DateTime? shiftEnd);
        Task<bool> SendWhatsAppReportAsync(string phoneNumber, string reportPath, string employeeName);
    }
}
