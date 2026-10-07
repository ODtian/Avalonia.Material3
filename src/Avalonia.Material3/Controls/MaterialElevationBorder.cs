using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Threading;
using System.Collections.Specialized;

namespace Avalonia.Material3.Controls;

// The surface's authored BoxShadow remains its target; the renderer paints the source-prescribed
// Dp elevation trajectory without changing layout, brushes, or caller shadow overrides.
internal class MaterialElevationBorder : Border
{
    private readonly MaterialElevationTrack _elevation;
    private readonly ShadowValues _paint = new();
    protected override Type StyleKeyOverride => typeof(Border);
    public MaterialElevationBorder()
    {
        _elevation = new(this, () => GetBaseValue(BoxShadowProperty).GetValueOrDefault(), Paint);
        Bind(BoxShadowProperty, _paint, BindingPriority.Animation);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoxShadowProperty && change.Priority != BindingPriority.Animation) _elevation?.Retarget();
    }
    private void Paint()
    {
        _paint.Publish(_elevation.Shadows);
        InvalidateVisual();
    }
    // Animation priority preserves authored style/local targets and the native Border renderer.
    private sealed class ShadowValues : IObservable<BoxShadows>
    {
        private readonly List<IObserver<BoxShadows>> _observers = [];
        public IDisposable Subscribe(IObserver<BoxShadows> observer)
        { _observers.Add(observer); return new Subscription(() => _observers.Remove(observer)); }
        internal void Publish(BoxShadows value)
        { foreach (var observer in _observers) observer.OnNext(value); }
        private sealed class Subscription(Action dispose) : IDisposable
        { public void Dispose() => dispose(); }
    }
}

internal sealed class MaterialElevationTrack
{
    private readonly Control _surface;
    private readonly Func<BoxShadows> _target;
    private readonly Action _invalidate;
    private readonly MaterialMotionValue _height;
    private readonly MaterialMotionSettings _motion;
    private readonly double[] _levels = [0, 1, 3, 6, 8, 12];
    private readonly BoxShadows[] _recipes = new BoxShadows[6];
    private Button? _owner;
    private readonly List<IDisposable> _resources = [];
    private bool _attaching;
    private bool _resourceRefreshQueued;
    private int _previousInteraction;
    private int _outgoingInteraction;
    private bool _recognized;
    private bool _initialized;
    private double _targetHeight;
    private static readonly SplineEasing Incoming = new(.4, 0, .2, 1);
    private static readonly SplineEasing Outgoing = new(.4, 0, .6, 1);

