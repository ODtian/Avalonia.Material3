using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Gallery.Pages;

namespace SheetsHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<SheetsApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class SheetsApplication : Application
{
    private readonly MaterialTheme _theme = new();
    public override void Initialize() => Styles.Add(_theme);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-14 Sheets package host", Width = 900, Height = 700, Content = new SheetsPage(_theme) };
        base.OnFrameworkInitializationCompleted();
    }
}
