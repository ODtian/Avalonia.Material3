using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>An explicit activation, including repeat activation of the selected destination.</summary>
public sealed class MaterialNavigationItemInvokedEventArgs(MaterialNavigationItem item) : RoutedEventArgs(MaterialNavigation.ItemInvokedEvent)
{
    public MaterialNavigationItem Item { get; } = item;
}

public enum MaterialNavigationItemLayout { Vertical, Horizontal }

/// <summary>Shared single-selection and page-content contract for Material navigation controls.</summary>
public abstract class MaterialNavigation : ItemsControl
{
    public static readonly StyledProperty<MaterialNavigationItemLayout> ItemLayoutProperty = AvaloniaProperty.Register<MaterialNavigation, MaterialNavigationItemLayout>(nameof(ItemLayout), validate: value => Enum.IsDefined(value));
    public MaterialNavigationItemLayout ItemLayout { get => GetValue(ItemLayoutProperty); set => SetValue(ItemLayoutProperty, value); }
    public static readonly DirectProperty<MaterialNavigation, double> HeaderWidthProperty = AvaloniaProperty.RegisterDirect<MaterialNavigation, double>(nameof(HeaderWidth), c => c.HeaderWidth);
    private double _headerWidth = double.NaN;
    public double HeaderWidth => _headerWidth;
    public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<MaterialNavigation, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<MaterialNavigation, MaterialNavigationItem?> SelectedItemProperty = AvaloniaProperty.RegisterDirect<MaterialNavigation, MaterialNavigationItem?>(nameof(SelectedItem), c => c.SelectedItem);
    public static readonly DirectProperty<MaterialNavigation, object?> SelectedContentProperty = AvaloniaProperty.RegisterDirect<MaterialNavigation, object?>(nameof(SelectedContent), c => c.SelectedContent);
    public static readonly DirectProperty<MaterialNavigation, IDataTemplate?> SelectedContentTemplateProperty = AvaloniaProperty.RegisterDirect<MaterialNavigation, IDataTemplate?>(nameof(SelectedContentTemplate), c => c.SelectedContentTemplate);
    public static readonly RoutedEvent<MaterialNavigationItemInvokedEventArgs> ItemInvokedEvent = RoutedEvent.Register<MaterialNavigation, MaterialNavigationItemInvokedEventArgs>(nameof(ItemInvoked), RoutingStrategies.Bubble);
    public event EventHandler<MaterialNavigationItemInvokedEventArgs>? ItemInvoked { add => AddHandler(ItemInvokedEvent, value); remove => RemoveHandler(ItemInvokedEvent, value); }
    internal void NotifyInvoked(MaterialNavigationItem item) => RaiseEvent(new MaterialNavigationItemInvokedEventArgs(item));
    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent = RoutedEvent.Register<MaterialNavigation, SelectionChangedEventArgs>(nameof(SelectionChanged), RoutingStrategies.Bubble);
    private readonly List<MaterialNavigationItem> _subscribed = [];
    private MaterialNavigationItem? _selectedItem;
    private object? _selectedContent;
    private IDataTemplate? _selectedContentTemplate;
    private bool _synchronizing;
    private ScrollViewer? _headerScrollViewer;
    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public MaterialNavigationItem? SelectedItem => _selectedItem;
    public object? SelectedContent => _selectedContent;
    public IDataTemplate? SelectedContentTemplate => _selectedContentTemplate;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged { add => AddHandler(SelectionChangedEvent, value); remove => RemoveHandler(SelectionChangedEvent, value); }

