using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Data;

namespace Avalonia.Material3.Controls;

public partial class MaterialButtonGroup
{
    public MaterialIconButton OverflowButton { get; } = new() { Content = new MaterialSymbol { Symbol = "more_vert" }, IconVariant = MaterialIconButtonVariant.Filled, IsVisible = false };
    public IReadOnlyList<MaterialGroupButton> OverflowItems => Buttons.Where(button => _overflowed.Contains(button)).ToArray();
    public IReadOnlyList<MaterialButton> OverflowEntries => _overflowEntries.ToArray();
    public bool IsOverflowOpen => _popup.IsOpen;
    private readonly Popup _popup = new() { IsLightDismissEnabled = true, Placement = PlacementMode.BottomEdgeAlignedLeft };
    private readonly HashSet<Control> _overflowed = [];
    private readonly Dictionary<Control, Size> _naturalSizes = [];
    private readonly List<MaterialButton> _overflowEntries = [];
    private double _lastAvailableWidth = double.NaN;
    private bool _updatingOverflow;
    private double _overflowLayoutWidth;
    private Size NaturalSize(Control child) => _naturalSizes.GetValueOrDefault(child, child.DesiredSize);

    private void InitializeOverflow()
    {
        AutomationProperties.SetName(OverflowButton, "More options");
        VisualChildren.Add(OverflowButton);
        LogicalChildren.Add(OverflowButton);
        LogicalChildren.Add(_popup);
        _popup.PlacementTarget = OverflowButton;
        OverflowButton.Click += (_, _) => { if (IsOverflowOpen) CloseOverflow(); else OpenOverflow(); };
        _popup.Closed += (_, _) =>
        {
            _popup.Child = null; _overflowEntries.Clear();
            if (OverflowButton.IsVisible) OverflowButton.Focus(NavigationMethod.Directional);
        };
    }
    private void RestoreOverflow()
    {
        if (_updatingOverflow) return;
        _updatingOverflow = true;
        foreach (var child in _overflowed.Where(Children.Contains))
            if (!VisualChildren.Contains(child)) VisualChildren.Add(child);
        _overflowed.Clear();
        _updatingOverflow = false;
    }
    private void ApplyOverflow(double width)
    {
        if (Variant != MaterialButtonGroupVariant.Unconnected || this is MaterialSegmentedButtonGroup || Orientation != Avalonia.Layout.Orientation.Horizontal || _rows.Count <= 1)
        {
            OverflowButton.SetCurrentValue(IsVisibleProperty, false);
            if (_overflowed.Count > 0) RestoreOverflow();
            CloseOverflow(false);
            return;
        }
        var all = _rows.SelectMany(row => row).ToArray();
        OverflowButton.SetCurrentValue(IsVisibleProperty, true);
        OverflowButton.Measure(new Size(width, double.PositiveInfinity));
        List<Control> visible = [];
        var remaining = width - OverflowButton.DesiredSize.Width;
        foreach (var child in all)
        {
            var size = NaturalSize(child);
            // Native accepts the item before charging its following gap.
            if (size.Width > remaining) break;
            visible.Add(child); remaining -= size.Width + Spacing;
        }
        _overflowLayoutWidth = Math.Max(0, remaining + OverflowButton.DesiredSize.Width);
        _updatingOverflow = true;
        foreach (var child in all)
        {
            if (visible.Contains(child))
            {
                // Membership can change while the available width stays fixed.
                _overflowed.Remove(child);
                if (!VisualChildren.Contains(child)) VisualChildren.Add(child);
            }
            else
            {
                _overflowed.Add(child);
                VisualChildren.Remove(child);
            }
        }
        _updatingOverflow = false;
        visible.Add(OverflowButton);
        _rows.Clear(); _rows.Add(visible);
    }
    public void OpenOverflow()
    {
        if (!IsEffectivelyEnabled || OverflowItems.Count == 0 || IsOverflowOpen) return;
        var panel = new StackPanel { Spacing = 2, MaxWidth = Math.Max(48, Bounds.Width), Margin = new Thickness(8) };
        _overflowEntries.Clear();
        foreach (var original in OverflowItems)
        {
            var name = AutomationProperties.GetName(original);
            var entry = new MaterialButton
            {
                Content = string.IsNullOrWhiteSpace(name) ? original.Content is string text ? text : original.Content?.ToString() : name,
                IsEnabled = original.IsEffectivelyEnabled, IsToggle = original.IsToggle, IsChecked = original.IsChecked,
                Variant = MaterialButtonVariant.Text, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<object>((value, _) => new TextBlock { Text = value?.ToString(), TextWrapping = TextWrapping.Wrap })
            };
            entry.Bind(IsEnabledProperty, original.GetObservable(IsEffectivelyEnabledProperty));
            entry.Bind(MaterialButton.IsToggleProperty, original.GetObservable(MaterialButton.IsToggleProperty));
            entry.Bind(MaterialButton.IsCheckedProperty, original.GetObservable(MaterialButton.IsCheckedProperty));
            if (original.Content is string)
                entry.Bind(MaterialButton.ContentProperty, original.GetObservable(MaterialButton.ContentProperty));
            entry.LeadingIcon = original.IsToggle && original.IsChecked ? new MaterialSymbol { Symbol = "check", Size = 20 } : null;
            entry.Click += (_, _) => { original.ActivateFromOverflow(); CloseOverflow(); };
            _overflowEntries.Add(entry); panel.Children.Add(entry);
        }
        panel.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { CloseOverflow(); e.Handled = true; }
            else if (e.Key is Key.Down or Key.Up or Key.Home or Key.End)
            {
                var enabled = _overflowEntries.Where(entry => entry.IsEffectivelyEnabled).ToArray();
                if (enabled.Length == 0) return;
                var index = Math.Max(0, Array.FindIndex(enabled, entry => entry.IsFocused));
                var next = e.Key switch { Key.Home => 0, Key.End => enabled.Length - 1, Key.Down => (index + 1) % enabled.Length, _ => (index + enabled.Length - 1) % enabled.Length };
                enabled[next].Focus(NavigationMethod.Directional); e.Handled = true;
            }
        };
        var border = new Border
        {
            Child = new ScrollViewer { Content = panel, MaxHeight = Math.Max(48, (TopLevel.GetTopLevel(this)?.ClientSize.Height ?? 480) * .7) },
            CornerRadius = new CornerRadius(12)
        };
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable("M3.SurfaceContainerBrush"));
        _popup.Child = border;
        _popup.IsOpen = true;
        Dispatcher.UIThread.Post(() => { if (IsOverflowOpen) _overflowEntries.FirstOrDefault(entry => entry.IsEffectivelyEnabled)?.Focus(NavigationMethod.Directional); });
    }
    public void CloseOverflow() => CloseOverflow(true);
    private void CloseOverflow(bool restoreFocus)
    {
        if (!_popup.IsOpen) return;
        _popup.IsOpen = false;
        if (restoreFocus && OverflowButton.IsVisible) OverflowButton.Focus(NavigationMethod.Directional);
    }
}
