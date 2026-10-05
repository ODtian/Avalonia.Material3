using Avalonia.Media;

namespace Avalonia.Material3.Tokens;

/// <summary>WCAG 2.x sRGB contrast. Host overrides are measured, not silently recolored.</summary>
public static class MaterialColorContrast
{
    /// <summary>Composite the foreground over an opaque background, then measure relative luminance contrast.</summary>
    public static double Ratio(Color foreground, Color background)
    {
        if (background.A != 255) throw new ArgumentException("An opaque background is required.", nameof(background));
        var alpha = foreground.A / 255d;
        var front = Luminance(alpha * foreground.R + (1 - alpha) * background.R,
            alpha * foreground.G + (1 - alpha) * background.G, alpha * foreground.B + (1 - alpha) * background.B);
        var back = Luminance(background.R, background.G, background.B);
        return (Math.Max(front, back) + 0.05) / (Math.Min(front, back) + 0.05);
    }

    private static double Luminance(double red, double green, double blue) =>
        0.2126 * Linear(red / 255) + 0.7152 * Linear(green / 255) + 0.0722 * Linear(blue / 255);
    private static double Linear(double channel) => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
