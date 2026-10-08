using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

// This stock ControlTheme branch disappears when the consumer replaces Theme.
internal sealed class MaterialTimeDialogTemplate : FuncControlTemplate<MaterialDialog>
{
    public MaterialTimeDialogTemplate() : base((owner, scope) =>
    {
        var title = MaterialPickerSupport.Text("LabelMedium");
        title.Margin = new Thickness(0, 0, 0, 20);
        title.Bind(TextBlock.TextProperty, new TemplateBinding(MaterialDialog.TitleProperty));
        var body = new ContentPresenter { Name = "PART_ContentPresenter", HorizontalContentAlignment = HorizontalAlignment.Center };
        scope.Register(body.Name, body);
        body.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        body.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        var actions = new ContentPresenter { HorizontalContentAlignment = HorizontalAlignment.Stretch };
        actions.Bind(ContentPresenter.ContentProperty, new TemplateBinding(MaterialDialog.ActionsProperty));
        actions.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(MaterialDialog.ActionsTemplateProperty));
        var scroll = new ScrollViewer { Content = body,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var surface = new MaterialElevationBorder { Child = new MaterialTimeDialogPanel(title, scroll, actions) };
        surface.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        surface.Bind(Border.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        surface.Bind(Border.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        surface.Bind(Border.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        MaterialPickerSupport.Resource(surface, Border.BoxShadowProperty, "Elevation.Shadow3");
        return surface;
    }) { }
}
