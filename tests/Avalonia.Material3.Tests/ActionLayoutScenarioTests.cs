using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ActionLayoutScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Settled_extended_fab_has_an_integer_native_content_extent(double density)
    {
        var fab = new MaterialExtendedFab { Size = MaterialFabSize.Small, Icon = new Border { Width = 24, Height = 24 },
            Content = "Extended FAB", VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(fab, 240, 120); host.Window.SetRenderScaling(density); host.Render();
        Assert.Equal(Math.Ceiling(fab.Bounds.Width * density), fab.Bounds.Width * density, precision: 6);
        var original = fab.Bounds.Width;
        fab.IsExpanded = false; host.Render(); fab.IsExpanded = true; host.Render();
        Assert.Equal(original, fab.Bounds.Width);
    }

    [AvaloniaTheory]
    [InlineData(false, 1.25, 51, 5)]
    [InlineData(true, 1.25, 51, 5)]
    [InlineData(false, 3.5, 140, 14)]
    public void Stock_action_padding_rounds_each_native_physical_edge(bool group, double density, int height, int top)
    {
        MaterialButton button = group ? new MaterialGroupButton() : new MaterialButton();
        button.Content = new Border { Width = 24, Height = 20 };
        button.Padding = new Thickness(16, 10); button.Background = Brushes.Black;
        button.VerticalAlignment = VerticalAlignment.Top;
        using var host = new GeometryHost(button, 160, 100); host.Window.SetRenderScaling(density); host.Render();
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var x = (int)(button.Bounds.Center.X * density); var rows = new List<int>();
        for (var y = 0; y < bitmap.PixelSize.Height; y++)
            if (System.Runtime.InteropServices.Marshal.ReadByte(pixels.Address, y * pixels.RowBytes + x * 4 + 1) == 0) rows.Add(y);
        Assert.Equal(top, rows[0]); Assert.Equal(height, rows.Count);
        Assert.Equal(new Thickness(16, 10), button.Padding);
        Assert.Equal(48 * density, button.Bounds.Height * density, precision: 6);
    }

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