    protected MaterialNavigation()
    {
        ItemsView.CollectionChanged += ItemsChanged;
        RefreshPresentation();
    }
    protected void RefreshPresentation()
    {
        SetAndRaise(HeaderWidthProperty, ref _headerWidth, this is MaterialNavigationRail rail ? (rail.IsExpanded ? rail.ExpandedWidth : rail.UseNarrowWidth ? 80 : 96) : double.NaN);
        PseudoClasses.Set(":bar", this is MaterialNavigationBar);
        PseudoClasses.Set(":rail", this is MaterialNavigationRail);
        PseudoClasses.Set(":horizontal", ItemLayout == MaterialNavigationItemLayout.Horizontal);
        PseudoClasses.Set(":tabs", this is MaterialTabs);
        PseudoClasses.Set(":primary", this is MaterialTabs { Variant: MaterialTabVariant.Primary });
        PseudoClasses.Set(":secondary", this is MaterialTabs { Variant: MaterialTabVariant.Secondary });
        PseudoClasses.Set(":scrollable", this is MaterialTabs { Layout: MaterialTabLayout.Scrollable });
        PseudoClasses.Set(":expanded", this is MaterialNavigationRail { IsExpanded: true });
        foreach (var item in _subscribed) item.UpdatePresentation();
        ItemsPanelRoot?.InvalidateMeasure();
        InvalidateMeasure();
        RevealSelection();
    }
    protected override Type StyleKeyOverride => typeof(MaterialNavigation);
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new MaterialNavigationAutomationPeer(this);
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        if (item is not MaterialNavigationItem) throw new ArgumentException("Navigation Items/ItemsSource must contain MaterialNavigationItem controls.");
        return false;
    }
    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var previous = SelectedItem;
        foreach (var item in _subscribed)
        {
            if (ItemsView.IndexOf(item) < 0) { item.SetSelected(false); item.Owner = null; item.UpdatePresentation(); }
            item.PropertyChanged -= ItemChanged;
        }
        _subscribed.Clear();
        foreach (var value in ItemsView)
        {
            if (value is not MaterialNavigationItem item) throw new ArgumentException("Navigation items must be MaterialNavigationItem controls.");
            if (item.Owner is not null && item.Owner != this) throw new InvalidOperationException("A navigation item cannot belong to two owners.");
            item.Owner = this;
            item.PropertyChanged += ItemChanged;
            _subscribed.Add(item);
            item.UpdatePresentation();
        }
        var retained = previous is null ? -1 : _subscribed.IndexOf(previous);
        SetCurrentValue(SelectedIndexProperty, retained >= 0 ? retained : Math.Clamp(SelectedIndex, 0, _subscribed.Count - 1 < 0 ? 0 : _subscribed.Count - 1));
        SynchronizeSelection();
    }
    private void ItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsEnabledProperty || e.Property == IsEffectivelyEnabledProperty || e.Property == IsVisibleProperty) UpdateTabStop();
        if (sender == SelectedItem && (e.Property == MaterialNavigationItem.PageContentProperty || e.Property == MaterialNavigationItem.PageContentTemplateProperty)) UpdateContent();
    }
    internal virtual void Activate(MaterialNavigationItem item) => SetCurrentValue(SelectedIndexProperty, _subscribed.IndexOf(item));
    internal virtual bool TryActivate(MaterialNavigationItem item) { Activate(item); return true; }
    internal bool Navigate(MaterialNavigationItem from, Key key)
    {
        var vertical = this is MaterialNavigationRail or MaterialNavigationDrawer;
        var direction = key switch
        {
            Key.Home => -2, Key.End => 2,
            Key.Down when vertical => 1, Key.Up when vertical => -1,
            Key.Right when !vertical => FlowDirection == FlowDirection.RightToLeft ? -1 : 1,
            Key.Left when !vertical => FlowDirection == FlowDirection.RightToLeft ? 1 : -1,
            _ => 0
        };
        if (direction == 0) return false;
        var eligible = _subscribed.Where(item => item.IsEffectivelyEnabled && item.IsVisible).ToList();
        if (eligible.Count == 0) return true;
        var index = eligible.IndexOf(from);
        var next = direction == -2 ? eligible[0] : direction == 2 ? eligible[^1] : eligible[(index + direction + eligible.Count) % eligible.Count];
        if (this is not MaterialNavigationDrawer) Activate(next);
        next.Focus(NavigationMethod.Directional);
        RevealSelection();
        return true;
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Presenter?.ApplyTemplate();
        _headerScrollViewer = e.NameScope.Find<ScrollViewer>("PART_HeaderScrollViewer");
        UpdateTabStop();
    }
    private void UpdateTabStop()
    {
        if (ItemsPanelRoot is not { } panel) return;
        KeyboardNavigation.SetTabNavigation(panel, KeyboardNavigationMode.Once);
        KeyboardNavigation.SetTabOnceActiveElement(panel, SelectedItem is { IsEffectivelyEnabled: true, IsVisible: true } selected ? selected : _subscribed.FirstOrDefault(item => item.IsEffectivelyEnabled && item.IsVisible));
    }
    private void RevealSelection() => Dispatcher.UIThread.Post(() =>
    {
        if (VisualRoot is null || _headerScrollViewer is not { } scroll || (_subscribed.FirstOrDefault(candidate => candidate.IsFocused) ?? SelectedItem) is not { } item) return;
        if (item.TranslatePoint(default, scroll) is not { } location) return;
        // Only scroll our header. Initial selection must not jump an enclosing gallery/page scroller.
        var offset = scroll.Offset;
        static double Reveal(double start, double length, double viewport, double current) =>
            start < 0 ? current + start : start + length > viewport ? current + start + length - viewport : current;
        scroll.Offset = new Vector(
            scroll.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled ? offset.X : Reveal(location.X, item.Bounds.Width, scroll.Viewport.Width, offset.X),
            scroll.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled ? offset.Y : Reveal(location.Y, item.Bounds.Height, scroll.Viewport.Height, offset.Y));
        // An already focused item must also remain reachable in an enclosing host scroller after resize.
        // Unlike initial/programmatic selection, this is an explicit user focus path.
        if (item.IsFocused) item.BringIntoView();
    }, DispatcherPriority.Loaded);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty) SynchronizeSelection();
        if (change.Property == ItemLayoutProperty) RefreshPresentation();
        if (change.Property == BoundsProperty) RevealSelection();
    }
    private void SynchronizeSelection()
    {
        if (_synchronizing) return;
        _synchronizing = true;
        var previous = SelectedItem;
        var index = SelectedIndex >= 0 && SelectedIndex < _subscribed.Count ? SelectedIndex : (_subscribed.Count > 0 ? 0 : -1);
        SetCurrentValue(SelectedIndexProperty, index);
        var next = index >= 0 ? _subscribed[index] : null;
        foreach (var item in _subscribed) item.SetSelected(item == next);
        SetAndRaise(SelectedItemProperty, ref _selectedItem, next);
        UpdateContent();
        _synchronizing = false;
        UpdateTabStop();
        RevealSelection();
        if (previous != next) RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, previous is null ? Array.Empty<object>() : new object[] { previous }, next is null ? Array.Empty<object>() : new object[] { next }));
    }
    private void UpdateContent()
    {
        SetAndRaise(SelectedContentTemplateProperty, ref _selectedContentTemplate, SelectedItem?.PageContentTemplate);
        SetAndRaise(SelectedContentProperty, ref _selectedContent, SelectedItem?.PageContent);
    }
}

/// <summary>A bottom navigation bar with horizontally distributed destinations.</summary>
public class MaterialNavigationBar : MaterialNavigation { }

/// <summary>A non-modal side rail. Expansion is presentation only and never replaces items or selection.</summary>
public class MaterialNavigationRail : MaterialNavigation
{
    public static readonly StyledProperty<double> ExpandedWidthProperty = AvaloniaProperty.Register<MaterialNavigationRail, double>(nameof(ExpandedWidth), 220, validate: value => double.IsFinite(value) && value >= 220 && value <= 360);
    public static readonly StyledProperty<bool> UseNarrowWidthProperty = AvaloniaProperty.Register<MaterialNavigationRail, bool>(nameof(UseNarrowWidth));
    public double ExpandedWidth { get => GetValue(ExpandedWidthProperty); set => SetValue(ExpandedWidthProperty, value); }
    public bool UseNarrowWidth { get => GetValue(UseNarrowWidthProperty); set => SetValue(UseNarrowWidthProperty, value); }
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialNavigationRail, bool>(nameof(IsExpanded), defaultBindingMode: BindingMode.TwoWay);
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty || change.Property == ExpandedWidthProperty || change.Property == UseNarrowWidthProperty) RefreshPresentation();
    }
}
