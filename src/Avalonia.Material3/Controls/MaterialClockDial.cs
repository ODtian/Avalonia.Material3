using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Data;
using Avalonia.Diagnostics;

namespace Avalonia.Material3.Controls;

public sealed class MaterialClockSelectionEventArgs(MaterialTimePickerPart part, int value, bool complete, bool cancelled = false) : EventArgs
{
    public MaterialTimePickerPart Part { get; } = part;
    public int Value { get; } = value;
    public bool Complete { get; } = complete;
    public bool Cancelled { get; } = cancelled;
}

/// <summary>An accessible clock mark retaining native button/toggle input and peer behavior.</summary>
public class MaterialClockNumber : ToggleButton
{
    public int Value { get; init; }
    protected override Type StyleKeyOverride => typeof(MaterialClockNumber);
}

/// <summary>Standard 256 DIP Material analog dial. Taps select hour/5-minute marks;
/// continuous pointer dragging and arrow keys select every minute. All visible marks are native actions.</summary>
public class MaterialClockDial : Panel
{
    public static readonly StyledProperty<int> ValueProperty = AvaloniaProperty.Register<MaterialClockDial, int>(nameof(Value), validate: v => v is >= 0 and <= 59);
    public static readonly StyledProperty<MaterialTimePickerPart> ActivePartProperty = AvaloniaProperty.Register<MaterialClockDial, MaterialTimePickerPart>(nameof(ActivePart), validate: Enum.IsDefined);
    public static readonly StyledProperty<bool> Is24HourProperty = AvaloniaProperty.Register<MaterialClockDial, bool>(nameof(Is24Hour));
    public static readonly StyledProperty<CultureInfo> CultureProperty = AvaloniaProperty.Register<MaterialClockDial, CultureInfo>(nameof(Culture), CultureInfo.InvariantCulture, validate: c => c is not null);
    public static readonly StyledProperty<IBrush?> SelectorBrushProperty = AvaloniaProperty.Register<MaterialClockDial, IBrush?>(nameof(SelectorBrush));
    public static readonly StyledProperty<IBrush?> DialBrushProperty = AvaloniaProperty.Register<MaterialClockDial, IBrush?>(nameof(DialBrush));
    public static readonly StyledProperty<string> ValueLabelProperty = AvaloniaProperty.Register<MaterialClockDial, string>(nameof(ValueLabel), "Hour");
    private bool _ready, _dragging, _ending;
    private IPointer? _pointer;
    private Point _start;
    private int _beforeValue;
    private MaterialTimePickerPart _beforePart;
    private readonly DialPaint _paint;
    private readonly MaterialMotionValue _angle, _faceAlpha;
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialFrameLease _confirmationFrames;
    private int? _confirmationValue;
    private double? _confirmationHold;
    private MaterialSnapshot? _oldFace;
    private bool _partTransition, _animateSelection, _capturingFace;
    public int Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public MaterialTimePickerPart ActivePart { get => GetValue(ActivePartProperty); set => SetValue(ActivePartProperty, value); }
    public bool Is24Hour { get => GetValue(Is24HourProperty); set => SetValue(Is24HourProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public IBrush? SelectorBrush { get => GetValue(SelectorBrushProperty); set => SetValue(SelectorBrushProperty, value); }
    public IBrush? DialBrush { get => GetValue(DialBrushProperty); set => SetValue(DialBrushProperty, value); }
    private double _diameter=256;
    private double FontScale => Math.Max(1,GetValue(TextBlock.FontSizeProperty)/16);
    private double NaturalSide => FontScale * _diameter;
    private Size _faceSize;
    private bool _arranged;
    private double FaceSide => _arranged ? Math.Max(0, Math.Min(_faceSize.Width, _faceSize.Height)) : NaturalSide;
    private double Scale => FaceSide / 256;
    private Point FaceOrigin => _arranged ? new((_faceSize.Width - FaceSide) / 2, (_faceSize.Height - FaceSide) / 2) : default;
    private Point FaceCenter => FaceOrigin + new Vector(128 * Scale, 128 * Scale);
    internal void SetDiameter(double diameter)
    {
        if(_diameter==diameter)return;
        _diameter=diameter;UpdateExtent();InvalidateMeasure();InvalidateArrange();_paint.InvalidateVisual();
    }
    private void UpdateExtent()
    {
        // Intrinsic extents sit below authored local values, bindings and styles.
        if (this.GetDiagnostic(WidthProperty).Priority == BindingPriority.Unset) SetCurrentValue(WidthProperty, NaturalSide);
        if (this.GetDiagnostic(HeightProperty).Priority == BindingPriority.Unset) SetCurrentValue(HeightProperty, NaturalSide);
    }
    public string ValueLabel { get => GetValue(ValueLabelProperty); set => SetValue(ValueLabelProperty, value); }
    public event EventHandler<MaterialClockSelectionEventArgs>? ValueSelected;
    public MaterialClockDial()
    {
        _paint = new DialPaint(this) { IsHitTestVisible = false };
        _confirmationFrames = MaterialRenderFrames.Bind(this, ConfirmSelection);
        _angle = new(this, -Math.PI / 2, _ =>
        {
            _paint.InvalidateVisual();
            foreach (var label in this.GetVisualDescendants().OfType<Themes.MaterialClockLabel>()) label.InvalidateVisual();
        });
        _faceAlpha = new(this, 1, value =>
        {
            foreach (var number in Children.OfType<MaterialClockNumber>()) number.Opacity = Math.Clamp(value, 0, 1);
            if (value >= 1) { _oldFace?.Dispose(); _oldFace = null; }
            _paint.InvalidateVisual();
        });
        _motion = new(this, () =>
        {
            UpdateAngle(_angle.IsRunning);
            if (_faceAlpha.IsRunning) _faceAlpha.Spring(1, _motion!.DefaultEffects);
        });
        UpdateExtent();
        Background = Brushes.Transparent;
        ClipToBounds = true;
        Bind(DialBrushProperty, this.GetResourceObservable("M3.SurfaceContainerHighestBrush"), Avalonia.Data.BindingPriority.Style);
        Bind(TextBlock.FontSizeProperty, this.GetResourceObservable("M3.BodyLargeFontSize"), Avalonia.Data.BindingPriority.Style);
        Bind(SelectorBrushProperty, this.GetResourceObservable("M3.PrimaryBrush"), Avalonia.Data.BindingPriority.Style);
        AddHandler(PointerPressedEvent, Pressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, Moved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, Released, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, CaptureLost);
        AddHandler(KeyDownEvent, DialKey);
        _ready = true; Rebuild();
    }
    private void Rebuild()
    {
        Children.Clear();
        Children.Add(_paint);
        var count = ActivePart == MaterialTimePickerPart.Hour && Is24Hour ? 24 : 12;
        for (var index = 0; index < count; index++)
        {
            var number = ActivePart == MaterialTimePickerPart.Minute ? index * 5 : Is24Hour ? index : index == 0 ? 12 : index;
            var action = new MaterialClockNumber { Value = number, Content = number.ToString("0", Culture) };
            AutomationProperties.SetName(action, number.ToString(Culture) + " " + ValueLabel);
            action.Click += (_, _) => CompleteNativeSelection(number);
            Children.Add(action);
        }
        UpdateSelection(); InvalidateMeasure();
    }
    private void UpdateSelection()
    {
        foreach (var number in Children.OfType<MaterialClockNumber>())
        {
            number.IsChecked = number.Value == (ActivePart == MaterialTimePickerPart.Hour && !Is24Hour ? (Value + 11) % 12 + 1 : Value);
            AutomationProperties.SetName(number, number.Value.ToString(Culture) + " " + ValueLabel);
        }
        AutomationProperties.SetItemStatus(this, Value.ToString(Culture) + " " + ValueLabel);
        _paint.InvalidateVisual();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var number in Children) number.Measure(new Size(48 * FontScale, 48 * FontScale));
        return new Size(NaturalSide, NaturalSide);
    }
    internal Point SelectorCenter => AnimatedPosition;
    internal double SelectorRadius => 24 * Scale;
    private Point Position(int number)
    {
        var index = ActivePart == MaterialTimePickerPart.Minute ? number / 5d : number % 12;
        var radius = ActivePart == MaterialTimePickerPart.Hour && Is24Hour && number >= 12 ? 69 : 101;
        var angle = index * Math.PI / 6 - Math.PI / 2;
        return FaceOrigin + new Vector((128 + radius * Math.Cos(angle)) * Scale, (128 + radius * Math.Sin(angle)) * Scale);
    }
    private double TargetAngle => (ActivePart == MaterialTimePickerPart.Minute ? Value / 60d : Value % 12 / 12d) * 2 * Math.PI - Math.PI / 2;
    private Point AnimatedPosition
    {
        get
        {
            var radius = ActivePart == MaterialTimePickerPart.Hour && Is24Hour && Value >= 12 ? 69 : 101;
            return FaceOrigin + new Vector((128 + radius * Math.Cos(_angle.Value)) * Scale, (128 + radius * Math.Sin(_angle.Value)) * Scale);
        }
    }
    private void UpdateAngle(bool animate)
    {
        if (_motion is null || _angle is null) return;
        var target = TargetAngle;
        while (_angle.Value - target > Math.PI) target += 2 * Math.PI;
        while (_angle.Value - target <= -Math.PI) target -= 2 * Math.PI;
        if (animate || _partTransition || _animateSelection) _angle.Spring(target, _motion.DefaultSpatial);
        else _angle.Snap(target);
    }
    private void CompleteNativeSelection(int value, bool tap = true)
    {
        CancelConfirmation();
        var part = ActivePart;
        _animateSelection = true;
        try { ValueSelected?.Invoke(this, new(part, value, !tap || part != MaterialTimePickerPart.Hour)); UpdateAngle(true); }
        finally { _animateSelection = false; }
        if (tap && part == MaterialTimePickerPart.Hour && ActivePart == part)
        {
            _confirmationValue = value;
            _confirmationFrames.Restart(); _confirmationFrames.SetRunning(true);
        }
    }
    private bool ConfirmSelection(MaterialFrame frame)
    {
        if (_confirmationValue is not { } value || ActivePart != MaterialTimePickerPart.Hour) return false;
        if (_angle.IsRunning) return true;
        _confirmationHold ??= frame.Elapsed.TotalSeconds;
        if (frame.Elapsed.TotalSeconds - _confirmationHold < .1) return true;
        _confirmationValue = null; _confirmationHold = null;
        ValueSelected?.Invoke(this, new(MaterialTimePickerPart.Hour, value, true));
        return false;
    }
    private void CancelConfirmation()
    {
        _confirmationValue = null; _confirmationHold = null; _confirmationFrames.SetRunning(false);
    }
    private void CaptureFace()
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || _motion.DefaultEffects.IsInstant)
        { _oldFace?.Dispose(); _oldFace = null; return; }
        MaterialSnapshot? face;
        _capturingFace = true;
        _paint.InvalidateVisual();
        try { face = MaterialSnapshot.Capture(this, new Rect(FaceOrigin, new Size(FaceSide, FaceSide))); } finally { _capturingFace = false; _paint.InvalidateVisual(); }
        _oldFace?.Dispose(); _oldFace = face;
    }
    internal void SetSelection(MaterialTimePickerPart part, int value, string label)
    {
        _partTransition = part != ActivePart;
        var changed = _partTransition || value != Value;
        try { ActivePart = part; Value = value; ValueLabel = label; if (changed) UpdateAngle(_partTransition || _animateSelection); }
        finally { _partTransition = false; }
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _faceSize = finalSize; _arranged = true;
        _paint.Arrange(new Rect(finalSize));
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var target = (int)Math.Floor(48 * FontScale * density + .5);
        var side = (int)Math.Floor(FaceSide * density + .5);
        var center = side / 2 - target / 2;
        var theta = (float)(Math.PI * 2) / 12;
        foreach (var number in Children.OfType<MaterialClockNumber>())
        {
            var index = ActivePart == MaterialTimePickerPart.Minute ? number.Value / 5 : number.Value % 12;
            var radius = (float)((ActivePart == MaterialTimePickerPart.Hour && Is24Hour && number.Value >= 12 ? 69 : 101) * Scale * density);
            // Native stores full-circle/theta/index multiplication as Float, then
            // subtracts the Double quarter-circle before cos/sin and roundToInt.
            var angle = theta * index - Math.PI / 2;
            // Compose CircularLayout integer-halves the measured face and target,
            // then rounds each polar offset once. Keep ink and mask on that frame.
            var left = Math.Floor(radius * Math.Cos(angle) + center + .5);
            var top = Math.Floor(radius * Math.Sin(angle) + center + .5);
            number.Arrange(new Rect(FaceOrigin.X + left / density, FaceOrigin.Y + top / density, target / density, target / density));
        }
        return finalSize;
    }
    private void Draw(DrawingContext context)
    {
        if (!_capturingFace)
        {
            var center = FaceCenter;
            context.DrawEllipse(DialBrush, null, center, 128 * Scale, 128 * Scale);
            var endpoint = AnimatedPosition;
            context.DrawLine(new Pen(SelectorBrush, 2), center, endpoint);
            context.DrawEllipse(SelectorBrush, null, center, 4 * FontScale, 4 * FontScale);
            context.DrawEllipse(SelectorBrush, null, endpoint, 24 * Scale, 24 * Scale);
        }
        if (_oldFace is not null && _faceAlpha.Value < 1)
        {
            using var opacity = context.PushOpacity(1 - Math.Clamp(_faceAlpha.Value, 0, 1));
            _oldFace.Draw(context, new Rect(FaceOrigin, new Size(FaceSide, FaceSide)));
        }
    }
    private int FromPoint(Point point, bool tap)
    {
        point = new Point((point.X - FaceOrigin.X) / Scale, (point.Y - FaceOrigin.Y) / Scale);
        var angle = (Math.Atan2(point.X - 128, 128 - point.Y) + Math.PI * 2) % (Math.PI * 2);
        var units = ActivePart == MaterialTimePickerPart.Minute ? 60 : 12;
        var number = (int)Math.Round(angle / (Math.PI * 2) * units) % units;
        if (ActivePart == MaterialTimePickerPart.Minute) return tap ? (int)Math.Round(number / 5d) * 5 % 60 : number;
        if (Is24Hour) return number + (new Vector(point.X - 128, point.Y - 128).Length < 74 ? 12 : 0);
        return number == 0 ? 12 : number;
    }
    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        // The dial owns radial input, not its ancestor's vertical panning recognizer.
        // Native button mouse taps remain untouched; this is Avalonia's public gesture arbitration seam.
        e.PreventGestureRecognition();
        if (_pointer is not null || !IsEffectivelyEnabled || e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        CancelConfirmation();
        _pointer = e.Pointer; _start = e.GetPosition(this); _beforeValue = Value; _beforePart = ActivePart;
        if (e.Pointer.Type != PointerType.Mouse || e.Source == this)
        {
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }
    private void Moved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer != _pointer || e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var position = e.GetPosition(this);
        if (!_dragging && new Vector(position.X - _start.X, position.Y - _start.Y).Length < 4) return;
        _dragging = true; e.Pointer.Capture(this);
        ValueSelected?.Invoke(this, new(ActivePart, FromPoint(e.GetPosition(this), false), false));
        var point = e.GetPosition(this);
        var angle = Math.Atan2(point.Y - FaceCenter.Y, point.X - FaceCenter.X);
        while (angle - _angle.Value > Math.PI) angle -= 2 * Math.PI;
        while (angle - _angle.Value <= -Math.PI) angle += 2 * Math.PI;
        _angle.Snap(angle);
        e.Handled = true;
    }
    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _pointer) return;
        var dragged = _dragging;
        var isBackground = e.Source is not Control ||
            e.Source is Control control && control is not MaterialClockNumber && !control.GetVisualAncestors().OfType<MaterialClockNumber>().Any();
        _ending = true;
        _pointer = null; _dragging = false;
        if (e.Pointer.Captured == this) e.Pointer.Capture(null);
        _ending = false;
        if (dragged || isBackground)
        {
            CompleteNativeSelection(FromPoint(e.GetPosition(this), !dragged), tap: !dragged);
            e.Handled = true;
        }
    }
    private void CaptureLost(object? sender, PointerCaptureLostEventArgs e)
    { if (!_ending && e.Source == this) CancelDrag(); }
    private void CancelDrag()
    {
        var pointer = _pointer; var dragged = _dragging;
        _pointer = null; _dragging = false; _ending = true; pointer?.Capture(null); _ending = false;
        if (dragged) ValueSelected?.Invoke(this, new(_beforePart, _beforeValue, false, true));
    }
    private void DialKey(object? sender, KeyEventArgs e)
    {
        var step = e.Key switch { Key.Right or Key.Up => 1, Key.Left or Key.Down => -1, _ => 0 };
        if (step == 0) return;
        var units = ActivePart == MaterialTimePickerPart.Minute ? 60 : Is24Hour ? 24 : 12;
        var next = (Value + step + units) % units;
        if (units == 12 && next == 0) next = 12;
        ValueSelected?.Invoke(this, new(ActivePart, next, false));
        e.Handled = true;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { CancelConfirmation(); CancelDrag(); _oldFace?.Dispose(); _oldFace = null; base.OnDetachedFromVisualTree(e); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!_ready) return;
        if (change.Property == WidthProperty || change.Property == HeightProperty) UpdateExtent();
        if (change.Property == IsEnabledProperty && !IsEnabled) { CancelConfirmation(); CancelDrag(); }
        if (change.Property == ActivePartProperty) { CancelConfirmation(); CaptureFace(); CancelDrag(); Rebuild(); _faceAlpha.Snap(0); _faceAlpha.Spring(1, _motion.DefaultEffects); UpdateAngle(true); }
        else if (change.Property == Is24HourProperty || change.Property == CultureProperty) { CancelDrag(); Rebuild(); }
        if (change.Property == ValueProperty) { if (!_animateSelection) CancelConfirmation(); UpdateSelection(); UpdateAngle(_partTransition || _animateSelection); }
        else if (change.Property == ValueLabelProperty) UpdateSelection();
        if (change.Property == SelectorBrushProperty || change.Property == DialBrushProperty) _paint.InvalidateVisual();
        if (change.Property == TextBlock.FontSizeProperty)
        { CancelDrag(); UpdateExtent(); InvalidateMeasure(); _paint.InvalidateVisual(); }
    }
    private sealed class DialPaint(MaterialClockDial owner) : Control
    {
        public override void Render(DrawingContext context) => owner.Draw(context);
    }
}

public class MaterialTimeSelector : MaterialButton
{
    public MaterialTimeSelector() { IsToggle = true; }
    protected override Type StyleKeyOverride => typeof(MaterialTimeSelector);
}
public class MaterialTimePeriodButton : MaterialButton
{
    public MaterialTimePeriodButton() { IsToggle = true; }
    protected override Type StyleKeyOverride => typeof(MaterialTimePeriodButton);
}
