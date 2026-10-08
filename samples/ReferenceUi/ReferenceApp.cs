using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;

namespace Material3.ReferenceUi;

public sealed class ReferenceApp : Application
{
    public static ReferenceConfiguration Configuration { get; set; } = new();
    public MaterialTheme MaterialTheme { get; private set; } = null!;
    public override void Initialize()
    {
        MaterialTheme = new MaterialTheme();
        MaterialTheme.Typography = MaterialTheme.Typography with { FontFamily = new FontFamily("avares://ReferenceUi/Assets/Fonts#Roboto") };
        Styles.Add(MaterialTheme);
    }
    public ReferenceShell CreateReferenceView()
    {
        var configuration = Configuration;
        CultureInfo.CurrentCulture = configuration.Culture; CultureInfo.CurrentUICulture = configuration.Culture;
        MaterialTheme.LightColorScheme = configuration.Palette == "classic" ? MaterialColorScheme.Light : MaterialColorScheme.Light with
        {
            OnPrimaryContainer = Color.Parse("#4F378B"), OnSecondaryContainer = Color.Parse("#4A4458"),
            OnTertiaryContainer = Color.Parse("#633B48"), OnErrorContainer = Color.Parse("#8C1D18")
        };
        RequestedThemeVariant = configuration.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        return new ReferenceShell(MaterialTheme, configuration);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3 · Avalonia reference", Width = 1240d / 3.5, Height = 2772d / 3.5 - 48, Content = CreateReferenceView() };
        else if (ApplicationLifetime is IActivityApplicationLifetime activity) activity.MainViewFactory = CreateReferenceView;
        base.OnFrameworkInitializationCompleted();
    }
}
