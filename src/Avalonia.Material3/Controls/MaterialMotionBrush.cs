using Avalonia.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialMotionBrush
{
    private readonly MaterialMotionValue[] _channels;
    private readonly Action<IBrush?> _paint;
    private IBrush? _target;
    private bool _solid;
    internal IBrush? Value { get; private set; }
    internal MaterialMotionBrush(Control owner, IBrush? initial, Action<IBrush?> paint)
    {
        Value = _target = initial; _paint = paint;
        var color = (initial as ISolidColorBrush)?.Color ?? default;
        _channels = [
            new(owner, color.A, _ => Paint()), new(owner, color.R, _ => Paint()),
            new(owner, color.G, _ => Paint()), new(owner, color.B, _ => Paint())];
    }
    internal void Set(IBrush? target, MaterialSpring spring)
    {
        _target = target;
        _solid = target is ISolidColorBrush;
        if (target is not ISolidColorBrush brush)
        {
            foreach (var channel in _channels) channel.Snap(channel.Value);
            Value = target; _paint(target); return;
        }
        var color = brush.Color;
        _channels[0].Spring(color.A, spring); _channels[1].Spring(color.R, spring);
        _channels[2].Spring(color.G, spring); _channels[3].Spring(color.B, spring);
        Paint();
    }
    internal void Snap(IBrush? target) => Set(target, new MaterialSpring(1, 100) { IsInstant = true });
    private void Paint()
    {
        if (!_solid) return;
        static byte Component(MaterialMotionValue value) => (byte)Math.Clamp(Math.Round(value.Value), 0, 255);
        var color = Color.FromArgb(Component(_channels[0]), Component(_channels[1]), Component(_channels[2]), Component(_channels[3]));
        Value = new ImmutableSolidColorBrush(color);
        _paint(Value);
    }
}
