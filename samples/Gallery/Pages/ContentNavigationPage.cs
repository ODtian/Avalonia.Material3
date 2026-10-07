using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Package-only content navigation host. Generic pages, input and responsive policy belong to this host, not the library.</summary>
public sealed class ContentNavigationPage : Grid
{
    private readonly MaterialTheme _theme;
    private readonly List<MaterialNavigation> _previews = [];
    public IReadOnlyList<MaterialNavigation> Previews => _previews;
    public MaterialTabs MainTabs { get; } = new();
    public MaterialNavigationRail ResponsiveRail { get; } = new();
    public ScrollViewer Scroller { get; }
    public TextBlock Status { get; } = new() { Text = "Ready: Home content", TextWrapping = TextWrapping.Wrap };
    public MaterialButton ModeButton { get; } = new() { Content = "Light / Dark 明暗", Variant = MaterialButtonVariant.Tonal };
    public MaterialButton FontButton { get; } = new() { Content = "Font 100 / 200% 字号", Variant = MaterialButtonVariant.Tonal };
    public MaterialButton LongLabelButton { get; } = new() { Content = "Long labels 长文案", Variant = MaterialButtonVariant.Tonal };
    public MaterialButton MarkRead { get; private set; } = null!;
    private bool _longLabels;
    private int _invocations;

