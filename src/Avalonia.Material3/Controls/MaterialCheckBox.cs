using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Avalonia.Material3.Controls;

/// <summary>A Material checkbox with Avalonia's two/three-state binding, input and toggle automation contract.</summary>
[PseudoClasses(":error")]
public class MaterialCheckBox : CheckBox
{
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialMotionBrush _fill;
    private readonly MaterialMotionBrush _border;
    private Border? _box;
    public MaterialCheckBox()
    {
        _fill = new(this, null, value => { if (_box is not null) _box.Background = value; });
        _border = new(this, null, value => { if (_box is not null) _box.BorderBrush = value; });
        _motion = new(this, UpdateColors);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _box = e.NameScope.Find<Border>("Box");
        _fill.Snap(Background); _border.Snap(BorderBrush);
    }
    private void UpdateColors()
    {
        if (_motion is null || _box is null) return;
        var spring = IsChecked == false ? _motion.FastEffects : _motion.DefaultEffects;
        if (IsEffectivelyEnabled) { _fill.Set(Background, spring); _border.Set(BorderBrush, spring); }
        else { _fill.Snap(Background); _border.Snap(BorderBrush); }
    }
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<MaterialCheckBox, bool>(nameof(IsError));
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MaterialCheckBox, string?>(nameof(ErrorText));

    /// <summary>Shows validation feedback without changing the checked value or blocking input.</summary>
    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    /// <summary>Visible and accessible error explanation when IsError is true.</summary>
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    protected override Type StyleKeyOverride => typeof(MaterialCheckBox);
    protected override AutomationPeer OnCreateAutomationPeer() => new SelectionToggleAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsErrorProperty)
            PseudoClasses.Set(":error", IsError);
        if (change.Property == BackgroundProperty || change.Property == BorderBrushProperty ||
            change.Property == IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty)
            UpdateColors();
    }
}
