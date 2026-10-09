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
    private static readonly StyledProperty<IBrush?> SelectedInkProperty = AvaloniaProperty.Register<MaterialClockDial, IBrush?>("SelectedInk");
    private static readonly StyledProperty<IBrush?> NormalInkProperty = AvaloniaProperty.Register<MaterialClockDial, IBrush?>("NormalInk");
    private readonly MaterialMotionValue _angle, _faceAlpha;
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialFrameLease _confirmationFrames;
    private readonly MaterialFrameLease _textOptionsFrames;
    private int? _confirmationValue;
    private double? _confirmationHold;
    private MaterialSnapshot? _oldFace;
    private MaterialNativeText.GlyphPaint[]? _oldGlyphs;
    private Rect _oldGlyphBounds;
    private bool _oldFaceIsText, _capturingNormalFace;
    internal bool CapturingNormalFace => _capturingNormalFace;
    private bool _partTransition, _animateSelection, _capturingFace;
    private readonly List<AvaloniaObject> _paintBrushes = [];
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
        _textOptionsFrames = MaterialRenderFrames.Bind(this, ReconcileTextOptions, ignoreOwnerEnabled: true);
        _angle = new(this, -(float)(Math.PI * 2) / 4f, _ =>
        {
            _paint.InvalidateVisual();
            foreach (var label in this.GetVisualDescendants().OfType<Themes.MaterialClockLabel>()) label.InvalidateVisual();
        });
        _faceAlpha = new(this, 1, value =>
        {
            foreach (var number in Children.OfType<MaterialClockNumber>()) number.Opacity = Math.Clamp(value, 0, 1);
            if (value >= 1) ClearRetiringFace();
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
        MaterialPickerSupport.Resource(this, SelectedInkProperty, "OnPrimaryBrush");
        MaterialPickerSupport.Resource(this, NormalInkProperty, "OnSurfaceBrush");
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
    private bool _compositeDrawn;
    internal bool NativeSelectorComposition => _compositeDrawn && !_capturingFace;
    private Point Position(int number)
    {
        var index = ActivePart == MaterialTimePickerPart.Minute ? number / 5d : number % 12;
        var radius = ActivePart == MaterialTimePickerPart.Hour && Is24Hour && number >= 12 ? 69 : 101;
        var angle = index * Math.PI / 6 - Math.PI / 2;
        return FaceOrigin + new Vector((128 + radius * Math.Cos(angle)) * Scale, (128 + radius * Math.Sin(angle)) * Scale);
    }
    private double TargetAngle => ActivePart == MaterialTimePickerPart.Minute
        ? (float)(Math.PI * 2) / 60f * Value - (float)(Math.PI * 2) / 4f
        : (float)(Math.PI * 2) / 12f * (Value % 12) - (float)(Math.PI * 2) / 4f;
    private Point AnimatedPosition
    {
        get
        {
            var diameter = (float)FaceSide;
            var handleRadius = 24f * (diameter / 256f);
            var ratio = ActivePart == MaterialTimePickerPart.Hour && Is24Hour && Value >= 12 ? 69f / 256f : 101f / 256f;
            var length = Math.Max(0, diameter * ratio - handleRadius) + handleRadius;
            var angle = (float)_angle.Value;
            return FaceOrigin + new Vector(length * (float)Math.Cos(angle) + diameter / 2f,
                length * (float)Math.Sin(angle) + diameter / 2f);
        }
    }
    private void UpdateAngle(bool animate)
    {
        if (_motion is null || _angle is null) return;
        var target = TargetAngle;
        if (animate || _partTransition || _animateSelection)
        {
            var circle = (float)(Math.PI * 2);
            while (_angle.Value - target > circle / 2f) target += circle;
            while (_angle.Value - target <= -circle / 2f) target -= circle;
            _angle.Spring(target, _motion.DefaultSpatial);
        }
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
        { ClearRetiringFace(); return; }
        var region = new Rect(FaceOrigin, new Size(FaceSide, FaceSide));
        if (GetValue(SelectedInkProperty) is ISolidColorBrush { Opacity: 1, Color.A: 255 } selected
            && CaptureGlyphs(selected.Color, Math.Clamp(_faceAlpha.Value, 0, 1)) is { } current)
        {
            var captured = new List<MaterialNativeText.GlyphPaint>();
            if (_oldGlyphs is { } previous && _faceAlpha.Value < 1)
                foreach (var glyph in previous) captured.Add(new CapturedGlyph(glyph.Retain(), _oldGlyphBounds, region, 1 - _faceAlpha.Value));
            foreach (var glyph in current) captured.Add(new CapturedGlyph(glyph, region, region, _faceAlpha.Value));
            ClearRetiringFace(); _oldGlyphs = captured.ToArray(); _oldGlyphBounds = region;
            return;
        }
        MaterialSnapshot? face;
        var retained = _oldGlyphs?.Select(glyph => (MaterialNativeText.GlyphPaint)new CapturedGlyph(glyph.Retain(), _oldGlyphBounds, region, 1 - _faceAlpha.Value)).ToArray();
        _capturingFace = true;
        _capturingNormalFace = this.GetVisualDescendants().OfType<Themes.MaterialClockLabel>().Count() == Children.OfType<MaterialClockNumber>().Count();
        var text = _capturingNormalFace;
        _paint.InvalidateVisual();
        try { face = MaterialSnapshot.Capture(this, region); }
        catch { if (retained is not null) foreach (var glyph in retained) glyph.Dispose(); throw; }
        finally { _capturingFace = false; _capturingNormalFace = false; _paint.InvalidateVisual(); }
        ClearRetiringFace(); _oldFace = face; _oldFaceIsText = text;
        _oldGlyphs = retained; _oldGlyphBounds = region;
    }
    private void ClearRetiringFace()
    {
        _oldFace?.Dispose(); _oldFace = null;
        if (_oldGlyphs is { } glyphs) foreach (var glyph in glyphs) glyph.Dispose();
        _oldGlyphs = null; _oldFaceIsText = false;
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
        _compositeDrawn = false;
        if (!_capturingFace)
        {
            var center = FaceCenter;
            if (TryComposite(context)) _compositeDrawn = true;
            else if (!DrawRetiringGlyphs(context))
            {
                context.DrawEllipse(DialBrush, null, center, 128 * Scale, 128 * Scale);
                var endpoint = AnimatedPosition;
                context.DrawLine(new Pen(SelectorBrush, 2), center, endpoint);
                context.DrawEllipse(SelectorBrush, null, center, 4, 4);
                context.DrawEllipse(SelectorBrush, null, endpoint, 24 * Scale, 24 * Scale);
            }
        }
        if (_oldFace is not null && _faceAlpha.Value < 1)
        {
            using var opacity = context.PushOpacity(1 - Math.Clamp(_faceAlpha.Value, 0, 1));
            var region = new Rect(FaceOrigin, new Size(FaceSide, FaceSide));
            if (!_oldFaceIsText || _capturingNormalFace) _oldFace.Draw(context, region);
            else
            {
                var selector = new EllipseGeometry(new Rect(AnimatedPosition - new Vector(SelectorRadius, SelectorRadius), new Size(SelectorRadius * 2, SelectorRadius * 2)));
                var outside = new GeometryGroup { FillRule = FillRule.EvenOdd, Children = { new RectangleGeometry(region), selector } };
                using (context.PushGeometryClip(outside)) _oldFace.Draw(context, region);
                using (context.PushGeometryClip(new EllipseGeometry(selector.Rect)))
                using (_oldFace.OpacityMask(context, region)) context.DrawRectangle(GetValue(SelectedInkProperty), null, region);
            }
        }
    }
    private bool DrawRetiringGlyphs(DrawingContext context)
    {
        if (_oldGlyphs is not { } glyphs || _faceAlpha.Value >= 1
            || DialBrush is not ISolidColorBrush { Opacity: 1, Color.A: 255 } background
            || SelectorBrush is not ISolidColorBrush { Opacity: 1, Color.A: 255 } primary
            || GetValue(SelectedInkProperty) is not ISolidColorBrush { Opacity: 1, Color.A: 255 } selected) return false;
        var region = new Rect(FaceOrigin, new Size(FaceSide, FaceSide));
        var retained = glyphs.Select(glyph => (MaterialNativeText.GlyphPaint)new CapturedGlyph(glyph.Retain(), _oldGlyphBounds, region, 1 - _faceAlpha.Value)).ToArray();
        context.Custom(new MaterialClockCompositeDraw(new Rect(Bounds.Size), FaceCenter, FaceSide / 2, AnimatedPosition, SelectorRadius,
            (float)_angle.Value, 2, 4, background.Color, primary.Color, selected.Color, TopLevel.GetTopLevel(this)?.RenderScaling ?? 1, retained));
        return true;
    }
    private bool TryComposite(DrawingContext context)
    {
        if (_oldFace is not null) return false;
        if (DialBrush is not ISolidColorBrush { Opacity: 1, Color.A: 255 } background
            || SelectorBrush is not ISolidColorBrush { Opacity: 1, Color.A: 255 } primary
            || GetValue(SelectedInkProperty) is not ISolidColorBrush { Opacity: 1, Color.A: 255 } selected) return false;
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var alpha = Math.Clamp(_faceAlpha.Value, 0, 1);
        if (CaptureGlyphs(selected.Color, alpha) is not { } incoming) return false;
        var region = new Rect(FaceOrigin, new Size(FaceSide, FaceSide));
        var glyphs = new List<MaterialNativeText.GlyphPaint>();
        if (_oldGlyphs is { } outgoing && alpha < 1)
            foreach (var glyph in outgoing) glyphs.Add(new CapturedGlyph(glyph.Retain(), _oldGlyphBounds, region, 1 - alpha));
        foreach (var glyph in incoming) glyphs.Add(alpha == 1 ? glyph : new CapturedGlyph(glyph, region, region, alpha));
        context.Custom(new MaterialClockCompositeDraw(new Rect(Bounds.Size), FaceCenter, FaceSide / 2, AnimatedPosition, SelectorRadius,
            (float)_angle.Value, 2, 4, background.Color, primary.Color, selected.Color, density, glyphs.ToArray()));
        return true;
    }
    private MaterialNativeText.GlyphPaint[]? CaptureGlyphs(Color selected, double alpha)
    {
        var labels = this.GetVisualDescendants().OfType<Themes.MaterialClockLabel>().ToArray();
        if (labels.Length != Children.OfType<MaterialClockNumber>().Count()) return null;
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var glyphs = new List<MaterialNativeText.GlyphPaint>();
        foreach (var label in labels)
        {
            if (label.CreateGlyphPaint(this, density, selected, alpha) is not { } glyph)
            {
                foreach (var captured in glyphs) captured.Dispose();
                return null;
            }
            var number = label.GetVisualAncestors().OfType<MaterialClockNumber>().First();
            var brush = number.Foreground as ISolidColorBrush;
            var usesRole = ReferenceEquals(number.Foreground, GetValue(NormalInkProperty));
            Color? Colour()
            {
                var value = usesRole ? GetValue(NormalInkProperty) as ISolidColorBrush : brush;
                if (value is null) return null;
                var colour = value.Color;
                return Color.FromArgb((byte)Math.Clamp(Math.Round(colour.A * value.Opacity), 0, 255), colour.R, colour.G, colour.B);
            }
            glyphs.Add(new RoleGlyph(glyph, Colour, Colour()));
        }
        return glyphs.ToArray();
    }
    private sealed class CapturedGlyph(MaterialNativeText.GlyphPaint glyph, Rect source, Rect destination, double alpha) : MaterialNativeText.GlyphPaint
    {
        public void Dispose() => glyph.Dispose();
        public MaterialNativeText.GlyphPaint Retain() => new CapturedGlyph(glyph.Retain(), source, destination, alpha);
        public void Paint(SkiaSharp.SKCanvas canvas, double opacity, double density, Color? normal = null)
        {
            var saved = canvas.Save();
            try
            {
                canvas.Translate((float)destination.X, (float)destination.Y);
                canvas.Scale((float)(destination.Width / source.Width), (float)(destination.Height / source.Height));
                canvas.Translate(-(float)source.X, -(float)source.Y);
                glyph.Paint(canvas, opacity * alpha, density, normal);
            }
            finally { canvas.RestoreToCount(saved); }
        }
    }
    private sealed class RoleGlyph(MaterialNativeText.GlyphPaint glyph, Func<Color?> colour, Color? recorded) : MaterialNativeText.GlyphPaint
    {
        public void Dispose() => glyph.Dispose();
        public MaterialNativeText.GlyphPaint Retain() => new RoleGlyph(glyph.Retain(), colour, colour());
        public void Paint(SkiaSharp.SKCanvas canvas, double opacity, double density, Color? normal = null)
            => glyph.Paint(canvas, opacity, density, normal ?? recorded);
    }
    internal void InvalidateComposite()
    {
        _paint.InvalidateVisual();
        foreach (var label in this.GetVisualDescendants().OfType<Themes.MaterialClockLabel>()) label.InvalidateVisual();
    }
    private void WatchPaintBrushes()
    {
        foreach (var brush in _paintBrushes) brush.PropertyChanged -= PaintBrushChanged;
        _paintBrushes.Clear();
        if (!this.IsAttachedToVisualTree()) return;
        foreach (var brush in new[] { DialBrush, SelectorBrush, GetValue(SelectedInkProperty), GetValue(NormalInkProperty) }.OfType<AvaloniaObject>().Distinct())
        { _paintBrushes.Add(brush); brush.PropertyChanged += PaintBrushChanged; }
    }
    private void PaintBrushChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { InvalidateComposite(); }
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
    private bool ReconcileTextOptions(MaterialFrame frame)
    {
        var inherited = new TextOptions();
        foreach (var visual in this.GetVisualAncestors().Reverse().Append(this))
            inherited = TextOptions.GetTextOptions(visual).MergeWith(inherited);
        foreach (var label in this.GetVisualDescendants().OfType<Themes.MaterialClockLabel>())
            label.ReconcileTextOptions(inherited);
        return true;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        WatchPaintBrushes();
        _textOptionsFrames.Restart(); _textOptionsFrames.SetRunning(true);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { foreach (var brush in _paintBrushes) brush.PropertyChanged -= PaintBrushChanged; _paintBrushes.Clear(); _textOptionsFrames.SetRunning(false); CancelConfirmation(); CancelDrag(); ClearRetiringFace(); base.OnDetachedFromVisualTree(e); }
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
        if (change.Property == SelectorBrushProperty || change.Property == DialBrushProperty || change.Property == SelectedInkProperty || change.Property == NormalInkProperty)
        { WatchPaintBrushes(); InvalidateComposite(); }
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
