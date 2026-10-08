using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// Compose measures requiredSize18 to integer pixels, then places that canvas with
// integer centre alignment. Box and path use the very same measured coordinate frame.
internal sealed class MaterialCheckboxCanvas : Panel
{
    public MaterialCheckboxCanvas() => UseLayoutRounding = false;
    internal static double Stroke(double value, double density) => value == 2 ? Math.Floor(value * density) / density : value;
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(18, 18));
        return new Size(18, 18);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var pixels = Math.Round(18 * density, MidpointRounding.AwayFromZero);
        var x = Math.Round((finalSize.Width * density - pixels) / 2, MidpointRounding.AwayFromZero) / density;
        var y = Math.Round((finalSize.Height * density - pixels) / 2, MidpointRounding.AwayFromZero) / density;
        foreach (var child in Children) child.Arrange(new Rect(x, y, pixels / density, pixels / density));
        return finalSize;
    }
}
