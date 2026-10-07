using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class InitialLabelScenarioTests
{
    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled, 1, 16)]
    [InlineData(MaterialTextFieldVariant.Outlined, 1, 24)]
    [InlineData(MaterialTextFieldVariant.Filled, 2, 32)]
    [InlineData(MaterialTextFieldVariant.Outlined, 2, 32)]
    public void Empty_field_initial_label_uses_the_current_container_height(MaterialTextFieldVariant variant, double scale, double top)
    {
        using var host = new ButtonHost();
        host.Theme.Typography = host.Theme.Typography with { Scale = scale };
        var field = new MaterialTextField { Label = "Initial label", Variant = variant };
        host.Window.Content = new StackPanel { Children = { field } };
        host.Capture();
        var label = field.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == field.Label && text.IsEffectivelyVisible);
        var shown = new Rect(label.Bounds.Size).TransformToAABB(label.TransformToVisual(field)!.Value);
        Assert.Equal(top, shown.Top, 4);
        Assert.Equal(24 * scale, shown.Height, 4);
        Assert.False(field.IsFocused);
    }
}
