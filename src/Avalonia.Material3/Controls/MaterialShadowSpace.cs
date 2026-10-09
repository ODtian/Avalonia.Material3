using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// Window lighting depends on the caster's live coordinate space. Parent layout and
// mutable transforms must re-record the retained shadow without changing motion.
internal sealed class MaterialShadowSpace
{
    private readonly Control _owner;
    private readonly Action _invalidate;
    private readonly Dictionary<Visual, Transform?> _visuals = [];
    private TopLevel? _root;
    private Matrix? _matrix;
    private double _density;
    private PixelPoint _origin;
    private PixelRect? _screen;
    private double _opacity;
    internal MaterialShadowSpace(Control owner, Action invalidate)
    {
        _owner = owner; _invalidate = invalidate;
        owner.AttachedToVisualTree += Attached; owner.DetachedFromVisualTree += Detached;
    }
    private void Attached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        _root = TopLevel.GetTopLevel(_owner);
        foreach (var visual in _owner.GetVisualAncestors().Prepend(_owner))
        {
            visual.PropertyChanged += Changed; var transform = visual.RenderTransform as Transform;
            if (transform is not null) transform.Changed += TransformChanged;
            _visuals[visual] = transform;
        }
        if (_root is not null) _root.ScalingChanged += ScalingChanged;
        if (_root?.Screens is { } screens) screens.Changed += ScalingChanged;
        if (_root is WindowBase window) window.PositionChanged += PositionChanged;
        _owner.LayoutUpdated += LayoutUpdated; Refresh();
    }
    private void Detached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        foreach (var pair in _visuals)
        { pair.Key.PropertyChanged -= Changed; if (pair.Value is { } transform) transform.Changed -= TransformChanged; }
        _visuals.Clear(); _owner.LayoutUpdated -= LayoutUpdated;
        if (_root is not null) _root.ScalingChanged -= ScalingChanged;
        if (_root?.Screens is { } screens) screens.Changed -= ScalingChanged;
        if (_root is WindowBase window) window.PositionChanged -= PositionChanged;
        _root = null; _matrix = null;
    }
    private void Changed(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property == Visual.RenderTransformProperty && sender is Visual visual)
        {
            if (_visuals[visual] is { } previous) previous.Changed -= TransformChanged;
            var transform = visual.RenderTransform as Transform; _visuals[visual] = transform;
            if (transform is not null) transform.Changed += TransformChanged;
        }
        if (args.Property == Visual.BoundsProperty || args.Property == Visual.RenderTransformProperty ||
            args.Property == Visual.RenderTransformOriginProperty || args.Property == Visual.OpacityProperty) Refresh();
    }
    private void LayoutUpdated(object? sender, EventArgs args) => Refresh();
    private void TransformChanged(object? sender, EventArgs args) => Refresh();
    private void ScalingChanged(object? sender, EventArgs args) => Refresh();
    private void PositionChanged(object? sender, PixelPointEventArgs args) => Refresh();
    private void Refresh()
    {
        if (_root?.PlatformImpl is null || !_root.IsAttachedToVisualTree()) return;
        var matrix = _owner.TransformToVisual(_root); var density = _root.RenderScaling;
        var origin = _root.PointToScreen(default); var screen = _root.Screens?.ScreenFromTopLevel(_root)?.Bounds;
        var opacity = _owner.Opacity;
        if (_matrix == matrix && _density == density && _origin == origin && _screen == screen && _opacity == opacity) return;
        _matrix = matrix; _density = density; _origin = origin; _screen = screen; _opacity = opacity; _invalidate();
    }
}
