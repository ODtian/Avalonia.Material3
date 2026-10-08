using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// A reveal mask overlays the caller's current clip and releases without rewriting it.
internal sealed class MaterialClipLease : IObservable<Geometry?>, IDisposable
{
    private readonly Control _child;
    private readonly IDisposable _binding;
    private readonly List<IObserver<Geometry?>> _observers = [];
    private Geometry _mask;
    internal Control Child => _child;
    internal MaterialClipLease(Control child, Geometry mask)
    {
        _child = child; _mask = mask;
        child.PropertyChanged += Changed;
        _binding = child.Bind(Visual.ClipProperty, this, BindingPriority.Animation);
    }
    internal void Update(Geometry mask) { _mask = mask; Publish(); }
    private void Changed(object? sender, AvaloniaPropertyChangedEventArgs change)
    { if (change.Property == Visual.ClipProperty && change.Priority != BindingPriority.Animation) Publish(); }
    private Geometry Clip()
    {
        var authored = _child.GetBaseValue(Visual.ClipProperty).GetValueOrDefault();
        return authored is null ? _mask : new CombinedGeometry(GeometryCombineMode.Intersect, authored, _mask);
    }
    private void Publish()
    { var value = Clip(); foreach (var observer in _observers) observer.OnNext(value); }
    public IDisposable Subscribe(IObserver<Geometry?> observer)
    { _observers.Add(observer); observer.OnNext(Clip()); return new Subscription(() => _observers.Remove(observer)); }
    public void Dispose() { _child.PropertyChanged -= Changed; _binding.Dispose(); }
    private sealed class Subscription(Action dispose) : IDisposable
    { public void Dispose() => dispose(); }
}
