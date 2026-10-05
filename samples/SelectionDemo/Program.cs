using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Gallery.Pages;

namespace SelectionDemo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<SelectionDemoApplication>()
        .UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}

public class SelectionDemoApplication : Application
{
    private readonly MaterialTheme _theme = new();

    public override void Initialize()
    {
        // SimpleTheme supplies only the demo's scrolling scaffold. M3 themes override the window
        // and explicitly keyed Material controls; the library does not depend on SimpleTheme.
        Styles.Add(new SimpleTheme());
        Styles.Add(_theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window
            {
                Title = "M3-06 Selection form — package consumer",
                Width = 680,
                Height = 920,
                MinWidth = 280,
                MinHeight = 420,
                RequestedThemeVariant = ThemeVariant.Light,
                Content = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Content = new SelectionFormPage(_theme) { Margin = new Thickness(24) }
                }
            };
        base.OnFrameworkInitializationCompleted();
    }
}
