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
            var storeName = await GetSettingAsync("StoreName") ?? "Anchor POS";
            var subject = $"Shift Report - {storeName} - {employeeName} - {shiftStart:yyyy-MM-dd}";
            
            var body = $"Shift Report for {employeeName}\n\n" +
                      $"Start Time: {shiftStart:yyyy-MM-dd HH:mm}\n" +
                      $"End Time: {shiftEnd?.ToString("yyyy-MM-dd HH:mm") ?? "In Progress"}\n\n" +
                      $"Please find the detailed shift report attached.\n\n" +
                      $"{storeName}";

            return await SendEmailAsync(toEmail, subject, body, reportPath);
        }

        public async Task<bool> SendWhatsAppReportAsync(string phoneNumberString, string reportPath, string employeeName)
        {
            try
            {
                // Run Selenium automation on a background thread so the UI doesn't freeze.
                await Task.Run(() => 
                {
                    // Clean and split phone numbers (e.g., separated by commas or semicolons)
                    var phoneNumbers = phoneNumberString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(p => p.Replace("+", "").Replace(" ", "").Trim())
                                                        .Where(p => !string.IsNullOrEmpty(p))
                                                        .ToList();

                    if (!phoneNumbers.Any()) return;
                    
                    // Set up Chrome profile so the user stays logged in
                    var userDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS", "WhatsAppProfile");
                    
                    var options = new OpenQA.Selenium.Chrome.ChromeOptions();
                    options.AddArgument($"--user-data-dir={userDataDir}");
                    options.AddArgument("--no-sandbox");
                    options.AddArgument("--disable-dev-shm-usage");
                    options.AddArgument("--remote-allow-origins=*");
                    
                    // Enable headless mode for true background automation
                    options.AddArgument("--headless=new");
                    
                    // Essential for headless mode on some sites (WhatsApp Web) so it doesn't serve a mobile version or detect headlessness and block us
                    options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    options.AddArgument("--window-size=1920,1080");

                    // Configure the ChromeDriver Service to hide the irritating black command prompt window
                    var service = OpenQA.Selenium.Chrome.ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    // Initialize driver with both service and options
                    using var driver = new OpenQA.Selenium.Chrome.ChromeDriver(service, options);
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(120);
                    
                    // We set a long timeout of 60 seconds. If the user isn't logged in, 
                    // this gives them 60 seconds to scan the QR code before it fails.
                    var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(60));

                    foreach (var cleanPhone in phoneNumbers)
                    {
                        var message = $"*AUTOMATED SHIFT REPORT*\n" +
                                      $"*Employee:* {employeeName}\n" +
                                      $"*Generated:* {DateTime.Now:f}\n\n" +
                                      $"Please find the detailed shift report attached below.\n" +
                                      $"_This is an automated message from Anchor POS._";
                                      
                        var url = $"https://web.whatsapp.com/send?phone={cleanPhone}&text={Uri.EscapeDataString(message)}";
                        
                        driver.Navigate().GoToUrl(url);

                        try 
                        {
                            // 1. Wait for the main text input to appear (this confirms chat is loaded & logged in)
                            // WhatsApp puts the input in a footer div with contenteditable='true'
                            var chatInput = wait.Until(d => d.FindElement(OpenQA.Selenium.By.XPath("//footer//div[@contenteditable='true']")));
                            chatInput.SendKeys(OpenQA.Selenium.Keys.Enter);
                            
                            Thread.Sleep(1500); // Pause to let message send

                            // The ultimate, final headless bypass: DO NOT try to click WhatsApp's UI menus.
                            // The absolute safest way to force WhatsApp to accept a file in background memory is to literally 
                            // inject a completely fresh file input into the HTML via Javascript, hook it into WhatsApp's React fiber tree
                            // or fallback to the natively exposed generic document input.
                            
                            var fileInputs = wait.Until(d => {
                                // Just grab ANY file input on the DOM. We don't care which one. We will force it to behave.
                                var elements = d.FindElements(OpenQA.Selenium.By.CssSelector("input[type='file']"));
                                return elements.Count > 0 ? elements : null;
                            });

                            // WhatsApp ALWAYS has at least 1 file input on the DOM, even with the menu closed.
                            var baseInput = fileInputs.First();
                            
                            // 1. Force it visible
                            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript(
                                "arguments[0].style.display = 'block'; arguments[0].style.visibility = 'visible';", baseInput);
                            
                            // 2. Strip ALL restrictions off it using Javascript so it accepts the Excel file unconditionally
                            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript(
                                "arguments[0].removeAttribute('accept');", baseInput);
                            
                            // 3. Inject the document directly
                            baseInput.SendKeys(reportPath);
                            
                            Thread.Sleep(3000); // Wait for document preview screen to fully render

                            // 4. Send the file. The preview screen has a different structure.
                            // The most robust way is to just send the Enter key to the active element.
                            try 
                            {
                                var sendFileBtn = driver.FindElement(OpenQA.Selenium.By.XPath("//div[@aria-label='Send'] | //span[@data-icon='send']/.. | //div[@role='button'][.//span[@data-icon='send']]"));
                                sendFileBtn.Click();
                            }
                            catch
                            {
                                new OpenQA.Selenium.Interactions.Actions(driver).SendKeys(OpenQA.Selenium.Keys.Enter).Perform();
                            }

                            // Wait a few seconds for the file upload to complete over the network
                            Thread.Sleep(7000);
                        }
                        catch (Exception innerEx)
                        {
                            try 
                            { 
                                var screenshot = ((OpenQA.Selenium.ITakesScreenshot)driver).GetScreenshot();
                                screenshot.SaveAsFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WhatsApp_Fail_Screenshot.png"));
                            } catch { } // ignore screenshot fails
                            throw innerEx;
                        }
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                var errorLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WhatsApp_Error.txt");
                File.WriteAllText(errorLogPath, $"Time: {DateTime.Now}\nError: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
