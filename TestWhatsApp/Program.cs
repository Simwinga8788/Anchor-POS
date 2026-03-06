using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        Console.WriteLine("Starting WhatsApp Test...");
        try
        {
            var phoneNumberString = "+260972996902";
            var employeeName = "Test Employee";
            var reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestReport.txt");
            File.WriteAllText(reportPath, "This is a test report.");

            var phoneNumbers = phoneNumberString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(p => p.Replace("+", "").Replace(" ", "").Trim())
                                                .Where(p => !string.IsNullOrEmpty(p))
                                                .ToList();

            Console.WriteLine($"Found {phoneNumbers.Count} numbers. e.g. {phoneNumbers.FirstOrDefault()}");

            var userDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPOS", "WhatsAppProfile");
            Directory.CreateDirectory(userDataDir);
            Console.WriteLine($"UserDataDir: {userDataDir}");

            var options = new OpenQA.Selenium.Chrome.ChromeOptions();
            options.AddArgument($"--user-data-dir={userDataDir}");
            
            Console.WriteLine("Initializing ChromeDriver...");
            using var driver = new OpenQA.Selenium.Chrome.ChromeDriver(options);
            Console.WriteLine("ChromeDriver initialized!");

            driver.Navigate().GoToUrl("https://google.com");
            Console.WriteLine("Navigated to Google.");
            Thread.Sleep(2000);
            
            Console.WriteLine("Closing driver...");
            driver.Quit();
            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
        }
    }
}
