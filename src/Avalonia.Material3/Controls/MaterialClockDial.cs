using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

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
    public int Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public MaterialTimePickerPart ActivePart { get => GetValue(ActivePartProperty); set => SetValue(ActivePartProperty, value); }
    public bool Is24Hour { get => GetValue(Is24HourProperty); set => SetValue(Is24HourProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public IBrush? SelectorBrush { get => GetValue(SelectorBrushProperty); set => SetValue(SelectorBrushProperty, value); }
    public IBrush? DialBrush { get => GetValue(DialBrushProperty); set => SetValue(DialBrushProperty, value); }
    private double Scale => Math.Max(1, GetValue(TextBlock.FontSizeProperty) / 16);
    public string ValueLabel { get => GetValue(ValueLabelProperty); set => SetValue(ValueLabelProperty, value); }
    public event EventHandler<MaterialClockSelectionEventArgs>? ValueSelected;
    public MaterialClockDial()
    {
        _paint = new DialPaint(this) { IsHitTestVisible = false };
        Width = Height = 256;
        Background = Brushes.Transparent;
        MaterialPickerSupport.Resource(this, DialBrushProperty, "SurfaceContainerHighestBrush");
        MaterialPickerSupport.Resource(this, TextBlock.FontSizeProperty, "BodyLargeFontSize");
        MaterialPickerSupport.Resource(this, SelectorBrushProperty, "PrimaryBrush");
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
            var action = new MaterialClockNumber { Value = number, Content = number.ToString(ActivePart == MaterialTimePickerPart.Minute ? "00" : "0", Culture) };
            AutomationProperties.SetName(action, number.ToString(Culture) + " " + ValueLabel);
            action.Click += (_, _) => ValueSelected?.Invoke(this, new(ActivePart, number, true));
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
        foreach (var number in Children) number.Measure(new Size(48 * Scale, 48 * Scale));
        return new Size(256 * Scale, 256 * Scale);
    }
    internal Point SelectorCenter => Position(Value);
    internal double SelectorRadius => 24 * Scale;
    private Point Position(int number)
    {
        var index = ActivePart == MaterialTimePickerPart.Minute ? number / 5d : number % 12;
        var radius = ActivePart == MaterialTimePickerPart.Hour && Is24Hour && number >= 12 ? 69 : 101;
        var angle = index * Math.PI / 6 - Math.PI / 2;
        return new Point((128 + radius * Math.Cos(angle)) * Scale, (128 + radius * Math.Sin(angle)) * Scale);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _paint.Arrange(new Rect(0, 0, 256 * Scale, 256 * Scale));
        foreach (var number in Children.OfType<MaterialClockNumber>())
        {
            var center = Position(number.Value);
            number.Arrange(new Rect(center.X - 24 * Scale, center.Y - 24 * Scale, 48 * Scale, 48 * Scale));
        }
        return finalSize;
    }
    private void Draw(DrawingContext context)
    {
        var center = new Point(128 * Scale, 128 * Scale);
        context.DrawEllipse(DialBrush, null, center, 128 * Scale, 128 * Scale);
        var endpoint = Position(Value);
        context.DrawLine(new Pen(SelectorBrush, 2), center, endpoint);
        context.DrawEllipse(SelectorBrush, null, center, 4 * Scale, 4 * Scale);
        context.DrawEllipse(SelectorBrush, null, endpoint, 24 * Scale, 24 * Scale);
    }
    private int FromPoint(Point point, bool tap)
    {
        point = new Point(point.X / Scale, point.Y / Scale);
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
            ValueSelected?.Invoke(this, new(ActivePart, FromPoint(e.GetPosition(this), !dragged), true));
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
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { CancelDrag(); base.OnDetachedFromVisualTree(e); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!_ready) return;
        if (change.Property == IsEnabledProperty && !IsEnabled) CancelDrag();
        if (change.Property == ActivePartProperty || change.Property == Is24HourProperty || change.Property == CultureProperty) { CancelDrag(); Rebuild(); }
        if (change.Property == ValueProperty || change.Property == ValueLabelProperty) UpdateSelection();
        if (change.Property == SelectorBrushProperty || change.Property == DialBrushProperty) _paint.InvalidateVisual();
        if (change.Property == TextBlock.FontSizeProperty)
        { CancelDrag(); Width = Height = 256 * Scale; InvalidateMeasure(); _paint.InvalidateVisual(); }
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
