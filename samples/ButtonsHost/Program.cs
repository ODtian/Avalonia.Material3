using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Gallery.Pages;

namespace ButtonsHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<ButtonsApplication>()
        .UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}

public sealed class ButtonsApplication : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new MaterialTheme { Motion = new MaterialMotion { ReduceMotion = true } });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window
            {
                Title = "M3-03 Expressive buttons package host", Width = 1100, Height = 850,
                Content = new ScrollViewer { Content = new ExpressiveButtonsPage() }
            };
        base.OnFrameworkInitializationCompleted();
    }
}
