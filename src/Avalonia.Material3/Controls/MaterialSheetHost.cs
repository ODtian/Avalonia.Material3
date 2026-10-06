using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>Standard sheets in normal layout; bottom sheets reserve their peek footprint.</summary>
public class MaterialSheetHost : ContentControl
{
    public static readonly StyledProperty<MaterialSheet?> SheetProperty =
        AvaloniaProperty.Register<MaterialSheetHost, MaterialSheet?>(nameof(Sheet));
    public MaterialSheet? Sheet { get => GetValue(SheetProperty); set => SetValue(SheetProperty, value); }
    public static readonly StyledProperty<Size?> AvailableSizeProperty =
        AvaloniaProperty.Register<MaterialSheetHost, Size?>(nameof(AvailableSize), validate: MaterialSheet.IsValidSize);
    public static readonly StyledProperty<bool> ReserveVisibleExtentProperty =
        AvaloniaProperty.Register<MaterialSheetHost, bool>(nameof(ReserveVisibleExtent));
    public Size? AvailableSize { get => GetValue(AvailableSizeProperty); set => SetValue(AvailableSizeProperty, value); }
    public bool ReserveVisibleExtent { get => GetValue(ReserveVisibleExtentProperty); set => SetValue(ReserveVisibleExtentProperty, value); }
    protected override Type StyleKeyOverride => typeof(MaterialSheetHost);
}

/// <summary>Public replacement-template layout primitive: content then sheet presenters.</summary>
public class MaterialSheetHostPanel : Panel
{
    private MaterialSheetHost? _host;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _host = TemplatedParent as MaterialSheetHost;
        if (_host is not null) _host.PropertyChanged += HostChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_host is not null) _host.PropertyChanged -= HostChanged;
        _host = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void HostChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialSheetHost.AvailableSizeProperty || e.Property == MaterialSheetHost.ReserveVisibleExtentProperty || e.Property == MaterialSheetHost.SheetProperty)
            InvalidateMeasure();
    }
    private static Size Constrain(MaterialSheetHost host, Size size) => host.AvailableSize is { } available ?
        new Size(Math.Min(size.Width, available.Width), Math.Min(size.Height, available.Height)) : size;
    private static double Reservation(MaterialSheetHost host, Size size) => host.Sheet is not { } sheet || sheet.State == MaterialSheetState.Hidden ? 0 :
        Math.Min(sheet.IsSideSheet ? size.Width : size.Height, sheet.IsSideSheet || host.ReserveVisibleExtent ? sheet.VisibleExtent + (sheet.IsSideSheet && sheet.IsDetached ? 32 : 0) : sheet.PeekExtent);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TemplatedParent is not MaterialSheetHost host || Children.Count != 2) return base.MeasureOverride(availableSize);
        availableSize = Constrain(host, availableSize);
        if (!double.IsFinite(availableSize.Width) || !double.IsFinite(availableSize.Height)) throw new InvalidOperationException("MaterialSheetHost requires bounded layout.");
        var detached = host.Sheet is { IsSideSheet: true, IsDetached: true };
        Children[1].Measure(detached ? new Size(Math.Max(0, availableSize.Width - 32), Math.Max(0, availableSize.Height - 32)) : availableSize);
        var reservation = Reservation(host, availableSize);
        Children[0].Measure(host.Sheet?.IsSideSheet == true ? new Size(availableSize.Width - reservation, availableSize.Height) : new Size(availableSize.Width, availableSize.Height - reservation));
        return availableSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (TemplatedParent is not MaterialSheetHost host || Children.Count != 2) return base.ArrangeOverride(finalSize);
        var sizeAvailable = Constrain(host, finalSize);
        var reservation = Reservation(host, sizeAvailable);
        var size = Children[1].DesiredSize;
        if (host.Sheet?.IsSideSheet == true)
        {
            var left = host.Sheet.IsPhysicalLeft;
            var inset = host.Sheet.IsDetached ? Math.Min(16, Math.Min(sizeAvailable.Width, sizeAvailable.Height) / 2) : 0;
            Children[0].Arrange(new Rect(left ? reservation : 0, 0, sizeAvailable.Width - reservation, sizeAvailable.Height));
            Children[1].Arrange(new Rect(left ? inset : Math.Max(inset, sizeAvailable.Width - size.Width - inset), inset, size.Width, Math.Max(0, sizeAvailable.Height - 2 * inset)));
        }
        else
        {
            Children[0].Arrange(new Rect(0, 0, sizeAvailable.Width, sizeAvailable.Height - reservation));
            Children[1].Arrange(new Rect((sizeAvailable.Width - size.Width) / 2, sizeAvailable.Height - size.Height, size.Width, size.Height));
        }
        return finalSize;
    }
}
