using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Gallery.Pages;

namespace SecondaryFeedbackHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<FeedbackApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class FeedbackApplication : Application
{
    private readonly MaterialTheme _theme = new();
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(_theme); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-13 Secondary feedback package host", Width = 900, Height = 820, Content = new SecondaryFeedbackPage(_theme) };
        base.OnFrameworkInitializationCompleted();
    }
}
