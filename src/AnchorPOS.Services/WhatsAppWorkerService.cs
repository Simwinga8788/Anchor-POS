using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
// Win32: only used to strip the taskbar button, NOT to hide the window
// (SW_HIDE freezes Chrome's render loop — off-screen position is used instead)

namespace SurfPOS.Services
{
    public interface IWhatsAppWorkerService
    {
        void EnqueueReport(string phoneNumbers, string reportPath, string employeeName);
        void Start();
    }

    public class WhatsAppReportJob
    {
        public string PhoneNumbers { get; set; } = string.Empty;
        public string ReportPath   { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
    }

    public class WhatsAppWorkerService : IWhatsAppWorkerService
    {
        // ── Win32 P/Invoke ───────────────────────────────────────────────────────
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern int  GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int  SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int SW_HIDE         = 0;
        private const int GWL_EXSTYLE     = -20;
        private const int WS_EX_APPWINDOW = 0x00040000;  // shows button on taskbar
        private const int WS_EX_TOOLWINDOW= 0x00000080;  // hides button from taskbar
        // ────────────────────────────────────────────────────────────────────────

        private readonly ConcurrentQueue<WhatsAppReportJob> _queue = new();
        private bool _isRunning = false;

        public void EnqueueReport(string phoneNumbers, string reportPath, string employeeName)
        {
            _queue.Enqueue(new WhatsAppReportJob
            {
                PhoneNumbers = phoneNumbers,
                ReportPath   = reportPath,
                EmployeeName = employeeName
            });
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Task.Run(ProcessQueueLoopAsync);
        }

        private async Task ProcessQueueLoopAsync()
        {
            while (_isRunning)
            {
                if (_queue.TryDequeue(out var job))
                {
                    try   { ProcessJob(job); }
                    catch (Exception ex)
                    {
                        var log = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            "WhatsAppWorker_Error.txt");
                        File.AppendAllText(log, $"\nTime: {DateTime.Now}\nError: {ex.Message}\nStack: {ex.StackTrace}\n");
                    }
                }
                await Task.Delay(2000);
            }
        }

