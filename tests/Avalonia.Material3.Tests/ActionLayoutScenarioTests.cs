using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ActionLayoutScenarioTests
{
    [AvaloniaFact]
    public void Button_group_uses_its_native_available_width_without_a_shadow_measurement_inset()
    {
        var group = new MaterialButtonGroup { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        var first = new MaterialGroupButton { Content = "First" }; var last = new MaterialGroupButton { Content = "Last" };
        group.Children.Add(first); group.Children.Add(last);
        using var host = new GeometryHost(group, 220, 120);
        Assert.Equal(0, first.Bounds.Left); Assert.Equal(group.Bounds.Width, last.Bounds.Right);
        Assert.Equal(48, group.Bounds.Height);
    }

    [AvaloniaFact]
    public void Split_small_actions_wrap_the_native_48_DIP_target_with_only_the_2_DIP_join_gap()
    {
        var split = new MaterialSplitButton { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        split.MainButton.Content = "Save";
        using var host = new GeometryHost(split, 220, 120);
        Assert.Equal(48, split.Bounds.Height);
        Assert.Equal(0, split.MainButton.Bounds.Left);
        Assert.Equal(2, split.SecondaryButton.Bounds.Left - split.MainButton.Bounds.Right);
        Assert.Equal(split.Bounds.Width, split.SecondaryButton.Bounds.Right);
    }

    [AvaloniaFact]
    public void Small_button_has_a_48_DIP_target_and_a_centered_40_DIP_container_without_horizontal_shadow_padding()
    {
        var button = new MaterialButton { Content = "Save", VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(button, 220, 120);
        Assert.Equal(48, button.Bounds.Height);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(button.Bounds.Center.X, 4));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(button.Bounds.Center.X, 3));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(1, 24));
    }

    [AvaloniaTheory]
    [InlineData(MaterialFabSize.Standard, 56, 1)]
    [InlineData(MaterialFabSize.Small, 48, 5)]
    public void Fab_container_and_interactive_target_follow_native_extents(MaterialFabSize size, double target, double paintedX)
    {
        var fab = new MaterialFab { Size = size, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(fab, 220, 120);
        host.Theme.Elevation = host.Theme.Elevation with { Shadow3 = default }; host.Render();
        Assert.Equal(target, fab.Bounds.Width); Assert.Equal(target, fab.Bounds.Height);
        Assert.Equal(Color.Parse("#EADDFF"), host.Pixel(paintedX, target / 2));
    }
}
