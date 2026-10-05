using Avalonia.Media;
using MaterialColorUtilities.ColorAppearance;

namespace Avalonia.Material3.Tokens;

/// <summary>Gamut-mapped HCT in MCU's default sRGB viewing conditions. Alpha is discarded.</summary>
public sealed record MaterialHct
{
    public double Hue { get; }
    public double Chroma { get; }
    public double Tone { get; }
    private readonly uint _argb;

    private MaterialHct(Hct hct)
    {
        Hue = hct.Hue;
        Chroma = hct.Chroma;
        Tone = hct.Tone;
        _argb = hct.ToInt();
    }

    public static MaterialHct FromColor(Color color) => new(Hct.FromInt(color.ToUInt32() | 0xff000000));

    /// <summary>Hue wraps; chroma must be nonnegative and tone in [0,100]. Returned coordinates are achieved, not requested.</summary>
    public static MaterialHct From(double hue, double chroma, double tone)
    {
        if (!double.IsFinite(hue) || !double.IsFinite(chroma) || chroma < 0 || !double.IsFinite(tone) || tone is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(hue), "HCT requires finite hue, nonnegative chroma, and tone in [0,100].");
        return new(Hct.From(hue, chroma, tone));
    }

    public Color ToColor() => Color.FromUInt32(_argb);
}
