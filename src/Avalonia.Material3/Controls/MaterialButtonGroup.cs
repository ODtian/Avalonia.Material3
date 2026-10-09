using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Automation.Peers;
using Avalonia.Material3.Tokens;

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
    private readonly Dictionary<MaterialGroupButton, PressExpansion> _expansions = [];
    private MaterialSpring _spring = new(.6, 1500);
    private readonly MaterialFrameLease _frames;
    private readonly MaterialFrameLease _textFrames;
    private readonly HashSet<Themes.MaterialActionLabel> _textLabels = [];
    private double[] _widths = [];
    private IDisposable? _springSubscription;
    public MaterialButtonGroup()
    {
        // Symmetric spring widths and origins must share one continuous layout axis.
        // Children inherit this local layout policy; text/stroke rendering retains its own rasterization.
        UseLayoutRounding = false;
        _frames = MaterialRenderFrames.Bind(this, AdvanceExpansion);
        _textFrames = MaterialRenderFrames.Bind(this, ReconcileTextOptions, ignoreOwnerEnabled: true);
        Children.CollectionChanged += TrackChildren;
        InitializeOverflow();
    }
    internal void RegisterTextLabel(Themes.MaterialActionLabel label)
    {
        _textLabels.Add(label); _textFrames.Restart(); _textFrames.SetRunning(true);
    }
    internal void UnregisterTextLabel(Themes.MaterialActionLabel label)
    { _textLabels.Remove(label); if (_textLabels.Count == 0) _textFrames.SetRunning(false); }
    private bool ReconcileTextOptions(MaterialFrame frame)
    { foreach (var label in _textLabels) label.ReconcileTextOptions(); return _textLabels.Count > 0; }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _springSubscription = this.GetResourceObservable("M3.Motion.FastSpatial").Subscribe(new SpringObserver(value =>
        {
            if (value is MaterialSpring spring)
            {
                if (_frames.IsRunning) _frames.Sample();
                _spring = spring;
                foreach (var track in _expansions.Values) track.Retarget(track.Target, _frames.Elapsed.TotalSeconds, _spring);
                if (_spring.IsInstant) InvalidateArrange();
            }
        }));
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _frames.SetRunning(false); _springSubscription?.Dispose(); _springSubscription = null;
        CloseOverflow(false);
        base.OnDetachedFromVisualTree(e);
    }
    private void RetargetExpansion(MaterialGroupButton button, bool pressed)
    {
        if (_frames.IsRunning) _frames.Sample();
        if (!_expansions.TryGetValue(button, out var track)) _expansions[button] = track = new();
        track.ReleasePending = !pressed && !_spring.IsInstant && track.Value <= .75;
        track.Retarget(pressed || track.ReleasePending ? 1 : 0, _frames.Elapsed.TotalSeconds, _spring);
        _frames.SetRunning(_expansions.Values.Any(item => item.IsMoving || item.ReleasePending));
        InvalidateArrange();
    }
    private bool AdvanceExpansion(MaterialFrame frame)
    {
        foreach (var track in _expansions.Values)
        {
            track.Sample(frame.Elapsed.TotalSeconds);
            // The reference lets even a short press visibly reach .75 before releasing.
            if (track.ReleasePending && track.Value > .75)
            { track.ReleasePending = false; track.Retarget(0, frame.Elapsed.TotalSeconds, _spring); }
        }
        InvalidateArrange();
        return _expansions.Values.Any(item => item.IsMoving || item.ReleasePending);
    }
    private sealed class PressExpansion
    {
        internal double Value, Target, Velocity;
        private double _from, _fromVelocity, _start;
        private MaterialSpring _spring = new(.6, 800);
        internal bool IsMoving, ReleasePending;
        internal void Retarget(double target, double now, MaterialSpring spring)
        {
            if (Target == target && _spring == spring && !spring.IsInstant) return;
            _from = Value; _fromVelocity = Velocity; _start = now; Target = target; _spring = spring;
            IsMoving = !spring.IsInstant && (Math.Abs(Value - target) > .0001 || Math.Abs(Velocity) > .01);
            if (!IsMoving) { Value = target; Velocity = 0; }
        }
        internal void Sample(double now)
        {
            if (!IsMoving) return;
            (Value, Velocity) = MaterialSpringResponse.Sample(Math.Max(0, now - _start), _from, Target, _fromVelocity, _spring);
            if (now - _start >= 10 || (Math.Abs(Value - Target) < .001 && Math.Abs(Velocity) < .01))
            { Value = Target; Velocity = 0; IsMoving = false; }
        }
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
            _expansions.Remove(removed);
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
        if (!_updatingOverflow && (args.Property == MaterialButton.ContentProperty || args.Property == MaterialButton.SizeProperty
            || args.Property == MaterialButton.ContentTemplateProperty || args.Property == MaterialButton.FontSizeProperty
            || args.Property == MaterialButton.FontFamilyProperty || args.Property == MaterialButton.FontWeightProperty
            || args.Property == MaterialButton.FontStyleProperty || args.Property == MaterialButton.LetterSpacingProperty
            || args.Property == TextBlock.LineHeightProperty || args.Property == MaterialButton.PaddingProperty
            || args.Property == WidthProperty || args.Property == MinWidthProperty || args.Property == MaxWidthProperty
            || args.Property == MaterialButton.IsVisibleProperty))
        {
            RestoreOverflow(); InvalidateMeasure();
        }
        if (args.Property == MaterialButton.IsCheckedProperty)
            Reconcile(sender as MaterialGroupButton);
        // Availability can create a required choice, but never activates/replaces an existing choice.
        if (args.Property == IsEffectivelyEnabledProperty &&
            SelectionMode == MaterialGroupSelectionMode.Single && !AllowEmptySelection &&
            !Buttons.Any(button => button.IsChecked))
            Reconcile();
        if (args.Property == MaterialButton.IsPressedProperty && Variant == MaterialButtonGroupVariant.Unconnected && this is not MaterialSegmentedButtonGroup)
        {
            if (sender is MaterialGroupButton button) RetargetExpansion(button, button.IsPressed);
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
        var availableWidth = Math.Max(48, availableSize.Width);
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
            return new Size(availableWidth, _rows.Sum(r => r.Max(c => c.DesiredSize.Height)) + Math.Max(0, _rows.Count - 1) * 2);
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
        return new Size(_rows.Select(r => r.Sum(c => c.DesiredSize.Width) + Math.Max(0, r.Count - 1) * Spacing).DefaultIfEmpty(0).Max(),
            _rows.Sum(r => r.Max(c => c.DesiredSize.Height)) + Math.Max(0, _rows.Count - 1) * Math.Max(2, Spacing));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        var spacing = Spacing;
        var horizontal = Orientation == Avalonia.Layout.Orientation.Horizontal;
        foreach (var row in _rows)
        {
            if (_widths.Length < row.Count) _widths = new double[row.Count];
            double height = 0, offset = 0;
            for (var i = 0; i < row.Count; i++)
            {
                var child = row[i];
                height = Math.Max(height, child.DesiredSize.Height);
                _widths[i] = this is MaterialSegmentedButtonGroup && horizontal
                    ? (finalSize.Width - (row.Count - 1) * spacing) / row.Count : child.DesiredSize.Width;
            }
            if (horizontal && row.Contains(OverflowButton))
            {
                var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
                var measured = _widths.Take(row.Count).Sum() + (row.Count - 1) * spacing;
                offset = Math.Min(0, Math.Truncate((Math.Round(finalSize.Width * density) - Math.Round(measured * density)) / 2) / density);
            }
            if (horizontal && Variant == MaterialButtonGroupVariant.Unconnected && this is not MaterialSegmentedButtonGroup)
            {
                for (var active = 0; active < row.Count; active++)
                {
                    if (row[active] is not MaterialGroupButton current || !_expansions.TryGetValue(current, out var track) || track.Value <= 0) continue;
                    double Limit(int i) => i >= 0 && i < row.Count && row[i] is MaterialButton neighbor
                        ? Math.Min(neighbor.Padding.Left, neighbor.Padding.Right) : 0;
                    var left = active - 1; var right = active + 1;
                    var middle = left >= 0 && right < row.Count;
                    var growth = track.Value * (middle
                        ? Math.Min(_widths[active] * ExpandedRatio / 2, Math.Min(Limit(left), Limit(right)))
                        : Math.Min(_widths[active] * ExpandedRatio, Limit(left >= 0 ? left : right)));
                    var compressLeft = left >= 0 ? Math.Min(growth, _widths[left]) : 0;
                    var compressRight = right < row.Count ? Math.Min(growth, _widths[right]) : 0;
                    if (left >= 0) _widths[left] -= compressLeft;
                    if (right < row.Count) _widths[right] -= compressRight;
                    _widths[active] += compressLeft + compressRight;
                }
            }
            for (var i = 0; i < row.Count; i++)
            {
                var child = row[i];
                var width = horizontal ? _widths[i] : finalSize.Width;
                if (child is MaterialGroupButton button && (button.RowFirst != (i == 0) || button.RowLast != (i == row.Count - 1)))
                {
                    button.RowFirst = i == 0;
                    button.RowLast = i == row.Count - 1;
                    button.UpdateGroupShape();
                }
                // Logical layout crosses Avalonia's RTL mirror once; no per-frame array or IndexOf scan.
                var left = child == OverflowButton ? offset - Math.Max(0, width - _overflowLayoutWidth) / 2 : offset;
                child.Arrange(new Rect(left, y, width, height));
                offset += width + spacing;
            }
            y += height + Math.Max(2, spacing);
        }
        return finalSize;
    }
}
