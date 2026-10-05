using Avalonia.Controls;

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
