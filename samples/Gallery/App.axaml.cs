using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Gallery;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // The demonstration owns its fonts. The reusable library and consumer Typography
        // defaults are not overwritten, and nothing is installed into the operating system.
        var theme = Styles.OfType<Avalonia.Material3.Themes.MaterialTheme>().Single();
        theme.Typography = theme.Typography with
        {
            FontFamily = new Avalonia.Media.FontFamily("avares://Gallery/Assets/Fonts#Roboto, avares://Gallery/Assets/Fonts#Noto Sans SC")
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
