using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>A bounded package-only host: genuine coplanar layout and modal overlay sessions are separate.</summary>
public sealed class SheetsPage : UserControl
{
    private readonly MaterialTheme _theme;
    private Size? _declaredAvailable;
    public MaterialOverlayHost Overlay { get; }
    public MaterialSheetHost Layout { get; } = new();
    public MaterialBottomSheet StandardBottom { get; }
    public MaterialSideSheet StandardSide { get; }
    public MaterialSheet? LastModal { get; private set; }
    public TextBlock Result { get; } = new() { Text = "No modal result", TextWrapping = TextWrapping.Wrap };

    public SheetsPage(MaterialTheme theme, MaterialOverlayHost? windowOverlayHost = null)
    {
        _theme = theme;
        Overlay = windowOverlayHost ?? new MaterialOverlayHost();
        AutomationProperties.SetName(Result, "Sheet result");
        AutomationProperties.SetLiveSetting(Result, AutomationLiveSetting.Polite);
        StandardBottom = new MaterialBottomSheet { Title = "Standard bottom information", ExpandedExtent = 560, Content = InformationBody("Standard bottom") };
        StandardSide = new MaterialSideSheet { Title = "Standard side information", Content = InformationBody("Standard side") };
        StandardBottom.Actions = StandardActions(StandardBottom);
        StandardSide.Actions = StandardActions(StandardSide);
        var main = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        main.Children.Add(new TextBlock { Text = "M3-14 / Information sheets", Classes = { "m3-title-large" }, TextWrapping = TextWrapping.Wrap });
        main.Children.Add(new TextBlock { Text = "Standard bottom reserves its 56 DIP peek; standard side resizes this content. Modal sheets reuse the window-local focus/back stack. Drag the marker, or use its keyboard and automation actions.", TextWrapping = TextWrapping.Wrap });
        var forms = new WrapPanel();
        forms.Children.Add(Action("Open modal bottom", () => ShowModal(false)));
        forms.Children.Add(Action("Open modal side", () => ShowModal(true)));
        forms.Children.Add(Action("Standard bottom", () => { Layout.Sheet = StandardBottom; StandardBottom.Collapse(); }));
        forms.Children.Add(Action("Standard side", () => { Layout.Sheet = StandardSide; StandardSide.Expand(); }));
        forms.Children.Add(Action("Hide standard side", () => StandardSide.Dismiss()));
        main.Children.Add(forms);
        var inputs = new WrapPanel();
        inputs.Children.Add(Action("Dark theme", () => { if (TopLevel.GetTopLevel(this) is Window window) window.RequestedThemeVariant = window.RequestedThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark; }));
        inputs.Children.Add(Action("200% font", () => _theme.Typography = _theme.Typography with { Scale = _theme.Typography.Scale == 2 ? 1 : 2 }));
        inputs.Children.Add(Action("Reduced motion", () => _theme.Motion = _theme.Motion with { ReduceMotion = !_theme.Motion.ReduceMotion }));
        inputs.Children.Add(Action("RTL layout", () => Overlay.FlowDirection = Overlay.FlowDirection == FlowDirection.RightToLeft ? FlowDirection.LeftToRight : FlowDirection.RightToLeft));
        inputs.Children.Add(Action("Available 600 × 500", () => { _declaredAvailable = _declaredAvailable is null ? new Size(600, 500) : null; Layout.AvailableSize = _declaredAvailable; }));
        inputs.Children.Add(Action("Reserve visible bottom", () => Layout.ReserveVisibleExtent = !Layout.ReserveVisibleExtent));
        inputs.Children.Add(Action("Detached standard side", () => { StandardSide.IsDetached = !StandardSide.IsDetached; Layout.Sheet = StandardSide; StandardSide.Expand(); }));
        main.Children.Add(inputs);
        main.Children.Add(new MaterialTextField { Label = "Underlying host editor", Text = "Host-owned value; modal input must not reach this editor." });
        main.Children.Add(Result);
        main.Children.Add(new TextBlock { Text = string.Join("\n", Enumerable.Repeat("Generic host content / 可复用信息内容", 30)), TextWrapping = TextWrapping.Wrap });
        var mainScroll = new ScrollViewer { Content = main, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        AutomationProperties.SetName(mainScroll, "Sheet main content");
        Layout.Content = mainScroll;
        Layout.Sheet = StandardBottom;
        if (windowOverlayHost is null) { Overlay.Content = Layout; Content = Overlay; }
        else Content = Layout;
    }

    private StackPanel InformationBody(string prefix)
    {
        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(new MaterialTextField { Label = "Sheet editor", Text = "Editable draft" });
        body.Children.Add(Action("Child action", () => Result.Text = "Child action executed"));
        body.Children.Add(new TextBlock { Text = string.Join("\n", Enumerable.Range(1, 50).Select(index => $"{prefix} / 信息 {index}: independently scrollable content.")), TextWrapping = TextWrapping.Wrap });
        return body;
    }
    private static WrapPanel StandardActions(MaterialSheet sheet)
    {
        var actions = new WrapPanel();
        actions.Children.Add(Action("Expand standard", () => sheet.Expand()));
        if (sheet is MaterialBottomSheet) actions.Children.Add(Action("Collapse standard", () => sheet.Collapse()));
        else actions.Children.Add(Action("Dismiss standard", () => sheet.Dismiss()));
        return actions;
    }
    private void ShowModal(bool side)
    {
        if (LastModal is not null) return;
        MaterialSheet sheet = side ? new MaterialSideSheet() : new MaterialBottomSheet { ExpandedExtent = 600 };
        sheet.Title = side ? "Modal side information" : "Modal bottom information";
        sheet.Content = InformationBody(side ? "Modal side" : "Modal bottom");
        var actions = new WrapPanel();
        actions.Children.Add(Action("Expand sheet", () => sheet.Expand()));
        if (!side) actions.Children.Add(Action("Collapse sheet", () => sheet.Collapse()));
        actions.Children.Add(Action("Host back", () => Overlay.RequestBack()));
        actions.Children.Add(Action("Dismiss sheet", () => sheet.Dismiss()));
        actions.Children.Add(Action("Nested dialog", () => new MaterialDialog { Title = "Nested sheet dialog", Content = "The nested dialog receives the first Back/Escape." }.Show(Overlay)));
        sheet.Actions = actions;
        var margin = _declaredAvailable is { } available ? new Thickness(0, 0, Math.Max(0, Overlay.Bounds.Width - available.Width), Math.Max(0, Overlay.Bounds.Height - available.Height)) : default;
        var session = sheet.Show(Overlay, new MaterialOverlayOptions { Placement = side ? MaterialOverlayPlacement.End : MaterialOverlayPlacement.Bottom, Margin = margin, CloseOnLightDismiss = true });
        LastModal = sheet;
        session.Closed += (_, result) =>
        {
            Result.Text = $"Sheet closed: {result.Reason}; no application value was implicitly committed.";
            AutomationProperties.SetItemStatus(Result, result.Reason.ToString());
            if (ReferenceEquals(LastModal, sheet)) LastModal = null;
        };
    }
    private static MaterialButton Action(string text, Action action)
    {
        var button = new MaterialButton { Content = text, Margin = new Thickness(0, 0, 8, 4) };
        button.Click += (_, _) => action();
        return button;
    }
}
