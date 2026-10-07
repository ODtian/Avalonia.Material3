using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>An action/option in a MaterialButtonGroup. Content, slots, commands and input retain MaterialButton behavior.</summary>
public class MaterialGroupButton : MaterialButton
{
    internal MaterialButtonGroup? Group { get; set; }
    internal void ActivateFromOverflow() { if (IsEffectivelyEnabled) OnClick(); }
    protected override void CommitToggleSelection()
    {
        if (IsChecked && Group is { SelectionMode: MaterialGroupSelectionMode.Single, AllowEmptySelection: false }) return;
        base.CommitToggleSelection();
    }
    internal event Action? GroupStateChanged;
    internal bool RowFirst { get; set; }
    internal bool RowLast { get; set; }
    public static readonly DirectProperty<MaterialGroupButton, CornerRadius> GroupCornerRadiusProperty =
        AvaloniaProperty.RegisterDirect<MaterialGroupButton, CornerRadius>(nameof(GroupCornerRadius), button => button.GroupCornerRadius);
    private CornerRadius _groupCornerRadius;
    /// <summary>Current target shape for group templates; effective full radii are normalized to rendered bounds by the theme.</summary>
    public CornerRadius GroupCornerRadius => _groupCornerRadius;
    public static readonly StyledProperty<object?> SelectionIconProperty =
        AvaloniaProperty.Register<MaterialGroupButton, object?>(nameof(SelectionIcon), new MaterialSymbolSource("check"));
    public object? SelectionIcon { get => GetValue(SelectionIconProperty); set => SetValue(SelectionIconProperty, value); }
    private double _full = 9999, _small = 8, _extraSmall = 4;
    private readonly List<IDisposable> _resources = [];
    private MaterialButtonGroup? _observedGroup;
    private MaterialGroupSelectionMode _observedMode;
    private bool _observedToggle, _wasTall;
    protected override Type StyleKeyOverride => typeof(MaterialGroupButton);
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new MaterialGroupItemAutomationPeer(this);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        foreach (var key in new[] { "M3.Shape.CornerFull", "M3.Shape.CornerSmall", "M3.Shape.CornerExtraSmall" })
            _resources.Add(this.GetResourceObservable(key).Subscribe(new ShapeObserver(value =>
            {
                if (value is CornerRadius radius)
                {
                    if (key.EndsWith("Full", StringComparison.Ordinal)) _full = radius.TopLeft;
                    else if (key.EndsWith("ExtraSmall", StringComparison.Ordinal)) _extraSmall = radius.TopLeft;
                    else _small = radius.TopLeft;
                    UpdateGroupShape();
                }
            })));
        UpdateGroupShape();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        foreach (var resource in _resources) resource.Dispose();
        _resources.Clear();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IsPressedProperty || e.Property == IsCheckedProperty || e.Property == CornerRadiusProperty
            || (e.Property == BoundsProperty && _wasTall != (Bounds.Height > ContainerHeight + 10.01)))
            UpdateGroupShape();
    }
    internal void UpdateGroupShape()
    {
        var mode = Group?.SelectionMode ?? MaterialGroupSelectionMode.None;
        if (_observedGroup != Group || _observedMode != mode || _observedToggle != IsToggle)
        {
            _observedGroup = Group; _observedMode = mode; _observedToggle = IsToggle;
            GroupStateChanged?.Invoke();
        }
        _wasTall = Bounds.Height > ContainerHeight + 10.01;
        var segmented = Group is MaterialSegmentedButtonGroup;
        PseudoClasses.Set(":segmented", segmented);
        PseudoClasses.Set(":connected", Group?.Variant == MaterialButtonGroupVariant.Connected);
        if (Group is null || (!segmented && Group.Variant == MaterialButtonGroupVariant.Unconnected))
        {
            SetAndRaise(GroupCornerRadiusProperty, ref _groupCornerRadius, CornerRadius);
            return;
        }
        var vertical = Group.Orientation == Avalonia.Layout.Orientation.Vertical;
        var first = vertical ? IsVerticalEdge(true) : RowFirst;
        var last = vertical ? IsVerticalEdge(false) : RowLast;
        var inner = segmented ? 0 : IsPressed ? _extraSmall : _small;
        var full = Bounds.Height > ContainerHeight + 10.01 ? Math.Min(_full, ContainerHeight / 2) : _full;
        CornerRadius target;
        if (!segmented && IsToggle && IsChecked && !IsPressed) target = new CornerRadius(full);
        else if (Group.Orientation == Avalonia.Layout.Orientation.Vertical)
            target = new CornerRadius(first ? full : inner, first ? full : inner, last ? full : inner, last ? full : inner);
        else
        {
            target = new CornerRadius(first ? full : inner, last ? full : inner, last ? full : inner, first ? full : inner);
        }
        SetAndRaise(GroupCornerRadiusProperty, ref _groupCornerRadius, target);
    }
    private bool IsVerticalEdge(bool first)
    {
        if (Group is null) return false;
        var children = Group.Children;
        for (var i = first ? 0 : children.Count - 1; first ? i < children.Count : i >= 0; i += first ? 1 : -1)
            if (children[i] is MaterialGroupButton { IsVisible: true } button) return button == this;
        return false;
    }
    private sealed class ShapeObserver(Action<object?> changed) : IObserver<object?>
    {
        public void OnNext(object? value) => changed(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}
