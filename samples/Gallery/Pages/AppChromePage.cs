using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Gallery.Pages;

/// <summary>Package-only host example. This page, not the component library, owns routes/history/results.</summary>
public sealed class AppChromePage : Grid
{
    private readonly MaterialTheme _theme;
    private readonly ContentControl _pagePresenter = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly Dictionary<string, Control> _pages = [];
    private readonly MaterialNavigationDrawerLayout _drawerLayout;
    private string _returnPage = "Home";
    private string _savedPage = "";
    private bool _longText;
    private int _recipe = 4;
    private bool? _wide;
    public MaterialOverlayHost Overlays { get; }
    public MaterialNavigationDrawer Drawer { get; } = new() { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false, Title = "Collections 收藏" };
    public MaterialTopAppBar TopBar { get; } = new() { Variant = MaterialTopAppBarVariant.MediumFlexible, Subtitle = "Host-owned page navigation", ScrollBehavior = MaterialAppBarScrollBehavior.ExitUntilCollapsed };
    public MaterialBottomAppBar BottomBar { get; } = new();
    public MaterialIconButton NavigationButton { get; } = new() { Content = "☰" };
    public MaterialIconButton SaveButton { get; } = new() { Content = "✓" };
    public MaterialButton DetailsButton { get; } = new() { Content = "Open details", Variant = MaterialButtonVariant.Tonal };
    public MaterialButton ThemeButton { get; } = new() { Content = "Light / dark", Variant = MaterialButtonVariant.Text };
    public MaterialButton FontButton { get; } = new() { Content = "Font 100 / 200%", Variant = MaterialButtonVariant.Text };
    public MaterialButton LongButton { get; } = new() { Content = "Long text", Variant = MaterialButtonVariant.Text };
    public MaterialButton RecipeButton { get; } = new() { Content = "Next app bar", Variant = MaterialButtonVariant.Text };
    public MaterialButton ScrollButton { get; } = new() { Content = "Scroll page", Variant = MaterialButtonVariant.Text };
    public MaterialButton BackButton { get; } = new() { Content = "Host back", Variant = MaterialButtonVariant.Text };
    public MaterialButton ModalButton { get; } = new() { Content = "Confirm destination", Variant = MaterialButtonVariant.Text };
    public TextBlock Status { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public ScrollViewer PageScroll { get; }
    public string CurrentPage { get; private set; } = "Home";
    public AppChromePage(MaterialTheme theme)
    {
        _theme = theme;
        TopBar.NavigationContent = NavigationButton;
        TopBar.Actions = SaveButton;
        AutomationProperties.SetName(NavigationButton, "Open navigation");
        AutomationProperties.SetName(SaveButton, "Save page");
        var bottomAction = new MaterialIconButton { Content = "→" };
        AutomationProperties.SetName(bottomAction, "Open details");
        var fab = new MaterialFab { Content = "+" };
        AutomationProperties.SetName(fab, "Create detail");
        BottomBar.Actions = bottomAction; BottomBar.FloatingAction = fab;
        bottomAction.Click += (_, _) => OpenDetails();
        fab.Click += (_, _) => OpenDetails();
        var controls = new WrapPanel { Children = { ThemeButton, FontButton, LongButton, RecipeButton, ScrollButton, BackButton } };
        var body = new StackPanel { Spacing = 12, Margin = new Thickness(16), Children = { controls, Status, DetailsButton, _pagePresenter } };
        PageScroll = new ScrollViewer { Content = body, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        TopBar.ScrollSource = PageScroll;
        var chrome = new DockPanel();
        DockPanel.SetDock(TopBar, Dock.Top); DockPanel.SetDock(BottomBar, Dock.Bottom);
        chrome.Children.Add(TopBar); chrome.Children.Add(BottomBar); chrome.Children.Add(PageScroll);
        Drawer.Footer = ModalButton;
        foreach (var route in new[] { "Home", "Library", "Unavailable" })
        {
            var page = new StackPanel { Spacing = 12 };
            page.Children.Add(new TextBlock { Text = route + " content 内容", FontSize = 24, TextWrapping = TextWrapping.Wrap });
            for (var i = 0; i < 24; i++) page.Children.Add(new TextBlock { Text = $"{route} paragraph {i + 1} — Collection 收藏、Latin text and host-owned page state. Scroll this actual page; the bound top bar collapses while the bottom actions remain available.", TextWrapping = TextWrapping.Wrap });
            _pages.Add(route, page);
            var item = new MaterialNavigationItem { Content = route == "Library" ? "Library 资料" : route, Icon = route == "Home" ? "⌂" : "▤", PageContent = page, IsEnabled = route != "Unavailable" };
            AutomationProperties.SetAutomationId(item, "chrome-destination-" + route.ToLowerInvariant());
            Drawer.Items.Add(item);
        }
        Drawer.SelectionChanged += (_, _) =>
        {
            var selected = Drawer.SelectedIndex;
            if (selected < 0) return;
            CurrentPage = selected == 1 ? "Library" : "Home";
            _pagePresenter.Content = Drawer.SelectedContent;
            PageScroll.Offset = default; TopBar.ResetScroll();
            UpdateTitle(); UpdateStatus();
        };
        _drawerLayout = new MaterialNavigationDrawerLayout { Drawer = Drawer, Content = chrome, IsEdgeSwipeEnabled = true };
        Overlays = new MaterialOverlayHost { Content = _drawerLayout };
        Overlays.PropertyChanged += (_, change) => { if (change.Property == MaterialOverlayHost.OpenCountProperty) UpdateStatus(); };
        Children.Add(Overlays);
        NavigationButton.Click += (_, _) => { if (CurrentPage == "Details") ReturnFromDetails(); else OpenNavigation(); };
        SaveButton.Click += (_, _) => { _savedPage = CurrentPage; UpdateStatus(); };
        DetailsButton.Click += (_, _) => OpenDetails();
        BackButton.Click += (_, _) => RequestBack();
        ModalButton.Click += (_, _) =>
        {
            var dialog = new MaterialDialog { Title = "Navigation confirmation", Content = "A dialog above the drawer receives Back / Escape first.", ConfirmResult = CurrentPage };
            var modalBack = new MaterialButton { Content = "Back from confirmation", Variant = MaterialButtonVariant.Text };
            AutomationProperties.SetAutomationId(modalBack, "chrome-dialog-back");
            modalBack.Click += (_, _) => RequestBack();
            dialog.Actions = modalBack;
            var session = dialog.Show(Overlays);
            session.Closed += (_, result) => { if (result.Reason == MaterialOverlayCloseReason.Confirmed) { _savedPage = result.Value as string ?? ""; UpdateStatus(); } };
        };
        ThemeButton.Click += (_, _) => { if (TopLevel.GetTopLevel(this) is Window window) window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark; };
        FontButton.Click += (_, _) => _theme.Typography = _theme.Typography with { FontFamily = new FontFamily("Arial"), Scale = _theme.Typography.Scale == 1 ? 2 : 1 };
        LongButton.Click += (_, _) =>
        {
            _longText = !_longText;
            ((MaterialNavigationItem)Drawer.Items[1]!).Content = _longText ? "Library 资料 — bilingual collections and a very long destination label" : "Library 资料";
            UpdateTitle();
        };
        RecipeButton.Click += (_, _) => { _recipe = (_recipe + 1) % 8; SetRecipe(); };
        ScrollButton.Click += (_, _) => PageScroll.Offset = new Vector(0, PageScroll.Offset.Y == 0 ? 300 : 0);
        SetId(Overlays, "chrome-overlays"); SetId(Drawer, "chrome-drawer"); SetId(TopBar, "chrome-top"); SetId(BottomBar, "chrome-bottom");
        SetId(NavigationButton, "chrome-navigation"); SetId(SaveButton, "chrome-save"); SetId(DetailsButton, "chrome-details");
        SetId(Status, "chrome-status"); SetId(ThemeButton, "chrome-theme"); SetId(FontButton, "chrome-font"); SetId(LongButton, "chrome-long");
        SetId(RecipeButton, "chrome-recipe"); SetId(ScrollButton, "chrome-scroll"); SetId(BackButton, "chrome-back"); SetId(ModalButton, "chrome-modal");
        AutomationProperties.SetName(Drawer, "Navigation menu");
        AutomationProperties.SetLiveSetting(Status, AutomationLiveSetting.Polite);
        _pagePresenter.Content = Drawer.SelectedContent;
        SetRecipe(); UpdateTitle(); UpdateStatus();
        LayoutUpdated += (_, _) => Adapt();
    }
    private static void SetId(Control control, string id) => AutomationProperties.SetAutomationId(control, id);
    private void SetRecipe()
    {
        TopBar.Variant = _recipe switch
        {
            0 => MaterialTopAppBarVariant.Small, 1 => MaterialTopAppBarVariant.CenterAligned, 2 => MaterialTopAppBarVariant.Medium,
            3 => MaterialTopAppBarVariant.Large, 4 or 5 => MaterialTopAppBarVariant.MediumFlexible, _ => MaterialTopAppBarVariant.LargeFlexible
        };
        TopBar.Subtitle = _recipe is 5 or 7 ? "Host-owned page navigation" : null;
        TopBar.CenterTitle = _recipe == 7;
        UpdateStatus();
    }
    public void OpenNavigation() => Drawer.IsOpen = true;
    public bool RequestBack()
    {
        // A vetoed or policy-disabled overlay must not accidentally navigate the underlying page.
        if (Overlays.OpenCount > 0) { Overlays.RequestBack(); return true; }
        if (CurrentPage == "Details") { ReturnFromDetails(); return true; }
        if (Drawer.Mode == MaterialNavigationDrawerMode.Standard && Drawer.IsStandardDismissible && Drawer.IsOpen) return Drawer.Close();
        return false;
    }
    private void OpenDetails()
    {
        if (CurrentPage != "Details") _returnPage = CurrentPage;
        CurrentPage = "Details";
        _pagePresenter.Content = new TextBlock { Text = "Details content — Back returns to the selected collection without discarding its saved result.", TextWrapping = TextWrapping.Wrap };
        PageScroll.Offset = default; TopBar.ResetScroll(); UpdateTitle(); UpdateStatus();
    }
    private void ReturnFromDetails()
    {
        CurrentPage = _returnPage;
        _pagePresenter.Content = _pages[CurrentPage];
        PageScroll.Offset = default; TopBar.ResetScroll(); UpdateTitle(); UpdateStatus();
    }
    private void UpdateTitle()
    {
        TopBar.Title = _longText ? CurrentPage + " 资料与收藏 — bilingual collection with host-owned page operations" : CurrentPage + " 收藏";
        NavigationButton.Content = CurrentPage == "Details" ? "←" : "☰";
        AutomationProperties.SetName(NavigationButton, CurrentPage == "Details" ? "Return to collection" : "Open navigation");
    }
    private void UpdateStatus() => Status.Text = $"Page = {CurrentPage}; selected = {Drawer.SelectedIndex}; {_savedPage} {(_savedPage.Length > 0 ? "saved" : "not saved")}; recipe {_recipe + 1}/8; overlays = {Overlays?.OpenCount ?? 0}.";
    private void Adapt()
    {
        if (Bounds.Width <= 0) return;
        var wide = Bounds.Width >= 720; // Deliberate example-host breakpoint, not an M3 normative token.
        if (_wide == wide) return;
        _wide = wide;
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        var drawerFocused = focused is not null && (focused == Drawer || Drawer.IsVisualAncestorOf(focused));
        Drawer.IsStandardDismissible = !wide;
        if (wide) { Drawer.Mode = MaterialNavigationDrawerMode.Standard; Drawer.IsOpen = true; }
        else
        {
            if (!drawerFocused) Drawer.IsOpen = false;
            Drawer.Mode = MaterialNavigationDrawerMode.Modal;
        }
    }
}
