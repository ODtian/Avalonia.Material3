using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Controls.Metadata;

namespace Avalonia.Material3.Controls;

/// <summary>Named, keyboard reachable gesture affordance. The owner sheet allocates handle pointer input.</summary>
[PseudoClasses(":vertical", ":dragging")]
public class MaterialSheetDragHandle : TemplatedControl
{
    public static readonly StyledProperty<MaterialSheet?> SheetProperty =
        AvaloniaProperty.Register<MaterialSheetDragHandle, MaterialSheet?>(nameof(Sheet));
    public MaterialSheet? Sheet { get => GetValue(SheetProperty); set => SetValue(SheetProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == SheetProperty)
        {
            if (e.OldValue is MaterialSheet previous) previous.PropertyChanged -= SheetChanged;
            if (e.NewValue is MaterialSheet current && TopLevel.GetTopLevel(this) is not null) current.PropertyChanged += SheetChanged;
            UpdateState();
        }
    }
    private void SheetChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => UpdateState();
    private void UpdateState()
    {
        PseudoClasses.Set(":vertical", Sheet?.IsSideSheet == true);
        PseudoClasses.Set(":dragging", Sheet?.IsDragging == true);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Sheet is { } sheet) { sheet.PropertyChanged -= SheetChanged; sheet.PropertyChanged += SheetChanged; }
        UpdateState();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Sheet is { } sheet) sheet.PropertyChanged -= SheetChanged;
        base.OnDetachedFromVisualTree(e);
    }
    protected override Type StyleKeyOverride => typeof(MaterialSheetDragHandle);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialSheetHandleAutomationPeer(this);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Sheet is not { } sheet || !IsEffectivelyEnabled || e.Handled) return;
        switch (e.Key)
        {
            case Key.Up: case Key.Home: sheet.Expand(); break;
            case Key.Down: sheet.Collapse(); break;
            case Key.End: sheet.Dismiss(); break;
            case Key.Enter: case Key.Space:
                if (sheet.State == MaterialSheetState.Expanded) { if (!sheet.Collapse()) sheet.Dismiss(); }
                else sheet.Expand();
                break;
            case Key.Left: case Key.Right:
                if (!sheet.IsSideSheet) return;
                if ((e.Key == Key.Right) == sheet.IsPhysicalLeft) sheet.Expand(); else sheet.Dismiss();
                break;
            default: return;
        }
        e.Handled = true;
    }
}
