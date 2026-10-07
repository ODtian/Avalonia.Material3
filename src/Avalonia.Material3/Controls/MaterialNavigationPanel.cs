using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Controls.Presenters;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>The default non-virtualizing header layout. Items retain identity when presentation changes.</summary>
public class MaterialNavigationPanel : Panel
{
    private static readonly StyledProperty<IBrush?> IndicatorBrushProperty = AvaloniaProperty.Register<MaterialNavigationPanel, IBrush?>("IndicatorBrush");
    private MaterialNavigation? _owner;
    private readonly MaterialMotionValue _tabLeft, _tabWidth;
    private readonly MaterialMotionSettings _motion;
    private readonly TabIndicator _tabIndicator;
    private bool _tabReady;
    public MaterialNavigationPanel()
    {
        UseLayoutRounding = false;
        _tabIndicator = new(this) { IsHitTestVisible = false, ZIndex = int.MaxValue, UseLayoutRounding = false };
        VisualChildren.Add(_tabIndicator); LogicalChildren.Add(_tabIndicator);
        _tabLeft = new(this, 0, _ => _tabIndicator.InvalidateVisual());
        _tabWidth = new(this, 0, _ => _tabIndicator.InvalidateVisual());
        _motion = new(this, RetargetTab);
        MaterialPickerSupport.Resource(this, IndicatorBrushProperty, "PrimaryBrush");
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.GetVisualAncestors().OfType<MaterialNavigation>().FirstOrDefault();
        if (_owner is not null) _owner.PropertyChanged += OwnerChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        _owner = null;
        _tabReady = false;
        base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateMeasure();
    private void RetargetTab()
    {
        if (_owner is not MaterialTabs tabs || tabs.SelectedItem is not { } selected || !Children.Contains(selected) || selected.Bounds.Width <= 0) return;
        var width = selected.Bounds.Width;
        if (tabs.Variant == MaterialTabVariant.Primary)
            width = Math.Max(24, selected.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(content => content.Name == "PART_ContentPresenter")?.Bounds.Width ?? 24);
        var left = selected.Bounds.Left + (selected.Bounds.Width - width) / 2;
        if (!_tabReady)
        {
            _tabReady = true;
            _tabLeft.Snap(left); _tabWidth.Snap(width);
        }
        else
        {
            _tabLeft.Spring(left, _motion.DefaultSpatial); _tabWidth.Spring(width, _motion.DefaultSpatial);
        }
    }
    private sealed class TabIndicator(MaterialNavigationPanel owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            if (owner._owner is not MaterialTabs tabs || !owner._tabReady || owner.GetValue(IndicatorBrushProperty) is not { } brush) return;
            var height = tabs.Variant == MaterialTabVariant.Primary ? 3d : 2d;
            var corners = tabs.Variant == MaterialTabVariant.Primary ? new CornerRadius(3, 3, 0, 0) : default;
            context.DrawRectangle(brush, null, new RoundedRect(new Rect(owner._tabLeft.Value, Math.Max(0, Bounds.Height - height), Math.Max(0, owner._tabWidth.Value), height), corners));
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var vertical = _owner is MaterialNavigationRail;
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : 0;
        var scrollable = _owner is MaterialTabs { Layout: MaterialTabLayout.Scrollable };
        // Long labels grow vertically instead of creating a single tab wider than its viewport.
        var cell = scrollable ? Math.Max(90, _owner!.Bounds.Width > 0 ? _owner.Bounds.Width : 320)
            : !vertical && Children.Count > 0 && width > 0 ? Math.Max(48, width / Children.Count) : double.PositiveInfinity;
        double desiredWidth = 0, desiredHeight = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(vertical ? availableSize.Width : cell, double.PositiveInfinity));
            if (vertical) { desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width); desiredHeight += child.DesiredSize.Height; }
            else { desiredWidth += scrollable ? child.DesiredSize.Width : Math.Max(child.DesiredSize.Width, double.IsFinite(cell) ? cell : 0); desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height); }
        }
        if (vertical && Children.Count > 0) desiredHeight += (Children.Count - 1) * 4;
        return new Size(desiredWidth, desiredHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!VisualChildren.Contains(_tabIndicator)) VisualChildren.Add(_tabIndicator);
        _tabIndicator.Measure(finalSize); _tabIndicator.Arrange(new Rect(finalSize));
        var vertical = _owner is MaterialNavigationRail;
        var scrollable = _owner is MaterialTabs { Layout: MaterialTabLayout.Scrollable };
        var cell = Children.Count > 0 ? Math.Max(48, finalSize.Width / Children.Count) : 0;
        double offset = 0;
        foreach (var child in Children)
        {
            var itemWidth = scrollable ? child.DesiredSize.Width : cell;
            child.Arrange(vertical ? new Rect(0, offset, finalSize.Width, child.DesiredSize.Height) : new Rect(offset, 0, itemWidth, finalSize.Height));
            offset += vertical ? child.DesiredSize.Height + 4 : itemWidth;
        }
        RetargetTab();
        return finalSize;
    }
}
