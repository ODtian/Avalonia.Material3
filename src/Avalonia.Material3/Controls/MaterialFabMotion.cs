namespace Avalonia.Material3.Controls;

// The reference animates disclosure geometry; ordinary size configuration is immediate.
internal sealed class MaterialFabMotion : AvaloniaObject
{
    public static readonly AttachedProperty<bool> GeometryAnimatedProperty = AvaloniaProperty.RegisterAttached<MaterialFabMotion, MaterialFab, bool>("GeometryAnimated");
    public static bool GetGeometryAnimated(MaterialFab owner) => owner.GetValue(GeometryAnimatedProperty);
    public static void SetGeometryAnimated(MaterialFab owner, bool value) => owner.SetValue(GeometryAnimatedProperty, value);
}
