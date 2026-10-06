using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Baseline-aware rich tooltip layout. Template children: title, supporting content, action.</summary>
public class MaterialTooltipPanel : Panel
{
    public MaterialTooltipPanel() => UseLayoutRounding = false;
    public static readonly StyledProperty<MaterialTooltipVariant> VariantProperty = AvaloniaProperty.Register<MaterialTooltipPanel, MaterialTooltipVariant>(nameof(Variant));
    public MaterialTooltipVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    static MaterialTooltipPanel() => AffectsMeasure<MaterialTooltipPanel>(VariantProperty);
    private double _titleY, _bodyY, _actionY;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return base.MeasureOverride(availableSize);
        foreach (var child in Children) child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        if (Variant == MaterialTooltipVariant.Plain) return Children[1].DesiredSize;
        var title = Children[0]; var body = Children[1]; var action = Children[2];
        var titleText = title as TextBlock;
        var bodyText = body as TextBlock ?? body.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        var titleBaseline = titleText?.TextLayout.TextLines.FirstOrDefault()?.Baseline ?? title.DesiredSize.Height;
        var bodyBaseline = bodyText?.TextLayout.TextLines.FirstOrDefault()?.Baseline ?? 0;
        _titleY = title.IsVisible ? Math.Max(0, Math.Max(28, titleBaseline + 12) - titleBaseline) : 0;
        var lastTitleBaseline = titleBaseline + Math.Max(0, title.DesiredSize.Height - (titleText?.TextLayout.TextLines.FirstOrDefault()?.Height ?? title.DesiredSize.Height));
        var baselineGap = Math.Max(24, title.DesiredSize.Height - lastTitleBaseline + bodyBaseline + 4);
        _bodyY = title.IsVisible ? _titleY + lastTitleBaseline + baselineGap - bodyBaseline : 16;
        _actionY = _bodyY + body.DesiredSize.Height + 16;
        var height = _actionY + (action.IsVisible ? Math.Max(48, action.DesiredSize.Height) + 8 : 0);
        return new Size(Children.Max(child => child.DesiredSize.Width), height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3) return base.ArrangeOverride(finalSize);
        var title = Children[0]; var body = Children[1]; var action = Children[2];
        title.Arrange(new Rect(0, _titleY, finalSize.Width, title.DesiredSize.Height));
        body.Arrange(new Rect(0, Variant == MaterialTooltipVariant.Rich ? _bodyY : 0, finalSize.Width, body.DesiredSize.Height));
        action.Arrange(new Rect(0, _actionY, Math.Min(finalSize.Width, action.DesiredSize.Width), Math.Max(48, action.DesiredSize.Height)));
        return finalSize;
    }
}
