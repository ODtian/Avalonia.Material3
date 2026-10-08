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
using Avalonia.Threading;

namespace Avalonia.Material3.Controls;

public enum MaterialToolbarVariant { Docked, Floating }
public enum MaterialToolbarColor { Standard, Vibrant }
public enum MaterialToolbarFabPosition { Start, End }
public enum MaterialToolbarCollapseBehavior { ExpansionSlots, WholeToolbar }

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
    public static readonly StyledProperty<MaterialToolbarCollapseBehavior> CollapseBehaviorProperty = AvaloniaProperty.Register<MaterialToolbar, MaterialToolbarCollapseBehavior>(nameof(CollapseBehavior), validate: value => Enum.IsDefined(value));
    public static readonly DirectProperty<MaterialToolbar, bool> SurfaceIsExpandedProperty = AvaloniaProperty.RegisterDirect<MaterialToolbar, bool>(nameof(SurfaceIsExpanded), control => control.SurfaceIsExpanded);
    public MaterialToolbarCollapseBehavior CollapseBehavior { get => GetValue(CollapseBehaviorProperty); set => SetValue(CollapseBehaviorProperty, value); }
    /// <summary>The complete surface collapses only when WholeToolbar is selected and a disclosure FAB is supplied.</summary>
    public bool SurfaceIsExpanded => IsExpanded || CollapseBehavior == MaterialToolbarCollapseBehavior.ExpansionSlots || FloatingAction is null;
    private MaterialExpansionButton? _toggle;
    private bool _lastSurfaceIsExpanded = true;
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
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (FloatingAction is not null) FloatingAction.ToolbarExpansion = this;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (FloatingAction?.ToolbarExpansion == this) FloatingAction.ToolbarExpansion = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void ToggleClicked(object? sender, RoutedEventArgs e) => SetCurrentValue(IsExpandedProperty, !IsExpanded);
    private Button[] FocusableActions(bool includeSlots) => (includeSlots ? LeadingItems.Concat(Items).Concat(TrailingItems) : Items).OfType<Control>()
        .SelectMany(control => control is Button button ? new[] { button }.AsEnumerable() : control.GetVisualDescendants().OfType<Button>())
        .Where(control => control.Focusable && control.IsEffectivelyEnabled && control.IsEffectivelyVisible).ToArray();
    private void FocusCollapsedAction() => (_toggle ?? (Control?)FocusableActions(false).FirstOrDefault()
        ?? (FloatingAction is { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } fab ? fab : null))?.Focus(NavigationMethod.Tab);
    private void UpdateState()
    {
        PseudoClasses.Set(":docked", Variant == MaterialToolbarVariant.Docked);
        PseudoClasses.Set(":vibrant", Color == MaterialToolbarColor.Vibrant);
        PseudoClasses.Set(":with-fab", FloatingAction is not null);
        PseudoClasses.Set(":top", Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.TopEnd);
        PseudoClasses.Set(":left", (Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.BottomStart) != (FlowDirection == FlowDirection.RightToLeft));
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
        PseudoClasses.Set(":expanded", IsExpanded);
        if (_toggle is not null) AutomationProperties.SetName(_toggle, IsExpanded ? CollapseLabel : ExpandLabel);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        if (e.Key == Key.Escape && IsExpanded) { SetCurrentValue(IsExpandedProperty, false); if (SurfaceIsExpanded) FocusCollapsedAction(); else FloatingAction?.Focus(NavigationMethod.Tab); e.Handled = true; return; }
        if (!SurfaceIsExpanded && e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            SetCurrentValue(IsExpandedProperty, true);
            e.Handled = true;
            return;
        }
        var backward = Orientation == Orientation.Vertical ? Key.Up : FlowDirection == FlowDirection.RightToLeft ? Key.Right : Key.Left;
        var forward = Orientation == Orientation.Vertical ? Key.Down : FlowDirection == FlowDirection.RightToLeft ? Key.Left : Key.Right;
        if (e.Key != backward && e.Key != forward && e.Key is not (Key.Home or Key.End)) return;
        var actions = FocusableActions(IsExpanded);
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
        if (change.Property == FloatingActionProperty)
        {
            var old = change.GetOldValue<MaterialFab?>();
            if (old?.ToolbarExpansion == this) old.ToolbarExpansion = null;
            if (FloatingAction is not null) FloatingAction.ToolbarExpansion = this;
        }
        if (change.Property == IsExpandedProperty || change.Property == CollapseBehaviorProperty || change.Property == FloatingActionProperty)
        {
            var hadSurfaceFocus = LeadingItems.Concat(Items).Concat(TrailingItems).Any(item => item.IsKeyboardFocusWithin);
            var oldSurface = _lastSurfaceIsExpanded;
            _lastSurfaceIsExpanded = SurfaceIsExpanded;
            RaisePropertyChanged(SurfaceIsExpandedProperty, oldSurface, SurfaceIsExpanded);
            if (!SurfaceIsExpanded && hadSurfaceFocus)
                FloatingAction?.Focus(NavigationMethod.Tab);
            if (SurfaceIsExpanded && CollapseBehavior == MaterialToolbarCollapseBehavior.WholeToolbar && FloatingAction?.IsKeyboardFocusWithin == true)
                Dispatcher.UIThread.Post(() =>
                {
                    if (SurfaceIsExpanded && FloatingAction?.IsKeyboardFocusWithin == true)
                    {
                        var first = Items.FirstOrDefault(item => item.Focusable && item.IsEffectivelyEnabled && item.IsVisible);
                        first?.Focus(NavigationMethod.Tab);
                        first?.BringIntoView();
                    }
                }, DispatcherPriority.Loaded);
        }
        if (change.Property == VariantProperty || change.Property == OrientationProperty || change.Property == IsExpandedProperty || change.Property == ColorProperty || change.Property == AnchorProperty || change.Property == FlowDirectionProperty || change.Property == ExpandLabelProperty || change.Property == CollapseLabelProperty || change.Property == FloatingActionProperty)
        {
            UpdateState();
            if (SurfaceIsExpanded && change.Property == IsExpandedProperty && !IsExpanded && LeadingItems.Concat(TrailingItems).Any(item => item.IsKeyboardFocusWithin))
                FocusCollapsedAction();
        }
    }
}
