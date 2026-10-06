using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Automation.Peers;
using Avalonia.Material3.Tokens;
using Avalonia.Threading;
using System.Diagnostics;

namespace Avalonia.Material3.Controls;

public enum MaterialGroupSelectionMode { None, Single, Multiple }
public enum MaterialButtonGroupVariant { Unconnected, Connected }

/// <summary>A group of MaterialGroupButton children with action, single or multiple selection contracts.</summary>
public partial class MaterialButtonGroup : Panel
{
    public static readonly StyledProperty<MaterialButtonGroupVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, MaterialButtonGroupVariant>(nameof(Variant), validate: Enum.IsDefined);
    public static readonly StyledProperty<Avalonia.Layout.Orientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, Avalonia.Layout.Orientation>(nameof(Orientation), validate: Enum.IsDefined);
    public MaterialButtonGroupVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public Avalonia.Layout.Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double Spacing => this is MaterialSegmentedButtonGroup ? -1 : Variant == MaterialButtonGroupVariant.Connected ? 2 : 12;

    public static readonly StyledProperty<MaterialGroupSelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, MaterialGroupSelectionMode>(nameof(SelectionMode), validate: Enum.IsDefined);
    public static readonly StyledProperty<bool> AllowEmptySelectionProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, bool>(nameof(AllowEmptySelection));
    public static readonly StyledProperty<MaterialGroupButton?> SelectedItemProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, MaterialGroupButton?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);
    public MaterialGroupSelectionMode SelectionMode { get => GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }
    public bool AllowEmptySelection { get => GetValue(AllowEmptySelectionProperty); set => SetValue(AllowEmptySelectionProperty, value); }
    public MaterialGroupButton? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public IReadOnlyList<MaterialGroupButton> SelectedItems => SelectionMode == MaterialGroupSelectionMode.None ? [] : Buttons.Where(button => button.IsChecked).ToArray();
    public event EventHandler? SelectionChanged;
    internal IEnumerable<MaterialGroupButton> Buttons => Children.OfType<MaterialGroupButton>();
    private readonly HashSet<MaterialGroupButton> _tracked = [];
    private bool _updating;
    private MaterialGroupButton[] _selection = [];

    public static readonly StyledProperty<double> ExpandedRatioProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, double>(nameof(ExpandedRatio), .15, validate: value => double.IsFinite(value) && value >= 0 && value <= 1);
    public double ExpandedRatio { get => GetValue(ExpandedRatioProperty); set => SetValue(ExpandedRatioProperty, value); }
    private MaterialGroupButton? _expanding;
    private double _expansion, _from, _target;
    private MaterialSpring _spring = new(.6, 1500);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _elapsed = new();
    private IDisposable? _springSubscription;
    public MaterialButtonGroup()
    {
        Children.CollectionChanged += TrackChildren;
        InitializeOverflow();
        _timer.Tick += (_, _) =>
        {
            var t = _elapsed.Elapsed.TotalSeconds;
            var omega = Math.Sqrt(_spring.Stiffness);
            var z = _spring.DampingRatio;
            double response;
            if (Math.Abs(z - 1) < 1e-7) response = 1 - (1 + omega * t) * Math.Exp(-omega * t);
            else if (z < 1)
            {
                var r = Math.Sqrt(1 - z * z);
                response = 1 - Math.Exp(-z * omega * t) * (Math.Cos(omega * r * t) + z / r * Math.Sin(omega * r * t));
            }
            else
            {
                var r = Math.Sqrt(z * z - 1); var a = -omega * (z - r); var b = -omega * (z + r);
                response = 1 + (b * Math.Exp(a * t) - a * Math.Exp(b * t)) / (a - b);
            }
            _expansion = Math.Clamp(_from + (_target - _from) * response, 0, 1.5);
            if (!double.IsFinite(response) || t >= 10 || (t > .25 && Math.Abs(_expansion - _target) < .0001))
            { _expansion = _target; _timer.Stop(); }
            InvalidateArrange();
        };
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _springSubscription = this.GetResourceObservable("M3.Motion.FastSpatial").Subscribe(new SpringObserver(value =>
        {
            if (value is MaterialSpring spring)
            {
                _spring = spring;
                RetargetExpansion(_target);
            }
        }));
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop(); _elapsed.Stop(); _springSubscription?.Dispose(); _springSubscription = null;
        CloseOverflow(false);
        base.OnDetachedFromVisualTree(e);
    }
    private void RetargetExpansion(double target)
    {
        _target = target; _timer.Stop();
        if (_spring.IsInstant) _expansion = target;
        else { _from = _expansion; _elapsed.Restart(); _timer.Start(); }
        InvalidateArrange();
    }
    private sealed class SpringObserver(Action<object?> changed) : IObserver<object?>
    {
        public void OnNext(object? value) => changed(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    private void TrackChildren(object? sender, NotifyCollectionChangedEventArgs args)
    {
        foreach (var removed in _tracked.Where(button => !Children.Contains(button)).ToArray())
        {
            removed.PropertyChanged -= ChildChanged;
            removed.Group = null;
            removed.UpdateGroupShape();
            _naturalSizes.Remove(removed);
            _overflowed.Remove(removed);
            _tracked.Remove(removed);
        }
        foreach (var added in Buttons.Where(button => !_tracked.Contains(button)))
        {
            added.Group = this;
            added.PropertyChanged += ChildChanged;
            _tracked.Add(added);
        }
        RestoreOverflow();
        Reconcile();
        InvalidateMeasure();
    }

    private void ChildChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (!_updatingOverflow && (args.Property == MaterialButton.ContentProperty || args.Property == MaterialButton.SizeProperty || args.Property == MaterialButton.FontSizeProperty || args.Property == MaterialButton.IsVisibleProperty))
        {
            RestoreOverflow(); InvalidateMeasure();
        }
        if (args.Property == MaterialButton.IsCheckedProperty)
            Reconcile(sender as MaterialGroupButton);
        if (args.Property == MaterialButton.IsPressedProperty && Variant == MaterialButtonGroupVariant.Unconnected && this is not MaterialSegmentedButtonGroup)
        {
            if (sender is MaterialGroupButton { IsPressed: true } button) _expanding = button;
            RetargetExpansion(Buttons.Any(button => button.IsPressed) ? 1 : 0);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty || change.Property == OrientationProperty || change.Property == FlowDirectionProperty)
        {
            RestoreOverflow();
            foreach (var button in Buttons) button.UpdateGroupShape();
            InvalidateMeasure();
        }
        if (change.Property == SelectionModeProperty || change.Property == AllowEmptySelectionProperty)
            Reconcile();
        if (change.Property == SelectedItemProperty && !_updating)
        {
            if (SelectedItem is { } item && Buttons.Contains(item))
            {
                item.SetCurrentValue(MaterialButton.IsCheckedProperty, true);
                Reconcile(item);
            }
            else if (SelectedItem is null && AllowEmptySelection)
            {
                _updating = true;
                foreach (var button in Buttons) button.SetCurrentValue(MaterialButton.IsCheckedProperty, false);
                _updating = false;
                Reconcile();
            }
            else Reconcile();
        }
    }

    private void Reconcile(MaterialGroupButton? changed = null)
    {
        if (_updating) return;
        _updating = true;
        var selectionChanged = false;
        try
        {
            var buttons = Buttons.ToArray();
            foreach (var button in buttons) button.SetCurrentValue(MaterialButton.IsToggleProperty, SelectionMode != MaterialGroupSelectionMode.None);
            if (SelectionMode == MaterialGroupSelectionMode.Single)
            {
                var selected = changed is { IsChecked: true } ? changed : buttons.FirstOrDefault(button => button.IsChecked);
                if (selected is null && !AllowEmptySelection)
                    selected = changed ?? buttons.FirstOrDefault(button => button.IsEffectivelyEnabled);
                foreach (var button in buttons) button.SetCurrentValue(MaterialButton.IsCheckedProperty, button == selected);
            }
            foreach (var button in buttons) button.UpdateGroupShape();
            var selection = SelectionMode == MaterialGroupSelectionMode.None ? [] : buttons.Where(button => button.IsChecked).ToArray();
            SetCurrentValue(SelectedItemProperty, selection.FirstOrDefault());
            if (!_selection.SequenceEqual(selection))
            {
                _selection = selection;
                selectionChanged = true;
            }
        }
        finally { _updating = false; }
        if (selectionChanged) SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialGroupAutomationPeer(this);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var buttons = Buttons.Where(button => button.IsEffectivelyEnabled && button.IsVisible && !_overflowed.Contains(button)).ToArray();
        var index = Array.FindIndex(buttons, button => button.IsFocused);
        if (index < 0 || buttons.Length == 0) return;
        var key = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft && Orientation == Avalonia.Layout.Orientation.Horizontal
            ? e.Key switch { Key.Right => Key.Left, Key.Left => Key.Right, _ => e.Key } : e.Key;
        var next = key switch
        {
            Key.Home => 0, Key.End => buttons.Length - 1,
            Key.Right or Key.Down => (index + 1) % buttons.Length,
            Key.Left or Key.Up => (index + buttons.Length - 1) % buttons.Length,
            _ => -1
        };
        if (next >= 0) { buttons[next].Focus(NavigationMethod.Directional); e.Handled = true; }
    }

    private readonly List<List<Control>> _rows = [];
    protected override Size MeasureOverride(Size availableSize)
    {
        _rows.Clear();
        if (_lastAvailableWidth != availableSize.Width)
        { RestoreOverflow(); _lastAvailableWidth = availableSize.Width; }
        var availableWidth = Math.Max(48, availableSize.Width - 10);
        if (this is MaterialSegmentedButtonGroup && Orientation == Avalonia.Layout.Orientation.Horizontal)
        {
            var children = Children.Where(child => child.IsVisible).ToArray();
            if (children.Length == 0) return default;
            if (!double.IsFinite(availableWidth))
            {
                foreach (var child in children) child.Measure(Size.Infinity);
                availableWidth = children.Max(child => child.DesiredSize.Width) * children.Length + (children.Length - 1) * Spacing;
            }
            var columns = Math.Min(children.Length, Math.Max(1, (int)Math.Floor((availableWidth + 1) / 47)));
            foreach (var chunk in children.Chunk(columns))
            {
                var cellWidth = (availableWidth - (chunk.Length - 1) * Spacing) / chunk.Length;
                foreach (var child in chunk) child.Measure(new Size(cellWidth, double.PositiveInfinity));
                _rows.Add(chunk.ToList());
            }
            return new Size(availableWidth + 10, _rows.Sum(r => r.Max(c => c.DesiredSize.Height)) + Math.Max(0, _rows.Count - 1) * 2);
        }
        List<Control> row = [];
        double rowWidth = 0;
        foreach (var child in Children.Where(child => child.IsVisible))
        {
            if (!_overflowed.Contains(child))
            {
                child.Measure(new Size(availableWidth, double.PositiveInfinity));
                _naturalSizes[child] = child.DesiredSize;
            }
            if (row.Count > 0 && (Orientation == Avalonia.Layout.Orientation.Vertical || rowWidth + Spacing + NaturalSize(child).Width > availableWidth))
            {
                _rows.Add(row); row = []; rowWidth = 0;
            }
            row.Add(child);
            rowWidth += NaturalSize(child).Width + (row.Count > 1 ? Spacing : 0);
        }
        if (row.Count > 0) _rows.Add(row);
        ApplyOverflow(availableWidth);
        return new Size(_rows.Select(r => r.Sum(c => c.DesiredSize.Width) + Math.Max(0, r.Count - 1) * Spacing).DefaultIfEmpty(0).Max() + 10,
            _rows.Sum(r => r.Max(c => c.DesiredSize.Height)) + Math.Max(0, _rows.Count - 1) * Math.Max(2, Spacing));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        foreach (var row in _rows)
        {
            var height = row.Max(c => c.DesiredSize.Height);
            double offset = 5;
            var widths = row.Select(c => this is MaterialSegmentedButtonGroup && Orientation == Avalonia.Layout.Orientation.Horizontal
                ? (finalSize.Width - 10 - (row.Count - 1) * Spacing) / row.Count : c.DesiredSize.Width).ToArray();
            var active = _expanding is null ? -1 : row.IndexOf(_expanding);
            if (active >= 0 && Orientation == Avalonia.Layout.Orientation.Horizontal && Variant == MaterialButtonGroupVariant.Unconnected && this is not MaterialSegmentedButtonGroup)
            {
                var neighbors = new[] { active - 1, active + 1 }.Where(i => i >= 0 && i < row.Count).ToArray();
                var delta = Math.Min(widths[active] * ExpandedRatio * _expansion, neighbors.Sum(i => Math.Min(16, Math.Max(0, widths[i] - 48))));
                var compression = neighbors.Select(i => Math.Min(delta / neighbors.Length, Math.Min(16, Math.Max(0, widths[i] - 48)))).ToArray();
                var remaining = delta - compression.Sum();
                for (var n = 0; n < neighbors.Length; n++)
                {
                    var extra = Math.Min(remaining, Math.Min(16, Math.Max(0, widths[neighbors[n]] - 48)) - compression[n]);
                    compression[n] += extra; remaining -= extra;
                    widths[neighbors[n]] -= compression[n];
                }
                widths[active] += delta - remaining;
            }
            foreach (var child in row)
            {
                var width = Orientation == Avalonia.Layout.Orientation.Vertical ? finalSize.Width - 10 : widths[row.IndexOf(child)];
                // Avalonia mirrors the group at the LTR/RTL boundary. Keep layout/shape coordinates logical.
                var x = offset;
                child.Arrange(new Rect(x, y, width, height));
                offset += width + Spacing;
                if (child is MaterialGroupButton button)
                {
                    button.RowFirst = row.First() == child;
                    button.RowLast = row.Last() == child;
                    button.UpdateGroupShape();
                }
            }
            y += height + Math.Max(2, Spacing);
        }
        return finalSize;
    }
}
