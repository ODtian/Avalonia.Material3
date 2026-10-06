using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;

namespace Avalonia.Material3.Controls;

/// <summary>The four Material chip purposes.</summary>
public enum MaterialChipVariant { Assist, Filter, Input, Suggestion }

/// <summary>A Material chip retaining Button activation, command and toggle automation.</summary>
[TemplatePart("PART_RemoveButton", typeof(Button))]
[PseudoClasses(":assist", ":filter", ":input", ":suggestion", ":removable", ":elevated", ":dragged", ":avatar")]
public class MaterialChip : MaterialButton
{
    public static readonly StyledProperty<MaterialChipVariant> ChipVariantProperty =
        AvaloniaProperty.Register<MaterialChip, MaterialChipVariant>(nameof(ChipVariant), validate: Enum.IsDefined);
    public MaterialChipVariant ChipVariant { get => GetValue(ChipVariantProperty); set => SetValue(ChipVariantProperty, value); }

    public static readonly StyledProperty<object?> AvatarProperty = AvaloniaProperty.Register<MaterialChip, object?>(nameof(Avatar));
    public static readonly StyledProperty<IDataTemplate?> AvatarTemplateProperty = AvaloniaProperty.Register<MaterialChip, IDataTemplate?>(nameof(AvatarTemplate));
    public object? Avatar { get => GetValue(AvatarProperty); set => SetValue(AvatarProperty, value); }
    public IDataTemplate? AvatarTemplate { get => GetValue(AvatarTemplateProperty); set => SetValue(AvatarTemplateProperty, value); }
    public static readonly StyledProperty<bool> IsRemovableProperty =
        AvaloniaProperty.Register<MaterialChip, bool>(nameof(IsRemovable), true);
    public static readonly StyledProperty<string> RemoveButtonLabelProperty =
        AvaloniaProperty.Register<MaterialChip, string>(nameof(RemoveButtonLabel), "Remove token", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<bool> IsElevatedProperty = AvaloniaProperty.Register<MaterialChip, bool>(nameof(IsElevated));
    public static readonly StyledProperty<bool> IsDraggedProperty = AvaloniaProperty.Register<MaterialChip, bool>(nameof(IsDragged));
    public static readonly RoutedEvent<RoutedEventArgs> RemovalRequestedEvent =
        RoutedEvent.Register<MaterialChip, RoutedEventArgs>(nameof(RemovalRequested), RoutingStrategies.Bubble);
    public bool IsRemovable { get => GetValue(IsRemovableProperty); set => SetValue(IsRemovableProperty, value); }
    public string RemoveButtonLabel { get => GetValue(RemoveButtonLabelProperty); set => SetValue(RemoveButtonLabelProperty, value); }
    public bool IsElevated { get => GetValue(IsElevatedProperty); set => SetValue(IsElevatedProperty, value); }
    public bool IsDragged { get => GetValue(IsDraggedProperty); set => SetValue(IsDraggedProperty, value); }
    public event EventHandler<RoutedEventArgs>? RemovalRequested { add => AddHandler(RemovalRequestedEvent, value); remove => RemoveHandler(RemovalRequestedEvent, value); }
    private Button? _remove;
    public MaterialChip() => UpdateStates();
    protected override Type StyleKeyOverride => typeof(MaterialChip);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_remove is not null) _remove.Click -= RemoveClicked;
        base.OnApplyTemplate(e);
        _remove = e.NameScope.Find<Button>("PART_RemoveButton");
        if (_remove is not null) _remove.Click += RemoveClicked;
    }
    private void RemoveClicked(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (IsEffectivelyEnabled && IsRemovable && ChipVariant == MaterialChipVariant.Input)
            RaiseEvent(new RoutedEventArgs(RemovalRequestedEvent));
    }
    private void UpdateStates()
    {
        foreach (var variant in Enum.GetValues<MaterialChipVariant>())
            PseudoClasses.Set(":" + variant.ToString().ToLowerInvariant(), ChipVariant == variant);
        PseudoClasses.Set(":removable", ChipVariant == MaterialChipVariant.Input && IsRemovable);
        PseudoClasses.Set(":elevated", IsElevated && ChipVariant != MaterialChipVariant.Input);
        PseudoClasses.Set(":dragged", IsDragged);
        PseudoClasses.Set(":avatar", Avatar is not null);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChipVariantProperty)
            SetCurrentValue(IsToggleProperty, ChipVariant is MaterialChipVariant.Filter or MaterialChipVariant.Input);
        if (change.Property == ChipVariantProperty || change.Property == IsRemovableProperty ||
            change.Property == IsElevatedProperty || change.Property == IsDraggedProperty || change.Property == AvatarProperty) UpdateStates();
    }
}
