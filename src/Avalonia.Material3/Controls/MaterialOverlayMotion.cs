using Avalonia.Controls;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// The component owns its prescribed presentation. The host's generic content keeps its own visuals.
internal sealed class MaterialOverlayMotion : IDisposable
{
    private readonly MaterialOverlayLayer _layer;
    private readonly MaterialMotionSettings _settings;
    private readonly MaterialMotionValue _scale, _alpha, _offset, _scrim;
    private readonly ScaleTransform _transform = new(1, 1);
    private readonly TranslateTransform _translation = new();
    private enum Recipe { Feedback, Dialog, Drawer, Sheet }
    private readonly Recipe _recipe;
    private readonly bool _rtl;
    private readonly double _drawerWidth;
    private readonly MaterialNavigationDrawer? _drawer;
    private readonly MaterialSheet? _sheet;
    private double _capturedScale = 1, _capturedAlpha = 1, _capturedOffset;
    private readonly MaterialFrameLease _retirement;
    private bool _exiting;
    private bool _arranged;
    private Action? _release;
    private Snapshot? _snapshot;
    internal Rect? ExitBounds { get; private set; }
    internal MaterialOverlayMotion(MaterialOverlayLayer layer)
    {
        _layer = layer;
        _recipe = layer.Container.Child is MaterialDialog ? Recipe.Dialog : layer.Container.Child is MaterialNavigationDrawer ? Recipe.Drawer : layer.Container.Child is MaterialSheet ? Recipe.Sheet : Recipe.Feedback;
        _rtl = layer.Container.FlowDirection == FlowDirection.RightToLeft;
        _drawerWidth = (layer.Container.Child as MaterialNavigationDrawer)?.DrawerWidth ?? 0;
        _drawer = layer.Container.Child as MaterialNavigationDrawer;
        _sheet = layer.Container.Child as MaterialSheet;
        var transforms = new TransformGroup(); transforms.Children.Add(_transform); transforms.Children.Add(_translation);
        layer.Container.RenderTransform = transforms;
        layer.Container.RenderTransformOrigin = RelativePoint.Center;
        var initialScale = _recipe == Recipe.Feedback ? .8 : 1;
        _transform.ScaleX = _transform.ScaleY = initialScale;
        _scale = new(layer, initialScale, PaintScale);
        _alpha = new(layer, _recipe is Recipe.Drawer or Recipe.Sheet ? 1 : 0, PaintAlpha);
        _offset = new(layer, _recipe == Recipe.Dialog ? 20 : _recipe == Recipe.Drawer ? -1 : _recipe == Recipe.Sheet ? 1 : 0, PaintOffset);
        _scrim = new(layer, 0, value =>
        {
            if (_recipe == Recipe.Sheet) _layer.Scrim.Opacity = _layer.Options.ShowScrim ? _layer.Options.ScrimOpacity * Math.Clamp(value, 0, 1) : 0;
        });
        _scrim.Snap(0);
        _alpha.Snap(_alpha.Value); _offset.Snap(_offset.Value);
        _retirement = MaterialRenderFrames.Bind(layer, _ =>
        {
            if (_scale.IsRunning || _alpha.IsRunning || _offset.IsRunning || _scrim.IsRunning) return true;
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
            _offset.Spring(_exiting ? -1 : 0, _exiting ? _settings.FastEffects : _settings.DefaultSpatial,
                1 / Math.Max(1, _layer.Container.Bounds.Width > 0 ? _layer.Container.Bounds.Width : _drawerWidth));
        else if (_recipe == Recipe.Sheet)
        {
            if (_sheet?.IsSideSheet == true)
            {
                var distance = SideDistance;
                var duration = _settings.DefaultSpatial.IsInstant ? TimeSpan.Zero
                    : MaterialSideSheetMotion.Duration(((_exiting ? 1 : 0) - _offset.Value) * distance, distance, _layer.Bounds.Width);
                _offset.Tween(_exiting ? 1 : 0, duration, MaterialSideSheetMotion.Easing);
            }
            else _offset.Spring(_exiting ? 1 : 0, _exiting ? _settings.FastEffects : _settings.DefaultSpatial,
                1 / Math.Max(1, _layer.Container.Bounds.Height));
            _scrim.Spring(_exiting ? 0 : 1, _settings.DefaultEffects);
        }
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
        if (_recipe == Recipe.Sheet && _sheet is { } sheet)
        {
            if (sheet.IsSideSheet)
            {
                var atStart = _layer.Options.Placement == MaterialOverlayPlacement.Start;
                var sign = atStart != _rtl ? -1 : 1;
                _translation.X = offset * SideDistance * sign;
            }
            else _translation.Y = offset * _layer.Container.Bounds.Height;
        }
        if (_recipe == Recipe.Drawer)
        {
            var width = _layer.Container.Bounds.Width > 0 ? _layer.Container.Bounds.Width : _drawerWidth;
            _translation.X = offset * width * (_rtl ? -1 : 1);
            if (!_exiting) _drawer?.SetPresentationOffset(value * width);
            _layer.Scrim.Opacity = _layer.Options.ShowScrim ? _layer.Options.ScrimOpacity * Math.Clamp(1 + value, 0, 1) : 0;
        }
    }
    private double SideDistance => _layer.Options.Placement == MaterialOverlayPlacement.Start != _rtl
        ? _layer.Container.Bounds.Right : _layer.Bounds.Width - _layer.Container.Bounds.Left;
    internal void UpdateGeometry()
    {
        PaintOffset(_offset.Value);
        if (!_arranged) { _arranged = true; Refresh(); }
    }
    internal double DrawerOffset => _offset.Value * (_layer.Container.Bounds.Width > 0 ? _layer.Container.Bounds.Width : _drawerWidth);
    internal void SetDrawerGesture(double offset)
    {
        if (_exiting || _recipe != Recipe.Drawer) return;
        var width = _layer.Container.Bounds.Width > 0 ? _layer.Container.Bounds.Width : _drawerWidth;
        _offset.Snap(width > 0 ? Math.Clamp(offset / width, -1, 0) : 0);
    }
    internal void RestoreDrawerGesture()
    {
        if (!_exiting && _recipe == Recipe.Drawer) _offset.Spring(0, _settings.DefaultSpatial,
            1 / Math.Max(1, _layer.Container.Bounds.Width > 0 ? _layer.Container.Bounds.Width : _drawerWidth));
    }
    internal bool FreezeExit(Control content, Action release)
    {
        if (_settings.FastEffects.IsInstant || content.Bounds.Width <= 0 || content.Bounds.Height <= 0) return false;
        ExitBounds = _layer.Container.Bounds;
        _snapshot = new Snapshot(_layer);
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
        _scale.Dispose(); _alpha.Dispose(); _offset.Dispose(); _scrim.Dispose(); _retirement.Dispose(); _snapshot?.Dispose();
    }
    private sealed class Snapshot : Control, IDisposable
    {
        private readonly Size _size;
        private readonly MaterialSnapshot? _frame;
        protected override bool BypassFlowDirectionPolicies => true;
        private readonly Rect _destination;
        internal Snapshot(MaterialOverlayLayer layer)
        {
            _size = layer.Container.Bounds.Size;
            var source = PaintBounds(layer);
            IsHitTestVisible = false;
            // VisualBrush renders the attached layer through an absolute crop; ownership stays live.
            var scrim = layer.Scrim; var opacity = scrim.Opacity;
            try { scrim.Opacity = 0; _frame = MaterialSnapshot.Capture(layer, source); }
            finally { scrim.Opacity = opacity; }
            source = _frame?.Bounds ?? source;
            _destination = new Rect(source.Position - layer.Container.Bounds.Position, source.Size);
        }
        private static Rect PaintBounds(MaterialOverlayLayer layer)
        {
            var result = new Rect(layer.Container.Bounds.Size).TransformToAABB(layer.Container.TransformToVisual(layer)!.Value);
            foreach (var visual in layer.Container.GetVisualDescendants().Prepend(layer.Container))
            {
                if (!visual.IsEffectivelyVisible || visual.Bounds.Width <= 0 || visual.Bounds.Height <= 0) continue;
                var bounds = MaterialPaintBounds.GetLocal(visual);
                if (visual.TransformToVisual(layer) is { } transform) result = result.Union(bounds.TransformToAABB(transform));
            }
            return result.Intersect(new Rect(layer.Bounds.Size));
        }
        protected override Size MeasureOverride(Size availableSize) => _size;
        // Bitmap source rectangles use physical pixels; the destination retains its DIP crop.
        public override void Render(DrawingContext context) => _frame?.Draw(context, _destination);
        public void Dispose() => _frame?.Dispose();
    }
}
