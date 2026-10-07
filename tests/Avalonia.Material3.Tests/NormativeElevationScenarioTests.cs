using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Material3.Tokens;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativeElevationScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1d)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2d)]
    public void Elevated_button_paints_official_key_and_ambient_shadow_beyond_its_action_envelope(double scale)
    {
        // material-web703aed25 _elevation.scss: level1 key0/1/2/0 at30%,
        // ambient0/1/3/1 at15%; pinned AndroidX button level1, full40 face.
        var button = new MaterialButton { Width = 120, Height = 50, Variant = MaterialButtonVariant.Elevated,
            Background = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(32) };
        using var actual = new GeometryHost(new Grid { Children = { button } }, 184, 120);
        var actualPixels = actual.Offscreen(scale);
        var reference = new Border { Width = 110, Height = 40, CornerRadius = new CornerRadius(20),
            Background = Brushes.White, BoxShadow = BoxShadows.Parse("0 1 2 0 #4D000000, 0 1 3 1 #26000000"),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(37) };
        using var expected = new GeometryHost(new Grid { Children = { reference } }, 184, 120);
        var referencePixels = expected.Offscreen(scale);
        Assert.Equal(new Size(120, 50), button.Bounds.Size);
        // The lower blur tail crosses the action envelope's y82 edge. Compare visible
        // pixels to an independent literal recipe, including their gradient and extent.
        for (var y = (int)(77 * scale); y < 89 * scale; y++)
        for (var x = (int)(66 * scale); x < 118 * scale; x++)
            Assert.Equal(referencePixels[x, y], actualPixels[x, y]);
        Assert.NotEqual(Color.Parse("#FEF7FF"), actualPixels[(int)(92 * scale), (int)(80 * scale)]);
    }

    [AvaloniaTheory]
    [InlineData(1d)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2d)]
    public void Elevated_surfaces_emit_soft_shadows_while_an_explicit_host_viewport_keeps_its_clip(double scale)
    {
        // Each family supplies its own public surface recipe; reserve outside scene space.
        Control[] surfaces = [new MaterialCard { Variant = MaterialCardVariant.Elevated, IsInteractive = true },
            new MaterialFab(), new MaterialChip { IsElevated = true, Content = "Chip" },
            new MaterialMenu(), new MaterialSnackbar { Content = "Message" }];
        foreach (var surface in surfaces)
        {
            surface.Width = surface is MaterialFab ? 56 : 160; surface.Height = 96; surface.Margin = new Thickness(32);
            surface.HorizontalAlignment = HorizontalAlignment.Left; surface.VerticalAlignment = VerticalAlignment.Top;
            var viewport = new Grid { Children = { surface } };
            using var host = new GeometryHost(viewport, 240, 180);
            host.Window.MouseMove(new Point(32 + surface.Width / 2, 80)); host.Render();
            var before = surface.Bounds;
            var pixels = host.Offscreen(scale);
            var left = surface is MaterialChip ? 36 : 32; // chip has its pinned4-DIP target gutter
            var shadow = pixels[(int)((left - 1) * scale), (int)(80 * scale)];
            Assert.True(shadow.R < 254 && shadow.G < 247 && shadow.B < 255,
                $"{surface.GetType().Name} elevation must paint beyond x{left} surface bounds at scale{scale}: {shadow}.");
            Assert.Equal(before, surface.Bounds);
            // A caller's actual viewport remains authoritative. Restrict it through a
            // public clip and check that the same shadow is cropped exactly there.
            viewport.Clip = new RectangleGeometry(new Rect(left, 32, 160, 96));
            var clipped = host.Offscreen(scale);
            Assert.Equal(Color.Parse("#FEF7FF"), clipped[(int)((left - 1) * scale), (int)(80 * scale)]);
            Assert.Equal(before, surface.Bounds);
        }
    }

    [AvaloniaFact]
    public void Source_zero_shadow_surfaces_retain_tonal_roles_without_casting_a_default_shadow()
    {
        var top = new MaterialTopAppBar { Title = "Title" };
        Control[] surfaces = [top, new MaterialBottomAppBar(), new MaterialSearch { Label = "Search" },
            new MaterialNavigationBar(), new MaterialCard { Variant = MaterialCardVariant.Outlined, IsInteractive = true }];
        foreach (var surface in surfaces)
        {
            surface.Width = 240; surface.Height = 96; surface.Margin = new Thickness(32);
            surface.HorizontalAlignment = HorizontalAlignment.Left; surface.VerticalAlignment = VerticalAlignment.Top;
            using var host = new GeometryHost(new Grid { Children = { surface } }, 320, 180);
            if (surface == top) top.ApplyScrollDelta(20, 20);
            host.Window.MouseMove(new Point(152, 80)); host.Render();
            // Actual defaults: AppBar Surface tonal only, SearchDefaults shadow0,
            // NavigationBarDefaults elevation0, CardDefaults outlined hover0.
            Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(31, 80));
        }
    }

    [AvaloniaFact]
    public async Task Hover_elevation_paints_intermediate_depth_then_returns_with_the_pinned_outgoing_curve()
    {
        var card = new MaterialCard { Variant = MaterialCardVariant.Elevated, IsInteractive = true,
            Focusable = false, Width = 160, Height = 96, Margin = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(new Grid { Children = { card } }, 240, 180);
        host.Theme.Motion = new MaterialMotion(); host.Render();
        var bounds = card.Bounds;
        var idle = host.Pixel(112, 132);
        host.Window.MouseMove(new Point(112, 80)); host.Render();
        var entered = host.Pixel(112, 132);
        await Task.Delay(40); host.Render();
        var intermediate = host.Pixel(112, 132);
        await Task.Delay(180); host.Render();
        var hovered = host.Pixel(112, 132);
        Assert.Contains(new[] { entered, intermediate }, color => color.R < idle.R && color.R > hovered.R);
        Assert.Equal(bounds, card.Bounds);
        host.Window.MouseMove(new Point(220, 160)); host.Render();
        var exited = host.Pixel(112, 132);
        await Task.Delay(40); host.Render();
        var leaving = host.Pixel(112, 132);
        Assert.Contains(new[] { exited, leaving }, color => color.R > hovered.R && color.R < idle.R);
        await Task.Delay(180); host.Render();
        Assert.Equal(idle, host.Pixel(112, 132));
        Assert.Equal(bounds, card.Bounds);
    }

    [AvaloniaFact]
    public void Reordering_row_casts_its_shadow_outside_its_reveal_viewport_inside_the_list_scene()
    {
        var row = new MaterialListItem { Width = 200, Height = 96, Title = "Drag entry", IsReorderEnabled = true,
            HorizontalAlignment = HorizontalAlignment.Center };
        var list = new MaterialList { Width = 240, Height = 180, Margin = new Thickness(32), Children = { row } };
        using var host = new GeometryHost(new Grid { Children = { list } }, 320, 260);
        var box = GeometryHost.Box(row, host.Window);
        var before = host.Pixel(box.Left - 1, box.Center.Y);
        var handle = row.GetVisualDescendants().OfType<MaterialButton>().Single(b => AutomationProperties.GetName(b) == "Reorder item");
        var point = GeometryHost.Box(handle, host.Window).Center;
        host.Window.MouseDown(point, MouseButton.Left); host.Render();
        Assert.True(row.IsReordering);
        Assert.True(host.Pixel(box.Left - 1, box.Center.Y).R < before.R);
        host.Window.MouseUp(point, MouseButton.Left); host.Render();
        Assert.Equal(before, host.Pixel(box.Left - 1, box.Center.Y));
        Assert.Equal(box, GeometryHost.Box(row, host.Window));
    }
}
