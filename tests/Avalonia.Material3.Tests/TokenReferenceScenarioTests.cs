using System.Text.Json;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Material3.Tests;

public class TokenReferenceScenarioTests
{
    [AvaloniaFact]
    public void Published_resources_match_the_entire_pinned_normative_token_scales()
    {
        var theme = new MaterialTheme();
        using var stream = typeof(TokenReferenceScenarioTests).Assembly.GetManifestResourceStream("M3.AndroidXTokens.json")!;
        using var json = JsonDocument.Parse(stream);
        foreach (var token in json.RootElement.GetProperty("resources").EnumerateObject())
        {
            Assert.True(theme.Resources.TryGetResource(token.Name, ThemeVariant.Light, out var value), token.Name);
            var actual = value switch { CornerRadius radius => radius.TopLeft, FontWeight weight => (double)(int)weight, double number => number, _ => throw new InvalidOperationException(token.Name) };
            Assert.Equal(token.Value.GetDouble(), actual);
        }
        foreach (var token in json.RootElement.GetProperty("durations").EnumerateObject())
        {
            Assert.True(theme.Resources.TryGetResource(token.Name, ThemeVariant.Light, out var value), token.Name);
            Assert.Equal(TimeSpan.FromMilliseconds(token.Value.GetDouble()), value);
        }
        var easings = new MaterialMotion().GetEasings().ToDictionary(pair => "M3.Motion." + pair.Key, pair => pair.Value);
        foreach (var token in json.RootElement.GetProperty("easings").EnumerateObject())
        {
            var values = token.Value.EnumerateArray().Select(value => value.GetDouble()).ToArray();
            Assert.Equal(new MaterialCubicBezier(values[0], values[1], values[2], values[3]), easings[token.Name]);
            Assert.True(theme.Resources.TryGetResource(token.Name, ThemeVariant.Light, out var value));
            Assert.IsType<SplineEasing>(value);
        }
        foreach (var scheme in json.RootElement.GetProperty("springs").EnumerateObject())
        {
            var springs = (scheme.Name == "Standard" ? MaterialSpringScheme.Standard : MaterialSpringScheme.Expressive).GetSprings().ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var spring in scheme.Value.EnumerateObject())
                Assert.Equal(new MaterialSpring(spring.Value[0].GetDouble(), spring.Value[1].GetDouble()), springs[spring.Name]);
        }
        foreach (var vector in json.RootElement.GetProperty("staticSchemes").EnumerateArray())
        {
            var scheme = vector.GetProperty("dark").GetBoolean() ? MaterialColorScheme.Dark : MaterialColorScheme.Light;
            var variant = vector.GetProperty("dark").GetBoolean() ? ThemeVariant.Dark : ThemeVariant.Light;
            foreach (var role in vector.GetProperty("roles").EnumerateObject())
            {
                var expected = Color.Parse(role.Value.GetString()!);
                Assert.Equal(expected, scheme.Get(Enum.Parse<MaterialColorRole>(role.Name)));
                Assert.True(theme.Resources.TryGetResource($"M3.{role.Name}Color", variant, out var color));
                Assert.Equal(expected, color);
                Assert.True(theme.Resources.TryGetResource($"M3.{role.Name}Brush", variant, out var brush));
                Assert.Equal(expected, Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color);
            }
        }
    }

    [Fact]
    public void HCT_gamut_hue_wrap_and_tone_boundaries_match_independent_MCU_vectors()
    {
        using var stream = typeof(TokenReferenceScenarioTests).Assembly.GetManifestResourceStream("M3.ReferenceVectors.json")!;
        using var vectors = JsonDocument.Parse(stream);
        foreach (var vector in vectors.RootElement.GetProperty("hct").EnumerateArray())
            Assert.Equal(Color.Parse(vector.GetProperty("color").GetString()!), MaterialHct.From(vector.GetProperty("hue").GetDouble(),
                vector.GetProperty("chroma").GetDouble(), vector.GetProperty("tone").GetDouble()).ToColor());
    }

    [Fact]
    public void Host_platform_palette_and_sRGB_HCT_coordinates_match_the_independent_oracle()
    {
        using var stream = typeof(TokenReferenceScenarioTests).Assembly.GetManifestResourceStream("M3.ReferenceVectors.json")!;
        using var vectors = JsonDocument.Parse(stream);
        foreach (var vector in vectors.RootElement.GetProperty("roundTrips").EnumerateArray())
        {
            var color = Color.Parse(vector.GetProperty("color").GetString()!);
            var hct = MaterialHct.FromColor(color);
            Assert.InRange(Math.Abs(vector.GetProperty("hue").GetDouble() - hct.Hue), 0, 1e-9);
            Assert.InRange(Math.Abs(vector.GetProperty("chroma").GetDouble() - hct.Chroma), 0, 1e-9);
            Assert.InRange(Math.Abs(vector.GetProperty("tone").GetDouble() - hct.Tone), 0, 1e-9);
            Assert.Equal(color, hct.ToColor());
        }
        var keys = vectors.RootElement.GetProperty("platformKeys");
        var palette = new MaterialPalette(Key("Primary"), Key("Secondary"), Key("Tertiary"), Key("Neutral"), Key("NeutralVariant"), Key("Error"));
        foreach (var vector in vectors.RootElement.GetProperty("platform").EnumerateArray())
        {
            var scheme = MaterialColorScheme.FromPalette(palette, vector.GetProperty("dark").GetBoolean());
            foreach (var role in vector.GetProperty("roles").EnumerateObject())
                Assert.Equal(Color.Parse(role.Value.GetString()!), scheme.Get(Enum.Parse<MaterialColorRole>(role.Name)));
        }
        MaterialTonalPalette Key(string name) => new(keys.GetProperty(name)[0].GetDouble(), keys.GetProperty(name)[1].GetDouble());
    }

    [AvaloniaFact]
    public void Invalid_token_inputs_are_rejected_without_replacing_a_working_theme()
    {
        var theme = new MaterialTheme();
        Assert.Throws<ArgumentException>(() => theme.Typography = new() { BodyLarge = new(16, 12, 0, FontWeight.Normal) });
        Assert.Throws<ArgumentException>(() => theme.Shapes = new() { CornerMedium = double.NaN });
        Assert.Throws<ArgumentException>(() => theme.States = new() { HoverStateLayerOpacity = 1.1 });
        Assert.Throws<ArgumentException>(() => theme.Elevation = new() { Level5 = -1 });
        Assert.Throws<ArgumentException>(() => theme.Motion = new() { Springs = new() { DefaultSpatial = new(0, 380) } });
        Assert.Throws<ArgumentException>(() => theme.DynamicColors = new(null!, MaterialColorScheme.Dark));
        Assert.Throws<ArgumentException>(() => MaterialColorScheme.FromPalette(new(null!, new(0, 16), new(60, 24), new(0, 4), new(0, 8), new(25, 84))));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaterialHct.From(double.NaN, 20, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaterialHct.From(0, -1, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaterialHct.From(0, 20, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaterialTonalPalette(0, 20).Tone(-1));
        Assert.Equal(12, theme.Shapes.CornerMedium);
        Assert.Equal(16, theme.Typography.BodyLarge.FontSize);
    }
}
