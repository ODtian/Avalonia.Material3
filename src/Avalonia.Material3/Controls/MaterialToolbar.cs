using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

public enum MaterialToolbarVariant { Docked, Floating }
public enum MaterialToolbarColor { Standard, Vibrant }
public enum MaterialToolbarFabPosition { Start, End }

/// <summary>A docked or floating action surface. Core Items remain available; leading/trailing actions expand along Orientation.</summary>
public class MaterialToolbar : TemplatedControl, IMaterialExpansion
{
    public static readonly StyledProperty<MaterialToolbarVariant> VariantProperty = AvaloniaProperty.Register<MaterialToolbar, MaterialToolbarVariant>(nameof(Variant), MaterialToolbarVariant.Floating, validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<MaterialToolbar, Orientation>(nameof(Orientation), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialToolbar, bool>(nameof(IsExpanded), true, defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<MaterialToolbar, AvaloniaList<Control>> ItemsProperty = AvaloniaProperty.RegisterDirect<MaterialToolbar, AvaloniaList<Control>>(nameof(Items), control => control.Items);
    public static readonly DirectProperty<MaterialToolbar, AvaloniaList<Control>> LeadingItemsProperty = AvaloniaProperty.RegisterDirect<MaterialToolbar, AvaloniaList<Control>>(nameof(LeadingItems), control => control.LeadingItems);
    public static readonly DirectProperty<MaterialToolbar, AvaloniaList<Control>> TrailingItemsProperty = AvaloniaProperty.RegisterDirect<MaterialToolbar, AvaloniaList<Control>>(nameof(TrailingItems), control => control.TrailingItems);
    public MaterialToolbarVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public AvaloniaList<Control> Items { get; } = new();
    public AvaloniaList<Control> LeadingItems { get; } = new();
    public AvaloniaList<Control> TrailingItems { get; } = new();
    public static readonly StyledProperty<MaterialToolbarColor> ColorProperty = AvaloniaProperty.Register<MaterialToolbar, MaterialToolbarColor>(nameof(Color), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<MaterialActionAnchor> AnchorProperty = AvaloniaProperty.Register<MaterialToolbar, MaterialActionAnchor>(nameof(Anchor), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<MaterialFab?> FloatingActionProperty = AvaloniaProperty.Register<MaterialToolbar, MaterialFab?>(nameof(FloatingAction));
    public static readonly StyledProperty<MaterialToolbarFabPosition> FloatingActionPositionProperty = AvaloniaProperty.Register<MaterialToolbar, MaterialToolbarFabPosition>(nameof(FloatingActionPosition), MaterialToolbarFabPosition.End, validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<string> ExpandLabelProperty = AvaloniaProperty.Register<MaterialToolbar, string>(nameof(ExpandLabel), "Expand toolbar", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> CollapseLabelProperty = AvaloniaProperty.Register<MaterialToolbar, string>(nameof(CollapseLabel), "Collapse toolbar", validate: value => !string.IsNullOrWhiteSpace(value));
    public MaterialToolbarColor Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public MaterialActionAnchor Anchor { get => GetValue(AnchorProperty); set => SetValue(AnchorProperty, value); }
    public MaterialFab? FloatingAction { get => GetValue(FloatingActionProperty); set => SetValue(FloatingActionProperty, value); }
    public MaterialToolbarFabPosition FloatingActionPosition { get => GetValue(FloatingActionPositionProperty); set => SetValue(FloatingActionPositionProperty, value); }
    public string ExpandLabel { get => GetValue(ExpandLabelProperty); set => SetValue(ExpandLabelProperty, value); }
    public string CollapseLabel { get => GetValue(CollapseLabelProperty); set => SetValue(CollapseLabelProperty, value); }
    private MaterialExpansionButton? _toggle;
    Control IMaterialExpansion.ExpansionControl => this;
    void IMaterialExpansion.SetExpanded(bool expanded) => SetCurrentValue(IsExpandedProperty, expanded);
    protected override Type StyleKeyOverride => typeof(MaterialToolbar);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialExpansionAutomationPeer(this, this, AutomationControlType.ToolBar);
    public MaterialToolbar() => UpdateState();
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_toggle is not null) { _toggle.Click -= ToggleClicked; _toggle.Expansion = null; }
        base.OnApplyTemplate(e);
        _toggle = e.NameScope.Find<MaterialExpansionButton>("PART_Toggle");
        if (_toggle is not null) { _toggle.Expansion = this; _toggle.Click += ToggleClicked; }
        UpdateState();
    }
    private void ToggleClicked(object? sender, RoutedEventArgs e) => SetCurrentValue(IsExpandedProperty, !IsExpanded);
    private void UpdateState()
    {
        PseudoClasses.Set(":docked", Variant == MaterialToolbarVariant.Docked);
        PseudoClasses.Set(":vibrant", Color == MaterialToolbarColor.Vibrant);
        PseudoClasses.Set(":top", Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.TopEnd);
        PseudoClasses.Set(":left", (Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.BottomStart) != (FlowDirection == FlowDirection.RightToLeft));
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
        PseudoClasses.Set(":expanded", IsExpanded);
        if (_toggle is not null) AutomationProperties.SetName(_toggle, IsExpanded ? CollapseLabel : ExpandLabel);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && IsExpanded) { SetCurrentValue(IsExpandedProperty, false); _toggle?.Focus(NavigationMethod.Tab); e.Handled = true; return; }
        var backward = Orientation == Orientation.Vertical ? Key.Up : FlowDirection == FlowDirection.RightToLeft ? Key.Right : Key.Left;
        var forward = Orientation == Orientation.Vertical ? Key.Down : FlowDirection == FlowDirection.RightToLeft ? Key.Left : Key.Right;
        if (e.Key != backward && e.Key != forward && e.Key is not (Key.Home or Key.End)) return;
        var actions = (IsExpanded ? LeadingItems.Concat(Items).Concat(TrailingItems) : Items).OfType<Control>()
            .SelectMany(control => control is Button button ? new[] { button }.AsEnumerable() : control.GetVisualDescendants().OfType<Button>())
            .Where(control => control.Focusable && control.IsEffectivelyEnabled && control.IsEffectivelyVisible).ToArray();
        if (actions.Length == 0) return;
        var current = Array.FindIndex(actions, control => control.IsKeyboardFocusWithin);
        var index = e.Key == Key.Home ? 0 : e.Key == Key.End ? actions.Length - 1 : e.Key == forward ? (current + 1) % actions.Length : (current + actions.Length - 1) % actions.Length;
        actions[index].Focus(NavigationMethod.Directional);
        actions[index].BringIntoView();
        e.Handled = true;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty || change.Property == OrientationProperty || change.Property == IsExpandedProperty || change.Property == ColorProperty || change.Property == AnchorProperty || change.Property == FlowDirectionProperty || change.Property == ExpandLabelProperty || change.Property == CollapseLabelProperty)
        {
            UpdateState();
            if (change.Property == IsExpandedProperty && !IsExpanded && LeadingItems.Concat(TrailingItems).Any(item => item.IsKeyboardFocusWithin))
                _toggle?.Focus(NavigationMethod.Tab);
        }
    }
}
