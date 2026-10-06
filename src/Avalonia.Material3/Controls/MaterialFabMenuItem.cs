namespace Avalonia.Material3.Controls;

/// <summary>A labelled FAB-menu action. Content is the label; LeadingIcon/LeadingIconTemplate is the icon slot.</summary>
public class MaterialFabMenuItem : MaterialButton
{
    protected override Type StyleKeyOverride => typeof(MaterialFabMenuItem);
    public MaterialFabMenuItem() => Size = MaterialButtonSize.Medium;
}
