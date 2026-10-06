using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Default adaptive app-bar layout, preserving bounded title/action slots at large fonts.</summary>
public class MaterialAppBarPanel : Panel
{
    private MaterialTopAppBar? Owner { get; set; }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Owner = this.GetVisualAncestors().OfType<MaterialTopAppBar>().FirstOrDefault();
        if (Owner is not null) Owner.PropertyChanged += OwnerChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Owner is not null) Owner.PropertyChanged -= OwnerChanged;
        Owner = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateMeasure();
    private double _topHeight;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 4 || Owner is not { } owner) return default;
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : 1000;
        Children[0].Measure(new Size(Math.Max(0, width * .3), double.PositiveInfinity));
        Children[1].Measure(new Size(Math.Max(0, width * .45), double.PositiveInfinity));
        _topHeight = Math.Max(64, Math.Max(Children[0].DesiredSize.Height, Children[1].DesiredSize.Height) + 8);
        var expandedTitle = owner.IsTwoRow && !owner.UsesCollapsedTitle;
        var titleWidth = Math.Max(0, width - (expandedTitle && owner.CollapsedFraction == 0 ? 32 : Children[0].DesiredSize.Width + Children[1].DesiredSize.Width + 32));
        // Finite hosts retain space for page content. Overflow title/subtitle remains scrollable and
        // fully named, rather than forcing a giant app bar or ellipsizing important host text.
        var budget = double.IsFinite(availableSize.Height) ? Math.Max(owner.NominalHeight, availableSize.Height * .5) : double.PositiveInfinity;
        Children[3].Measure(new Size(titleWidth, double.IsFinite(budget) ? Math.Max(24, budget * .2) : budget));
        var titleBudget = Math.Max(28, budget - (expandedTitle ? _topHeight + owner.TitleBottomPadding : 8) - Children[3].DesiredSize.Height);
        Children[2].Measure(new Size(titleWidth, titleBudget));
        var collapsed = Math.Max(_topHeight, owner.UsesCollapsedTitle || !owner.IsTwoRow ? Children[2].DesiredSize.Height + Children[3].DesiredSize.Height + 8 : 64);
        var expanded = owner.IsTwoRow && !owner.UsesCollapsedTitle
            ? Math.Max(owner.NominalHeight, _topHeight + Children[2].DesiredSize.Height + Children[3].DesiredSize.Height)
            : owner.IsTwoRow ? Math.Max(owner.NominalHeight, Math.Min(owner.ExpandedHeight, budget)) : collapsed;
        owner.SetMeasuredHeights(collapsed, expanded);
        return new Size(width, owner.RenderedHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 4 || Owner is not { } owner) return finalSize;
        // Coordinates are logical. Avalonia applies the inherited FlowDirection mirror once.
        void Arrange(Control child, double x, double y, double width, double height) =>
            child.Arrange(new Rect(x, y, Math.Max(0, width), Math.Max(0, height)));
        var navigation = Children[0].DesiredSize.Width;
        var actions = Children[1].DesiredSize.Width;
        Arrange(Children[0], 4, 4, navigation, _topHeight - 8);
        Arrange(Children[1], finalSize.Width - actions - 4, 4, actions, _topHeight - 8);
        var expanded = owner.IsTwoRow && !owner.UsesCollapsedTitle;
        var start = expanded && owner.CollapsedFraction == 0 ? 16 : navigation + 16;
        var available = Math.Max(0, finalSize.Width - start - (expanded && owner.CollapsedFraction == 0 ? 16 : actions + 16));
        var centered = owner.CenterTitle || owner.Variant == MaterialTopAppBarVariant.CenterAligned;
        var titleWidth = centered ? Math.Min(available, Children[2].DesiredSize.Width) : available;
        var x = centered ? Math.Clamp((finalSize.Width - titleWidth) / 2, start, start + available - titleWidth) : start;
        var titleHeight = Children[2].DesiredSize.Height;
        var subtitleHeight = Children[3].DesiredSize.Height;
        var lastSlot = subtitleHeight > 0 ? Children[3] : Children[2];
        var text = lastSlot.GetVisualDescendants().OfType<TextBlock>().LastOrDefault(candidate => candidate.IsEffectivelyVisible);
        var descender = 0.0;
        if (text is not null && text.TextLayout.TextLines.Count > 0 && text.DesiredSize.Height <= lastSlot.DesiredSize.Height)
        {
            var lines = text.TextLayout.TextLines;
            var baseline = lines.Take(lines.Count - 1).Sum(line => line.Height) + lines[^1].Baseline;
            descender = Math.Max(0, lastSlot.DesiredSize.Height - baseline);
        }
        // The pinned reference aligns the last baseline, not the text-box bottom. If the nominal
        // expanded row cannot fit that padding, clamp it to its remaining space rather than
        // changing the recipe's height solely to add padding.
        var padding = Math.Clamp(owner.TitleBottomPadding - descender, 0, Math.Max(0, finalSize.Height - _topHeight - titleHeight - subtitleHeight));
        var y = expanded ? finalSize.Height - titleHeight - subtitleHeight - padding
            : (finalSize.Height - titleHeight - subtitleHeight) / 2;
        Arrange(Children[2], x, y, titleWidth, titleHeight);
        Arrange(Children[3], centered ? start : x, y + titleHeight, available, subtitleHeight);
        return finalSize;
    }
}
