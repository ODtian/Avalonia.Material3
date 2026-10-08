using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialDateDialogTemplate : FuncControlTemplate<MaterialDialog>
{
    public MaterialDateDialogTemplate() : base((owner, scope) =>
    {
        var body = new ContentPresenter { Name = "PART_ContentPresenter" };
        scope.Register(body.Name, body);
        body.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        body.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        var actions = new MaterialDialogActionsPanel { Name = "DefaultActions", Margin = new Thickness(0, 0, 6, 8) };
        scope.Register(actions.Name, actions);
        foreach (var confirm in new[] { false, true })
        {
            var property = confirm ? MaterialDialog.ConfirmTextProperty : MaterialDialog.CancelTextProperty;
            var button = new MaterialButton { Name = confirm ? "PART_ConfirmButton" : "PART_CancelButton", Variant = MaterialButtonVariant.Text };
            scope.Register(button.Name, button);
            button.Bind(ContentControl.ContentProperty, new TemplateBinding(property));
            button.Bind(Automation.AutomationProperties.NameProperty, new TemplateBinding(property));
            if (confirm) button.Bind(Input.InputElement.IsEnabledProperty, new TemplateBinding(MaterialDialog.IsConfirmEnabledProperty));
            actions.Children.Add(button);
        }
        var custom = new ContentPresenter { Name = "CustomActions", Margin = new Thickness(0, 0, 6, 8) };
        scope.Register(custom.Name, custom);
        custom.Bind(ContentPresenter.ContentProperty, new TemplateBinding(MaterialDialog.ActionsProperty));
        custom.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(MaterialDialog.ActionsTemplateProperty));
        var footer = new Panel { Children = { actions, custom } };
        var bodyClip = new Border { ClipToBounds=true, Child = new MaterialDateDialogPanel(body, footer) };
        bodyClip.Bind(Border.CornerRadiusProperty,new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        var surface = new MaterialElevationBorder { Child = bodyClip };
        surface.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        surface.Bind(Border.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        surface.Bind(Border.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        surface.Bind(Border.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        MaterialPickerSupport.Resource(surface, Border.BoxShadowProperty, "Elevation.Shadow3");
        return surface;
    }) { }
}

internal sealed class MaterialDateDialogPanel : Panel
{
    private readonly Control _body, _actions;
    internal MaterialDateDialogPanel(Control body, Control actions)
    { _body=body; _actions=actions; Children.Add(body); Children.Add(actions); }
    protected override Size MeasureOverride(Size availableSize)
    {
        _actions.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        _body.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - _actions.DesiredSize.Height)));
        return new(Math.Max(_body.DesiredSize.Width, _actions.DesiredSize.Width), _body.DesiredSize.Height + _actions.DesiredSize.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var height = Math.Max(0, finalSize.Height - _actions.DesiredSize.Height);
        _body.Arrange(new Rect(0, 0, finalSize.Width, height));
        _actions.Arrange(new Rect(0, height, finalSize.Width, _actions.DesiredSize.Height));
        return finalSize;
    }
}