    internal MaterialElevationTrack(Control surface, Func<BoxShadows> target, Action invalidate)
    {
        _surface = surface; _target = target; _invalidate = invalidate;
        _height = new(surface, 0, _ => invalidate());
        _motion = new(surface, () => Retarget());
        surface.AttachedToVisualTree += (_, _) =>
        {
            _owner = surface.GetVisualAncestors().OfType<Button>().FirstOrDefault();
            if (_owner is not null) { _owner.PropertyChanged += OwnerChanged; _owner.Classes.CollectionChanged += ClassesChanged; }
            _initialized = false; Retarget();
            _attaching = true;
            for (var i = 0; i < 6; i++)
            {
                _resources.Add(surface.GetResourceObservable($"M3.Elevation.Level{i}").Subscribe(new ResourceObserver(ResourcesChanged)));
                _resources.Add(surface.GetResourceObservable($"M3.Elevation.Shadow{i}").Subscribe(new ResourceObserver(ResourcesChanged)));
            }
            _attaching = false;
        };
        surface.DetachedFromVisualTree += (_, _) =>
        {
            if (_owner is not null) { _owner.PropertyChanged -= OwnerChanged; _owner.Classes.CollectionChanged -= ClassesChanged; }
            foreach (var subscription in _resources) subscription.Dispose();
            _resources.Clear();
            _owner = null; _height.Snap(_targetHeight); _initialized = false;
        };
    }
    private void ClassesChanged(object? sender, NotifyCollectionChangedEventArgs args) => Retarget();
    private void ResourcesChanged(object? value)
    {
        if (_attaching) Retarget();
        else if (_motion.IsAttached && !_resourceRefreshQueued)
        {
            _outgoingInteraction = 0;
            _resourceRefreshQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                _resourceRefreshQueued = false;
                if (_motion.IsAttached) Retarget();
            }, DispatcherPriority.Render);
        }
    }
    private sealed class ResourceObserver(Action<object?> changed) : IObserver<object?>
    { public void OnNext(object? value) => changed(value); public void OnError(Exception error) { } public void OnCompleted() { } }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == Button.IsPressedProperty || change.Property == InputElement.IsPointerOverProperty
            || change.Property == InputElement.IsFocusedProperty || change.Property == InputElement.IsEffectivelyEnabledProperty
            || change.Property == MaterialCard.IsDraggedProperty || change.Property == MaterialChip.IsDraggedProperty
            || change.Property == MaterialListItem.IsReorderingProperty)
            Retarget();
    }
    internal void Retarget()
    {
        if (_motion is null) return;
        var target = _target();
        var level = -1;
        for (var i = 0; i < 6; i++)
        {
            if (_surface.TryFindResource($"M3.Elevation.Level{i}", _surface.ActualThemeVariant, out var height) && height is double value)
                _levels[i] = value;
            if (_surface.TryFindResource($"M3.Elevation.Shadow{i}", _surface.ActualThemeVariant, out var recipe) && recipe is BoxShadows shadows)
                _recipes[i] = shadows;
            if (_recipes[i] == target && level < 0) level = i;
        }
        var interaction = Interaction();
        var previous = _previousInteraction;
        var nextHeight = level >= 0 ? _levels[level] : 0;
        if (interaction == 0 && previous != 0) _outgoingInteraction = previous;
        else if (interaction != 0) _outgoingInteraction = 0;
        // A single pointer change notifies both pseudo-classes and the public property.
        // Preserve the outgoing epoch when the second notification describes the same state.
        if (_initialized && _recognized && level >= 0 && nextHeight == _targetHeight && interaction == previous
            && !_motion.FastEffects.IsInstant && _owner is { IsEffectivelyEnabled: true })
        { _invalidate(); return; }
        _previousInteraction = interaction;
        _recognized = level >= 0;
        _targetHeight = nextHeight;
        if (!_initialized || !_motion.IsAttached || _motion.FastEffects.IsInstant || _owner is null || !_owner.IsEffectivelyEnabled || !_recognized)
        {
            _height.Snap(_targetHeight); _initialized = _motion.IsAttached;
        }
        else if (_targetHeight == _height.Value && !_height.IsRunning) _height.Snap(_targetHeight);
        else if (interaction != 0)
            _height.Tween(_targetHeight, TimeSpan.FromMilliseconds(120), Incoming);
        else if (_outgoingInteraction != 0)
            _height.Tween(_targetHeight, TimeSpan.FromMilliseconds(_outgoingInteraction == 1 ? 120 : 150), Outgoing);
        else _height.Snap(_targetHeight);
        _invalidate();
    }
    private int Interaction()
    {
        if (_owner is null || !_owner.IsEffectivelyEnabled) return 0;
        if (_owner.IsPressed) return 4;
        if (_owner is MaterialCard { IsDragged: true } or MaterialChip { IsDragged: true } or MaterialListItem { IsReordering: true }) return 3;
        if (_owner.IsFocused) return 2;
        return _owner.IsPointerOver ? 1 : 0;
    }
    internal BoxShadows Shadows
    {
        get
        {
            if (!_recognized || !_initialized) return _target();
            var height = _height.Value;
            if (height <= _levels[0]) return _recipes[0];
            for (var i = 1; i < 6; i++)
            {
                if (height > _levels[i]) continue;
                if (_levels[i] <= _levels[i - 1]) return _target();
                var progress = (height - _levels[i - 1]) / (_levels[i] - _levels[i - 1]);
                return Mix(_recipes[i - 1], _recipes[i], progress);
            }
            return _recipes[5];
        }
    }
    private static BoxShadows Mix(BoxShadows from, BoxShadows to, double progress)
    {
        if (progress <= 0) return from;
        if (progress >= 1) return to;
        var count = Math.Max(from.Count, to.Count);
        if (count == 0) return default;
        BoxShadow At(int index)
        {
            var a = index < from.Count ? from[index] : new BoxShadow { Color = to[index].Color };
            var b = index < to.Count ? to[index] : new BoxShadow { Color = from[index].Color };
            static double Lerp(double a, double b, double p) => a + (b - a) * p;
            byte Channel(byte a, byte b) => (byte)Math.Clamp(Math.Round(Lerp(a, b, progress)), 0, 255);
            return new BoxShadow { OffsetX = Lerp(a.OffsetX, b.OffsetX, progress), OffsetY = Lerp(a.OffsetY, b.OffsetY, progress),
                Blur = Lerp(a.Blur, b.Blur, progress), Spread = Lerp(a.Spread, b.Spread, progress), IsInset = b.IsInset,
                Color = Color.FromArgb(Channel(a.Color.A, b.Color.A), Channel(a.Color.R, b.Color.R), Channel(a.Color.G, b.Color.G), Channel(a.Color.B, b.Color.B)) };
        }
        return count == 1 ? new BoxShadows(At(0)) : new BoxShadows(At(0), Enumerable.Range(1, count - 1).Select(At).ToArray());
    }
}
