using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Gallery.Pages;

namespace AppChromeHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<ChromeApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class ChromeApplication : Application
{
    private readonly MaterialTheme _theme = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(_theme); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-16 package app bars and drawer", Width = 1000, Height = 700, MinWidth = 200, MinHeight = 300, Content = new AppChromePage(_theme) };
        base.OnFrameworkInitializationCompleted();
    }
}
