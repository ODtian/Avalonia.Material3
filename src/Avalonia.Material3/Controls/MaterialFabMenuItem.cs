using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>A labelled FAB-menu action. Content is the label; LeadingIcon/LeadingIconTemplate is the icon slot.</summary>
public class MaterialFabMenuItem : MaterialButton
{
    internal void SetMenuMask(bool masked) => PseudoClasses.Set(":menu-masked", masked);
    protected override Type StyleKeyOverride => typeof(MaterialFabMenuItem);
    public MaterialFabMenuItem() => Size = MaterialButtonSize.Medium;
}
