using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>A non-modal anchored list of primary actions. Items are real buttons, retaining content, commands and Invoke.</summary>
public class MaterialFabMenu : TemplatedControl, IMaterialExpansion
{
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialFabMenu, bool>(nameof(IsExpanded), defaultBindingMode: BindingMode.TwoWay);
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public static readonly DirectProperty<MaterialFabMenu, AvaloniaList<MaterialFabMenuItem>> ItemsProperty = AvaloniaProperty.RegisterDirect<MaterialFabMenu, AvaloniaList<MaterialFabMenuItem>>(nameof(Items), control => control.Items);
    public AvaloniaList<MaterialFabMenuItem> Items { get; } = new();
    public static readonly StyledProperty<MaterialActionAnchor> AnchorProperty = AvaloniaProperty.Register<MaterialFabMenu, MaterialActionAnchor>(nameof(Anchor), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<MaterialFabSize> TriggerSizeProperty = AvaloniaProperty.Register<MaterialFabMenu, MaterialFabSize>(nameof(TriggerSize), validate: value => Enum.IsDefined(value));
    public MaterialFabSize TriggerSize { get => GetValue(TriggerSizeProperty); set => SetValue(TriggerSizeProperty, value); }
    public static readonly StyledProperty<object?> ToggleIconProperty = AvaloniaProperty.Register<MaterialFabMenu, object?>(nameof(ToggleIcon), new MaterialSymbolSource("add"));
    public static readonly StyledProperty<object?> CloseIconProperty = AvaloniaProperty.Register<MaterialFabMenu, object?>(nameof(CloseIcon), new MaterialSymbolSource("close"));
    public static readonly StyledProperty<IDataTemplate?> ToggleIconTemplateProperty = AvaloniaProperty.Register<MaterialFabMenu, IDataTemplate?>(nameof(ToggleIconTemplate));
    public static readonly StyledProperty<IDataTemplate?> CloseIconTemplateProperty = AvaloniaProperty.Register<MaterialFabMenu, IDataTemplate?>(nameof(CloseIconTemplate));
    public static readonly StyledProperty<string> ExpandLabelProperty = AvaloniaProperty.Register<MaterialFabMenu, string>(nameof(ExpandLabel), "Expand actions", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> CollapseLabelProperty = AvaloniaProperty.Register<MaterialFabMenu, string>(nameof(CollapseLabel), "Collapse actions", validate: value => !string.IsNullOrWhiteSpace(value));
    public MaterialActionAnchor Anchor { get => GetValue(AnchorProperty); set => SetValue(AnchorProperty, value); }
    public object? ToggleIcon { get => GetValue(ToggleIconProperty); set => SetValue(ToggleIconProperty, value); }
    public object? CloseIcon { get => GetValue(CloseIconProperty); set => SetValue(CloseIconProperty, value); }
    public IDataTemplate? ToggleIconTemplate { get => GetValue(ToggleIconTemplateProperty); set => SetValue(ToggleIconTemplateProperty, value); }
    public IDataTemplate? CloseIconTemplate { get => GetValue(CloseIconTemplateProperty); set => SetValue(CloseIconTemplateProperty, value); }
    public string ExpandLabel { get => GetValue(ExpandLabelProperty); set => SetValue(ExpandLabelProperty, value); }
    public string CollapseLabel { get => GetValue(CollapseLabelProperty); set => SetValue(CollapseLabelProperty, value); }
    private MaterialExpansionButton? _toggle;
    private IMaterialActionDisclosure? _reveal;
    private TopLevel? _topLevel;
    private bool _restoreFocus = true;
    Control IMaterialExpansion.ExpansionControl => this;
    void IMaterialExpansion.SetExpanded(bool expanded) => SetCurrentValue(IsExpandedProperty, expanded);
    protected override Type StyleKeyOverride => typeof(MaterialFabMenu);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialExpansionAutomationPeer(this, this, AutomationControlType.Menu);

    public MaterialFabMenu()
    {
        AddHandler(Button.ClickEvent, ActionClicked);
        UpdateAnchor();
    }
    private void UpdateAnchor()
    {
        PseudoClasses.Set(":top", Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.TopEnd);
        PseudoClasses.Set(":left", (Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.BottomStart) != (FlowDirection == FlowDirection.RightToLeft));
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_toggle is not null) { _toggle.Click -= ToggleClicked; _toggle.Expansion = null; }
        if (_reveal is not null) _reveal.Settled -= RevealSettled;
        base.OnApplyTemplate(e);
        _reveal = e.NameScope.Find<Control>("MenuReveal") as IMaterialActionDisclosure;
        if (_reveal is not null) _reveal.Settled += RevealSettled;
        _toggle = e.NameScope.Find<MaterialExpansionButton>("PART_Toggle");
        if (_toggle is not null) { _toggle.Expansion = this; _toggle.Click += ToggleClicked; }
        UpdateToggleName();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(PointerPressedEvent, OutsidePressed, RoutingStrategies.Tunnel, true);
        _topLevel?.AddHandler(GotFocusEvent, HostFocusChanged, RoutingStrategies.Bubble, true);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(PointerPressedEvent, OutsidePressed);
        _topLevel?.RemoveHandler(GotFocusEvent, HostFocusChanged);
        _topLevel = null;
        Collapse(false);
        base.OnDetachedFromVisualTree(e);
    }
    private bool Contains(object? source) => source is Visual visual && (visual == this || this.IsVisualAncestorOf(visual));
    private void OutsidePressed(object? sender, PointerPressedEventArgs e) { if (IsExpanded && !Contains(e.Source)) Collapse(false); }
    private void HostFocusChanged(object? sender, RoutedEventArgs e) { if (IsExpanded && !Contains(e.Source)) Collapse(false); }
    private void Collapse(bool restoreFocus)
    {
        _restoreFocus = restoreFocus;
        try { SetCurrentValue(IsExpandedProperty, false); }
        finally { _restoreFocus = true; }
    }
    private void RevealSettled() => Dispatcher.UIThread.Post(() =>
    {
        if (IsExpanded && _topLevel is not null && Items.FirstOrDefault(item => item.IsEffectivelyEnabled && item.IsVisible) is { IsFocused: true } first)
            first.BringIntoView();
    }, DispatcherPriority.Loaded);
    private void ToggleClicked(object? sender, RoutedEventArgs e) => SetCurrentValue(IsExpandedProperty, !IsExpanded);
    private void ActionClicked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is MaterialFabMenuItem && IsExpanded) Collapse(true);
    }
    private void UpdateToggleName()
    {
        if (_toggle is not null)
        {
            AutomationProperties.SetName(_toggle, IsExpanded ? CollapseLabel : ExpandLabel);
            _toggle.SetCurrentValue(ContentControl.ContentProperty, IsExpanded ? CloseIcon : ToggleIcon);
            _toggle.SetCurrentValue(ContentControl.ContentTemplateProperty, IsExpanded ? CloseIconTemplate : ToggleIconTemplate);
        }
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsExpanded) return;
        if (e.Key == Key.Escape) { Collapse(true); e.Handled = true; return; }
        if (e.Key is not (Key.Up or Key.Down or Key.Home or Key.End)) return;
        var enabled = Items.Where(item => item.IsEffectivelyEnabled && item.IsEffectivelyVisible).ToArray();
        if (enabled.Length == 0) return;
        var current = Array.FindIndex(enabled, item => item.IsFocused);
        var index = e.Key switch { Key.Home => 0, Key.End => enabled.Length - 1, Key.Down => (current + 1) % enabled.Length, _ => (current + enabled.Length - 1) % enabled.Length };
        enabled[index].Focus(NavigationMethod.Directional);
        enabled[index].BringIntoView();
        e.Handled = true;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AnchorProperty || change.Property == FlowDirectionProperty) UpdateAnchor();
        if (change.Property == ToggleIconProperty || change.Property == CloseIconProperty || change.Property == ToggleIconTemplateProperty || change.Property == CloseIconTemplateProperty || change.Property == ExpandLabelProperty || change.Property == CollapseLabelProperty) UpdateToggleName();
        if (change.Property == IsEffectivelyEnabledProperty && !MaterialModalPaintScope.IsEnabledForPaint(this)) Collapse(false);
        if (change.Property == IsExpandedProperty)
        {
            if (IsExpanded && !IsEffectivelyEnabled) { Collapse(false); return; }
            PseudoClasses.Set(":expanded", IsExpanded);
            UpdateToggleName();
            if (IsExpanded)
                Dispatcher.UIThread.Post(() =>
                {
                    if (IsExpanded && _topLevel is not null)
                    {
                        var first = Items.FirstOrDefault(item => item.IsEffectivelyEnabled && item.IsVisible);
                        first?.Focus(NavigationMethod.Tab);
                        // Focus semantics are immediate, but scrolling a zero/partial viewport must not
                        // chase the reveal clip. Keyboard navigation still explicitly scrolls its target.
                        if (_reveal?.IsRevealing != true) first?.BringIntoView();
                    }
                }, DispatcherPriority.Loaded);
            else if (_restoreFocus) _toggle?.Focus(NavigationMethod.Tab);
        }
    }
}
