using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Data;
using Avalonia.Controls.Metadata;
using Avalonia.Automation.Provider;
using Avalonia.Automation;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Interactivity;
using System.Runtime.CompilerServices;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

public enum MaterialMenuVariant { Standard, Vibrant, LegacyDropdown }
public enum MaterialMenuToggleMode { None, Check, Radio }

/// <summary>A window-local Material menu. Items are named MaterialMenuItem controls or decorative separators.</summary>
[PseudoClasses(":vibrant", ":segmented")]
public class MaterialMenu : ItemsControl
{
    public static readonly StyledProperty<MaterialMenuVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialMenu, MaterialMenuVariant>(nameof(Variant), inherits: true, validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<bool> IsSegmentedProperty =
        AvaloniaProperty.Register<MaterialMenu, bool>(nameof(IsSegmented), inherits: true);
    public MaterialMenuVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public bool IsSegmented { get => GetValue(IsSegmentedProperty); set => SetValue(IsSegmentedProperty, value); }
    public static readonly DirectProperty<MaterialMenu, bool> IsOpenProperty =
        AvaloniaProperty.RegisterDirect<MaterialMenu, bool>(nameof(IsOpen), menu => menu.IsOpen);
    private bool _isOpen;
    private MaterialOverlayHost? _host;
    private MaterialMenu? _parentMenu;
    private MaterialMenu? _childMenu;
    private MaterialFeedbackLifetime? _lifetime;
    private string _typeAhead = "";
    private DateTimeOffset _lastTyping;
    private static readonly ConditionalWeakTable<MaterialOverlayHost, MenuSlot> Slots = new();
    private sealed class MenuSlot { public MaterialMenu? Current; }
    public bool IsOpen => _isOpen;
    public MaterialOverlaySession? Session { get; private set; }
    protected override Type StyleKeyOverride => typeof(MaterialMenu);
    protected override AutomationPeer OnCreateAutomationPeer() => new MenuPeer(this);
    internal IEnumerable<MaterialMenuItem> Rows => Items.SelectMany(item => item switch
    {
        MaterialMenuItem row => new[] { row },
        MaterialMenuGroup group => group.Items.OfType<MaterialMenuItem>(),
        _ => Enumerable.Empty<MaterialMenuItem>()
    });
    public MaterialMenu()
    {
        Focusable = true;
        Items.CollectionChanged += (_, _) => ReconcileItems();
    }
    internal void ReconcileItems()
    {
        var groups = Items.OfType<MaterialMenuGroup>().ToArray();
        for (var index = 0; index < groups.Length; index++) groups[index].SetGroupPosition(index, groups.Length);
        if (!IsOpen) return;
        if (_childMenu is { IsOpen: true } child && !Rows.Any(row => row.Submenu == child)) child.Dismiss();
        Dispatcher.UIThread.Post(() =>
        {
            if (Session is { IsTop: true } && !Rows.Any(row => row.IsFocused && CanFocus(row)))
                (Rows.FirstOrDefault(CanFocus) as Control ?? this).Focus(NavigationMethod.Tab);
        });
    }
    public MaterialOverlaySession Show(MaterialOverlayHost host, Control anchor, MaterialOverlayOptions? options = null) => ShowCore(host, anchor, options, null);
    private MaterialOverlaySession ShowCore(MaterialOverlayHost host, Control anchor, MaterialOverlayOptions? options, MaterialMenu? parentMenu)
    {
        if (IsOpen) throw new InvalidOperationException("This menu is already open.");
        if (IsSegmented && Variant == MaterialMenuVariant.LegacyDropdown) throw new InvalidOperationException("Segmented groups use expressive standard or vibrant tokens, not legacy MenuTokens.");
        if (!anchor.IsEffectivelyVisible || !anchor.IsEffectivelyEnabled) throw new InvalidOperationException("Menus need a visible enabled anchor.");
        var slot = Slots.GetOrCreateValue(host);
        if (parentMenu is null && slot.Current is { IsOpen: true } previous && !previous.Dismiss())
            throw new InvalidOperationException("The existing menu is covered or its close was vetoed.");
        _parentMenu = parentMenu;
        ReconcileItems();
        var session = host.Show(this, options ?? new MaterialOverlayOptions
        {
            Placement = MaterialOverlayPlacement.Anchor, Anchor = anchor, Margin = new Thickness(8, 48),
            ShowScrim = false, CloseOnLightDismiss = true, InitialFocus = Rows.FirstOrDefault(CanFocus), ReturnFocus = anchor
        });
        Session = session;
        _host = host;
        _lifetime = new MaterialFeedbackLifetime(host, session, null, TimeProvider.System, default, anchor);
        var lifetime = _lifetime;
        lifetime.StateChanged += (_, _) => { if (lifetime.IsEnding && _childMenu is { IsOpen: true } child) child.Dismiss(); };
        if (parentMenu is null) slot.Current = this;
        var window = TopLevel.GetTopLevel(host) as Window;
        EventHandler deactivated = (_, _) => lifetime.RequestDismiss();
        if (window is not null) window.Deactivated += deactivated;
        SetAndRaise(IsOpenProperty, ref _isOpen, true);
        session.Closed += (_, result) =>
        {
            Session = null; _host = null; SetAndRaise(IsOpenProperty, ref _isOpen, false);
            _lifetime?.Dispose(); _lifetime = null;
            if (window is not null) window.Deactivated -= deactivated;
            if (slot.Current == this) slot.Current = null;
            if (_parentMenu is { } parent) parent._childMenu = null;
            if (result.Reason == MaterialOverlayCloseReason.LightDismiss && _parentMenu is { } owner)
                Dispatcher.UIThread.Post(() => owner.Dismiss());
        };
        return session;
    }
    public bool Dismiss()
    {
        if (_childMenu is { IsOpen: true } child && !child.Dismiss()) return false;
        return Session?.Dismiss() ?? false;
    }
    public IDisposable AttachContext(MaterialOverlayHost host, Control target) => new ContextAttachment(this, host, target);
    /// <summary>Show at a point local to the live target. No screen-coordinate or native popup ownership is assumed.</summary>
    public MaterialOverlaySession ShowContext(MaterialOverlayHost host, Control target, Point localPoint) =>
        Show(host, target, new MaterialOverlayOptions
        {
            Placement = MaterialOverlayPlacement.Anchor, Anchor = target, AnchorPoint = localPoint, Margin = new Thickness(8, 48),
            ShowScrim = false, CloseOnLightDismiss = true, ReturnFocus = target, InitialFocus = Rows.FirstOrDefault(CanFocus)
        });
    internal bool OpenSubmenu(MaterialMenuItem item)
    {
        if (!item.IsEffectivelyEnabled || item.Submenu is not { } child || _host is null) return false;
        if (_childMenu == child && child.IsOpen) return true;
        if (_childMenu is { IsOpen: true } previous && !previous.Dismiss()) return false;
        if (Session is not { IsTop: true }) return false;
        child.Variant = Variant; child.IsSegmented = IsSegmented;
        child.ShowCore(_host, item, new MaterialOverlayOptions
        {
            Placement = MaterialOverlayPlacement.Anchor, Anchor = item, AnchorPosition = MaterialOverlayAnchorPosition.End,
            Margin = new Thickness(8, 48), IsModal = false, ShowScrim = false, CloseOnLightDismiss = true,
            ReturnFocus = item, InitialFocus = child.Rows.FirstOrDefault(CanFocus)
        }, this);
        _childMenu = child;
        return true;
    }
    internal bool Activate(MaterialMenuItem item, Action action)
    {
        if (!item.IsEffectivelyEnabled || _lifetime?.IsEnding == true || _parentMenu?._lifetime?.IsEnding == true) return false;
        if (item.Submenu is not null) return OpenSubmenu(item);
        if (_childMenu is { IsOpen: true } child && !child.Dismiss()) return false;
        if (Session is not { IsTop: true }) return false;
        if (item.Command?.CanExecute(item.CommandParameter) == false) return false;
        if (_parentMenu is { } parent && !item.StaysOpenOnClick)
        {
            if (!Dismiss()) return false;
            return parent.Activate(item, action);
        }
        if (!item.StaysOpenOnClick && !Session.Close(item.Value, action)) return false;
        if (item.StaysOpenOnClick) action();
        if (item.ToggleMode == MaterialMenuToggleMode.Check) item.SetCurrentValue(MaterialMenuItem.IsCheckedProperty, !item.IsChecked);
        if (item.ToggleMode == MaterialMenuToggleMode.Radio)
        {
            var selectionMenu = item.GetVisualAncestors().OfType<MaterialMenu>().FirstOrDefault() ?? this;
            foreach (var other in selectionMenu.Rows.Where(row => row.ToggleMode == MaterialMenuToggleMode.Radio && row.GroupName == item.GroupName))
                other.SetCurrentValue(MaterialMenuItem.IsCheckedProperty, other == item);
            item.SetCurrentValue(MaterialMenuItem.IsCheckedProperty, true);
        }
        return true;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty) PseudoClasses.Set(":vibrant", Variant == MaterialMenuVariant.Vibrant);
        if (change.Property == VariantProperty) PseudoClasses.Set(":legacy", Variant == MaterialMenuVariant.LegacyDropdown);
        if (change.Property == IsSegmentedProperty) PseudoClasses.Set(":segmented", IsSegmented);
    }
    private static bool CanFocus(MaterialMenuItem item) => item.IsEffectivelyEnabled && item.IsEffectivelyVisible;
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        var forward = FlowDirection == FlowDirection.RightToLeft ? Key.Left : Key.Right;
        var backward = FlowDirection == FlowDirection.RightToLeft ? Key.Right : Key.Left;
        if (e.Key == forward && Rows.FirstOrDefault(item => item.IsFocused) is { } row && OpenSubmenu(row)) { e.Handled = true; return; }
        if ((e.Key == backward || e.Key == Key.Escape) && _parentMenu is not null) { Dismiss(); e.Handled = true; return; }
        if (e.Key == Key.Tab) { Dismiss(); e.Handled = true; return; }
        if (e.Key is Key.Left or Key.Right && e.Source is Control source &&
            source.GetVisualAncestors().OfType<MaterialMenuGroup>().FirstOrDefault() is { Orientation: Orientation.Horizontal } group)
        {
            var members = group.Items.OfType<MaterialMenuItem>().Where(CanFocus).ToArray();
            var index = Array.FindIndex(members, item => item.IsFocused);
            if (members.Length > 0) members[(index + (e.Key == forward ? 1 : members.Length - 1)) % members.Length].Focus(NavigationMethod.Directional);
            e.Handled = true; return;
        }
        if (e.Key is not (Key.Up or Key.Down or Key.Home or Key.End)) return;
        var rows = Rows.Where(CanFocus).ToArray();
        if (rows.Length > 0)
        {
            var index = Array.FindIndex(rows, item => item.IsFocused);
            var next = e.Key switch { Key.Home => 0, Key.End => rows.Length - 1, Key.Up => (index - 1 + rows.Length) % rows.Length, _ => (index + 1) % rows.Length };
            rows[next].Focus(NavigationMethod.Directional); rows[next].BringIntoView();
        }
        e.Handled = true;
    }
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Handled || string.IsNullOrWhiteSpace(e.Text)) return;
        var now = DateTimeOffset.UtcNow;
        if (now - _lastTyping > TimeSpan.FromSeconds(1)) _typeAhead = "";
        _lastTyping = now; _typeAhead += e.Text;
        var rows = Rows.Where(CanFocus).ToArray();
        var current = Array.FindIndex(rows, row => row.IsFocused);
        var match = Enumerable.Range(1, rows.Length).Select(offset => rows[(current + offset + rows.Length) % rows.Length])
            .FirstOrDefault(row => (row.Content as string)?.StartsWith(_typeAhead, StringComparison.CurrentCultureIgnoreCase) == true);
        if (match is not null) { match.Focus(NavigationMethod.Directional); match.BringIntoView(); e.Handled = true; }
    }
    private sealed class ContextAttachment : IDisposable
    {
        private readonly MaterialMenu _menu;
        private readonly MaterialOverlayHost _host;
        private readonly Control _target;
        private bool _disposed;
        public ContextAttachment(MaterialMenu menu, MaterialOverlayHost host, Control target)
        {
            _menu = menu; _host = host; _target = target;
            target.ContextRequested += Request; target.KeyDown += Key;
            target.DetachedFromVisualTree += Detach;
        }
        private void Request(object? sender, ContextRequestedEventArgs e)
        {
            if (_disposed || _menu.IsOpen || !_target.IsEffectivelyEnabled) return;
            var point = e.TryGetPosition(_target, out var pointer) ? pointer : new Point(0, _target.Bounds.Height);
            _menu.ShowContext(_host, _target, point); e.Handled = true;
        }
        private void Key(object? sender, KeyEventArgs e)
        {
            if (e.Key == Input.Key.Apps || e.Key == Input.Key.F10 && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (!_menu.IsOpen && _target.IsEffectivelyEnabled) _menu.ShowContext(_host, _target, new Point(0, _target.Bounds.Height));
                e.Handled = true;
            }
        }
        private void Detach(object? sender, VisualTreeAttachmentEventArgs e) => Dispose();
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            _target.ContextRequested -= Request; _target.KeyDown -= Key; _target.DetachedFromVisualTree -= Detach;
            if (_menu.Session?.Options.Anchor == _target) _menu._lifetime?.RequestDismiss();
        }
    }
    private sealed class MenuPeer(MaterialMenu menu) : ControlAutomationPeer(menu)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;
        protected override string GetClassNameCore() => nameof(MaterialMenu);
    }
}

