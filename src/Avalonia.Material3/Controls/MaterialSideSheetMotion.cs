using Avalonia.Animation.Easings;

namespace Avalonia.Material3.Controls;

// MDC SideSheetBehavior / pinned AndroidX ViewDragHelper:256ms base,600ms cap, quintic ease-out.
internal static class MaterialSideSheetMotion
{
    internal static IEasing Easing { get; } = new Quintic();
    internal static TimeSpan Duration(double distance, double range, double parentWidth, double velocity = 0)
    {
        if (distance == 0) return TimeSpan.Zero;
        var duration = (Math.Abs(distance) / Math.Max(1, range) + 1) * 256;
        if (Math.Abs(velocity) > 0)
        {
            var half = parentWidth / 2;
            var influence = Math.Sin((Math.Min(1, Math.Abs(distance) / Math.Max(1, parentWidth)) - .5) * .47123894);
            duration = 4 * Math.Round(1000 * Math.Abs((half + half * influence) / velocity));
        }
        return TimeSpan.FromMilliseconds(Math.Min(600, (int)duration));
    }
    private sealed class Quintic : Easing
    {
        public override double Ease(double progress) => 1 + Math.Pow(progress - 1, 5);
    }
}
