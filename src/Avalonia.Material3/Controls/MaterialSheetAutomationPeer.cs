using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialSheetAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    private readonly MaterialSheet _sheet;
    public MaterialSheetAutomationPeer(MaterialSheet sheet) : base(sheet)
    {
        _sheet = sheet;
        sheet.PropertyChanged += (_, e) =>
        {
            if (e.Property == MaterialSheet.StateProperty)
            {
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, null, ExpandCollapseState);
            }
            if (e.Property == MaterialSheet.StateProperty || e.Property == MaterialSheet.IsModalProperty || e.Property == MaterialSheet.IsDraggingProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, null, GetItemStatus());
            if (e.Property == MaterialSheet.TitleProperty) RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, e.OldValue, GetName());
        };
    }
    internal static ExpandCollapseState Expansion(MaterialSheet? sheet) => sheet?.State switch
    {
        MaterialSheetState.Expanded => ExpandCollapseState.Expanded,
        MaterialSheetState.PartiallyExpanded => ExpandCollapseState.PartiallyExpanded,
        _ => ExpandCollapseState.Collapsed
    };
    protected override string? GetNameCore() => base.GetNameCore() ?? _sheet.Title;
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() ??
        $"{(_sheet.IsDragging ? "dragging" : _sheet.State == MaterialSheetState.Expanded ? "expanded" : _sheet.State == MaterialSheetState.PartiallyExpanded ? "partially expanded" : "hidden")}; {(_sheet.IsModal ? "modal" : "standard")}";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
    protected override string GetClassNameCore() => _sheet.GetType().Name;
    public bool ShowsMenu => false;
    public ExpandCollapseState ExpandCollapseState => Expansion(_sheet);
    public void Expand() { EnsureEnabled(); _sheet.Expand(); }
    public void Collapse() { EnsureEnabled(); if (_sheet.IsSideSheet) _sheet.Dismiss(); else _sheet.Collapse(); }
    protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider) ? this : base.GetProviderCore(providerType);
}

internal sealed class MaterialSheetHandleAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider, IInvokeProvider
{
    private readonly MaterialSheetDragHandle _handle;
    public MaterialSheetHandleAutomationPeer(MaterialSheetDragHandle handle) : base(handle)
    {
        _handle = handle;
        handle.PropertyChanged += (_, e) =>
        {
            if (e.Property == MaterialSheetDragHandle.SheetProperty)
            {
                if (e.OldValue is MaterialSheet previous) previous.PropertyChanged -= SheetChanged;
                if (e.NewValue is MaterialSheet current && Avalonia.Controls.TopLevel.GetTopLevel(handle) is not null) current.PropertyChanged += SheetChanged;
            }
        };
        handle.AttachedToVisualTree += (_, _) =>
        {
            if (handle.Sheet is { } sheet) { sheet.PropertyChanged -= SheetChanged; sheet.PropertyChanged += SheetChanged; }
        };
        handle.DetachedFromVisualTree += (_, _) => { if (handle.Sheet is { } sheet) sheet.PropertyChanged -= SheetChanged; };
        if (handle.Sheet is { } sheet && Avalonia.Controls.TopLevel.GetTopLevel(handle) is not null) sheet.PropertyChanged += SheetChanged;
    }
    private void SheetChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialSheet.StateProperty) RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, null, ExpandCollapseState);
        if (e.Property == MaterialSheet.HandleTextProperty) RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, e.OldValue, GetName());
    }
    protected override string? GetNameCore() => base.GetNameCore() ?? _handle.Sheet?.HandleText;
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() ?? $"{_handle.Sheet?.ExpandText}: Up/Home; {_handle.Sheet?.CollapseText}: Down; {_handle.Sheet?.DismissText}: End";
    protected override string GetClassNameCore() => nameof(MaterialSheetDragHandle);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
    public bool ShowsMenu => false;
    public ExpandCollapseState ExpandCollapseState => MaterialSheetAutomationPeer.Expansion(_handle.Sheet);
    public void Expand() { EnsureEnabled(); _handle.Sheet?.Expand(); }
    public void Collapse() { EnsureEnabled(); if (_handle.Sheet?.IsSideSheet == true) _handle.Sheet.Dismiss(); else _handle.Sheet?.Collapse(); }
    public void Invoke() { EnsureEnabled(); if (_handle.Sheet?.State == MaterialSheetState.Expanded) Collapse(); else Expand(); }
    protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider) || providerType == typeof(IInvokeProvider) ? this : base.GetProviderCore(providerType);
}