/// <summary>An actionable row. Value is host data; invocation returns it through the menu session.</summary>
[PseudoClasses(":checked", ":checkable", ":vibrant", ":segmented", ":submenu")]
public class MaterialMenuItem : Button
{
    public static readonly StyledProperty<object?> ValueProperty = AvaloniaProperty.Register<MaterialMenuItem, object?>(nameof(Value));
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public static readonly StyledProperty<MaterialMenuToggleMode> ToggleModeProperty = AvaloniaProperty.Register<MaterialMenuItem, MaterialMenuToggleMode>(nameof(ToggleMode), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<MaterialMenuItem, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> StaysOpenOnClickProperty = AvaloniaProperty.Register<MaterialMenuItem, bool>(nameof(StaysOpenOnClick));
    public static readonly StyledProperty<string?> GroupNameProperty = AvaloniaProperty.Register<MaterialMenuItem, string?>(nameof(GroupName));
    public static readonly StyledProperty<MaterialMenu?> SubmenuProperty = AvaloniaProperty.Register<MaterialMenuItem, MaterialMenu?>(nameof(Submenu));
    public static readonly StyledProperty<object?> LeadingIconProperty = AvaloniaProperty.Register<MaterialMenuItem, object?>(nameof(LeadingIcon));
    public static readonly StyledProperty<object?> TrailingIconProperty = AvaloniaProperty.Register<MaterialMenuItem, object?>(nameof(TrailingIcon));
    public static readonly StyledProperty<string?> SupportingTextProperty = AvaloniaProperty.Register<MaterialMenuItem, string?>(nameof(SupportingText));
    public static readonly StyledProperty<string?> TrailingTextProperty = AvaloniaProperty.Register<MaterialMenuItem, string?>(nameof(TrailingText));
    public static readonly StyledProperty<bool> IsIconOnlyProperty = AvaloniaProperty.Register<MaterialMenuItem, bool>(nameof(IsIconOnly));
    public static readonly StyledProperty<IBrush?> SecondaryForegroundProperty = AvaloniaProperty.Register<MaterialMenuItem, IBrush?>(nameof(SecondaryForeground));
    public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<MaterialMenuItem, IBrush?>(nameof(IconForeground));
    public static readonly StyledProperty<MaterialMenuVariant> VariantProperty = MaterialMenu.VariantProperty.AddOwner<MaterialMenuItem>();
    public static readonly StyledProperty<bool> IsSegmentedProperty = MaterialMenu.IsSegmentedProperty.AddOwner<MaterialMenuItem>();
    public MaterialMenuToggleMode ToggleMode { get => GetValue(ToggleModeProperty); set => SetValue(ToggleModeProperty, value); }
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public bool StaysOpenOnClick { get => GetValue(StaysOpenOnClickProperty); set => SetValue(StaysOpenOnClickProperty, value); }
    public string? GroupName { get => GetValue(GroupNameProperty); set => SetValue(GroupNameProperty, value); }
    public MaterialMenu? Submenu { get => GetValue(SubmenuProperty); set => SetValue(SubmenuProperty, value); }
    public object? LeadingIcon { get => GetValue(LeadingIconProperty); set => SetValue(LeadingIconProperty, value); }
    public object? TrailingIcon { get => GetValue(TrailingIconProperty); set => SetValue(TrailingIconProperty, value); }
    public string? SupportingText { get => GetValue(SupportingTextProperty); set => SetValue(SupportingTextProperty, value); }
    public string? TrailingText { get => GetValue(TrailingTextProperty); set => SetValue(TrailingTextProperty, value); }
    public bool IsIconOnly { get => GetValue(IsIconOnlyProperty); set => SetValue(IsIconOnlyProperty, value); }
    public IBrush? SecondaryForeground { get => GetValue(SecondaryForegroundProperty); set => SetValue(SecondaryForegroundProperty, value); }
    public IBrush? IconForeground { get => GetValue(IconForegroundProperty); set => SetValue(IconForegroundProperty, value); }
    internal void SetGroupPosition(int index, int count)
    {
        PseudoClasses.Set(":first", index == 0); PseudoClasses.Set(":last", index == count - 1);
        PseudoClasses.Set(":standalone", count == 1);
    }
    public static readonly StyledProperty<Orientation> GroupOrientationProperty = MaterialMenuGroup.OrientationProperty.AddOwner<MaterialMenuItem>();
    public Orientation GroupOrientation => GetValue(GroupOrientationProperty);
    public MaterialMenuVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public bool IsSegmented { get => GetValue(IsSegmentedProperty); set => SetValue(IsSegmentedProperty, value); }
    protected override Type StyleKeyOverride => typeof(MaterialMenuItem);
    protected override AutomationPeer OnCreateAutomationPeer() => new MenuItemPeer(this);
    protected override void OnClick()
    {
        var menu = this.GetVisualAncestors().OfType<MaterialMenu>().FirstOrDefault();
        menu?.Activate(this, () => base.OnClick());
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty) PseudoClasses.Set(":checked", IsChecked);
        if (change.Property == ToggleModeProperty) PseudoClasses.Set(":checkable", ToggleMode != MaterialMenuToggleMode.None);
        if (change.Property == VariantProperty) PseudoClasses.Set(":vibrant", Variant == MaterialMenuVariant.Vibrant);
        if (change.Property == VariantProperty) PseudoClasses.Set(":legacy", Variant == MaterialMenuVariant.LegacyDropdown);
        if (change.Property == IsSegmentedProperty) PseudoClasses.Set(":segmented", IsSegmented);
        if (change.Property == SubmenuProperty) PseudoClasses.Set(":submenu", Submenu is not null);
        if (change.Property == IsIconOnlyProperty) PseudoClasses.Set(":icon-only", IsIconOnly);
        if (change.Property == GroupOrientationProperty) PseudoClasses.Set(":horizontal", GroupOrientation == Orientation.Horizontal);
        if (change.Property == IsEnabledProperty || change.Property == IsVisibleProperty)
            this.GetVisualAncestors().OfType<MaterialMenu>().FirstOrDefault()?.ReconcileItems();
    }
    private sealed class MenuItemPeer : ButtonAutomationPeer, IToggleProvider, IExpandCollapseProvider
    {
        private readonly MaterialMenuItem item;
        private volatile bool _toggle, _submenu;
        public MenuItemPeer(MaterialMenuItem owner) : base(owner)
        {
            item = owner; _toggle = item.ToggleMode != MaterialMenuToggleMode.None; _submenu = item.Submenu is not null;
            item.PropertyChanged += (_, change) =>
            {
                if (change.Property == ToggleModeProperty) _toggle = item.ToggleMode != MaterialMenuToggleMode.None;
                if (change.Property == SubmenuProperty) _submenu = item.Submenu is not null;
                if (change.Property == IsCheckedProperty)
                {
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                        change.GetOldValue<bool>() ? ToggleState.On : ToggleState.Off,
                        change.GetNewValue<bool>() ? ToggleState.On : ToggleState.Off);
                    RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, null, GetItemStatus());
                }
            };
        }
        public ToggleState ToggleState => item.IsChecked ? ToggleState.On : ToggleState.Off;
        public void Toggle() { if (item.ToggleMode != MaterialMenuToggleMode.None) item.OnClick(); }
        public ExpandCollapseState ExpandCollapseState => item.Submenu?.IsOpen == true ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public bool ShowsMenu => true;
        public void Expand() => item.GetVisualAncestors().OfType<MaterialMenu>().FirstOrDefault()?.OpenSubmenu(item);
        public void Collapse() => item.Submenu?.Dismiss();
        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IToggleProvider) && !_toggle ||
            providerType == typeof(IExpandCollapseProvider) && !_submenu ? null : base.GetProviderCore(providerType);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;
        protected override string GetClassNameCore() => nameof(MaterialMenuItem);
        protected override string? GetHelpTextCore() => base.GetHelpTextCore() ?? item.SupportingText;
        protected override string? GetItemStatusCore() => item.ToggleMode == MaterialMenuToggleMode.None ? base.GetItemStatusCore() : item.IsChecked ? "Checked" : "Unchecked";
    }
}

