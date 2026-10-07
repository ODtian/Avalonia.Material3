using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Collections.Specialized;

namespace Avalonia.Material3.Controls;

// Common and Android patterned press recipes share the independent state-layer lifecycle.
internal sealed class MaterialStateLayer : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<MaterialStateLayer>();
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<MaterialStateLayer>();
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    private sealed class Ripple(Point origin, double start, double startRadius, double endRadius, double noiseStart, IPointer? pointer, Key? key)
    {
        internal Point Origin = origin;
        internal double Start = start, StartRadius = startRadius, EndRadius = endRadius;
        internal double? Released;
        internal readonly double NoiseStart = noiseStart;
        internal readonly IPointer? Pointer = pointer;
        internal readonly Key? Key = key;
    }
    private readonly MaterialFrameLease _frames;
    private readonly List<Ripple> _ripples = [];
    private Control? _owner;
    private double _alpha, _fromAlpha, _toAlpha, _stateStart, _stateDuration;
    private double _hover = .08, _focus = .10, _press = .10, _drag = .16;
    private bool _reduced;
    private bool _wasDragging;
    private MaterialRippleStyle _themeStyle, _style;
    private bool _initializing;
    private readonly List<IDisposable> _resources = [];
    private static readonly Avalonia.Animation.Easings.SplineEasing RadiusEasing = new(.4, 0, .2, 1);
    static MaterialStateLayer() => AffectsRender<MaterialStateLayer>(BackgroundProperty, CornerRadiusProperty);
    public MaterialStateLayer()
    {
        IsHitTestVisible = false;
        Opacity = 1;
        _frames = MaterialRenderFrames.Bind(this, Advance);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Opacity = 1;
        Transitions = null;
        _owner = TemplatedParent as Control ?? this.GetVisualAncestors().OfType<Avalonia.Controls.Primitives.TemplatedControl>().FirstOrDefault();
        if (_owner is null) return;
        _owner.PropertyChanged += OwnerChanged;
        _owner.Classes.CollectionChanged += ClassesChanged;
        _owner.AddHandler(PointerPressedEvent, Pressed, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(PointerReleasedEvent, Released, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(PointerCaptureLostEvent, CaptureLost, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(KeyDownEvent, KeyPressed, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(KeyUpEvent, KeyReleased, RoutingStrategies.Tunnel, true);
        _initializing = true;
        _alpha = _fromAlpha = _toAlpha = _stateDuration = 0;
        Observe("HoverStateLayerOpacity", value => _hover = value is double alpha ? alpha : .08);
        Observe("FocusStateLayerOpacity", value => _focus = value is double alpha ? alpha : .10);
        Observe("PressedStateLayerOpacity", value => _press = value is double alpha ? alpha : .10);
        Observe("DraggedStateLayerOpacity", value => _drag = value is double alpha ? alpha : .16);
        Observe("ReduceMotion", value => { _reduced = value is true; UpdateState(); });
        Observe("RippleStyle", value => { _themeStyle = value is MaterialRippleStyle style ? style : MaterialRippleStyle.Solid; UpdateStyle(); });
        _initializing = false;
        UpdateState();
        InvalidateVisual();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null)
        {
            _owner.PropertyChanged -= OwnerChanged;
            _owner.Classes.CollectionChanged -= ClassesChanged;
            _owner.RemoveHandler(PointerPressedEvent, Pressed); _owner.RemoveHandler(PointerReleasedEvent, Released);
            _owner.RemoveHandler(PointerCaptureLostEvent, CaptureLost);
            _owner.RemoveHandler(KeyDownEvent, KeyPressed); _owner.RemoveHandler(KeyUpEvent, KeyReleased);
        }
        _owner = null;
        foreach (var subscription in _resources) subscription.Dispose();
        _resources.Clear(); _ripples.Clear(); _frames.SetRunning(false);
        InvalidateVisual();
        base.OnDetachedFromVisualTree(e);
    }
    private void Observe(string key, Action<object?> change) => _resources.Add(
        this.GetResourceObservable("M3." + key).Subscribe(new Observer(value => { change(value); UpdateState(); })));
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialRipple.StyleProperty) UpdateStyle();
        if (change.Property == IsPointerOverProperty || change.Property == IsKeyboardFocusWithinProperty ||
            change.Property == IsEffectivelyEnabledProperty || change.Property == Button.IsPressedProperty
            || change.Property == MaterialContentItem.IsInteractiveProperty) UpdateState();
    }
    private void UpdateStyle()
    {
        var style = _owner?.GetValue(MaterialRipple.StyleProperty) ?? _themeStyle;
        if (_style == style) return;
        if (style == MaterialRippleStyle.Patterned) MaterialPatternedRippleDraw.Prepare();
        _style = style; _ripples.Clear(); UpdateState(); InvalidateVisual();
    }
    private void ClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateState();
    private void UpdateState()
    {
        if (_owner is null || _initializing) return;
        var enabled = _owner.IsEffectivelyEnabled && (_owner is not MaterialContentItem item || item.IsInteractive);
        var dragging = _owner.Classes.Contains(":dragging") || _owner.Classes.Contains(":dragged") || _owner.Classes.Contains(":reordering");
        var focused = _owner.Classes.Contains(":focus-visible");
        var target = !enabled ? 0 : dragging ? _drag : focused ? _focus : _owner.IsPointerOver ? _hover : 0;
        if (target == _toAlpha && !_reduced && enabled) return;
        _frames.Sample(); _fromAlpha = _alpha; _toAlpha = target;
        _stateStart = _frames.Elapsed.TotalSeconds;
        _stateDuration = !enabled || _reduced ? 0 : dragging || focused ? .045 : _wasDragging ? .150 : .015;
        _wasDragging = dragging;
        if (_stateDuration == 0) { _alpha = target; if (!enabled) _ripples.Clear(); }
        _frames.SetRunning(true); InvalidateVisual();
    }
    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (!OwnsInput(e.Source) || _owner?.IsEffectivelyEnabled != true || _owner is MaterialContentItem { IsInteractive: false } || !e.GetCurrentPoint(_owner).Properties.IsLeftButtonPressed) return;
        AddRipple(e.GetPosition(this), e.Pointer);
    }
    private void Released(object? sender, PointerReleasedEventArgs e) => FinishRipples(e.Pointer);
    private void CaptureLost(object? sender, PointerCaptureLostEventArgs e) => FinishRipples(e.Pointer);
    private bool OwnsInput(object? source)
    {
        var input = (source as Visual)?.GetSelfAndVisualAncestors().OfType<Control>()
            .FirstOrDefault(control => control is Button or ToggleSwitch or TextBox or Slider);
        return input is null || input == _owner;
    }
    private void KeyPressed(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter && OwnsInput(e.Source) && _owner?.IsEffectivelyEnabled == true &&
            _owner is not MaterialContentItem { IsInteractive: false } && !_ripples.Any(ripple => ripple.Key == e.Key && !ripple.Released.HasValue))
            AddRipple(new Point(Bounds.Width / 2, Bounds.Height / 2), key: e.Key);
    }
    private void KeyReleased(object? sender, KeyEventArgs e) { if (e.Key is Key.Space or Key.Enter) FinishRipples(key: e.Key); }
    private void AddRipple(Point origin, IPointer? pointer = null, Key? key = null)
    {
        _frames.Sample();
        var time = _frames.Elapsed.TotalSeconds;
        foreach (var ripple in _ripples.Where(ripple => ripple.Pointer == pointer && ripple.Key == key)) ripple.Released ??= time;
        var size = Bounds.Size;
        var unbounded = _owner is MaterialCheckBox or MaterialRadioButton or MaterialSwitch;
        var end = unbounded ? 20 : Math.Sqrt(size.Width * size.Width + size.Height * size.Height) / 2 + 10;
        if (unbounded) origin = new Point(size.Width / 2, size.Height / 2);
        if (_ripples.Count >= 11) _ripples.RemoveAt(0);
        _ripples.Add(new(origin, time, unbounded ? 7.2 : Math.Max(size.Width, size.Height) * .3, end,
            Environment.TickCount64, pointer, key));
        _frames.SetRunning(true); InvalidateVisual();
    }
    private void FinishRipples(IPointer? pointer = null, Key? key = null)
    {
        _frames.Sample();
        foreach (var ripple in _ripples.Where(ripple => ripple.Pointer == pointer && ripple.Key == key)) ripple.Released ??= _frames.Elapsed.TotalSeconds;
        _frames.SetRunning(true); InvalidateVisual();
    }
    private bool Advance(MaterialFrame frame)
    {
        var time = frame.Elapsed.TotalSeconds;
        var stateFraction = _stateDuration == 0 ? 1 : Math.Clamp((time - _stateStart) / _stateDuration, 0, 1);
        _alpha = _fromAlpha + (_toAlpha - _fromAlpha) * stateFraction;
        var enter = _style == MaterialRippleStyle.Patterned ? .450 : .225;
        var exit = _style == MaterialRippleStyle.Patterned ? .375 : .150;
        _ripples.RemoveAll(ripple => ripple.Released is { } released && (_reduced || time >= Math.Max(released, ripple.Start + enter) + exit));
        InvalidateVisual();
        return stateFraction < 1 || _ripples.Any(ripple => ripple.Released.HasValue || !_reduced && time < ripple.Start + (_style == MaterialRippleStyle.Patterned ? 7 : .225));
    }
    public override void Render(DrawingContext context)
    {
        if (Background is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var rect = new Rect(Bounds.Size);
        using var clip = context.PushClip(new RoundedRect(rect, CornerRadius));
        if (_alpha > 0) using (context.PushOpacity(_alpha)) context.DrawRectangle(Background, null, rect);
        var time = _frames.Elapsed.TotalSeconds;
        foreach (var ripple in _ripples)
        {
            var age = Math.Max(0, time - ripple.Start);
            if (_style == MaterialRippleStyle.Patterned && Background is ISolidColorBrush color)
            {
                var progress = _reduced ? ripple.Released.HasValue ? 1 : .5 : ripple.Released is { } patternedReleased && time >= Math.Max(patternedReleased, ripple.Start + .450)
                    ? .5 + .5 * Math.Clamp((time - Math.Max(patternedReleased, ripple.Start + .450)) / .375, 0, 1)
                    : .5 * RadiusEasing.Ease(Math.Clamp(age / .450, 0, 1));
                var alphaByte = (byte)Math.Clamp(Math.Round(color.Color.A * color.Opacity * _press, MidpointRounding.AwayFromZero), 0, 255);
                var noiseFrom = (float)ripple.NoiseStart; var noiseTo = (float)(ripple.NoiseStart + 32);
                var noise = noiseFrom + (noiseTo - noiseFrom) * (float)Math.Clamp(age / 7, 0, 1);
                context.Custom(new MaterialPatternedRippleDraw(rect, ripple.Origin, ripple.EndRadius, progress,
                    noise, Color.FromArgb(alphaByte, color.Color.R, color.Color.G, color.Color.B),
                    TopLevel.GetTopLevel(this)?.RenderScaling ?? 1));
                continue;
            }
            var radiusFraction = _reduced ? 1 : Math.Clamp(age / .225, 0, 1);
            var alpha = _reduced ? 1 : Math.Clamp(age / .075, 0, 1);
            if (ripple.Released is { } released)
            {
                if (age < .225) alpha = 1;
                else alpha *= _reduced ? 0 : 1 - Math.Clamp((time - Math.Max(released, ripple.Start + .225)) / .15, 0, 1);
            }
            var eased = RadiusEasing.Ease(radiusFraction);
            var radius = ripple.StartRadius + (ripple.EndRadius - ripple.StartRadius) * eased;
            var center = new Point(ripple.Origin.X + (rect.Center.X - ripple.Origin.X) * radiusFraction,
                ripple.Origin.Y + (rect.Center.Y - ripple.Origin.Y) * radiusFraction);
            using (context.PushOpacity(_press * alpha)) context.DrawEllipse(Background, null, center, radius, radius);
        }
    }
    private sealed class Observer(Action<object?> next) : IObserver<object?>
    {
        public void OnNext(object? value) => next(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}
