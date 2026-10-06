using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Gallery.Pages;

namespace ProgressHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<ProgressApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class ProgressApplication : Application
{
    private readonly MaterialTheme _theme = new();
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(_theme); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-11 Progress package host", Width = 1000, Height = 780,
                Content = new ScrollViewer { Content = new ProgressFeedbackPage(_theme) } };
        base.OnFrameworkInitializationCompleted();
    }
}
