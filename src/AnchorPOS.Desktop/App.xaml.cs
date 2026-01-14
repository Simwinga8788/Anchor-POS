using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;
using SurfPOS.Services;
using SurfPOS.Desktop.Views;

namespace SurfPOS.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public App()
    {
        // Global exception handlers
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // Set culture to Zambia for Kwacha currency
        var culture = new System.Globalization.CultureInfo("en-ZM");
        var numberFormat = culture.NumberFormat;
        numberFormat.CurrencySymbol = "K";
        culture.NumberFormat = numberFormat;

        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}\n\nInner Exception:\n{e.Exception.InnerException?.Message}",
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true; // Prevent app from closing
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        MessageBox.Show(
            $"A critical error occurred:\n\n{exception?.Message}\n\nStack Trace:\n{exception?.StackTrace}",
            "Critical Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An async error occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}",
            "Async Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.SetObserved(); // Prevent app from closing
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<SurfDbContext>(options =>
            options.UseSqlServer("Server=.\\SQLEXPRESS;Database=SurfPOS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connection Timeout=60"));

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IReceiptPrinterService, ReceiptPrinterService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuditService, AuditService>();

        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<ProductManagementWindow>();
        services.AddTransient<ReportsWindow>();
        services.AddTransient<UserManagementWindow>();
        services.AddTransient<ReceiptPreviewDialog>();
        services.AddTransient<SettingsWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            // Initialize database and seed data
            using (var scope = _serviceProvider!.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SurfDbContext>();
                DbSeeder.SeedData(context);
            }

            // Show login window
            var loginWindow = _serviceProvider!.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            // Log to file
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SurfPOS_Error.txt");
            File.WriteAllText(logPath, $"Startup Error:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\nInner Exception:\n{ex.InnerException?.Message}");
            
            MessageBox.Show($"Application failed to start. Error log saved to Desktop.\n\n{ex.Message}", 
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

