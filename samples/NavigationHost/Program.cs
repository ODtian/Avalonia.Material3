using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Gallery.Pages;

namespace NavigationHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<NavigationApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class NavigationApplication : Application
{
    private readonly MaterialTheme _theme = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(_theme); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-15 package content navigation", Width = 1000, Height = 900, Content = new ContentNavigationPage(_theme) };
        base.OnFrameworkInitializationCompleted();
    }
}
