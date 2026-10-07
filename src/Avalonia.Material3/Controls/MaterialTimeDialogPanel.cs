using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialTimeDialogPanel : Panel
{
    private bool _landscape;
    private readonly Control title,content,actions;
    public MaterialTimeDialogPanel(Control title,Control content,Control actions)
    {this.title=title;this.content=content;this.actions=actions;Children.Add(title);Children.Add(content);Children.Add(actions);}
    protected override Size MeasureOverride(Size availableSize)
    {
        content.Measure(new Size(Math.Max(0,availableSize.Width-48),availableSize.Height));
        var width=content.DesiredSize.Width;
        title.Measure(new Size(width,double.PositiveInfinity));actions.Measure(new Size(width,double.PositiveInfinity));
        _landscape=content.DesiredSize.Width>content.DesiredSize.Height&&content.DesiredSize.Height>=200;
        var height=_landscape?(double.IsFinite(availableSize.Height)?availableSize.Height:16+content.DesiredSize.Height+4+actions.DesiredSize.Height+8)
            :24+title.DesiredSize.Height+content.DesiredSize.Height+actions.DesiredSize.Height+24;
        return new(width+48,height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        title.Arrange(new Rect(24,24,finalSize.Width-48,title.DesiredSize.Height));
        var y=_landscape?16+(finalSize.Height-(16+content.DesiredSize.Height+4+actions.DesiredSize.Height+8))/2:24+title.DesiredSize.Height;
        content.Arrange(new Rect(24,y,finalSize.Width-48,content.DesiredSize.Height));
        var actionsY=_landscape?y+content.DesiredSize.Height+4-(finalSize.Height>=384?16:0)
            +(finalSize.Height-(16+content.DesiredSize.Height+4+actions.DesiredSize.Height+8))/2:y+content.DesiredSize.Height;
        actions.Arrange(new Rect(24,actionsY,finalSize.Width-48,actions.DesiredSize.Height));
        return finalSize;
    }
}
