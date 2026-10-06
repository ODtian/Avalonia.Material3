using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Gallery.Pages;

namespace BrowseHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<BrowseApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class BrowseApplication : Application
{
    private readonly MaterialTheme _theme = new();
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(_theme); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var page = new CarouselRefreshPage();
            var panel = new DockPanel();
            var controls = new WrapPanel();
            DockPanel.SetDock(controls, Dock.Top);
            var window = new Window { Title = "M3-18 Browse package host", Width = 900, Height = 850, Content = panel, RequestedThemeVariant = ThemeVariant.Light };
            controls.Children.Add(Button("Light / Dark", "BrowseTheme", () => window.RequestedThemeVariant = window.RequestedThemeVariant == ThemeVariant.Light ? ThemeVariant.Dark : ThemeVariant.Light));
            controls.Children.Add(Button("320 / 900 width", "BrowseWidth", () => window.Width = window.Width > 400 ? 320 : 900));
            controls.Children.Add(Button("100 / 200% font", "BrowseFont", () => _theme.Typography = _theme.Typography with { Scale = _theme.Typography.Scale == 1 ? 2 : 1 }));
            panel.Children.Add(controls);
            panel.Children.Add(page);
            window.Closed += (_, _) => page.Dispose();
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
    private static MaterialButton Button(string text, string id, Action action)
    {
        var button = new MaterialButton { Content = text };
        AutomationProperties.SetAutomationId(button, id);
        button.Click += (_, _) => action();
        return button;
    }
}
