using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;

namespace SurfPOS.Services
{
    public class EmailService : IEmailService
    {
        private readonly SurfDbContext _context;

        public EmailService(SurfDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath = null)
        {
            try
            {
                // Get email settings from database
                var smtpHost = await GetSettingAsync("SmtpHost");
                var smtpPort = await GetSettingAsync("SmtpPort");
                var smtpUsername = await GetSettingAsync("SmtpUsername");
                var smtpPassword = await GetSettingAsync("SmtpPassword");
                var fromEmail = await GetSettingAsync("FromEmail");

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(fromEmail))
                {
                    // Email not configured
                    return false;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail);
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;

                // Add attachment if provided
                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    message.Attachments.Add(new Attachment(attachmentPath));
                }

                using var smtp = new SmtpClient(smtpHost, int.Parse(smtpPort ?? "587"));
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SendShiftReportAsync(string toEmail, string reportPath, string employeeName, DateTime shiftStart, DateTime? shiftEnd)
        {
            var subject = $"Shift Report - {employeeName} - {shiftStart:yyyy-MM-dd}";
            
            var body = $"Shift Report for {employeeName}\n\n" +
                      $"Start Time: {shiftStart:yyyy-MM-dd HH:mm}\n" +
                      $"End Time: {shiftEnd?.ToString("yyyy-MM-dd HH:mm") ?? "In Progress"}\n\n" +
                      $"Please find the detailed shift report attached.\n\n" +
                      $"Kenji's Beauty Space POS System";

            return await SendEmailAsync(toEmail, subject, body, reportPath);
        }

        public async Task<bool> SendWhatsAppReportAsync(string phoneNumber, string reportPath, string employeeName)
        {
            try
            {
                // WhatsApp Web API - opens WhatsApp with pre-filled message
                var message = $"Shift Report for {employeeName}\n\nReport file: {Path.GetFileName(reportPath)}";
                var encodedMessage = Uri.EscapeDataString(message);
                var whatsappUrl = $"https://wa.me/{phoneNumber.Replace("+", "")}?text={encodedMessage}";
                
                // Open WhatsApp in default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = whatsappUrl,
                    UseShellExecute = true
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> GetSettingAsync(string key)
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }
    }
}
