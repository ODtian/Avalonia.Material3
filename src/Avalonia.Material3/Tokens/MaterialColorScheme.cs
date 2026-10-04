using Avalonia.Media;

namespace Avalonia.Material3.Tokens;

/// <summary>The initial semantic color roles; complete and generated schemes are delivered in M3-02.</summary>
public sealed record MaterialColorScheme(
    Color Primary,
    Color OnPrimary,
    Color Surface,
    Color OnSurface,
    Color OnSurfaceVariant,
    Color Outline)
{
    public static MaterialColorScheme Light { get; } = new(
        Color.Parse("#6750A4"), Color.Parse("#FFFFFF"), Color.Parse("#FEF7FF"),
        Color.Parse("#1D1B20"), Color.Parse("#49454F"), Color.Parse("#79747E"));

    public static MaterialColorScheme Dark { get; } = new(
        Color.Parse("#D0BCFF"), Color.Parse("#381E72"), Color.Parse("#141218"),
        Color.Parse("#E6E0E9"), Color.Parse("#CAC4D0"), Color.Parse("#938F99"));
}
