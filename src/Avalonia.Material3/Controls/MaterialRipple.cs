using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>The two official platform ripple rendering recipes.</summary>
public enum MaterialRippleStyle { Solid, Patterned }

/// <summary>Overrides the theme ripple recipe for a control and its descendants. Null inherits the theme.</summary>
public sealed class MaterialRipple : AvaloniaObject
{
    private MaterialRipple() { }
    public static readonly AttachedProperty<MaterialRippleStyle?> StyleProperty =
        AvaloniaProperty.RegisterAttached<MaterialRipple, Control, MaterialRippleStyle?>("Style", inherits: true,
            validate: value => value is null || Enum.IsDefined(value.Value));
    public static MaterialRippleStyle? GetStyle(Control control) => control.GetValue(StyleProperty);
    public static void SetStyle(Control control, MaterialRippleStyle? value) => control.SetValue(StyleProperty, value);
}
