using Avalonia.Headless.XUnit;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ThemeAtomicUpdateScenarioTests
{
    [AvaloniaFact]
    public void Motion_consumers_receive_the_reduce_flag_durations_and_springs_in_one_reversible_update()
    {
        using var host = new ButtonHost();
        var resources = host.Theme.Resources;
        var snapshots = new List<(bool Reduced, TimeSpan StateDuration, TimeSpan ShortDuration, MaterialSpring Spatial, MaterialSpring Loading)>();
        host.Window.ResourcesChanged += (_, _) => snapshots.Add((
            (bool)resources["M3.ReduceMotion"]!, (TimeSpan)resources["M3.StateLayerDuration"]!,
            (TimeSpan)resources["M3.Motion.DurationShort1"]!, (MaterialSpring)resources["M3.Motion.FastSpatial"]!,
            (MaterialSpring)resources["M3.Motion.LoadingMorph"]!));
        host.Theme.Motion = new MaterialMotion { StateLayerDuration = TimeSpan.FromMilliseconds(80),
            DurationShort1 = TimeSpan.FromMilliseconds(73), Springs = MaterialSpringScheme.Standard };
        Assert.Equal((false, TimeSpan.FromMilliseconds(80), TimeSpan.FromMilliseconds(73),
            new MaterialSpring(.9, 1400), new MaterialSpring(.6, 200)), Assert.Single(snapshots));
        snapshots.Clear();
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        Assert.Equal((true, TimeSpan.Zero, TimeSpan.Zero,
            new MaterialSpring(.9, 1400) { IsInstant = true }, new MaterialSpring(.6, 200) { IsInstant = true }), Assert.Single(snapshots));
    }

    [AvaloniaFact]
    public void Shape_consumers_receive_scale_partial_edges_and_focus_geometry_together()
    {
        using var host = new ButtonHost();
        var resources = host.Theme.Resources;
        var snapshots = new List<(CornerRadius Large, CornerRadius Top, CornerRadius Button, CornerRadius Focus)>();
        host.Window.ResourcesChanged += (_, _) => snapshots.Add((
            (CornerRadius)resources["M3.Shape.CornerLarge"]!, (CornerRadius)resources["M3.Shape.CornerLargeTop"]!,
            (CornerRadius)resources["M3.ButtonCornerRadius"]!, (CornerRadius)resources["M3.ButtonFocusCornerRadius"]!));
        host.Theme.Shapes = new MaterialShapes { CornerLarge = 32, ButtonCornerRadius = 13, PressedButtonCornerRadius = 7 };
        Assert.Equal((new CornerRadius(32), new CornerRadius(32, 32, 0, 0), new CornerRadius(13), new CornerRadius(18)), Assert.Single(snapshots));
    }

    [AvaloniaFact]
    public void Typography_consumers_receive_one_complete_role_update_and_keep_host_resources()
    {
        using var host = new ButtonHost();
        var resources = host.Theme.Resources;
        var marker = new object(); resources["Host.Marker"] = marker;
        var snapshots = new List<(double LabelSize, double LabelHeight, double BodySize, double BodyHeight)>();
        host.Window.ResourcesChanged += (_, _) => snapshots.Add((
            (double)resources["M3.LabelLargeFontSize"]!, (double)resources["M3.LabelLargeLineHeight"]!,
            (double)resources["M3.BodyLargeFontSize"]!, (double)resources["M3.BodyLargeLineHeight"]!));
        host.Theme.Typography = new MaterialTypography { Scale = 2,
            LabelLarge = new(18, 26, .5, FontWeight.Bold), BodyLarge = new(20, 30, .8, FontWeight.Medium) };
        Assert.Equal((36d, 52d, 40d, 60d), Assert.Single(snapshots));
        Assert.Same(resources, host.Theme.Resources);
        Assert.Same(marker, resources["Host.Marker"]);
        host.Capture();
        Assert.Equal(36, host.Button.FontSize);
        Assert.Equal(FontWeight.Bold, host.Button.FontWeight);
    }
}
