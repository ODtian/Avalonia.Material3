using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avalonia.Material3.Controls;

/// <summary>A selectable destination. Content is the label; PageContent is the selected page.</summary>
public class MaterialNavigationItem : Button
{
    public static readonly StyledProperty<object?> BadgeProperty = AvaloniaProperty.Register<MaterialNavigationItem, object?>(nameof(Badge));
    public static readonly StyledProperty<IDataTemplate?> BadgeTemplateProperty = AvaloniaProperty.Register<MaterialNavigationItem, IDataTemplate?>(nameof(BadgeTemplate));
    public static readonly StyledProperty<string?> BadgeDescriptionProperty = AvaloniaProperty.Register<MaterialNavigationItem, string?>(nameof(BadgeDescription));
    public static readonly StyledProperty<object?> SelectedIconProperty = AvaloniaProperty.Register<MaterialNavigationItem, object?>(nameof(SelectedIcon));
    public static readonly DirectProperty<MaterialNavigationItem, object?> DisplayIconProperty = AvaloniaProperty.RegisterDirect<MaterialNavigationItem, object?>(nameof(DisplayIcon), c => c.DisplayIcon);
    private object? _displayIcon;
    public object? Badge { get => GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }
    public IDataTemplate? BadgeTemplate { get => GetValue(BadgeTemplateProperty); set => SetValue(BadgeTemplateProperty, value); }
    public string? BadgeDescription { get => GetValue(BadgeDescriptionProperty); set => SetValue(BadgeDescriptionProperty, value); }
    public object? SelectedIcon { get => GetValue(SelectedIconProperty); set => SetValue(SelectedIconProperty, value); }
    public object? DisplayIcon => _displayIcon;
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<MaterialNavigationItem, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<MaterialNavigationItem, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<object?> PageContentProperty = AvaloniaProperty.Register<MaterialNavigationItem, object?>(nameof(PageContent));
    public static readonly StyledProperty<IDataTemplate?> PageContentTemplateProperty = AvaloniaProperty.Register<MaterialNavigationItem, IDataTemplate?>(nameof(PageContentTemplate));
    public static readonly DirectProperty<MaterialNavigationItem, bool> IsSelectedProperty = AvaloniaProperty.RegisterDirect<MaterialNavigationItem, bool>(nameof(IsSelected), c => c.IsSelected);
    private bool _isSelected;
    internal MaterialNavigation? Owner { get; set; }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public object? PageContent { get => GetValue(PageContentProperty); set => SetValue(PageContentProperty, value); }
    public IDataTemplate? PageContentTemplate { get => GetValue(PageContentTemplateProperty); set => SetValue(PageContentTemplateProperty, value); }
    public bool IsSelected => _isSelected;
    internal void SetSelected(bool value)
    {
        SetAndRaise(IsSelectedProperty, ref _isSelected, value);
        PseudoClasses.Set(":selected", value);
        UpdateIcon();
    }
    private void UpdateIcon() => SetAndRaise(DisplayIconProperty, ref _displayIcon, IsSelected && SelectedIcon is not null ? SelectedIcon : Icon);
    internal void UpdatePresentation()
    {
        PseudoClasses.Set(":bar", Owner is MaterialNavigationBar);
        PseudoClasses.Set(":rail", Owner is MaterialNavigationRail);
        PseudoClasses.Set(":horizontal", Owner is MaterialTabs tabs ? tabs.Variant == MaterialTabVariant.Secondary : Owner?.ItemLayout == MaterialNavigationItemLayout.Horizontal);
        PseudoClasses.Set(":tabs", Owner is MaterialTabs);
        PseudoClasses.Set(":primary", Owner is MaterialTabs { Variant: MaterialTabVariant.Primary });
        PseudoClasses.Set(":secondary", Owner is MaterialTabs { Variant: MaterialTabVariant.Secondary });
        PseudoClasses.Set(":scrollable", Owner is MaterialTabs { Layout: MaterialTabLayout.Scrollable });
        PseudoClasses.Set(":icon", Icon is not null);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty || change.Property == SelectedIconProperty) { UpdateIcon(); UpdatePresentation(); }
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.None && Owner?.Navigate(this, e.Key) == true) { e.Handled = true; return; }
        base.OnKeyDown(e);
    }
    protected override Type StyleKeyOverride => typeof(MaterialNavigationItem);
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new MaterialNavigationItemAutomationPeer(this);
    protected override void OnClick()
    {
        if (!IsEffectivelyEnabled) return;
        Owner?.Activate(this);
        base.OnClick();
        Owner?.NotifyInvoked(this);
    }
}
