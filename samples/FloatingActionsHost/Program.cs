using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Gallery.Pages;

namespace FloatingActionsHost;

internal static class Program
{
    internal static bool Smoke;
    [STAThread]
    public static void Main(string[] args)
    {
        Smoke = args.Contains("--smoke");
        AppBuilder.Configure<FloatingActionsApplication>().UsePlatformDetect().UseHarfBuzz().StartWithClassicDesktopLifetime(args);
    }
}
public sealed class FloatingActionsApplication : Application
{
    private readonly MaterialTheme _theme = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(_theme); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window { Title = "M3-04 Floating actions package host", Width = 1000, Height = 850, Content = Program.Smoke ? CreateSmokeScene() : new FloatingActionsPage(_theme) };
        base.OnFrameworkInitializationCompleted();
    }
    private Control CreateSmokeScene()
    {
        var result = new TextBlock { Text = "Waiting" };
        AutomationProperties.SetAutomationId(result, "floating-native-result");
        var fab = new MaterialFab { Content = "+" };
        AutomationProperties.SetName(fab, "Create document");
        AutomationProperties.SetAutomationId(fab, "floating-native-fab");
        var count = 0;
        fab.Click += (_, _) => result.Text = $"Created {++count}";
        var menu = new MaterialFabMenu { Margin = new Thickness(16) };
        AutomationProperties.SetName(menu, "Creation actions");
        AutomationProperties.SetAutomationId(menu, "floating-native-menu");
        foreach (var label in new[] { "Document", "Folder", "Unavailable" })
        {
            var item = new MaterialFabMenuItem { Content = label, LeadingIcon = "+", IsEnabled = label != "Unavailable" };
            AutomationProperties.SetAutomationId(item, "floating-native-" + label);
            item.Click += (_, _) => result.Text = label + " selected";
            menu.Items.Add(item);
        }
        var toolbar = new MaterialToolbar { Variant = MaterialToolbarVariant.Docked, Color = MaterialToolbarColor.Vibrant };
        AutomationProperties.SetName(toolbar, "Editing toolbar");
        AutomationProperties.SetAutomationId(toolbar, "floating-native-toolbar");
        toolbar.Items.Add(new MaterialIconButton { Content = "★", IsToggle = true });
        toolbar.LeadingItems.Add(new MaterialIconButton { Content = "↶" });
        toolbar.TrailingItems.Add(new MaterialIconButton { Content = "▣" });
        var preferences = new WrapPanel { ItemSpacing = 8 };
        var disable = new MaterialButton { Content = "Enable / disable" };
        AutomationProperties.SetAutomationId(disable, "floating-native-disable");
        disable.Click += (_, _) => { fab.IsEnabled = !fab.IsEnabled; menu.IsEnabled = !menu.IsEnabled; toolbar.IsEnabled = !toolbar.IsEnabled; };
        preferences.Children.Add(disable);
        var mode = new MaterialButton { Content = "Light / dark" };
        AutomationProperties.SetAutomationId(mode, "floating-native-mode");
        mode.Click += (_, _) => { if (TopLevel.GetTopLevel(mode) is Window window) window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark; };
        preferences.Children.Add(mode);
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), Margin = new Thickness(16) };
        Grid.SetRow(toolbar, 0); grid.Children.Add(toolbar);
        var actions = new StackPanel { Spacing = 8, Margin = new Thickness(8), Children = { fab, result } };
        Grid.SetRow(actions, 1); grid.Children.Add(actions);
        Grid.SetRow(menu, 2); grid.Children.Add(menu);
        Grid.SetRow(preferences, 3); grid.Children.Add(preferences);
        return grid;
    }
}
