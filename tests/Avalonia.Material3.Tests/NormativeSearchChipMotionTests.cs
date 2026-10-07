using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativeSearchChipMotionTests
{
    [AvaloniaFact]
    public async Task Selectable_chip_retains_host_icons_while_their_width_and_opacity_follow_the_fixed_shape_recipe()
    {
        var chip = new MaterialChip { ChipVariant = MaterialChipVariant.Filter, Content = "Atlas",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        using var host = new SearchScenarioHost(chip);
        var bare = chip.Bounds.Width;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            FastSpatial = new(.6, 100), SlowEffects = new(1, 100), FastEffects = new(1, 100), DefaultEffects = new(1, 100) } };
        host.Capture();
        var icon = new Border { Width = 18, Height = 18, Background = Brushes.Green };
        chip.LeadingIcon = icon; host.Window.UpdateLayout();
        Assert.Equal(bare, chip.Bounds.Width);
        Assert.Equal(0, icon.GetVisualAncestors().Prepend(icon).Aggregate(1d, (alpha, node) => alpha * node.Opacity));
        var widths = new List<double>();
        for (var frame = 0; frame < 24; frame++)
        {
            await Task.Delay(16); host.Capture(); widths.Add(chip.Bounds.Width);
        }
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        Assert.Equal(bare + 26, chip.Bounds.Width); //18-DIP supplied glyph +8-DIP gap
        Assert.Contains(widths, width => width > bare && width < bare + 26);
        chip.IsChecked = true; host.Capture();
        Assert.Equal(1, icon.GetVisualAncestors().Prepend(icon).Aggregate(1d, (alpha, node) => alpha * node.Opacity));
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false }; host.Capture();
        chip.LeadingIcon = null; host.Window.UpdateLayout();
        Assert.NotNull(icon.GetVisualParent());
        Assert.Equal(bare + 26, chip.Bounds.Width);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        Assert.Equal(bare, chip.Bounds.Width); Assert.Null(icon.GetVisualParent());
    }

    [AvaloniaFact]
    public async Task Search_bool_expansion_preserves_the_header_while_results_expand_and_fade_through_the_pinned_tween()
    {
        var search = new MaterialSearch { Candidates = new[] {
            new MaterialSearchToken("a", "First"), new MaterialSearchToken("b", "Second"), new MaterialSearchToken("c", "Third") } };
        using var host = new SearchScenarioHost(search);
        var collapsed = search.Bounds.Height;
        host.Theme.Motion = new MaterialMotion(); host.Capture();
        search.Open(); host.Window.UpdateLayout();
        Assert.True(search.IsOpen); Assert.Equal(collapsed, search.Bounds.Height);
        var result = search.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "First");
        double Alpha() => result.GetVisualAncestors().Prepend(result).Aggregate(1d, (alpha, node) => alpha * node.Opacity);
        Assert.Equal(0, Alpha());
        var heights = new List<double>();
        var alphas = new List<double>();
        for (var frame = 0; frame < 24; frame++)
        {
            await Task.Delay(20); host.Capture(); heights.Add(search.Bounds.Height); alphas.Add(Alpha());
        }
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        var expanded = search.Bounds.Height;
        Assert.True(expanded > collapsed + 100);
        Assert.Contains(heights, height => height > collapsed && height < expanded);
        Assert.Contains(alphas, alpha => alpha > 0 && alpha < 1);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false }; host.Capture();
        search.Close(); host.Window.UpdateLayout();
        Assert.False(search.IsOpen); Assert.Equal(expanded, search.Bounds.Height);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        Assert.Equal(collapsed, search.Bounds.Height);
    }
}
