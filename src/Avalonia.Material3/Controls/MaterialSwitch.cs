using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Data;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Avalonia.Material3.Controls;

/// <summary>A Material switch retaining Avalonia's toggle binding, keyboard and drag behavior.</summary>
[PseudoClasses(":error", ":off-icon")]
public class MaterialSwitch : ToggleSwitch
{
    public static readonly StyledProperty<object?> OnIconProperty = AvaloniaProperty.Register<MaterialSwitch, object?>(nameof(OnIcon));
    public static readonly StyledProperty<object?> OffIconProperty = AvaloniaProperty.Register<MaterialSwitch, object?>(nameof(OffIcon));

    /// <summary>Optional decorative content in the selected 24 DIP handle; keep content within 16 DIP.</summary>
    public object? OnIcon { get => GetValue(OnIconProperty); set => SetValue(OnIconProperty, value); }
    /// <summary>Optional decorative content in the unselected handle, which grows to 24 DIP when supplied.</summary>
    public object? OffIcon { get => GetValue(OffIconProperty); set => SetValue(OffIconProperty, value); }

    public static readonly StyledProperty<bool> IsErrorProperty = MaterialCheckBox.IsErrorProperty.AddOwner<MaterialSwitch>();
    public static readonly StyledProperty<string?> ErrorTextProperty = MaterialCheckBox.ErrorTextProperty.AddOwner<MaterialSwitch>();

    /// <summary>Shows host validation feedback; M3 switch tokens do not define a separate error variant.</summary>
    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    /// <summary>Visible and accessible error explanation when IsError is true.</summary>
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    private IDisposable? _durationSubscription;
    private IDisposable? _easingSubscription;
    private TimeSpan _duration;
    private Easing _easing = new LinearEasing();
    private Panel? _moving;
    private Ellipse? _thumb;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _moving = e.NameScope.Find<Panel>("PART_MovingKnobs");
        _thumb = e.NameScope.Find<Ellipse>("Thumb");
        UpdateMotion();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _durationSubscription = this.GetResourceObservable("M3.Motion.DurationShort4").Subscribe(new MotionObserver(value =>
        {
            _duration = value is TimeSpan duration ? duration : TimeSpan.Zero;
            UpdateMotion();
        }));
        _easingSubscription = this.GetResourceObservable("M3.Motion.EasingStandard").Subscribe(new MotionObserver(value =>
        {
            _easing = value is Easing easing ? easing : new LinearEasing();
            UpdateMotion();
        }));
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _durationSubscription?.Dispose(); _durationSubscription = null;
        _easingSubscription?.Dispose(); _easingSubscription = null;
        if (_moving is not null) _moving.Transitions = null;
        if (_thumb is not null) _thumb.Transitions = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void UpdateMotion()
    {
        // Native ToggleSwitch removes/reinstates these transitions itself during thumb drag.
        // Clearing existing transition instances also snaps an in-flight live ReduceMotion change.
        var instant = IsPressed || !IsEffectivelyEnabled || _duration <= TimeSpan.Zero;
        var knobs = instant ? null : new Transitions { new DoubleTransition { Property = Canvas.LeftProperty, Duration = _duration, Easing = _easing } };
        if (instant && _moving is not null) _moving.Transitions = null;
        SetValue(KnobTransitionsProperty, knobs!, BindingPriority.Style);
        if (_thumb is not null)
        {
            _thumb.Transitions = null;
            if (!instant) _thumb.Transitions = new Transitions
            {
                new DoubleTransition { Property = WidthProperty, Duration = _duration, Easing = _easing },
                new DoubleTransition { Property = HeightProperty, Duration = _duration, Easing = _easing },
                new BrushTransition { Property = Shape.FillProperty, Duration = _duration, Easing = _easing }
            };
        }
    }
    private sealed class MotionObserver(Action<object?> update) : IObserver<object?>
    {
        public void OnNext(object? value) => update(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    protected override Type StyleKeyOverride => typeof(MaterialSwitch);
    protected override AutomationPeer OnCreateAutomationPeer() => new SelectionToggleAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsErrorProperty)
            PseudoClasses.Set(":error", IsError);
        else if (change.Property == OffIconProperty)
            PseudoClasses.Set(":off-icon", OffIcon is not null);
        if (change.Property == IsPressedProperty || change.Property == IsEnabledProperty)
            UpdateMotion();
    }

    static MaterialSwitch()
    {
        IsCheckedProperty.OverrideMetadata<MaterialSwitch>(new StyledPropertyMetadata<bool?>(false, coerce: (_, value) => value ?? false));
        IsThreeStateProperty.OverrideMetadata<MaterialSwitch>(new StyledPropertyMetadata<bool>(false, coerce: (_, _) => false));
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<MaterialSwitch>(AutomationControlType.CheckBox);
        OnContentProperty.OverrideDefaultValue<MaterialSwitch>(null);
        OffContentProperty.OverrideDefaultValue<MaterialSwitch>(null);
    }
}
