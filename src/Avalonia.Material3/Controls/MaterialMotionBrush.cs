using Avalonia.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialMotionBrush
{
    private readonly MaterialMotionValue[] _channels;
    private readonly Action<IBrush?> _paint;
    private bool _solid;
    internal IBrush? Value { get; private set; }
    internal MaterialMotionBrush(Control owner, IBrush? initial, Action<IBrush?> paint)
    {
        Value = initial; _paint = paint;
        var color = Coordinates((initial as ISolidColorBrush)?.Color ?? default, (initial as ISolidColorBrush)?.Opacity ?? 1);
        _channels = [
            new(owner, color.Alpha, _ => Paint()), new(owner, color.L, _ => Paint()),
            new(owner, color.A, _ => Paint()), new(owner, color.B, _ => Paint())];
    }
    internal void Set(IBrush? target, MaterialSpring spring)
    {
        _solid = target is ISolidColorBrush;
        if (target is not ISolidColorBrush brush)
        {
            foreach (var channel in _channels) channel.Snap(channel.Value);
            Value = target; _paint(target); return;
        }
        var color = Coordinates(brush.Color, brush.Opacity);
        _channels[0].Spring(color.Alpha, spring); _channels[1].Spring(color.L, spring);
        _channels[2].Spring(color.A, spring); _channels[3].Spring(color.B, spring);
        Paint();
    }
    internal void Snap(IBrush? target) => Set(target, new MaterialSpring(1, 100) { IsInstant = true });
    private void Paint()
    {
        if (!_solid) return;
        Value = FromCoordinates(_channels[0].Value, _channels[1].Value, _channels[2].Value, _channels[3].Value);
        _paint(Value);
    }
    internal static IBrush? Interpolate(IBrush? from, IBrush? to, double fraction)
    {
        if (fraction <= 0) return from;
        if (fraction >= 1) return to;
        if (from is not ISolidColorBrush first || to is not ISolidColorBrush second) return from;
        var a = Coordinates(first.Color, first.Opacity); var b = Coordinates(second.Color, second.Opacity);
        static double Mix(double start, double end, double p) => start + (end - start) * p;
        return FromCoordinates(Mix(a.Alpha, b.Alpha, fraction), Mix(a.L, b.L, fraction),
            Mix(a.A, b.A, fraction), Mix(a.B, b.B, fraction));
    }
    private static IBrush FromCoordinates(double alpha, double lightness, double a, double b)
    {
        lightness = Math.Clamp(lightness, 0, 1);
        a = Math.Clamp(a, -.5, .5); b = Math.Clamp(b, -.5, .5);
        var l = Math.Pow(lightness + .3963377774 * a + .2158037573 * b, 3);
        var m = Math.Pow(lightness - .1055613458 * a - .0638541728 * b, 3);
        var s = Math.Pow(lightness - .0894841775 * a - 1.2914855480 * b, 3);
        static byte Encode(double value)
        {
            var encoded = value <= .0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - .055;
            return (byte)Math.Clamp(Math.Round(encoded * 255), 0, 255);
        }
        var color = Color.FromArgb((byte)Math.Clamp(Math.Round(alpha * 255), 0, 255),
            Encode(4.0767416621 * l - 3.3077115913 * m + .2309699292 * s),
            Encode(-1.2684380046 * l + 2.6097574011 * m - .3413193965 * s),
            Encode(-.0041960863 * l - .7034186147 * m + 1.7076147010 * s));
        return new ImmutableSolidColorBrush(color);
    }
    // Compose's Color.VectorConverter animates alpha and Oklab coordinates.
    private static (double Alpha, double L, double A, double B) Coordinates(Color color, double opacity)
    {
        static double Linear(byte channel)
        {
            var value = channel / 255d;
            return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
        }
        var r = Linear(color.R); var g = Linear(color.G); var b = Linear(color.B);
        var l = Math.Cbrt(.4122214708 * r + .5363325363 * g + .0514459929 * b);
        var m = Math.Cbrt(.2119034982 * r + .6806995451 * g + .1073969566 * b);
        var s = Math.Cbrt(.0883024619 * r + .2817188376 * g + .6299787005 * b);
        return (color.A / 255d * opacity, .2104542553 * l + .7936177850 * m - .0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + .4505937099 * s,
            .0259040371 * l + .7827717662 * m - .8086757660 * s);
    }
}
