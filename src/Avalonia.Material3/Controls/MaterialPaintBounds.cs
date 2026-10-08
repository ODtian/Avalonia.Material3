using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// Render overflow is a paint contract, independent of the control's layout footprint.
internal interface IMaterialPaintOverflow
{
    Rect GetPaintBounds(Rect bounds);
}

internal static class MaterialPaintBounds
{
    internal static Rect GetLocal(Visual visual)
    {
        var bounds = new Rect(visual.Bounds.Size);
        return visual switch
        {
            IMaterialPaintOverflow overflow => overflow.GetPaintBounds(bounds),
            Border border => border.BoxShadow.TransformBounds(bounds),
            _ => bounds
        };
    }
}
