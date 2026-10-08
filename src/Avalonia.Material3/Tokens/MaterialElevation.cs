using Avalonia.Media;

namespace Avalonia.Material3.Tokens;

/// <summary>Six Material elevation heights and compatible Web BoxShadow tokens. Stock surfaces use the pinned Android/Skia native renderer; custom recipes retain Avalonia shadow painting.</summary>
public sealed record MaterialElevation
{
    public double Level0 { get; init; }
    public double Level1 { get; init; } = 1;
    public double Level2 { get; init; } = 3;
    public double Level3 { get; init; } = 6;
    public double Level4 { get; init; } = 8;
    public double Level5 { get; init; } = 12;
    public BoxShadows Shadow0 { get; init; }
    // material-web703aed25 elevation/internal/_elevation.scss: key30%, ambient15%.
    public BoxShadows Shadow1 { get; init; } = BoxShadows.Parse("0 1 2 0 #4D000000, 0 1 3 1 #26000000");
    public BoxShadows Shadow2 { get; init; } = BoxShadows.Parse("0 1 2 0 #4D000000, 0 2 6 2 #26000000");
    public BoxShadows Shadow3 { get; init; } = BoxShadows.Parse("0 1 3 0 #4D000000, 0 4 8 3 #26000000");
    public BoxShadows Shadow4 { get; init; } = BoxShadows.Parse("0 2 3 0 #4D000000, 0 6 10 4 #26000000");
    public BoxShadows Shadow5 { get; init; } = BoxShadows.Parse("0 4 4 0 #4D000000, 0 8 12 6 #26000000");

    public double GetLevel(int level) => level switch { 0 => Level0, 1 => Level1, 2 => Level2, 3 => Level3, 4 => Level4, 5 => Level5, _ => throw new ArgumentOutOfRangeException(nameof(level)) };
    public BoxShadows GetShadow(int level) => level switch { 0 => Shadow0, 1 => Shadow1, 2 => Shadow2, 3 => Shadow3, 4 => Shadow4, 5 => Shadow5, _ => throw new ArgumentOutOfRangeException(nameof(level)) };
    internal bool IsValid
    {
        get
        {
            for (var level = 0; level <= 5; level++)
            {
                if (!double.IsFinite(GetLevel(level)) || GetLevel(level) < 0) return false;
                foreach (var shadow in GetShadow(level))
                    if (!double.IsFinite(shadow.OffsetX) || !double.IsFinite(shadow.OffsetY)
                        || !double.IsFinite(shadow.Blur) || shadow.Blur < 0 || !double.IsFinite(shadow.Spread)) return false;
            }
            return true;
        }
    }
}
