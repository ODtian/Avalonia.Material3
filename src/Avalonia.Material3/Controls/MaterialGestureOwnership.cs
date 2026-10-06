using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// Setting controls own their pointer even when the presenter is the routed source.
// Browsing/background surfaces intentionally remain eligible for parent gesture arbitration.
internal static class MaterialGestureOwnership
{
    internal static bool IsInteractive(object? source) => source is Visual visual &&
        visual.GetSelfAndVisualAncestors().OfType<Control>()
            .Any(control => control is Button or TextBox or Slider or MaterialSlider);
}
