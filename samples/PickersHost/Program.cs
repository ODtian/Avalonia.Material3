using Avalonia;
using Avalonia.Controls;
using Avalonia.Material3.Themes;
using Gallery.Pages;

namespace PickersHost;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) => AppBuilder.Configure<PickerApplication>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}

public sealed class PickerApplication : Application
{
    private readonly MaterialTheme _theme = new();
    public override void Initialize() => Styles.Add(_theme);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "Material date and time pickers", Width = 900, Height = 800,
                Content = new DateTimePickersPage(_theme) };
        base.OnFrameworkInitializationCompleted();
    }
}
