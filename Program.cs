using Avalonia;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;

namespace ParkAccess;

sealed class Program
{
    public static AppSettings Settings { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        ConfigureLogging();
        LoadConfiguration();

        Log.Information("Application démarrée");
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Settings = config.Get<AppSettings>();
    }

    private static void ConfigureLogging()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "log.txt");

        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, fileSizeLimitBytes: null, rollOnFileSizeLimit: false)
            .CreateLogger();
    }
}
