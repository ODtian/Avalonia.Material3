using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Gallery.Pages;

namespace SearchHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<SearchApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class SearchApplication : Application
{
    private readonly MaterialTheme _theme = new();
    public override void Initialize() { Styles.Add(_theme); RequestedThemeVariant = ThemeVariant.Light; }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-08 Search and chips package host", Width = 900, Height = 850, MinWidth = 280,
                Content = new ScrollViewer { Content = new SearchChipsPage(_theme) } };
        base.OnFrameworkInitializationCompleted();
    }
}