        private void ProcessJob(WhatsAppReportJob job)
        {
            var phones = job.PhoneNumbers
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();

            // Persistent session folder so WhatsApp Web stays logged in
            var userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AnchorPOS", "WhatsAppProfile");

            var options = new OpenQA.Selenium.Chrome.ChromeOptions();
            options.AddArgument($"--user-data-dir={userDataDir}");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--remote-allow-origins=*");
            // Fully visible Chrome window — no hiding, no headless, no minimize.
            // We need to watch what's happening to debug the attachment flow.

            var driverService = OpenQA.Selenium.Chrome.ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

            using var driver = new OpenQA.Selenium.Chrome.ChromeDriver(driverService, options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(120);

            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(60));

            try
            {
                foreach (var phone in phones)
                {
                    var message = $"*AUTOMATED SHIFT REPORT*\n" +
                                  $"*Employee:* {job.EmployeeName}\n" +
                                  $"*Generated:* {DateTime.Now:f}\n\n" +
                                  $"Please find the detailed shift report attached below.\n" +
                                  $"_This is an automated message from Anchor POS._";

                    var url = $"https://web.whatsapp.com/send?phone={phone}&text={Uri.EscapeDataString(message)}";
                    driver.Navigate().GoToUrl(url);

                    // ── Step 1: Wait for chat to load, then send text ────────────────────
                    // Chat input: WhatsApp removed the <footer> wrapper in a recent update.
                    // data-tab="10" is WhatsApp's stable internal identifier for the message box.
                    var chatInput = wait.Until(d =>
                        d.FindElement(OpenQA.Selenium.By.XPath(
                            "//div[@contenteditable='true'][@data-tab='10'] | " +
                            "//div[@aria-placeholder='Type a message'] | " +
                            "//div[@title='Type a message']")));

                    chatInput.SendKeys(OpenQA.Selenium.Keys.Enter);
                    Thread.Sleep(2000);

                    // ── Step 2: Click the Attach (paperclip) button ──────────────────────
                    var attachBtn = wait.Until(d =>
                        d.FindElement(OpenQA.Selenium.By.XPath(
                            "//button[@aria-label='Attach'] | //div[@title='Attach'] | " +
                            "//span[@data-icon='attach-menu-plus'] | //span[@data-icon='plus']")));
                    Click(driver, attachBtn);
                    Thread.Sleep(1500);

                    // ── Step 3: Block native file dialog, then click Document ────────────
                    // When WhatsApp's JS calls fileInput.click() after Document is selected,
                    // it opens the native Windows Open dialog. We block this by overriding
                    // HTMLInputElement.prototype.click. Selenium's SendKeys bypasses this
                    // and injects the file directly via the WebDriver protocol.
                    var js = (OpenQA.Selenium.IJavaScriptExecutor)driver;
                    js.ExecuteScript(@"
                        window.__origFileClick = HTMLInputElement.prototype.click;
                        HTMLInputElement.prototype.click = function() {
                            if (this.type === 'file') return; // block dialog
                            return window.__origFileClick.apply(this, arguments);
                        };
                    ");

                    var docMenuItem = wait.Until(d =>
                    {
                        foreach (var el in d.FindElements(OpenQA.Selenium.By.XPath(
                            "//button[@aria-label='Document'] | //li[@aria-label='Document']")))
                        { try { if (el.Displayed) return el; } catch { } }

                        foreach (var el in d.FindElements(OpenQA.Selenium.By.XPath(
                            "//span[@data-icon='doc'] | //span[@data-icon='document'] | //span[@data-icon='document-outline'] | //span[@data-icon='media-doc-close']")))
                        { try { if (el.Displayed) return el; } catch { } }

                        foreach (var el in d.FindElements(OpenQA.Selenium.By.XPath(
                            "//*[normalize-space(text())='Document']")))
                        { try { if (el.Displayed) return el; } catch { } }

                        return null;
                    });
                    Click(driver, docMenuItem);
                    Thread.Sleep(2000);

                    // ── Step 4: Grab the Document file input ─────────────────────────────
                    var docInput = wait.Until(d =>
                    {
                        var inputs = d.FindElements(OpenQA.Selenium.By.CssSelector("input[type='file']"));
                        var specific = inputs.FirstOrDefault(i =>
                        {
                            var acc = i.GetAttribute("accept") ?? "";
                            return acc == "*" || acc == "" ||
                                   (!acc.Contains("image") && !acc.Contains("video") && !acc.Contains("audio"));
                        });
                        return specific ?? (inputs.Count > 1 ? inputs.Last() : null);
                    });

                    js.ExecuteScript(
                        "arguments[0].style.display='block'; arguments[0].style.visibility='visible'; arguments[0].style.opacity='1';",
                        docInput);
                    docInput.SendKeys(job.ReportPath);

                    // Restore the original click so the rest of WhatsApp works normally
                    js.ExecuteScript("HTMLInputElement.prototype.click = window.__origFileClick;");

                    Thread.Sleep(3500);

                    // ── Step 5: Click Send on the file preview screen ────────────────────
                    try
                    {
                        var sendBtn = wait.Until(d =>
                            d.FindElement(OpenQA.Selenium.By.XPath(
                                "//div[@aria-label='Send'] | //span[@data-icon='send']/..")));
                        Click(driver, sendBtn);
                    }
                    catch
                    {
                        new OpenQA.Selenium.Interactions.Actions(driver)
                            .SendKeys(OpenQA.Selenium.Keys.Enter).Perform();
                    }

                    Thread.Sleep(8000);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var shot = ((OpenQA.Selenium.ITakesScreenshot)driver).GetScreenshot();
                    shot.SaveAsFile(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "WhatsAppWorker_Fail_Screenshot.png"));
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Strips the taskbar button from Chrome windows that are NEW (not in existingPids),
        /// by setting WS_EX_TOOLWINDOW and clearing WS_EX_APPWINDOW.
        /// Does NOT call SW_HIDE — the window stays alive off-screen at (-32000,-32000).
        /// </summary>
        private static void RemoveTaskbarButton(HashSet<int> existingPids)
        {
            var newPids = new HashSet<int>(
                Process.GetProcessesByName("chrome")
                       .Select(p => p.Id)
                       .Where(id => !existingPids.Contains(id)));

            if (newPids.Count == 0) return;

            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (newPids.Contains((int)pid))
                {
                    var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    SetWindowLong(hWnd, GWL_EXSTYLE,
                        (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
                    // Do NOT ShowWindow(SW_HIDE) — that suspends Chrome's render loop
                }
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Clicks an element, falling back to a JavaScript click if Selenium's
        /// native click throws (e.g. element obscured by an overlay).
        /// </summary>
        private static void Click(OpenQA.Selenium.IWebDriver driver, OpenQA.Selenium.IWebElement el)
        {
            try { el.Click(); }
            catch { ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el); }
        }
    }
}
