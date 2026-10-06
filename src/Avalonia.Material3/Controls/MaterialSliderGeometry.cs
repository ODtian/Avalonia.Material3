namespace Avalonia.Material3.Controls;

// One axis model for paint, endpoint targets and pointer inverse.24-DIP gutters preserve48-DIP
// targets; AndroidX Slider.kt 2428–2669 distinguishes outer edges from8-DIP cap centers.
internal readonly record struct MaterialSliderGeometry(double Length, double Breadth)
{
    public double Start => 24;
    public double End => Math.Max(Start, Length - 24);
    public double Axis => Breadth - 32;
    public double CapInset => Math.Min(8, (End - Start) / 2);
    public double Tick(double fraction) => Start + CapInset + fraction * (End - Start - 2 * CapInset);
    public double Thumb(double fraction, bool discrete) => discrete && fraction is > 0 and < 1
        ? Tick(fraction) : Start + fraction * (End - Start);
    public double Fraction(double position, bool discrete)
    {
        var inset = discrete ? CapInset : 0;
        var span = End - Start - 2 * inset;
        return span > 0 ? Math.Clamp((position - Start - inset) / span, 0, 1) : 0;
    }
    public static double Gap(double thumbWidth) => thumbWidth / 2 + 6;
}
