using Avalonia.Media;

namespace Avalonia.Material3.Tokens;

public enum MaterialTypeRole
{
    DisplayLarge, DisplayMedium, DisplaySmall, HeadlineLarge, HeadlineMedium, HeadlineSmall,
    TitleLarge, TitleMedium, TitleSmall, BodyLarge, BodyMedium, BodySmall, LabelLarge, LabelMedium, LabelSmall,
    DisplayLargeEmphasized, DisplayMediumEmphasized, DisplaySmallEmphasized,
    HeadlineLargeEmphasized, HeadlineMediumEmphasized, HeadlineSmallEmphasized,
    TitleLargeEmphasized, TitleMediumEmphasized, TitleSmallEmphasized,
    BodyLargeEmphasized, BodyMediumEmphasized, BodySmallEmphasized,
    LabelLargeEmphasized, LabelMediumEmphasized, LabelSmallEmphasized
}

/// <summary>Unscaled DIP metrics. FontFamily=null inherits the host's brand/plain font selection.</summary>
public sealed record MaterialTypeStyle(double FontSize, double LineHeight, double LetterSpacing, FontWeight FontWeight)
{
    public FontFamily? FontFamily { get; init; }
    internal bool IsValid => double.IsFinite(FontSize) && FontSize > 0 && double.IsFinite(LineHeight)
        && LineHeight >= FontSize && double.IsFinite(LetterSpacing) && (int)FontWeight is >= 1 and <= 1000;
}

/// <summary>All 15 standard and 15 emphasized AndroidX TypeScaleTokens roles, with host fonts and scale.</summary>
public sealed record MaterialTypography
{
    public FontFamily FontFamily { get; init; } = FontFamily.Default;
    public FontFamily? BrandFontFamily { get; init; }
    public FontFamily? PlainFontFamily { get; init; }
    public double Scale { get; init; } = 1;
    public MaterialTypeStyle DisplayLarge { get; init; } = new(57, 64, -0.2, FontWeight.Normal);
    public MaterialTypeStyle DisplayMedium { get; init; } = new(45, 52, 0, FontWeight.Normal);
    public MaterialTypeStyle DisplaySmall { get; init; } = new(36, 44, 0, FontWeight.Normal);
    public MaterialTypeStyle HeadlineLarge { get; init; } = new(32, 40, 0, FontWeight.Normal);
    public MaterialTypeStyle HeadlineMedium { get; init; } = new(28, 36, 0, FontWeight.Normal);
    public MaterialTypeStyle HeadlineSmall { get; init; } = new(24, 32, 0, FontWeight.Normal);
    public MaterialTypeStyle TitleLarge { get; init; } = new(22, 28, 0, FontWeight.Normal);
    public MaterialTypeStyle TitleMedium { get; init; } = new(16, 24, 0.2, FontWeight.Medium);
    public MaterialTypeStyle TitleSmall { get; init; } = new(14, 20, 0.1, FontWeight.Medium);
    public MaterialTypeStyle BodyLarge { get; init; } = new(16, 24, 0.5, FontWeight.Normal);
    public MaterialTypeStyle BodyMedium { get; init; } = new(14, 20, 0.2, FontWeight.Normal);
    public MaterialTypeStyle BodySmall { get; init; } = new(12, 16, 0.4, FontWeight.Normal);
    public MaterialTypeStyle LabelLarge { get; init; } = new(14, 20, 0.1, FontWeight.Medium);
    public MaterialTypeStyle LabelMedium { get; init; } = new(12, 16, 0.5, FontWeight.Medium);
    public MaterialTypeStyle LabelSmall { get; init; } = new(11, 16, 0.5, FontWeight.Medium);
    public MaterialTypeStyle DisplayLargeEmphasized { get; init; } = new(57, 64, 0, FontWeight.Medium);
    public MaterialTypeStyle DisplayMediumEmphasized { get; init; } = new(45, 52, 0, FontWeight.Medium);
    public MaterialTypeStyle DisplaySmallEmphasized { get; init; } = new(36, 44, 0, FontWeight.Medium);
    public MaterialTypeStyle HeadlineLargeEmphasized { get; init; } = new(32, 40, 0, FontWeight.Medium);
    public MaterialTypeStyle HeadlineMediumEmphasized { get; init; } = new(28, 36, 0, FontWeight.Medium);
    public MaterialTypeStyle HeadlineSmallEmphasized { get; init; } = new(24, 32, 0, FontWeight.Medium);
    public MaterialTypeStyle TitleLargeEmphasized { get; init; } = new(22, 28, 0, FontWeight.Medium);
    public MaterialTypeStyle TitleMediumEmphasized { get; init; } = new(16, 24, 0.15, FontWeight.Bold);
    public MaterialTypeStyle TitleSmallEmphasized { get; init; } = new(14, 20, 0.1, FontWeight.Bold);
    public MaterialTypeStyle BodyLargeEmphasized { get; init; } = new(16, 24, 0.15, FontWeight.Medium);
    public MaterialTypeStyle BodyMediumEmphasized { get; init; } = new(14, 20, 0.25, FontWeight.Medium);
    public MaterialTypeStyle BodySmallEmphasized { get; init; } = new(12, 16, 0.4, FontWeight.Medium);
    public MaterialTypeStyle LabelLargeEmphasized { get; init; } = new(14, 20, 0.1, FontWeight.Bold);
    public MaterialTypeStyle LabelMediumEmphasized { get; init; } = new(12, 16, 0.5, FontWeight.Bold);
    public MaterialTypeStyle LabelSmallEmphasized { get; init; } = new(11, 16, 0.5, FontWeight.Bold);

