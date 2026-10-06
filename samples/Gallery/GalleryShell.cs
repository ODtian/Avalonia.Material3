using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Gallery.Pages;

namespace Gallery;

/// <summary>Explicit package-consumer navigation. Each visit owns a fresh page lifetime.</summary>
public sealed class GalleryShell : UserControl
{
    public sealed record Page(string Id, Func<Control> Create, bool OwnsViewport = false);
    private readonly ContentControl _presenter = new()
    { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly MaterialTheme _theme;
    private readonly TextBlock _caption = new() { TextWrapping = TextWrapping.Wrap };
    private int _index = -1;
    private int _actions;
    public MaterialOverlayHost Overlay { get; } = new();
    public IReadOnlyList<Page> Pages { get; }
    public Control? CurrentPage { get; private set; }
    public string CurrentPageId => _index < 0 ? "" : Pages[_index].Id;
    public MaterialButton ActionButton { get; } = new() { Name = "ActionButton", Content = "Run action" };
    public MaterialButton ThemeButton { get; } = new() { Name = "ThemeButton", Content = "Use dark theme" };
    public TextBlock ResultText { get; } = new() { Name = "ResultText", Text = "Waiting", TextWrapping = TextWrapping.Wrap };

    public GalleryShell(MaterialTheme theme)
    {
        _theme = theme;
        Pages = new Page[]
        {
            new("ThemeTokens", () => new ThemeTokensPage(theme)),
            new("ExpressiveButtons", () => new ExpressiveButtonsPage()),
            new("FloatingActions", () => new FloatingActionsPage(theme)),
            new("ButtonGroups", () => new ButtonGroupsPage(theme)),
            new("SelectionForm", () => new SelectionFormPage(theme)),
            new("TextFields", () => new TextFieldsPage(theme)),
            new("SearchChips", () => new SearchChipsPage(theme)),
            new("ContentHierarchy", () => new ContentHierarchyPage()),
            new("SliderSettings", () => new SliderSettingsPage()),
            new("ProgressFeedback", () => new ProgressFeedbackPage(theme)),
            new("Dialogs", () => new DialogsPage(theme, Overlay), true),
            new("SecondaryFeedback", () => new SecondaryFeedbackPage(theme, Overlay), true),
            new("Sheets", () => new SheetsPage(theme, Overlay), true),
            new("ContentNavigation", () => new ContentNavigationPage(theme), true),
            new("AppChrome", () => new AppChromePage(theme, Overlay), true),
            new("DateTimePickers", () => new DateTimePickersPage(theme, Overlay), true),
            new("CarouselRefresh", () => new CarouselRefreshPage())
        };
        AutomationProperties.SetAutomationId(ActionButton, "ActionButton");
        AutomationProperties.SetAutomationId(ThemeButton, "ThemeButton");
        AutomationProperties.SetAutomationId(ResultText, "ResultText");
        AutomationProperties.SetLiveSetting(ResultText, AutomationLiveSetting.Polite);
        AutomationProperties.SetAutomationId(_caption, "GalleryPage");
        ActionButton.Click += (_, _) => ResultText.Text = $"Action completed ({++_actions})";
        ThemeButton.Click += (_, _) =>
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;
            top.RequestedThemeVariant = top.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
            ThemeButton.Content = top.RequestedThemeVariant == ThemeVariant.Dark ? "Use light theme" : "Use dark theme";
        };
        var actions = new WrapPanel { Children = { ActionButton, ThemeButton } };
        actions.Children.Add(Button("Previous page", "GalleryPrevious", () => Navigate(Pages[(_index + Pages.Count - 1) % Pages.Count].Id)));
        actions.Children.Add(Button("Next page", "GalleryNext", () => Navigate(Pages[(_index + 1) % Pages.Count].Id)));
        actions.Children.Add(Button("Back", "GalleryBack", () => RequestBack()));
        var settings = new WrapPanel();
        settings.Children.Add(Button("Font 100 / 150 / 200%", "GalleryFont", () =>
            theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 1.5 : theme.Typography.Scale == 1.5 ? 2 : 1 }));
        settings.Children.Add(Button("Seed color", "GallerySeed", () =>
        { theme.DynamicColors = null; theme.SeedColor = theme.SeedColor == Colors.Teal ? Colors.Coral : Colors.Teal; }));
        settings.Children.Add(Button("Platform scheme / restore", "GalleryPlatform", () =>
            theme.DynamicColors = theme.DynamicColors is null ? new MaterialDynamicColors(MaterialColorScheme.FromSeed(Colors.DarkGreen), MaterialColorScheme.FromSeed(Colors.DarkGreen, true)) : null));
        settings.Children.Add(Button("Round / square", "GalleryShape", () =>
            theme.Shapes = theme.Shapes with { ButtonCornerRadius = theme.Shapes.ButtonCornerRadius == 20 ? 4 : 20 }));
        settings.Children.Add(Button("Reduce motion", "GalleryMotion", () =>
            theme.Motion = theme.Motion with { ReduceMotion = !theme.Motion.ReduceMotion }));
        settings.Children.Add(Button("Window narrow / wide", "GalleryWindow", () =>
        { if (TopLevel.GetTopLevel(this) is Window window) { window.Width = window.Width > 400 ? 320 : 1000; window.Height = window.Width < 400 ? 500 : 800; } }));
        var header = new StackPanel { Margin = new Thickness(8), Spacing = 4, Children =
        {
            _caption, actions, ResultText, settings,
            new TextBlock { Text = "长文本、中文 / Latin multilingual content — resize and scroll; host owns fonts and platform inputs.", TextWrapping = TextWrapping.Wrap }
        } };
        var layout = new Grid { RowDefinitions = new RowDefinitions("170,*") };
        layout.Children.Add(new ScrollViewer { Content = header, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
        Grid.SetRow(_presenter, 1); layout.Children.Add(_presenter);
        Overlay.Content = layout;
        Content = Overlay;
        KeyDown += (_, e) => { if (e.Key == Key.BrowserBack) { e.Handled = RequestBack(); } };
        Navigate(Pages[0].Id);
    }
    public bool Navigate(string id)
    {
        if (Overlay.OpenCount > 0) return false;
        var next = -1;
        for (var i = 0; i < Pages.Count; i++) if (Pages[i].Id == id) next = i;
        if (next < 0) return false;
        if (next == _index) return true;
        _presenter.Content = null; // Detach before disposing; no dormant live page/timer remains.
        if (CurrentPage is IDisposable disposable) disposable.Dispose();
        _index = next;
        CurrentPage = Pages[next].Create();
        _presenter.Content = Pages[next].OwnsViewport ? CurrentPage : new ScrollViewer
        { Content = CurrentPage, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        _caption.Text = $"{next + 1}/{Pages.Count} · {CurrentPageId}";
        return true;
    }
    public bool RequestBack()
    {
        // False from RequestBack can mean collapse-first or veto, NOT permission to navigate.
        if (Overlay.OpenCount > 0) { Overlay.RequestBack(); return true; }
        return _index > 0 && Navigate(Pages[_index - 1].Id);
    }
    private static MaterialButton Button(string caption, string id, Action action)
    {
        var button = new MaterialButton { Content = caption, Variant = MaterialButtonVariant.Text };
        AutomationProperties.SetAutomationId(button, id);
        button.Click += (_, _) => action();
        return button;
    }
}
