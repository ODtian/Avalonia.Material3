using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Gallery.Pages;

namespace ButtonGroupsHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<GroupsApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
}
public sealed class GroupsApplication : Application
{
    private readonly MaterialTheme _theme = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(_theme); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-05 button groups package host", Width = 1100, Height = 850, Content = new ScrollViewer { Content = new ButtonGroupsPage(_theme) } };
        base.OnFrameworkInitializationCompleted();
    }
}