    public MaterialTypeStyle Get(MaterialTypeRole role) => role switch
    {
        MaterialTypeRole.DisplayLarge => DisplayLarge, MaterialTypeRole.DisplayMedium => DisplayMedium, MaterialTypeRole.DisplaySmall => DisplaySmall,
        MaterialTypeRole.HeadlineLarge => HeadlineLarge, MaterialTypeRole.HeadlineMedium => HeadlineMedium, MaterialTypeRole.HeadlineSmall => HeadlineSmall,
        MaterialTypeRole.TitleLarge => TitleLarge, MaterialTypeRole.TitleMedium => TitleMedium, MaterialTypeRole.TitleSmall => TitleSmall,
        MaterialTypeRole.BodyLarge => BodyLarge, MaterialTypeRole.BodyMedium => BodyMedium, MaterialTypeRole.BodySmall => BodySmall,
        MaterialTypeRole.LabelLarge => LabelLarge, MaterialTypeRole.LabelMedium => LabelMedium, MaterialTypeRole.LabelSmall => LabelSmall,
        MaterialTypeRole.DisplayLargeEmphasized => DisplayLargeEmphasized, MaterialTypeRole.DisplayMediumEmphasized => DisplayMediumEmphasized, MaterialTypeRole.DisplaySmallEmphasized => DisplaySmallEmphasized,
        MaterialTypeRole.HeadlineLargeEmphasized => HeadlineLargeEmphasized, MaterialTypeRole.HeadlineMediumEmphasized => HeadlineMediumEmphasized, MaterialTypeRole.HeadlineSmallEmphasized => HeadlineSmallEmphasized,
        MaterialTypeRole.TitleLargeEmphasized => TitleLargeEmphasized, MaterialTypeRole.TitleMediumEmphasized => TitleMediumEmphasized, MaterialTypeRole.TitleSmallEmphasized => TitleSmallEmphasized,
        MaterialTypeRole.BodyLargeEmphasized => BodyLargeEmphasized, MaterialTypeRole.BodyMediumEmphasized => BodyMediumEmphasized, MaterialTypeRole.BodySmallEmphasized => BodySmallEmphasized,
        MaterialTypeRole.LabelLargeEmphasized => LabelLargeEmphasized, MaterialTypeRole.LabelMediumEmphasized => LabelMediumEmphasized, MaterialTypeRole.LabelSmallEmphasized => LabelSmallEmphasized,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public FontFamily GetFontFamily(MaterialTypeRole role)
    {
        var brand = role.ToString().StartsWith("Display", StringComparison.Ordinal) || role.ToString().StartsWith("Headline", StringComparison.Ordinal)
            || role is MaterialTypeRole.TitleLarge or MaterialTypeRole.TitleLargeEmphasized;
        return Get(role).FontFamily ?? (brand ? BrandFontFamily : PlainFontFamily) ?? FontFamily;
    }

    internal bool IsValid => FontFamily is not null && double.IsFinite(Scale) && Scale > 0
        && Enum.GetValues<MaterialTypeRole>().All(role => Get(role) is { IsValid: true } style
            && double.IsFinite(style.FontSize * Scale) && double.IsFinite(style.LineHeight * Scale)
            && double.IsFinite(style.LetterSpacing * Scale));
}
