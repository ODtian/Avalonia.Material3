using Avalonia.Media;

namespace Avalonia.Material3.Tokens;

/// <summary>A platform or brand tonal palette using MCU's sRGB HCT gamut mapping.</summary>
public sealed record MaterialTonalPalette
{
    public double Hue { get; }
    public double Chroma { get; }

    public MaterialTonalPalette(double hue, double chroma)
    {
        if (!double.IsFinite(hue) || !double.IsFinite(chroma) || chroma < 0)
            throw new ArgumentOutOfRangeException(nameof(hue));
        Hue = ((hue % 360) + 360) % 360;
        Chroma = chroma;
    }

    public Color Tone(double tone)
    {
        if (!double.IsFinite(tone) || tone is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(tone));
        return MaterialHct.From(Hue, Chroma, tone).ToColor();
    }
}

/// <summary>Six immutable key palettes supplied by a platform, or generated with MCU CorePalette.of.</summary>
public sealed record MaterialPalette(
    MaterialTonalPalette Primary, MaterialTonalPalette Secondary, MaterialTonalPalette Tertiary,
    MaterialTonalPalette Neutral, MaterialTonalPalette NeutralVariant, MaterialTonalPalette Error)
{
    internal bool IsValid => Primary is not null && Secondary is not null && Tertiary is not null
        && Neutral is not null && NeutralVariant is not null && Error is not null;

    public static MaterialPalette FromSeed(Color seed)
    {
        var hct = MaterialHct.FromColor(seed);
        return new(new(hct.Hue, Math.Max(48, hct.Chroma)), new(hct.Hue, 16), new(hct.Hue + 60, 24),
            new(hct.Hue, 4), new(hct.Hue, 8), new(25, 84));
    }
}

/// <summary>A platform's resolved light/dark schemes. No platform access or wallpaper extraction occurs in the library.</summary>
public sealed record MaterialDynamicColors(MaterialColorScheme Light, MaterialColorScheme Dark)
{
    internal bool IsValid => Light is not null && Dark is not null;
    public static MaterialDynamicColors FromPalette(MaterialPalette palette) =>
        new(MaterialColorScheme.FromPalette(palette), MaterialColorScheme.FromPalette(palette, true));
}
