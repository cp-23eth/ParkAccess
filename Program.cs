using Avalonia;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ParkAccess;

sealed class Program
{
    public static AppSettings Settings { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        LoadConfiguration();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
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
}
