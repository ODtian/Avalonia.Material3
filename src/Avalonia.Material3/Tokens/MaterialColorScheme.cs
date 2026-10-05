using Avalonia.Media;

namespace Avalonia.Material3.Tokens;

public enum MaterialColorRole
{
    Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer,
    Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer,
    Tertiary, OnTertiary, TertiaryContainer, OnTertiaryContainer,
    Error, OnError, ErrorContainer, OnErrorContainer,
    Background, OnBackground, Surface, OnSurface, SurfaceVariant, OnSurfaceVariant,
    SurfaceDim, SurfaceBright, SurfaceContainerLowest, SurfaceContainerLow, SurfaceContainer,
    SurfaceContainerHigh, SurfaceContainerHighest, SurfaceTint,
    Outline, OutlineVariant, InverseSurface, InverseOnSurface, InversePrimary, Scrim, Shadow,
    PrimaryFixed, PrimaryFixedDim, OnPrimaryFixed, OnPrimaryFixedVariant,
    SecondaryFixed, SecondaryFixedDim, OnSecondaryFixed, OnSecondaryFixedVariant,
    TertiaryFixed, TertiaryFixedDim, OnTertiaryFixed, OnTertiaryFixedVariant
}

/// <summary>Complete semantic scheme. The original six-argument constructor and deconstruction remain compatible.</summary>
public sealed record MaterialColorScheme(
    Color Primary, Color OnPrimary, Color Surface, Color OnSurface, Color OnSurfaceVariant, Color Outline)
{
    public Color PrimaryContainer { get; init; } = Color.Parse("#EADDFF");
    public Color OnPrimaryContainer { get; init; } = Color.Parse("#21005D");
    public Color Secondary { get; init; } = Color.Parse("#625B71");
    public Color OnSecondary { get; init; } = Color.Parse("#FFFFFF");
    public Color SecondaryContainer { get; init; } = Color.Parse("#E8DEF8");
    public Color OnSecondaryContainer { get; init; } = Color.Parse("#1D192B");
    public Color Tertiary { get; init; } = Color.Parse("#7D5260");
    public Color OnTertiary { get; init; } = Color.Parse("#FFFFFF");
    public Color TertiaryContainer { get; init; } = Color.Parse("#FFD8E4");
    public Color OnTertiaryContainer { get; init; } = Color.Parse("#31111D");
    public Color Error { get; init; } = Color.Parse("#B3261E");
    public Color OnError { get; init; } = Color.Parse("#FFFFFF");
    public Color ErrorContainer { get; init; } = Color.Parse("#F9DEDC");
    public Color OnErrorContainer { get; init; } = Color.Parse("#410E0B");
    public Color Background { get; init; } = Color.Parse("#FEF7FF");
    public Color OnBackground { get; init; } = Color.Parse("#1D1B20");
    public Color SurfaceVariant { get; init; } = Color.Parse("#E7E0EC");
    public Color SurfaceDim { get; init; } = Color.Parse("#DED8E1");
    public Color SurfaceBright { get; init; } = Color.Parse("#FEF7FF");
    public Color SurfaceContainerLowest { get; init; } = Color.Parse("#FFFFFF");
    public Color SurfaceContainerLow { get; init; } = Color.Parse("#F7F2FA");
    public Color SurfaceContainer { get; init; } = Color.Parse("#F3EDF7");
    public Color SurfaceContainerHigh { get; init; } = Color.Parse("#ECE6F0");
    public Color SurfaceContainerHighest { get; init; } = Color.Parse("#E6E0E9");
    public Color SurfaceTint { get; init; } = Color.Parse("#6750A4");
    public Color OutlineVariant { get; init; } = Color.Parse("#CAC4D0");
    public Color InverseSurface { get; init; } = Color.Parse("#322F35");
    public Color InverseOnSurface { get; init; } = Color.Parse("#F5EFF7");
    public Color InversePrimary { get; init; } = Color.Parse("#D0BCFF");
    public Color Scrim { get; init; } = Colors.Black;
    public Color Shadow { get; init; } = Colors.Black;
    public Color PrimaryFixed { get; init; } = Color.Parse("#EADDFF");
    public Color PrimaryFixedDim { get; init; } = Color.Parse("#D0BCFF");
    public Color OnPrimaryFixed { get; init; } = Color.Parse("#21005D");
    public Color OnPrimaryFixedVariant { get; init; } = Color.Parse("#4F378B");
    public Color SecondaryFixed { get; init; } = Color.Parse("#E8DEF8");
    public Color SecondaryFixedDim { get; init; } = Color.Parse("#CCC2DC");
    public Color OnSecondaryFixed { get; init; } = Color.Parse("#1D192B");
    public Color OnSecondaryFixedVariant { get; init; } = Color.Parse("#4A4458");
    public Color TertiaryFixed { get; init; } = Color.Parse("#FFD8E4");
    public Color TertiaryFixedDim { get; init; } = Color.Parse("#EFB8C8");
    public Color OnTertiaryFixed { get; init; } = Color.Parse("#31111D");
    public Color OnTertiaryFixedVariant { get; init; } = Color.Parse("#633B48");

    public static MaterialColorScheme Light { get; } = new(
        Color.Parse("#6750A4"), Colors.White, Color.Parse("#FEF7FF"),
        Color.Parse("#1D1B20"), Color.Parse("#49454F"), Color.Parse("#79747E"));

    public static MaterialColorScheme Dark { get; } = new(
        Color.Parse("#D0BCFF"), Color.Parse("#381E72"), Color.Parse("#141218"),
        Color.Parse("#E6E0E9"), Color.Parse("#CAC4D0"), Color.Parse("#938F99"))
    {
        PrimaryContainer = Color.Parse("#4F378B"), OnPrimaryContainer = Color.Parse("#EADDFF"),
        Secondary = Color.Parse("#CCC2DC"), OnSecondary = Color.Parse("#332D41"),
        SecondaryContainer = Color.Parse("#4A4458"), OnSecondaryContainer = Color.Parse("#E8DEF8"),
        Tertiary = Color.Parse("#EFB8C8"), OnTertiary = Color.Parse("#492532"),
        TertiaryContainer = Color.Parse("#633B48"), OnTertiaryContainer = Color.Parse("#FFD8E4"),
        Error = Color.Parse("#F2B8B5"), OnError = Color.Parse("#601410"),
        ErrorContainer = Color.Parse("#8C1D18"), OnErrorContainer = Color.Parse("#F9DEDC"),
        Background = Color.Parse("#141218"), OnBackground = Color.Parse("#E6E0E9"),
        SurfaceVariant = Color.Parse("#49454F"), SurfaceDim = Color.Parse("#141218"),
        SurfaceBright = Color.Parse("#3B383E"), SurfaceContainerLowest = Color.Parse("#0F0D13"),
        SurfaceContainerLow = Color.Parse("#1D1B20"), SurfaceContainer = Color.Parse("#211F26"),
        SurfaceContainerHigh = Color.Parse("#2B2930"), SurfaceContainerHighest = Color.Parse("#36343B"),
        SurfaceTint = Color.Parse("#D0BCFF"), OutlineVariant = Color.Parse("#49454F"),
        InverseSurface = Color.Parse("#E6E0E9"), InverseOnSurface = Color.Parse("#322F35"), InversePrimary = Color.Parse("#6750A4")
    };

    public static MaterialColorScheme FromSeed(Color seed, bool isDark = false) => FromPalette(MaterialPalette.FromSeed(seed), isDark);

    /// <summary>AndroidX v0_210 role tones applied to MCU palettes. Default contrast only; no wallpaper/platform access.</summary>
    public static MaterialColorScheme FromPalette(MaterialPalette palette, bool isDark = false)
    {
        ArgumentNullException.ThrowIfNull(palette);
        if (!palette.IsValid) throw new ArgumentException("All six palettes are required.", nameof(palette));
        var p = palette.Primary; var s = palette.Secondary; var t = palette.Tertiary;
        var n = palette.Neutral; var v = palette.NeutralVariant; var e = palette.Error;
        var accent = isDark ? 80 : 40; var onAccent = isDark ? 20 : 100;
        var container = isDark ? 30 : 90; var onContainer = isDark ? 90 : 10;
        return new(p.Tone(accent), p.Tone(onAccent), n.Tone(isDark ? 6 : 98), n.Tone(isDark ? 90 : 10), v.Tone(isDark ? 80 : 30), v.Tone(isDark ? 60 : 50))
        {
            PrimaryContainer = p.Tone(container), OnPrimaryContainer = p.Tone(onContainer),
            Secondary = s.Tone(accent), OnSecondary = s.Tone(onAccent), SecondaryContainer = s.Tone(container), OnSecondaryContainer = s.Tone(onContainer),
            Tertiary = t.Tone(accent), OnTertiary = t.Tone(onAccent), TertiaryContainer = t.Tone(container), OnTertiaryContainer = t.Tone(onContainer),
            Error = e.Tone(accent), OnError = e.Tone(onAccent), ErrorContainer = e.Tone(container), OnErrorContainer = e.Tone(onContainer),
            Background = n.Tone(isDark ? 6 : 98), OnBackground = n.Tone(isDark ? 90 : 10),
            SurfaceVariant = v.Tone(isDark ? 30 : 90), SurfaceTint = p.Tone(accent),
            SurfaceDim = n.Tone(isDark ? 6 : 87), SurfaceBright = n.Tone(isDark ? 24 : 98),
            SurfaceContainerLowest = n.Tone(isDark ? 4 : 100), SurfaceContainerLow = n.Tone(isDark ? 10 : 96),
            SurfaceContainer = n.Tone(isDark ? 12 : 94), SurfaceContainerHigh = n.Tone(isDark ? 17 : 92), SurfaceContainerHighest = n.Tone(isDark ? 22 : 90),
            OutlineVariant = v.Tone(isDark ? 30 : 80), InverseSurface = n.Tone(isDark ? 90 : 20),
            InverseOnSurface = n.Tone(isDark ? 20 : 95), InversePrimary = p.Tone(isDark ? 40 : 80), Scrim = n.Tone(0), Shadow = n.Tone(0),
            PrimaryFixed = p.Tone(90), PrimaryFixedDim = p.Tone(80), OnPrimaryFixed = p.Tone(10), OnPrimaryFixedVariant = p.Tone(30),
            SecondaryFixed = s.Tone(90), SecondaryFixedDim = s.Tone(80), OnSecondaryFixed = s.Tone(10), OnSecondaryFixedVariant = s.Tone(30),
            TertiaryFixed = t.Tone(90), TertiaryFixedDim = t.Tone(80), OnTertiaryFixed = t.Tone(10), OnTertiaryFixedVariant = t.Tone(30)
        };
    }

    public Color Get(MaterialColorRole role) => role switch
    {
        MaterialColorRole.Primary => Primary, MaterialColorRole.OnPrimary => OnPrimary,
        MaterialColorRole.PrimaryContainer => PrimaryContainer, MaterialColorRole.OnPrimaryContainer => OnPrimaryContainer,
        MaterialColorRole.Secondary => Secondary, MaterialColorRole.OnSecondary => OnSecondary,
        MaterialColorRole.SecondaryContainer => SecondaryContainer, MaterialColorRole.OnSecondaryContainer => OnSecondaryContainer,
        MaterialColorRole.Tertiary => Tertiary, MaterialColorRole.OnTertiary => OnTertiary,
        MaterialColorRole.TertiaryContainer => TertiaryContainer, MaterialColorRole.OnTertiaryContainer => OnTertiaryContainer,
        MaterialColorRole.Error => Error, MaterialColorRole.OnError => OnError,
        MaterialColorRole.ErrorContainer => ErrorContainer, MaterialColorRole.OnErrorContainer => OnErrorContainer,
        MaterialColorRole.Background => Background, MaterialColorRole.OnBackground => OnBackground,
        MaterialColorRole.Surface => Surface, MaterialColorRole.OnSurface => OnSurface,
        MaterialColorRole.SurfaceVariant => SurfaceVariant, MaterialColorRole.OnSurfaceVariant => OnSurfaceVariant,
        MaterialColorRole.SurfaceDim => SurfaceDim, MaterialColorRole.SurfaceBright => SurfaceBright,
        MaterialColorRole.SurfaceContainerLowest => SurfaceContainerLowest, MaterialColorRole.SurfaceContainerLow => SurfaceContainerLow,
        MaterialColorRole.SurfaceContainer => SurfaceContainer, MaterialColorRole.SurfaceContainerHigh => SurfaceContainerHigh,
        MaterialColorRole.SurfaceContainerHighest => SurfaceContainerHighest, MaterialColorRole.SurfaceTint => SurfaceTint,
        MaterialColorRole.Outline => Outline, MaterialColorRole.OutlineVariant => OutlineVariant,
        MaterialColorRole.InverseSurface => InverseSurface, MaterialColorRole.InverseOnSurface => InverseOnSurface,
        MaterialColorRole.InversePrimary => InversePrimary, MaterialColorRole.Scrim => Scrim, MaterialColorRole.Shadow => Shadow,
        MaterialColorRole.PrimaryFixed => PrimaryFixed, MaterialColorRole.PrimaryFixedDim => PrimaryFixedDim,
        MaterialColorRole.OnPrimaryFixed => OnPrimaryFixed, MaterialColorRole.OnPrimaryFixedVariant => OnPrimaryFixedVariant,
        MaterialColorRole.SecondaryFixed => SecondaryFixed, MaterialColorRole.SecondaryFixedDim => SecondaryFixedDim,
        MaterialColorRole.OnSecondaryFixed => OnSecondaryFixed, MaterialColorRole.OnSecondaryFixedVariant => OnSecondaryFixedVariant,
        MaterialColorRole.TertiaryFixed => TertiaryFixed, MaterialColorRole.TertiaryFixedDim => TertiaryFixedDim,
        MaterialColorRole.OnTertiaryFixed => OnTertiaryFixed, MaterialColorRole.OnTertiaryFixedVariant => OnTertiaryFixedVariant,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
