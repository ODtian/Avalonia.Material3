using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;

namespace Material3.ReferenceUi;

/// <summary>A literal host-layout port of the official Android reference, using packaged controls.</summary>
public sealed partial class ReferenceShell : UserControl, IDisposable
{
    public static IReadOnlyList<string> SceneIds { get; } = Array.AsReadOnly(new[] { "date-range", "date-range-7-24", "date-range-9-16", "date-single", "time", "selection", "selection-disabled", "selection-disabled-off", "slider", "fields", "buttons", "ripple", "fab", "progress", "carousel", "navigation", "overlays" });
    private readonly ContentControl _scene = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly Grid _layout = new() { RowDefinitions = new RowDefinitions("Auto,*") };
    private readonly List<IDisposable> _sceneLifetime = [];
    private readonly MaterialIconButton _home;
    private readonly MaterialIconButton _themeButton;
    private readonly ReferenceConfiguration _configuration;
    private Thickness _safeArea;
    public MaterialTheme MaterialTheme { get; }
    public MaterialOverlayHost Overlay { get; } = new();
    public MaterialTopAppBar TopBar { get; } = new();
    public string Scene { get; private set; } = "home";
    public bool Dark { get; private set; }
    public Control? CurrentScene => _scene.Content as Control;

    public ReferenceShell(MaterialTheme theme, ReferenceConfiguration configuration)
    {
        configuration.Validate();
        MaterialTheme = theme; _configuration = configuration; Dark = configuration.Dark;
        _home = IconButton("arrow_back", "home", "Home", () => Navigate("home"));
        _themeButton = IconButton(Dark ? "light_mode" : "dark_mode", "theme", "Toggle theme", ToggleTheme);
        TopBar.Actions = _themeButton;
        _layout.Children.Add(TopBar); Grid.SetRow(_scene, 1); _layout.Children.Add(_scene);
        Overlay.Content = _layout; Overlay.Bind(BackgroundProperty, new DynamicResourceExtension("M3.SurfaceBrush"));
        Content = Overlay;
        KeyDown += (_, args) => { if (args.Key == Key.Escape && RequestBack()) args.Handled = true; };
        AttachedToVisualTree += (_, _) => ApplyTheme();
        Navigate(configuration.Scene);
    }
    public bool Navigate(string scene)
    {
        if (Overlay.OpenCount > 0) return false;
        if (scene != "home" && !SceneIds.Contains(scene, StringComparer.Ordinal)) return false;
        _scene.Content = null;
        foreach (var lifetime in _sceneLifetime) lifetime.Dispose(); _sceneLifetime.Clear();
        Scene = scene; TopBar.Title = "M3 · " + scene;
        TopBar.NavigationContent = scene == "home" ? null : _home;
        _scene.Content = scene switch
        {
            "home" => CreateHome(), "date-range" or "date-range-7-24" or "date-range-9-16" => CreateDateRange(scene),
            "date-single" => CreateDateSingle(), "time" => CreateTime(), "selection" => CreateSelection(),
            "selection-disabled" => CreateSelection(false),
            "selection-disabled-off" => CreateSelection(false, false),
            "slider" => CreateSlider(), "fields" => CreateFields(), "buttons" => CreateButtons(),
            "ripple" => CreateRipple(), "fab" => CreateFab(), "progress" => CreateProgress(),
            "carousel" => CreateCarousel(), "navigation" => CreateNavigation(), "overlays" => CreateOverlays(),
            _ => throw new ArgumentOutOfRangeException(nameof(scene))
        };
        return true;
    }
    public bool RequestBack()
    {
        if (Overlay.OpenCount > 0) { Overlay.RequestBack(); return true; }
        return Scene != "home" && Navigate("home");
    }
    public void SetSafeArea(Thickness insets) { _safeArea = insets; _layout.Margin = insets; }
    private void ToggleTheme() { Dark = !Dark; ApplyTheme(); }
    private void ApplyTheme()
    {
        if (TopLevel.GetTopLevel(this) is { } root) root.RequestedThemeVariant = Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        _themeButton.Content = Symbol(Dark ? "light_mode" : "dark_mode");
    }
    private Control CreateHome()
    {
        var column = new StackPanel { Margin = new Thickness(16), Spacing = 8 };
        column.Children.Add(Text("Official Compose Material3 1.5.0-beta01", MaterialTypeRole.BodySmall));
        foreach (var id in SceneIds) column.Children.Add(Button(id, "scene-" + id, () => Navigate(id), MaterialButtonVariant.Tonal, true));
        return Scroll(column);
    }
    private static StackPanel SceneColumn() => new() { Margin = new Thickness(16), Spacing = 16 };
    private static Control Scroll(StackPanel column) => new ScrollViewer { Content = column, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
    private static StackPanel Row(double spacing, params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        foreach (var child in children) { child.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(child); }
        return row;
    }
    private static T SetId<T>(T control, string tag) where T : Control { AutomationProperties.SetAutomationId(control, tag); return control; }
    private static TextBlock Text(string text, MaterialTypeRole role = MaterialTypeRole.BodyLarge)
    {
        var label = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        label.Classes.Add(role switch { MaterialTypeRole.BodySmall => "m3-body-small", MaterialTypeRole.TitleLarge => "m3-title-large", MaterialTypeRole.TitleMedium => "m3-title-medium", MaterialTypeRole.HeadlineSmall => "m3-headline-small", _ => "m3-body-large" });
        return label;
    }
    private static MaterialSymbol Symbol(string name, double size = 24) => new() { Symbol = name, Size = size, Filled = name != "favorite_border" };
    private static MaterialButton Button(string text, string tag, Action action, MaterialButtonVariant variant = MaterialButtonVariant.Filled, bool fill = false)
    {
        var button = SetId(new MaterialButton { Content = text, Variant = variant, HorizontalAlignment = fill ? HorizontalAlignment.Stretch : HorizontalAlignment.Left }, tag);
        button.Click += (_, _) => action(); return button;
    }
    private static MaterialIconButton IconButton(string icon, string tag, string description, Action action)
    {
        var button = SetId(new MaterialIconButton { Content = Symbol(icon), HorizontalAlignment = HorizontalAlignment.Left }, tag);
        AutomationProperties.SetName(button, description); button.Click += (_, _) => action(); return button;
    }
    public void Dispose() { foreach (var lifetime in _sceneLifetime) lifetime.Dispose(); _sceneLifetime.Clear(); }
}
