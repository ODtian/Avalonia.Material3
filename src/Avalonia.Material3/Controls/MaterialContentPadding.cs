using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

// Compose PaddingNode rounds each edge before measuring the content. Logical
// authored padding remains on the owner; this stock slot projects it at live DPI.
internal sealed class MaterialContentPadding : Decorator
{
    private Thickness Insets => MaterialPhysicalLayout.Insets(Padding, TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    protected override Size MeasureOverride(Size availableSize) => LayoutHelper.MeasureChild(Child, availableSize, Insets);
    protected override Size ArrangeOverride(Size finalSize) => LayoutHelper.ArrangeChild(Child, finalSize, Insets);
}

internal static class MaterialPhysicalLayout
{
    internal static double Round(double value, double density) => Math.Floor(value * density + .5) / density;
    internal static Thickness Insets(Thickness value, double density) => new(
        Round(value.Left, density), Round(value.Top, density), Round(value.Right, density), Round(value.Bottom, density));
}

// Integer Alignment.Center of a measured stock surface inside its touch target.
// Decoration siblings occupy the full target as in the original grid template.
internal sealed class MaterialActionLayoutPanel : Panel
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var height = index == 0 ? Math.Min(finalSize.Height, child.DesiredSize.Height) : finalSize.Height;
            var top = MaterialPhysicalLayout.Round((finalSize.Height - height) / 2, density);
            child.Arrange(new Rect(0, top, finalSize.Width, height));
        }
        return finalSize;
    }
}