    public ContentNavigationPage(MaterialTheme theme)
    {
        _theme = theme;
        RowDefinitions = new RowDefinitions("*");
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var button in new[] { ModeButton, FontButton, LongLabelButton }) actions.Children.Add(button);
        var scrollMode = new MaterialButton { Content = "Fixed / Scrollable 固定/滚动", Variant = MaterialButtonVariant.Text };
        var disable = new MaterialButton { Content = "Enable / Disable Library", Variant = MaterialButtonVariant.Text };
        actions.Children.Add(scrollMode);
        actions.Children.Add(disable);
        foreach (var button in actions.Children.OfType<MaterialButton>())
        {
            button.MaxWidth = 260;
            button.ContentTemplate = new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
        }
        AutomationProperties.SetAutomationId(Status, "navigation-status");
        // Settings/status scroll with the page too, so 200% typography cannot starve the content viewport.
        var examples = new StackPanel { Spacing = 12, Margin = new Thickness(12) };
        examples.Children.Add(actions);
        examples.Children.Add(Status);
        examples.Children.Add(Heading("Content navigation / 实际内容导航"));
        Populate(MainTabs, "main", 5);
        MainTabs.MinHeight = 220;
        examples.Children.Add(MainTabs);
        Populate(ResponsiveRail, "responsive", 3);
        ResponsiveRail.Height = 250;
        examples.Children.Add(Heading("Responsive non-modal rail: resize wide ↔ narrow (selection/focus retained)"));
        examples.Children.Add(ResponsiveRail);
        examples.Children.Add(Heading("Pinned bar / rail / tab variants — every selection displays its own page"));
        AddPreview(examples, "Bar — vertical items", new MaterialNavigationBar());
        AddPreview(examples, "Bar — horizontal items", new MaterialNavigationBar { ItemLayout = MaterialNavigationItemLayout.Horizontal });
        AddPreview(examples, "Rail — collapsed / vertical", new MaterialNavigationRail());
        AddPreview(examples, "Rail — collapsed / horizontal", new MaterialNavigationRail { ItemLayout = MaterialNavigationItemLayout.Horizontal });
        AddPreview(examples, "Rail — expanded / vertical", new MaterialNavigationRail { IsExpanded = true });
        AddPreview(examples, "Rail — expanded / horizontal", new MaterialNavigationRail { IsExpanded = true, ItemLayout = MaterialNavigationItemLayout.Horizontal });
        AddPreview(examples, "Primary tabs — fixed", new MaterialTabs());
        AddPreview(examples, "Primary tabs — scrollable", new MaterialTabs { Layout = MaterialTabLayout.Scrollable });
        AddPreview(examples, "Secondary tabs — fixed", new MaterialTabs { Variant = MaterialTabVariant.Secondary });
        AddPreview(examples, "Secondary tabs — scrollable", new MaterialTabs { Variant = MaterialTabVariant.Secondary, Layout = MaterialTabLayout.Scrollable });
        examples.Children.Add(new TextBlock { Text = "Use Tab / Shift+Tab; Left/Right or Up/Down on the rail; Home/End; Enter/Space. Touch drag and wheel scroll the overflowing headers. Badge text is localized by the host. No product routes or services are present.", TextWrapping = TextWrapping.Wrap });
        Scroller = new ScrollViewer { Content = examples };
        Children.Add(Scroller);
        AutomationProperties.SetAutomationId(ModeButton, "navigation-mode");
        AutomationProperties.SetAutomationId(FontButton, "navigation-font");
        AutomationProperties.SetAutomationId(LongLabelButton, "navigation-long");
        AutomationProperties.SetAutomationId(disable, "navigation-disable");
        AutomationProperties.SetAutomationId(scrollMode, "navigation-scroll-mode");
        ModeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window) window.RequestedThemeVariant = window.RequestedThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        };
        FontButton.Click += (_, _) => _theme.Typography = _theme.Typography with { Scale = _theme.Typography.Scale == 1 ? 2 : 1 };
        LongLabelButton.Click += (_, _) =>
        {
            _longLabels = !_longLabels;
            foreach (var navigation in _previews.Concat(new MaterialNavigation[] { MainTabs, ResponsiveRail }))
                ((MaterialNavigationItem)navigation.Items[1]!).Content = _longLabels ? "Library 资料收藏与参考 / A very long bilingual collection label" : "Library 资料";
        };
        scrollMode.Click += (_, _) => MainTabs.Layout = MainTabs.Layout == MaterialTabLayout.Fixed ? MaterialTabLayout.Scrollable : MaterialTabLayout.Fixed;
        disable.Click += (_, _) => ((MaterialNavigationItem)MainTabs.Items[1]!).IsEnabled = !((MaterialNavigationItem)MainTabs.Items[1]!).IsEnabled;
        SizeChanged += (_, _) =>
        {
            foreach (var button in actions.Children.OfType<MaterialButton>()) button.MaxWidth = Math.Max(48, Math.Min(300, Bounds.Width - 12));
            MainTabs.Layout = Bounds.Width < 600 ? MaterialTabLayout.Scrollable : MaterialTabLayout.Fixed;
            ResponsiveRail.IsExpanded = Bounds.Width >= 720;
            ResponsiveRail.ItemLayout = ResponsiveRail.IsExpanded ? MaterialNavigationItemLayout.Horizontal : MaterialNavigationItemLayout.Vertical;
        };
    }

    private void AddPreview(StackPanel examples, string title, MaterialNavigation navigation)
    {
        _previews.Add(navigation);
        examples.Children.Add(Heading(title));
        Populate(navigation, "preview-" + _previews.Count, navigation is MaterialTabs { Layout: MaterialTabLayout.Scrollable } ? 6 : 3);
        navigation.MinHeight = navigation is MaterialNavigationRail ? 250 : 170;
        if (navigation is MaterialNavigationRail) navigation.Height = 250;
        examples.Children.Add(navigation);
    }
    private void Populate(MaterialNavigation navigation, string id, int count)
    {
        AutomationProperties.SetAutomationId(navigation, "navigation-" + id);
        AutomationProperties.SetName(navigation, id + " destinations");
        var labels = new[] { "Home 首页", "Library 资料", "Activity 动态", "Topics 主题", "Settings 设置", "More 更多" };
        for (var i = 0; i < count; i++)
        {
            var label = labels[i];
            var page = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
            var body = new TextBlock { Text = label + " content — This is a real host-owned page, not a label-only navigation result. 通用页面内容。", TextWrapping = TextWrapping.Wrap };
            AutomationProperties.SetAutomationId(body, id + "-content-" + i);
            page.Children.Add(body);
            var item = new MaterialNavigationItem { Content = label, Icon = Symbols.Create(i == 0 ? "home" : i == 1 ? "view_list" : "star"), SelectedIcon = Symbols.Create(i == 0 ? "home" : i == 1 ? "view_list" : "star", filled: true), PageContent = page };
            AutomationProperties.SetAutomationId(item, "navigation-" + id + "-" + i);
            if (i == 1)
            {
                var badge = new MaterialBadge { Count = 3 };
                item.Badge = badge;
                item.BadgeDescription = "3 unread";
                var read = new MaterialButton { Content = "Mark read 标记已读", Variant = MaterialButtonVariant.Text };
                read.Click += (_, _) => { badge.Count = 0; item.BadgeDescription = null; Status.Text = "Library badge cleared; content action completed"; };
                page.Children.Add(read);
                if (id == "main") { MarkRead = read; AutomationProperties.SetAutomationId(read, "navigation-mark-read"); }
            }
            else if (i == 2) { item.Badge = new MaterialBadge(); item.BadgeDescription = "New activity"; }
            navigation.Items.Add(item);
        }
        navigation.SelectionChanged += (_, _) => Status.Text = $"{id}: current content = {navigation.SelectedItem?.Content}; selected index = {navigation.SelectedIndex}";
        navigation.ItemInvoked += (_, e) => Status.Text = $"{id}: {e.Item.Content}; content synchronized; invocation #{++_invocations}";
    }
    private static TextBlock Heading(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.Bold };
}
