using Avalonia;
using System;
using System.IO;

namespace Mass.Launcher;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string crashFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.AppendAllText(crashFile, $"{DateTime.Now}: Unhandled Exception: {error.ExceptionObject}\n");
            }
            catch { }
        };

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
             try
            {
                string crashFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.AppendAllText(crashFile, $"{DateTime.Now}: Main Loop Error: {ex}\n");
            }
            catch { }
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
