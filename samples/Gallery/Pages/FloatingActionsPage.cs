using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Standalone package-only FAB/toolbar gallery. Aggregate navigation is owned by the gallery integration ticket.</summary>
public sealed class FloatingActionsPage : UserControl
{
    public IReadOnlyList<MaterialFab> FabSamples { get; }
    public IReadOnlyList<MaterialExtendedFab> ExtendedSamples { get; }
    public IReadOnlyList<MaterialToolbar> Toolbars { get; }
    public MaterialFabMenu PreviewMenu { get; }
    public TextBlock Result { get; } = new() { Text = "Choose an action", TextWrapping = TextWrapping.Wrap };

    public FloatingActionsPage(MaterialTheme theme)
    {
        var panel = new StackPanel { Spacing = 16, Margin = new Thickness(16) };
        Content = new ScrollViewer { Content = panel };
        panel.Children.Add(Heading("Floating actions & expanding toolbars"));
        panel.Children.Add(new TextBlock { Text = "Primary actions, anchored choices and editing tools. Tab/Space/Enter; arrows/Home/End; Escape dismisses. Resize this window, change theme/text or reduce motion without recreating the controls.", TextWrapping = TextWrapping.Wrap });
        var preferences = new WrapPanel { ItemSpacing = 8, LineSpacing = 8 };
        preferences.Children.Add(Action("Light / dark", "floating-mode", () =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        }));
        preferences.Children.Add(Action("Text 100 / 200%", "floating-font", () => theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 2 : 1 }));
        preferences.Children.Add(Action("Reduce motion", "floating-motion", () => theme.Motion = theme.Motion with { ReduceMotion = !theme.Motion.ReduceMotion }));
        preferences.Children.Add(Action("Enable / disable", "floating-enabled", ToggleEnabled));
        panel.Children.Add(preferences);
        AutomationProperties.SetAutomationId(Result, "floating-result");
        AutomationProperties.SetName(Result, "Action result");
        panel.Children.Add(Result);
        panel.Children.Add(Heading("Standard / small / medium / large FAB"));
        var fabs = new List<MaterialFab>();
        var fabRow = new WrapPanel { ItemSpacing = 12, LineSpacing = 12 };
        foreach (var size in Enum.GetValues<MaterialFabSize>())
        {
            var fab = new MaterialFab { Size = size, Content = Symbols.Create("add"), Command = new ResultCommand(value => Result.Text = $"{value} action completed"), CommandParameter = size };
            AutomationProperties.SetName(fab, $"{size} create action");
            fabs.Add(fab);
            fabRow.Children.Add(new StackPanel { Children = { new TextBlock { Text = size.ToString() }, fab } });
        }
        FabSamples = fabs;
        panel.Children.Add(fabRow);
        panel.Children.Add(Heading("Extended FAB — every size, text-only and label collapse"));
        var extended = new List<MaterialExtendedFab>();
        foreach (var size in Enum.GetValues<MaterialFabSize>())
        {
            var fab = new MaterialExtendedFab { Size = size, Icon = Symbols.Create("add"), Content = $"{size} — Create 新文档", Command = new ResultCommand(value => Result.Text = $"{value} extended action completed"), CommandParameter = size };
            extended.Add(fab);
            panel.Children.Add(fab);
        }
        var textOnly = new MaterialExtendedFab { Content = "Text-only extended action", Command = new ResultCommand(_ => Result.Text = "Text-only action completed") };
        extended.Add(textOnly);
        ExtendedSamples = extended;
        panel.Children.Add(textOnly);
        panel.Children.Add(Action("Expand / collapse labels", "floating-labels", () => { foreach (var fab in extended) fab.IsExpanded = !fab.IsExpanded; }));
        var longLabel = new MaterialExtendedFab { Icon = Symbols.Create("add"), Content = "Create a new shared document 新建共享文档 with a longer mixed-language label", HorizontalAlignment = HorizontalAlignment.Stretch };
        longLabel.Click += (_, _) => Result.Text = "Long label action completed";
        panel.Children.Add(longLabel);
        panel.Children.Add(Heading("Multi-action FAB menu — anchored, non-modal, scrollable"));
        PreviewMenu = new MaterialFabMenu { Margin = new Thickness(16) };
        AutomationProperties.SetName(PreviewMenu, "Creation choices");
        PreviewMenu.Items.Add(new MaterialFabMenuItem { Content = "Document 新文档", LeadingIcon = Symbols.Create("add"), Command = new ResultCommand(_ => Result.Text = "Document created") });
        PreviewMenu.Items.Add(new MaterialFabMenuItem { Content = "Folder 文件夹", LeadingIcon = Symbols.Create("folder"), Command = new ResultCommand(_ => Result.Text = "Folder created") });
        PreviewMenu.Items.Add(new MaterialFabMenuItem { Content = "Unavailable action", LeadingIcon = Symbols.Create("close"), IsEnabled = false });
        panel.Children.Add(new Border { Height = 340, Background = new SolidColorBrush(Colors.Transparent), Child = new Grid { Children = { PreviewMenu } } });
        var anchors = new WrapPanel { ItemSpacing = 8, LineSpacing = 8 };
        foreach (var anchor in Enum.GetValues<MaterialActionAnchor>())
            anchors.Children.Add(Action(anchor.ToString(), "floating-anchor-" + anchor, () => PreviewMenu.Anchor = anchor));
        anchors.Children.Add(Action("LTR / RTL", "floating-rtl", () => PreviewMenu.FlowDirection = PreviewMenu.FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight));
        panel.Children.Add(anchors);
        var triggerSizes = new WrapPanel { ItemSpacing = 8, LineSpacing = 8 };
        foreach (var size in Enum.GetValues<MaterialFabSize>())
            triggerSizes.Children.Add(Action("Menu " + size, "floating-menu-size-" + size, () => PreviewMenu.TriggerSize = size));
        panel.Children.Add(triggerSizes);
        panel.Children.Add(Heading("Docked / floating × horizontal / vertical × standard / vibrant"));
        panel.Children.Add(new TextBlock { Text = "Docked and standard floating previews collapse leading/trailing slots. Vibrant floating previews collapse the whole surface to a 56→80 DIP disclosure FAB. Activate the collapsed FAB to reopen; activation while expanded executes its primary action.", TextWrapping = TextWrapping.Wrap });
        var toolbars = new List<MaterialToolbar>();
        foreach (var variant in Enum.GetValues<MaterialToolbarVariant>())
        foreach (var orientation in Enum.GetValues<Orientation>())
        foreach (var color in Enum.GetValues<MaterialToolbarColor>())
        {
            var toolbar = new MaterialToolbar { Variant = variant, Orientation = orientation, Color = color, Anchor = MaterialActionAnchor.BottomEnd };
            AutomationProperties.SetName(toolbar, $"{variant} {orientation} {color} editing tools");
            toolbar.Items.Add(Tool("star", "Select", Result));
            toolbar.Items.Add(Tool("edit", "Edit", Result));
            toolbar.LeadingItems.Add(Tool("undo", "Undo", Result));
            toolbar.TrailingItems.Add(Tool("content_copy", "Copy", Result));
            if (variant == MaterialToolbarVariant.Floating)
            {
                toolbar.FloatingAction = new MaterialFab { Content = Symbols.Create("add"), Command = new ResultCommand(_ => Result.Text = "Toolbar primary action completed") };
                toolbar.FloatingActionPosition = color == MaterialToolbarColor.Standard ? MaterialToolbarFabPosition.Start : MaterialToolbarFabPosition.End;
                toolbar.CollapseBehavior = color == MaterialToolbarColor.Standard ? MaterialToolbarCollapseBehavior.ExpansionSlots : MaterialToolbarCollapseBehavior.WholeToolbar;
                AutomationProperties.SetName(toolbar.FloatingAction, "Toolbar create action");
            }
            toolbars.Add(toolbar);
            panel.Children.Add(new TextBlock { Text = $"{variant} / {orientation} / {color}", TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new Grid { Height = orientation == Orientation.Vertical ? 390 : 140, Children = { toolbar } });
        }
        Toolbars = toolbars;
        panel.Children.Add(Action("Expand / collapse toolbars", "floating-toolbars", () => { foreach (var toolbar in toolbars) toolbar.IsExpanded = !toolbar.IsExpanded; }));
    }
    private void ToggleEnabled()
    {
        foreach (var fab in FabSamples) fab.IsEnabled = !fab.IsEnabled;
        foreach (var fab in ExtendedSamples) fab.IsEnabled = !fab.IsEnabled;
        foreach (var toolbar in Toolbars) toolbar.IsEnabled = !toolbar.IsEnabled;
        PreviewMenu.IsEnabled = !PreviewMenu.IsEnabled;
    }
    private static TextBlock Heading(string text) => new() { Text = text, FontSize = 22, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap };
    private static MaterialButton Action(string text, string id, Action action)
    {
        var button = new MaterialButton { Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, Variant = MaterialButtonVariant.Tonal };
        AutomationProperties.SetName(button, text);
        AutomationProperties.SetAutomationId(button, id);
        button.Click += (_, _) => action();
        return button;
    }
    private static MaterialIconButton Tool(string icon, string label, TextBlock result)
    {
        var button = new MaterialIconButton { Content = Symbols.Create(icon), IsToggle = true, Command = new ResultCommand(_ => result.Text = $"Tool selected: {label}") };
        AutomationProperties.SetName(button, label);
        return button;
    }
    private sealed class ResultCommand(Action<object?> action) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
