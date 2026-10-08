using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace Avalonia.Material3.Controls;

/// <summary>The two Material 3 text field container treatments.</summary>
public enum MaterialTextFieldVariant
{
    Filled,
    Outlined
}

/// <summary>A Material text field retaining Avalonia's native editor, selection, undo and IME client.</summary>
[TemplatePart("PART_ClearButton", typeof(Button))]
[PseudoClasses(":outlined", ":invalid", ":clearable", ":unlabelled", ":leading", ":trailing", ":prefix", ":suffix", ":supporting")]
public class MaterialTextField : TextBox
{
    public static readonly StyledProperty<MaterialTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialTextField, MaterialTextFieldVariant>(nameof(Variant),
            validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(Label));

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(SupportingText));
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<MaterialTextField, bool>(nameof(IsError));
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(ErrorText));
    public static readonly DirectProperty<MaterialTextField, bool> HasErrorProperty =
        AvaloniaProperty.RegisterDirect<MaterialTextField, bool>(nameof(HasError), field => field.HasError);
    public static readonly DirectProperty<MaterialTextField, string?> EffectiveSupportingTextProperty =
        AvaloniaProperty.RegisterDirect<MaterialTextField, string?>(nameof(EffectiveSupportingText), field => field.EffectiveSupportingText);

    public static readonly StyledProperty<string?> PrefixTextProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(PrefixText));
    public static readonly StyledProperty<string?> SuffixTextProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(SuffixText));
    public static readonly StyledProperty<bool> ShowClearButtonProperty =
        AvaloniaProperty.Register<MaterialTextField, bool>(nameof(ShowClearButton));
    public static readonly StyledProperty<string> ClearButtonLabelProperty =
        AvaloniaProperty.Register<MaterialTextField, string>(nameof(ClearButtonLabel), "Clear text",
            validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<bool> ShowCounterProperty =
        AvaloniaProperty.Register<MaterialTextField, bool>(nameof(ShowCounter));
    public static readonly DirectProperty<MaterialTextField, string> CounterTextProperty =
        AvaloniaProperty.RegisterDirect<MaterialTextField, string>(nameof(CounterText), field => field.CounterText);

    private string _counterText = "0";
    private Button? _clearButton;
    private bool _hasError;
    private string? _effectiveSupportingText;

    public string? PrefixText { get => GetValue(PrefixTextProperty); set => SetValue(PrefixTextProperty, value); }
    public string? SuffixText { get => GetValue(SuffixTextProperty); set => SetValue(SuffixTextProperty, value); }
    public bool ShowClearButton { get => GetValue(ShowClearButtonProperty); set => SetValue(ShowClearButtonProperty, value); }
    public string ClearButtonLabel { get => GetValue(ClearButtonLabelProperty); set => SetValue(ClearButtonLabelProperty, value); }
    public bool ShowCounter { get => GetValue(ShowCounterProperty); set => SetValue(ShowCounterProperty, value); }
    /// <summary>UTF-16 code unit count, matching Avalonia TextBox.MaxLength. MaxLength=0 means unlimited.</summary>
    public string CounterText => _counterText;

    public string? SupportingText { get => GetValue(SupportingTextProperty); set => SetValue(SupportingTextProperty, value); }
    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    /// <summary>True for host-controlled errors or inherited binding validation errors.</summary>
    public bool HasError => _hasError;
    /// <summary>Explicit error, first native validation message, or helper text, in that order.</summary>
    public string? EffectiveSupportingText => _effectiveSupportingText;

    public MaterialTextFieldVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public MaterialTextField() => PseudoClasses.Set(":unlabelled", true);

    protected override Type StyleKeyOverride => typeof(MaterialTextField);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialTextFieldAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_clearButton is not null)
            _clearButton.Click -= ClearClicked;
        base.OnApplyTemplate(e);
        _clearButton = e.NameScope.Find<Button>("PART_ClearButton");
        if (_clearButton is not null)
            _clearButton.Click += ClearClicked;
    }

    private void ClearClicked(object? sender, RoutedEventArgs e)
    {
        if (!IsReadOnly && IsEffectivelyEnabled)
        {
            // Reconnect the native IME/editor client before editing; the clear action itself can own focus.
            // Clearing while the client is detached would reset native undo history on the next focus.
            Focus();
            Clear();
        }
    }

    // Interactive slots have their own keyboard/text focus. Do not let their bubbling events edit this value.
    // No keys or text are remapped: the focused editor still delegates unchanged to the native TextBox pipeline.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsFocused) base.OnKeyDown(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (IsFocused) base.OnTextInput(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InnerLeftContentProperty)
            PseudoClasses.Set(":leading", InnerLeftContent is not null);
        if (change.Property == InnerRightContentProperty)
            PseudoClasses.Set(":trailing", InnerRightContent is not null);
        if (change.Property == PrefixTextProperty)
            PseudoClasses.Set(":prefix", !string.IsNullOrEmpty(PrefixText));
        if (change.Property == SuffixTextProperty)
            PseudoClasses.Set(":suffix", !string.IsNullOrEmpty(SuffixText));
        if (change.Property == LabelProperty)
            PseudoClasses.Set(":unlabelled", string.IsNullOrWhiteSpace(Label));
        if (change.Property == VariantProperty)
            PseudoClasses.Set(":outlined", Variant == MaterialTextFieldVariant.Outlined);
        if (change.Property == TextProperty || change.Property == MaxLengthProperty)
            SetAndRaise(CounterTextProperty, ref _counterText, MaxLength > 0 ? $"{Text?.Length ?? 0} / {MaxLength}" : $"{Text?.Length ?? 0}");
        if (change.Property == TextProperty || change.Property == ShowClearButtonProperty || change.Property == IsReadOnlyProperty)
            PseudoClasses.Set(":clearable", ShowClearButton && !IsReadOnly && !string.IsNullOrEmpty(Text));
        if (change.Property == ErrorTextProperty || change.Property == SupportingTextProperty || change.Property == IsErrorProperty ||
            change.Property == DataValidationErrors.ErrorsProperty || change.Property == DataValidationErrors.HasErrorsProperty)
            UpdateValidation();
        if (change.Property == ShowCounterProperty) UpdateSupportingState();
    }

    private void UpdateValidation()
    {
        var bindingError = DataValidationErrors.GetErrors(this)?.Select(error => error is Exception exception ? exception.Message : error?.ToString())
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
        SetAndRaise(HasErrorProperty, ref _hasError, IsError || !string.IsNullOrEmpty(ErrorText) || DataValidationErrors.GetHasErrors(this));
        SetAndRaise(EffectiveSupportingTextProperty, ref _effectiveSupportingText,
            !string.IsNullOrEmpty(ErrorText) ? ErrorText : bindingError ?? SupportingText);
        PseudoClasses.Set(":invalid", HasError);
        UpdateSupportingState();
    }
    private void UpdateSupportingState() => PseudoClasses.Set(":supporting", EffectiveSupportingText is not null || ShowCounter);
}