/// <summary>One expressive group; horizontal groups wrap at constrained window widths rather than clip targets.</summary>
public class MaterialMenuGroup : ItemsControl
{
    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<MaterialMenuGroup, bool>(nameof(IsActive), true);
    public bool IsActive { get => GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialMenuGroup, Orientation>(nameof(Orientation), Orientation.Vertical, inherits: true);
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public static readonly StyledProperty<MaterialMenuVariant> VariantProperty = MaterialMenu.VariantProperty.AddOwner<MaterialMenuGroup>();
    public MaterialMenuVariant Variant => GetValue(VariantProperty);
    public MaterialMenuGroup()
    {
        Items.CollectionChanged += (_, _) => this.GetVisualAncestors().OfType<MaterialMenu>().FirstOrDefault()?.ReconcileItems();
        ItemsPanel = new FuncTemplate<Panel?>(() =>
        {
            var panel = new MaterialMenuGroupPanel();
            panel.Bind(MaterialMenuGroupPanel.OrientationProperty, new Binding(nameof(Orientation)) { Source = this });
            return panel;
        });
    }
    protected override Type StyleKeyOverride => typeof(MaterialMenuGroup);
    internal void SetGroupPosition(int index, int count)
    {
        PseudoClasses.Set(":first", index == 0); PseudoClasses.Set(":last", index == count - 1);
        PseudoClasses.Set(":standalone", count == 1);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty) PseudoClasses.Set(":vibrant", Variant == MaterialMenuVariant.Vibrant);
        if (change.Property == IsActiveProperty) PseudoClasses.Set(":inactive", !IsActive);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ControlAutomationPeer(this);
}

internal sealed class MaterialMenuGroupPanel : Panel
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialMenuGroupPanel, Orientation>(nameof(Orientation));
    public Orientation Orientation => GetValue(OrientationProperty);
    private int _columns = 1;
    private double _rowHeight;
    protected override Size MeasureOverride(Size availableSize)
    {
        _columns = Orientation == Orientation.Horizontal ? Math.Max(1, Math.Min(Children.Count, (int)(double.IsFinite(availableSize.Width) ? availableSize.Width / 48 : Children.Count))) : 1;
        var childWidth = double.IsFinite(availableSize.Width) ? Math.Max(0, (availableSize.Width - (_columns - 1) * 2) / _columns) : double.PositiveInfinity;
        _rowHeight = 0;
        var width = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(childWidth, double.PositiveInfinity));
            _rowHeight = Math.Max(_rowHeight, child.DesiredSize.Height); width = Math.Max(width, child.DesiredSize.Width);
        }
        for (var index = 0; index < Children.Count; index++)
            if (Children[index] is MaterialMenuItem item) item.SetGroupPosition(index, Children.Count);
        var rows = (Children.Count + _columns - 1) / _columns;
        return new Size(width * _columns + (_columns - 1) * 2, rows * _rowHeight + Math.Max(0, rows - 1) * 2);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0, (finalSize.Width - (_columns - 1) * 2) / _columns);
        for (var index = 0; index < Children.Count; index++)
            Children[index].Arrange(new Rect(index % _columns * (width + 2), index / _columns * (_rowHeight + 2), width, _rowHeight));
        return finalSize;
    }
}
