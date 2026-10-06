using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>Reports the already-applied visual order; hosts can mirror the change to their domain collection.</summary>
public sealed class MaterialListReorderedEventArgs(MaterialListItem item, int oldIndex, int newIndex) : EventArgs
{
    public MaterialListItem Item { get; } = item;
    public int OldIndex { get; } = oldIndex;
    public int NewIndex { get; } = newIndex;
}

/// <summary>A non-virtualizing collection of Material list items; selection is owned by the host.</summary>
public class MaterialList : StackPanel
{
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = AvaloniaProperty.Register<MaterialList, CornerRadius>(nameof(CornerRadius),
        validate: value => double.IsFinite(value.TopLeft) && value.TopLeft >= 0 && value.TopLeft == value.TopRight && value.TopLeft == value.BottomLeft && value.TopLeft == value.BottomRight);
    /// <summary>Uniform rounded container shape. The list owns its rounded clipping geometry.</summary>
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CornerRadiusProperty || change.Property == BoundsProperty)
            SetCurrentValue(ClipProperty, Bounds.Width > 0 && Bounds.Height > 0 ? new RectangleGeometry(new Rect(Bounds.Size), CornerRadius.TopLeft, CornerRadius.TopLeft) : null);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ListPeer(this);
    private sealed class ListPeer(MaterialList owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
    }
    public event EventHandler<MaterialListReorderedEventArgs>? ItemReordered;

    /// <summary>Moves an enabled, reorder-enabled direct child to a zero-based child index.</summary>
    public bool MoveItem(MaterialListItem item, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(item);
        var oldIndex = Children.IndexOf(item);
        if (!IsEffectivelyEnabled || !item.IsEffectivelyEnabled || !item.IsReorderEnabled || oldIndex < 0 || newIndex < 0 || newIndex >= Children.Count || oldIndex == newIndex) return false;
        Children.Move(oldIndex, newIndex);
        ItemReordered?.Invoke(this, new(item, oldIndex, newIndex));
        return true;
    }
}
