namespace Avalonia.Material3.Tokens;

/// <summary>Rounded shape scale, in DIP. Partial edges use these same configurable radii.</summary>
public sealed record MaterialShapes
{
    public double CornerNone { get; init; }
    public double CornerExtraSmall { get; init; } = 4;
    public double CornerSmall { get; init; } = 8;
    public double CornerMedium { get; init; } = 12;
    public double CornerLarge { get; init; } = 16;
    public double CornerLargeIncreased { get; init; } = 20;
    public double CornerExtraLarge { get; init; } = 28;
    public double CornerExtraLargeIncreased { get; init; } = 32;
    public double CornerExtraExtraLarge { get; init; } = 48;
    /// <summary>A saturated radius which Avalonia clamps to half the shortest edge.</summary>
    public double CornerFull { get; init; } = 9999;
    public double ButtonCornerRadius { get; init; } = 20;
    public double PressedButtonCornerRadius { get; init; } = 8;

    public IEnumerable<KeyValuePair<string, double>> GetRadii()
    {
        yield return new(nameof(CornerNone), CornerNone);
        yield return new(nameof(CornerExtraSmall), CornerExtraSmall);
        yield return new(nameof(CornerSmall), CornerSmall);
        yield return new(nameof(CornerMedium), CornerMedium);
        yield return new(nameof(CornerLarge), CornerLarge);
        yield return new(nameof(CornerLargeIncreased), CornerLargeIncreased);
        yield return new(nameof(CornerExtraLarge), CornerExtraLarge);
        yield return new(nameof(CornerExtraLargeIncreased), CornerExtraLargeIncreased);
        yield return new(nameof(CornerExtraExtraLarge), CornerExtraExtraLarge);
        yield return new(nameof(CornerFull), CornerFull);
    }

    internal bool IsValid => GetRadii().All(pair => double.IsFinite(pair.Value) && pair.Value >= 0)
        && double.IsFinite(ButtonCornerRadius) && ButtonCornerRadius >= 0 && double.IsFinite(ButtonCornerRadius + 5)
        && double.IsFinite(PressedButtonCornerRadius) && PressedButtonCornerRadius >= 0 && double.IsFinite(PressedButtonCornerRadius + 5);
}
