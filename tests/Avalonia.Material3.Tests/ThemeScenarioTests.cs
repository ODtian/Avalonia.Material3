using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ThemeScenarioTests
{
    [Fact]
    public void Seed_schemes_match_every_pinned_reference_role_in_both_modes()
    {
        using var stream = typeof(ThemeScenarioTests).Assembly.GetManifestResourceStream("M3.ReferenceVectors.json")!;
        using var vectors = System.Text.Json.JsonDocument.Parse(stream);
        foreach (var vector in vectors.RootElement.GetProperty("schemes").EnumerateArray())
        {
            var scheme = MaterialColorScheme.FromSeed(Color.Parse(vector.GetProperty("seed").GetString()!), vector.GetProperty("dark").GetBoolean());
            foreach (var role in vector.GetProperty("roles").EnumerateObject())
                Assert.Equal(Color.Parse(role.Value.GetString()!), scheme.Get(Enum.Parse<MaterialColorRole>(role.Name)));
        }
    }

    [AvaloniaFact]
    public void Existing_component_follows_seed_platform_and_mode_and_restores_explicit_schemes()
    {
        using var host = new ButtonHost();
        var initial = host.Capture();
        host.Theme.SeedColor = Color.Parse("#006C4C");
        host.Capture();
        Assert.Equal(Color.Parse("#006C4C"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        host.Theme.DynamicColors = new(MaterialColorScheme.Light with { Primary = Colors.Red, Secondary = Colors.Green },
            MaterialColorScheme.Dark with { Primary = Colors.Blue, Secondary = Colors.Yellow });
        host.Capture();
        Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        host.Window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        host.Capture();
        Assert.Equal(Colors.Blue, Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.True(host.Button.TryFindResource("M3.SecondaryBrush", host.Window.ActualThemeVariant, out var secondary));
        Assert.Equal(Colors.Yellow, Assert.IsAssignableFrom<ISolidColorBrush>(secondary).Color);
        host.Theme.DynamicColors = null;
        host.Theme.SeedColor = null;
        host.Window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        Assert.Equal(initial, host.Capture());
    }

    [AvaloniaFact]
    public void Mixed_text_uses_complete_emphasized_typography_and_rescales_in_place()
    {
        using var host = new ButtonHost();
        var text = new TextBlock { Text = "中文与 Latin 123 — 放大字体仍可阅读", TextWrapping = TextWrapping.Wrap };
        text.Classes.Add("m3-body-large-emphasized");
        ((StackPanel)host.Window.Content!).Children.Add(text);
        host.Capture();
        Assert.Equal(16, text.FontSize);
        Assert.Equal(24, text.LineHeight);
        Assert.Equal(0.15, text.LetterSpacing);
        Assert.Equal(FontWeight.Medium, text.FontWeight);
        host.Theme.Typography = new MaterialTypography { FontFamily = new FontFamily("Microsoft YaHei, Arial"), Scale = 2 };
        host.Capture();
        Assert.Equal(32, text.FontSize);
        Assert.Equal(48, text.LineHeight);
        Assert.Equal(0.3, text.LetterSpacing);
        Assert.Equal(new FontFamily("Microsoft YaHei, Arial"), text.FontFamily);
        Assert.True(text.Bounds.Height >= 48);
        Assert.True(host.Button.Bounds.Height >= 48);
        Assert.Equal(57, new MaterialTypography().DisplayLarge.FontSize);
        Assert.Equal(FontWeight.Bold, new MaterialTypography().LabelSmallEmphasized.FontWeight);
    }

    [AvaloniaFact]
    public void Host_changes_shape_elevation_and_state_feedback_without_recreating_content()
    {
        using var host = new ButtonHost();
        host.Theme.Shapes = new MaterialShapes { CornerMedium = 18 };
        host.Theme.Elevation = new MaterialElevation { Level2 = 4 };
        host.Capture();
        Assert.True(host.Button.TryFindResource("M3.Shape.CornerMedium", out var radius));
        Assert.Equal(new CornerRadius(18), radius);
        Assert.True(host.Button.TryFindResource("M3.Elevation.Level2", out var elevation));
        Assert.Equal(4d, elevation);
        host.Window.MouseMove(host.Center);
        var hovered = host.Capture();
        host.Theme.States = new MaterialStates { HoverStateLayerOpacity = 0.5, DisabledButtonContainerOpacity = 0.3 };
        Assert.NotEqual(hovered, host.Capture());
        host.Button.IsEnabled = false;
        host.Capture();
        Assert.Equal(0.3, Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Opacity);
    }

    [AvaloniaFact]
    public void Runtime_reduce_motion_updates_all_durations_and_springs_and_can_be_reversed()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion();
        Assert.True(host.Button.TryFindResource("M3.Motion.DurationExtraLong4", out var duration));
        Assert.Equal(TimeSpan.FromMilliseconds(1000), duration);
        Assert.True(host.Button.TryFindResource("M3.Motion.FastSpatial", out var spring));
        Assert.Equal(new MaterialSpring(0.6, 800), spring);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        Assert.True(host.Button.TryFindResource("M3.Motion.DurationExtraLong4", out duration));
        Assert.Equal(TimeSpan.Zero, duration);
        Assert.True(host.Button.TryFindResource("M3.Motion.FastSpatial", out spring));
        Assert.True(Assert.IsType<MaterialSpring>(spring).IsInstant);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false, Springs = MaterialSpringScheme.Standard };
        Assert.True(host.Button.TryFindResource("M3.Motion.FastSpatial", out spring));
        Assert.Equal(new MaterialSpring(0.9, 1400), spring);
    }

    [Fact]
    public void Generated_text_pairs_meet_normal_text_contrast_and_host_can_measure_overrides()
    {
        Assert.Equal(21, MaterialColorContrast.Ratio(Colors.Black, Colors.White), 8);
        Assert.Equal(1, MaterialColorContrast.Ratio(Colors.White, Colors.White), 8);
        foreach (var seed in new[] { "#6750A4", "#FF0000", "#00FF00", "#0000FF", "#808080", "#000000", "#FFFFFF", "#006C4C" })
        foreach (var dark in new[] { false, true })
        {
            var scheme = MaterialColorScheme.FromSeed(Color.Parse(seed), dark);
            foreach (var pair in new[] { (scheme.OnPrimary, scheme.Primary), (scheme.OnSecondary, scheme.Secondary),
                (scheme.OnTertiary, scheme.Tertiary), (scheme.OnError, scheme.Error), (scheme.OnPrimaryContainer, scheme.PrimaryContainer),
                (scheme.OnSecondaryContainer, scheme.SecondaryContainer), (scheme.OnTertiaryContainer, scheme.TertiaryContainer),
                (scheme.OnErrorContainer, scheme.ErrorContainer), (scheme.OnSurface, scheme.Surface), (scheme.OnSurfaceVariant, scheme.Surface) })
                Assert.True(MaterialColorContrast.Ratio(pair.Item1, pair.Item2) >= 4.5, $"{seed}/{dark}: {pair}");
        }
    }

    [AvaloniaFact]
    public void Existing_button_consumes_the_host_label_role_font_and_weight_override()
    {
        using var host = new ButtonHost();
        host.Theme.Typography = new MaterialTypography
        {
            LabelLarge = new(18, 24, 0.2, FontWeight.Bold) { FontFamily = new FontFamily("Arial") },
            PlainFontFamily = new FontFamily("Microsoft YaHei"), BrandFontFamily = new FontFamily("Times New Roman")
        };
        host.Capture();
        Assert.Equal(18, host.Button.FontSize);
        Assert.Equal(FontWeight.Bold, host.Button.FontWeight);
        Assert.Equal(new FontFamily("Arial"), host.Button.FontFamily);
        Assert.Equal(new FontFamily("Microsoft YaHei"), host.Window.FontFamily);
    }

    // Independent literals from Google's MCU hct_test.ts, pinned in m3-02.md.
    [Fact]
    public void Host_can_round_trip_reference_red_through_HCT()
    {
        var hct = MaterialHct.FromColor(Color.Parse("#FF0000"));
        Assert.Equal(27.408, hct.Hue, 3);
        Assert.Equal(113.358, hct.Chroma, 3);
        Assert.Equal(53.233, hct.Tone, 3);
        Assert.Equal(Color.Parse("#FF0000"), hct.ToColor());
    }
}
