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

    private readonly MaterialMotionSettings _motion;
    private readonly MaterialMotionValue _travel;
    private readonly MaterialMotionValue _size;
    private Panel? _moving;
    private Ellipse? _thumb;
    private bool _painting;
    public MaterialSwitch()
    {
        _travel = new(this, 0, value =>
        {
            if (_moving is null) return;
            _painting = true;
            try { Canvas.SetLeft(_moving, value); }
            finally { _painting = false; }
        });
        _size = new(this, 16, value => { if (_thumb is not null) _thumb.Width = _thumb.Height = value; });
        _motion = new(this, UpdateMotion);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_moving is not null) _moving.PropertyChanged -= MovingChanged;
        _moving = e.NameScope.Find<Panel>("PART_MovingKnobs");
        _thumb = e.NameScope.Find<Ellipse>("Thumb");
        if (_moving is not null) _moving.PropertyChanged += MovingChanged;
        _travel.Snap(IsChecked == true ? 20 : 0);
        _size.Snap(IsPressed ? 28 : IsChecked == true || OffIcon is not null ? 24 : 16);
        UpdateMotion();
    }
    private void MovingChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (!_painting && IsPressed && change.Property == Canvas.LeftProperty && _moving is not null)
            _travel.Snap(Canvas.GetLeft(_moving));
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _travel.Snap(IsChecked == true ? 20 : 0);
        _size.Snap(IsChecked == true || OffIcon is not null ? 24 : 16);
        if (_moving is not null) _moving.Transitions = null;
        if (_thumb is not null) _thumb.Transitions = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void UpdateMotion()
    {
        if (_motion is null) return;
        SetValue(KnobTransitionsProperty, null!, BindingPriority.Style);
        if (_moving is not null) _moving.Transitions = null;
        if (_thumb is not null)
            _thumb.Transitions = null;
        var size = IsPressed ? 28 : IsChecked == true || OffIcon is not null ? 24 : 16;
        if (IsPressed) { _size.Snap(size); _travel.Snap(IsChecked == true ? 20 : 0); }
        else { _size.Spring(size, _motion.FastSpatial); _travel.Spring(IsChecked == true ? 20 : 0, _motion.FastSpatial); }
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
        if (change.Property == IsPressedProperty || change.Property == IsCheckedProperty || change.Property == OffIconProperty)
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
