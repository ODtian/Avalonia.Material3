using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NativeShadowScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Nonuniform_corners_cast_their_convex_outline_without_a_rectangular_substitution(double density)
    {
        var card = new MaterialCard { Variant = MaterialCardVariant.Elevated, Width = 160, Height = 100,
            Margin = new Thickness(32), CornerRadius = new CornerRadius(0, 28, 0, 28), Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(card, 400, 300); host.Window.SetRenderScaling(density); host.Render();
        var box = GeometryHost.Box(card, host.Window);
        Assert.True(host.Pixel(box.Left - .01, box.Top + 1).R < 254);
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(box.Right - 1, box.Top + 1));
        var transform = new ScaleTransform(1, .5); card.RenderTransform = transform; host.Render();
        Assert.Null(Record.Exception(host.Render));
    }

    [AvaloniaFact]
    public void Authored_shadow_recipe_and_nonuniform_border_keep_the_original_renderer()
    {
        var button = new MaterialButton { Variant = MaterialButtonVariant.Elevated, Width = 160, Margin = new Thickness(32),
            BorderThickness = new Thickness(1, 2, 3, 4), BorderBrush = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(button, 400, 300);
        host.Theme.Resources["M3.Elevation.Shadow1"] = BoxShadows.Parse("0 0 0 8 #FF00FF00"); host.Render();
        var box = GeometryHost.Box(button, host.Window);
        Assert.Equal(new Thickness(1, 2, 3, 4), button.BorderThickness);
        Assert.Equal(Color.FromRgb(0, 255, 0), host.Pixel(box.Center.X, box.Center.Y + button.ContainerHeight / 2 + 4));
    }

    [AvaloniaFact]
    public void Zero_caster_transform_recovers_its_visible_native_shadow()
    {
        var scale = new ScaleTransform(0, 0);
        var button = new MaterialButton { Variant = MaterialButtonVariant.Elevated, Width = 160, Margin = new Thickness(32),
            RenderTransform = scale, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(button, 400, 300);
        Assert.Null(Record.Exception(host.Render));
        scale.ScaleX = scale.ScaleY = 1; host.Render();
        var box = GeometryHost.Box(button, host.Window);
        Assert.InRange(host.Pixel(box.Center.X, box.Center.Y + button.ContainerHeight / 2 + .01).R, 230, 253);
    }

    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Default_level_one_has_the_native_low_alpha_and_finite_penumbra(double density)
    {
        var button = new MaterialButton { Variant = MaterialButtonVariant.Elevated, Width = 160, Margin = new Thickness(32),
            Background = Brushes.White, CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(button, 400, 300);
        host.Window.SetRenderScaling(density); host.Render();
        var box = GeometryHost.Box(button, host.Window);
        var bottom = box.Center.Y + button.ContainerHeight / 2;
        var pixel = host.Pixel(box.Center.X, bottom + .01 / density);
        Assert.True(pixel.R is >= 231 and <= 253, pixel.ToString());
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(box.Center.X, bottom + 4));
    }
}
