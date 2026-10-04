using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>A Material filled button with Avalonia's content, command, input and automation behavior.</summary>
public class MaterialButton : Button
{
    protected override Type StyleKeyOverride => typeof(MaterialButton);
}
