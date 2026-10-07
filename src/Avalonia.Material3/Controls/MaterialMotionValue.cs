using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// An internal scalar track shared by source-prescribed component recipes.
internal sealed class MaterialMotionValue : IDisposable
{
    private readonly MaterialFrameLease _frames;
    private readonly Action<double> _paint;
    private MaterialSpring? _spring;
    private IEasing _easing = new LinearEasing();
    private double _from, _target, _initialVelocity, _velocity;
    private double _duration, _delay;
    private double _threshold = .01;
    internal double Value { get; private set; }
    internal bool IsRunning => _frames.IsRunning;

    internal MaterialMotionValue(Control owner, double initial, Action<double> paint)
    {
        Value = _from = _target = initial;
        _paint = paint;
        _frames = MaterialRenderFrames.Bind(owner, Advance, ignoreOwnerEnabled: true);
    }
    internal void Snap(double target)
    {
        _frames.SetRunning(false);
        _spring = null;
        _from = _target = Value = target;
        _velocity = 0;
        _paint(Value);
    }
    internal void Spring(double target, MaterialSpring spring, double visibilityThreshold = .01)
    {
        _frames.Sample();
        if (spring.IsInstant) { Snap(target); return; }
        if (target == _target && _spring == spring) return;
        _from = Value; _target = target; _initialVelocity = _velocity;
        _spring = spring; _delay = 0;
        _threshold = visibilityThreshold;
        _frames.Restart(); _frames.SetRunning(true);
        _paint(Value);
    }
    internal void Tween(double target, TimeSpan duration, IEasing? easing = null, TimeSpan delay = default)
    {
        _frames.Sample();
        if (duration <= TimeSpan.Zero && delay <= TimeSpan.Zero) { Snap(target); return; }
        if (target == _target && _spring is null && _frames.IsRunning) return;
        _from = Value; _target = target; _velocity = 0;
        _spring = null; _duration = duration.TotalSeconds; _delay = delay.TotalSeconds;
        _easing = easing ?? new LinearEasing();
        _frames.Restart(); _frames.SetRunning(true);
    }
    private bool Advance(MaterialFrame frame)
    {
        if (!_frames.IsRunning) return false;
        var time = Math.Max(0, frame.Elapsed.TotalSeconds - _delay);
        bool running;
        if (_spring is { } spring)
        {
            (Value, _velocity) = MaterialSpringResponse.Sample(time, _from, _target, _initialVelocity, spring);
            running = Math.Abs(Value - _target) > _threshold || Math.Abs(_velocity) > _threshold * 62.5;
        }
        else
        {
            var fraction = _duration <= 0 ? (frame.Elapsed.TotalSeconds >= _delay ? 1 : 0) : Math.Clamp(time / _duration, 0, 1);
            Value = _from + (_target - _from) * _easing.Ease(fraction);
            running = fraction < 1;
        }
        if (!running) { Value = _target; _velocity = 0; }
        _paint(Value);
        return running;
    }
    public void Dispose() => _frames.Dispose();
}

internal sealed class MaterialMotionSettings
{
    private readonly Control _owner;
    private readonly Action _changed;
    private readonly List<IDisposable> _subscriptions = [];
    private bool _initializing;
    private static MaterialSpring Instant => new(1, 100) { IsInstant = true };
    internal MaterialSpring FastSpatial { get; private set; } = Instant;
    internal MaterialSpring DefaultSpatial { get; private set; } = Instant;
    internal MaterialSpring FastEffects { get; private set; } = Instant;
    internal MaterialSpring DefaultEffects { get; private set; } = Instant;
    internal MaterialSpring SlowEffects { get; private set; } = Instant;
    internal bool IsAttached { get; private set; }

    internal MaterialMotionSettings(Control owner, Action changed)
    {
        _owner = owner; _changed = changed;
        owner.AttachedToVisualTree += (_, _) => Attach();
        owner.DetachedFromVisualTree += (_, _) =>
        {
            IsAttached = false;
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions.Clear();
        };
        if (owner.IsAttachedToVisualTree()) Attach();
    }
    private void Attach()
    {
        IsAttached = true; _initializing = true;
        Observe("FastSpatial", value => FastSpatial = value);
        Observe("DefaultSpatial", value => DefaultSpatial = value);
        Observe("FastEffects", value => FastEffects = value);
        Observe("DefaultEffects", value => DefaultEffects = value);
        Observe("SlowEffects", value => SlowEffects = value);
        _initializing = false; _changed();
    }
    private void Observe(string name, Action<MaterialSpring> set) => _subscriptions.Add(
        _owner.GetResourceObservable("M3.Motion." + name).Subscribe(new Observer(value =>
        {
            set(value is MaterialSpring spring ? spring : Instant);
            if (!_initializing) _changed();
        })));
    private sealed class Observer(Action<object?> next) : IObserver<object?>
    {
        public void OnNext(object? value) => next(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}
