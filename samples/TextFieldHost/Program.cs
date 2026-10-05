using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Gallery.Pages;

namespace TextFieldHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<TextFieldApp>()
        .UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
}

public sealed class TextFieldApp : Application
{
    public override void Initialize()
    {
        // Only the standalone host's scrolling chrome needs SimpleTheme; Material fields have their own templates.
        Styles.Add(new SimpleTheme());
        Styles.Add(new MaterialTheme());
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window
            {
                Title = "Material text fields — M3-07", Width = 620, Height = 880, MinWidth = 280,
                Content = new ScrollViewer { Content = new TextFieldsPage { Margin = new Thickness(24) } }
            };
        base.OnFrameworkInitializationCompleted();
    }
}
