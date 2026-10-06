using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Avalonia.Material3.Controls;

// Private inherited token projection. No second public theme API or hardcoded mixed corners.
internal sealed class MaterialMenuShapeRoles
{
    public static readonly AttachedProperty<CornerRadius> ExtraSmall = AvaloniaProperty.RegisterAttached<MaterialMenuShapeRoles, Control, CornerRadius>("MenuExtraSmallRole", new CornerRadius(4), inherits: true);
    public static readonly AttachedProperty<CornerRadius> Small = AvaloniaProperty.RegisterAttached<MaterialMenuShapeRoles, Control, CornerRadius>("MenuSmallRole", new CornerRadius(8), inherits: true);
    public static readonly AttachedProperty<CornerRadius> Medium = AvaloniaProperty.RegisterAttached<MaterialMenuShapeRoles, Control, CornerRadius>("MenuMediumRole", new CornerRadius(12), inherits: true);
    public static readonly AttachedProperty<CornerRadius> Large = AvaloniaProperty.RegisterAttached<MaterialMenuShapeRoles, Control, CornerRadius>("MenuLargeRole", new CornerRadius(16), inherits: true);
    public static readonly AttachedProperty<CornerRadius> Full = AvaloniaProperty.RegisterAttached<MaterialMenuShapeRoles, Control, CornerRadius>("MenuFullRole", new CornerRadius(9999), inherits: true);
    public static bool IsRole(AvaloniaProperty property) => property == ExtraSmall || property == Small || property == Medium || property == Large || property == Full;
    public static void Bind(Control menu)
    {
        menu.Bind(ExtraSmall, new DynamicResourceExtension("M3.Shape.CornerExtraSmall"));
        menu.Bind(Small, new DynamicResourceExtension("M3.Shape.CornerSmall"));
        menu.Bind(Medium, new DynamicResourceExtension("M3.Shape.CornerMedium"));
        menu.Bind(Large, new DynamicResourceExtension("M3.Shape.CornerLarge"));
        menu.Bind(Full, new DynamicResourceExtension("M3.Shape.CornerFull"));
    }
}
