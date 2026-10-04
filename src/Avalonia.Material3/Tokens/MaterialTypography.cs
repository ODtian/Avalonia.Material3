using Avalonia.Media;

namespace Avalonia.Material3.Tokens;

/// <summary>Host font selection and scale for the initial body and button-label roles.</summary>
public sealed record MaterialTypography
{
    public FontFamily FontFamily { get; init; } = FontFamily.Default;
    public double Scale { get; init; } = 1;

    internal bool IsValid => FontFamily is not null && double.IsFinite(Scale) && Scale > 0
        && double.IsFinite(16 * Scale);
}
