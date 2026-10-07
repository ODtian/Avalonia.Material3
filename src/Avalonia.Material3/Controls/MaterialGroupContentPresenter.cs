using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// Width motion consumes the reserved padding, preserving the measured native glyph lane.
internal sealed class MaterialGroupContentPresenter : Decorator
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var width = Math.Max(finalSize.Width, child.DesiredSize.Width);
            child.Arrange(new Rect((finalSize.Width - width) / 2, 0, width, finalSize.Height));
        }
        return finalSize;
    }
}
