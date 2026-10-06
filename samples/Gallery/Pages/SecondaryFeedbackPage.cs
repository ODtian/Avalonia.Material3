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

/// <summary>Package-only M3-13 host. Owns results and actions, not application business services.</summary>
public sealed class SecondaryFeedbackPage : UserControl
{
    private readonly List<IDisposable> _attachments = [];
    private readonly MaterialButton _context;
    private readonly MaterialButton _richEntry;
    private readonly MaterialButton _plainEntry;
    private readonly MaterialMenu _contextMenu;
    public MaterialOverlayHost Overlay { get; }
    public TextBlock Result { get; } = new() { Text = "Waiting", TextWrapping = TextWrapping.Wrap };
    public MaterialSnackbar Snackbar { get; }
    public MaterialTooltip PlainTooltip { get; }
    public MaterialTooltip RichTooltip { get; }
    public SecondaryFeedbackPage(MaterialTheme theme)
    {
        AutomationProperties.SetAutomationId(Result, "FeedbackResult");
        var standard = Entry("Standard menu", "MenuStandard");
        var vibrant = Entry("Vibrant menu", "MenuVibrant");
        var segmented = Entry("Segmented menu", "MenuSegmented");
        _context = Entry("Context menu (right click / Menu key)", "MenuContext");
        _plainEntry = Entry("Plain tooltip", "TooltipPlain");
        _richEntry = Entry("Rich tooltip", "TooltipRich");
        var snackbarEntry = Entry("Show Snackbar", "SnackbarEntry");
        var timed = Entry("Timed Snackbar", "SnackbarTimed");
        var themeEntry = Entry("Light / dark", "FeedbackTheme");
        var font = Entry("100 / 200% fonts", "FeedbackFont");
        var rtl = Entry("LTR / RTL", "FeedbackRtl");
        var back = Entry("Host back", "FeedbackBack");
        var panel = new StackPanel
        {
            Margin = new Thickness(16), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Menus, Tooltips & Snackbar / 次级操作", TextWrapping = TextWrapping.Wrap, FontSize = 22 },
                Result, standard, vibrant, segmented, _context, _plainEntry, _richEntry,
                snackbarEntry, timed, themeEntry, font, rtl, back
            }
        };
        Overlay = new MaterialOverlayHost { Content = new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled } };
        Content = Overlay;
        Snackbar = new MaterialSnackbar
        {
            Content = "Changes saved / 更改已保存", ActionContent = "Undo", ActionResult = "undo",
            ActionCommand = new HostCommand(() => Result.Text = "Snackbar: undo performed")
        };
        PlainTooltip = new MaterialTooltip { Content = "Opens related commands / 打开相关操作", EnableUserInput = false, ShowCaret = true };
        RichTooltip = new MaterialTooltip
        {
            Variant = MaterialTooltipVariant.Rich, Title = "Safe backups / 安全备份",
            Content = "Keep a copy before changing the document. Keyboard Tab reaches the action.",
            ActionContent = "Learn more", ActionResult = "learn", ShowCaret = true,
            ActionCommand = new HostCommand(() => Result.Text = "Tooltip: learn more performed")
        };
        standard.Click += (_, _) => PresentMenu(BuildMenu(), standard);
        vibrant.Click += (_, _) => PresentMenu(BuildMenu(MaterialMenuVariant.Vibrant), vibrant);
        segmented.Click += (_, _) => PresentMenu(BuildSegmentedMenu(), segmented);
        _contextMenu = BuildMenu();
        _contextMenu.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialMenu.IsOpenProperty && _contextMenu.IsOpen)
                ObserveMenu(_contextMenu.Session!);
        };
        _plainEntry.Click += (_, _) => { if (!PlainTooltip.IsOpen) PlainTooltip.Show(Overlay, _plainEntry); };
        _richEntry.Click += (_, _) => { if (!RichTooltip.IsOpen) RichTooltip.Show(Overlay, _richEntry); };
        snackbarEntry.Click += (_, _) =>
        {
            if (Snackbar.IsOpen) return;
            Snackbar.Content = "Changes saved / 更改已保存"; Snackbar.ActionContent = "Undo"; Snackbar.Duration = null;
            Snackbar.Show(Overlay).Closed += (_, result) => { if (result.Reason != MaterialOverlayCloseReason.Confirmed) Result.Text = $"Snackbar: {result.Reason}"; };
        };
        timed.Click += (_, _) =>
        {
            if (Snackbar.IsOpen) return;
            Snackbar.Content = "Temporary feedback (4 seconds) / 临时反馈"; Snackbar.ActionContent = null; Snackbar.Duration = TimeSpan.FromSeconds(4);
            Snackbar.Show(Overlay).Closed += (_, result) => Result.Text = $"Snackbar: {result.Reason}";
        };
        themeEntry.Click += (_, _) =>
        {
            var root = TopLevel.GetTopLevel(this);
            if (root is not null) root.RequestedThemeVariant = root.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        };
        font.Click += (_, _) => theme.Typography = theme.Typography with { Scale = theme.Typography.Scale < 2 ? 2 : 1 };
        rtl.Click += (_, _) => Overlay.FlowDirection = Overlay.FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        back.Click += (_, _) => Overlay.RequestBack();
    }
    private static MaterialButton Entry(string text, string id)
    {
        var button = new MaterialButton { Content = text, HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = 500 };
        AutomationProperties.SetAutomationId(button, id); return button;
    }
    private static MaterialMenu BuildMenu(MaterialMenuVariant variant = MaterialMenuVariant.Standard)
    {
        var copy = new MaterialMenuItem { Content = "Copy", Value = "copied", LeadingIcon = "▣" };
        AutomationProperties.SetAutomationId(copy, "MenuCopy");
        var pdf = new MaterialMenuItem { Content = "PDF / 文档", Value = "pdf" };
        AutomationProperties.SetAutomationId(pdf, "MenuPdf");
        var export = new MaterialMenuItem { Content = "Export", Submenu = new MaterialMenu { Items = { pdf, new MaterialMenuItem { Content = "Text", Value = "text" } } } };
        AutomationProperties.SetAutomationId(export, "MenuExport");
        var check = new MaterialMenuItem { Content = "Automatic backup", SupportingText = "Host-owned check state", ToggleMode = MaterialMenuToggleMode.Check, StaysOpenOnClick = true };
        AutomationProperties.SetAutomationId(check, "MenuCheck");
        return new MaterialMenu
        {
            Variant = variant, Items =
            {
                copy, new MaterialMenuItem { Content = "Unavailable action", IsEnabled = false },
                export, new MaterialDivider(), check
            }
        };
    }
    private MaterialMenu BuildSegmentedMenu()
    {
        var small = new MaterialMenuItem { Content = "Compact", GroupName = "density", ToggleMode = MaterialMenuToggleMode.Radio, IsChecked = true, StaysOpenOnClick = true };
        var large = new MaterialMenuItem { Content = "Comfortable", GroupName = "density", ToggleMode = MaterialMenuToggleMode.Radio, StaysOpenOnClick = true };
        large.Click += (_, _) => Result.Text = "Menu: comfortable selected";
        var icon = new MaterialMenuItem { Content = "↓", IsIconOnly = true, Value = "download" };
        AutomationProperties.SetName(icon, "Download");
        return new MaterialMenu
        {
            IsSegmented = true, Items =
            {
                new MaterialMenuGroup { Orientation = Orientation.Horizontal, Items = { small, large } },
                new MaterialMenuGroup { Items = { new MaterialMenuItem { Content = "Read offline / 离线阅读", SupportingText = "A long supporting description that wraps rather than truncates.", TrailingText = "New", Value = "offline" } } },
                new MaterialMenuGroup { Orientation = Orientation.Horizontal, Items = { icon, new MaterialMenuItem { Content = "Settings", Value = "settings" } } }
            }
        };
    }
    private void PresentMenu(MaterialMenu menu, Control anchor) => ObserveMenu(menu.Show(Overlay, anchor));
    private void ObserveMenu(MaterialOverlaySession session) => session.Closed += (_, result) =>
        Result.Text = result.Reason == MaterialOverlayCloseReason.Confirmed ? $"Menu: {result.Value}" : $"Menu: {result.Reason}";
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attachments.Add(_contextMenu.AttachContext(Overlay, _context));
        _attachments.Add(PlainTooltip.Attach(Overlay, _plainEntry));
        _attachments.Add(RichTooltip.Attach(Overlay, _richEntry));
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        foreach (var attachment in _attachments) attachment.Dispose();
        _attachments.Clear(); base.OnDetachedFromVisualTree(e);
    }
    private sealed class HostCommand(Action action) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
    }
}
