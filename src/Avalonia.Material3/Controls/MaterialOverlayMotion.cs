using Avalonia.Controls;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Material3.Controls;

// The component owns its prescribed presentation. The host's generic content keeps its own visuals.
internal sealed class MaterialOverlayMotion : IDisposable
{
    private readonly MaterialOverlayLayer _layer;
    private readonly MaterialMotionSettings _settings;
    private readonly MaterialMotionValue _scale, _alpha, _offset;
    private readonly ScaleTransform _transform = new(1, 1);
    private readonly TranslateTransform _translation = new();
    private enum Recipe { Feedback, Dialog, Drawer }
    private readonly Recipe _recipe;
    private readonly bool _rtl;
    private readonly double _drawerWidth;
    private readonly MaterialNavigationDrawer? _drawer;
    private double _capturedScale = 1, _capturedAlpha = 1, _capturedOffset;
    private readonly MaterialFrameLease _retirement;
    private bool _exiting;
    private bool _arranged;
    private Action? _release;
    private Snapshot? _snapshot;
    internal MaterialOverlayMotion(MaterialOverlayLayer layer)
    {
        _layer = layer;
        _recipe = layer.Container.Child is MaterialDialog ? Recipe.Dialog : layer.Container.Child is MaterialNavigationDrawer ? Recipe.Drawer : Recipe.Feedback;
        _rtl = layer.Container.FlowDirection == FlowDirection.RightToLeft;
        _drawerWidth = (layer.Container.Child as MaterialNavigationDrawer)?.DrawerWidth ?? 0;
        _drawer = layer.Container.Child as MaterialNavigationDrawer;
        var transforms = new TransformGroup(); transforms.Children.Add(_transform); transforms.Children.Add(_translation);
        layer.Container.RenderTransform = transforms;
        layer.Container.RenderTransformOrigin = RelativePoint.Center;
        var initialScale = _recipe == Recipe.Feedback ? .8 : 1;
        _transform.ScaleX = _transform.ScaleY = initialScale;
        _scale = new(layer, initialScale, PaintScale);
        _alpha = new(layer, _recipe == Recipe.Drawer ? 1 : 0, PaintAlpha);
        _offset = new(layer, _recipe == Recipe.Dialog ? 20 : _recipe == Recipe.Drawer ? -1 : 0, PaintOffset);
        _alpha.Snap(_alpha.Value); _offset.Snap(_offset.Value);
        _retirement = MaterialRenderFrames.Bind(layer, _ =>
        {
            if (_scale.IsRunning || _alpha.IsRunning || _offset.IsRunning) return true;
            _release?.Invoke(); return false;
        }, ignoreOwnerEnabled: true);
        _settings = new(layer, Refresh);
    }
    private void Refresh()
    {
        if (!_arranged) return;
        if (_recipe == Recipe.Feedback)
        {
            _scale.Spring(_exiting ? .8 : 1, _settings.FastSpatial);
            _alpha.Spring(_exiting ? 0 : 1, _settings.FastEffects);
        }
        else if (_recipe == Recipe.Drawer)
            _offset.Spring(_exiting ? -1 : 0, _exiting ? _settings.FastEffects : _settings.DefaultSpatial);
        else
        {
            var duration = _settings.FastEffects.IsInstant ? TimeSpan.Zero : TimeSpan.FromMilliseconds(_exiting ? 150 : 220);
            var easing = _exiting ? new SplineEasing(.3, 0, 1, 1) : new SplineEasing(.2, 0, 0, 1);
            _alpha.Tween(_exiting ? 0 : 1, duration, easing);
            _offset.Tween(_exiting ? -10 : 0, duration, easing);
        }
    }
    private void PaintScale(double value) => _transform.ScaleX = _transform.ScaleY = _exiting ? value / _capturedScale : value;
    private void PaintAlpha(double value)
    {
        _layer.Container.Opacity = Math.Clamp(_exiting ? value / Math.Max(.001, _capturedAlpha) : value, 0, 1);
        if (_recipe == Recipe.Dialog) _layer.Scrim.Opacity = _layer.Options.ShowScrim ? _layer.Options.ScrimOpacity * Math.Clamp(value, 0, 1) : 0;
    }
    private void PaintOffset(double value)
    {
        var offset = _exiting ? value - _capturedOffset : value;
        if (_recipe == Recipe.Dialog) _translation.Y = offset;
        if (_recipe == Recipe.Drawer)
        {
            var width = _layer.Container.Bounds.Width > 0 ? _layer.Container.Bounds.Width : _drawerWidth;
            _translation.X = offset * width * (_rtl ? -1 : 1);
            if (!_exiting) _drawer?.SetPresentationOffset(value * width);
            _layer.Scrim.Opacity = _layer.Options.ShowScrim ? _layer.Options.ScrimOpacity * Math.Clamp(1 + value, 0, 1) : 0;
        }
    }
    internal void UpdateGeometry()
    {
        PaintOffset(_offset.Value);
        if (!_arranged) { _arranged = true; Refresh(); }
    }
    internal bool FreezeExit(Control content, Action release)
    {
        if (_settings.FastEffects.IsInstant || content.Bounds.Width <= 0 || content.Bounds.Height <= 0) return false;
        var density = TopLevel.GetTopLevel(content)?.RenderScaling ?? 1;
        _snapshot = new Snapshot(_layer, density);
        _layer.Container.Child = _snapshot; // Original content is immediately reusable/unparented.
        // The attached-layer raster already includes the presented scale/alpha.
        _capturedScale = Math.Max(.001, _scale.Value); _capturedOffset = _offset.Value; _capturedAlpha = _alpha.Value;
        _layer.Container.FlowDirection = FlowDirection.LeftToRight;
        _layer.IsHitTestVisible = false; _layer.IsEnabled = false;
        _release = release; _exiting = true;
        PaintScale(_scale.Value); PaintAlpha(_alpha.Value); PaintOffset(_offset.Value);
        Refresh(); _retirement.SetRunning(true);
        return true;
    }
    public void Dispose()
    {
        _scale.Dispose(); _alpha.Dispose(); _offset.Dispose(); _retirement.Dispose(); _snapshot?.Dispose();
    }
    private sealed class Snapshot : Control, IDisposable
    {
        private const double Gutter = 64;
        private readonly Size _size;
        private readonly RenderTargetBitmap _bitmap;
        protected override bool BypassFlowDirectionPolicies => true;
        private readonly Rect _source;
        internal Snapshot(MaterialOverlayLayer layer, double scale)
        {
            _size = layer.Container.Bounds.Size;
            _source = layer.Container.Bounds.Inflate(Gutter);
            IsHitTestVisible = false;
            _bitmap = new(new PixelSize((int)Math.Ceiling(layer.Bounds.Width * scale),
                (int)Math.Ceiling(layer.Bounds.Height * scale)), new Vector(96 * scale, 96 * scale));
            // Capture the attached layer: theme, state, glyphs and surface overflow stay identical.
            var scrim = (Control)layer.Children[0]; var opacity = scrim.Opacity;
            try { scrim.Opacity = 0; _bitmap.Render(layer); }
            finally { scrim.Opacity = opacity; }
        }
        protected override Size MeasureOverride(Size availableSize) => _size;
        public override void Render(DrawingContext context) => context.DrawImage(_bitmap,
            _source, new Rect(-Gutter, -Gutter, _source.Width, _source.Height));
        public void Dispose() => _bitmap.Dispose();
    }
}
