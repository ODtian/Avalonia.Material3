using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SelectionAdaptationScenarioTests
{
    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Gallery_long_labels_wrap_grow_with_font_scale_and_reflow_in_a_wider_host(int example)
    {
        var page = new SelectionFormPage();
        var control = page.LongLabelExamples[example];
        page.Children.Remove(control);
        using var host = new SelectionHost(control);
        host.Window.Width = 280;
        host.Capture();
        var originalHeight = control.Bounds.Height;
        Assert.True(originalHeight > 48);
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Capture();
        Assert.Equal(32, control.FontSize);
        Assert.True(control.Bounds.Height > originalHeight);
        Assert.True(control.Bounds.Width <= 232);
        var enlargedHeight = control.Bounds.Height;
        host.Window.Width = 900;
        host.Capture();
        Assert.True(control.Bounds.Height < enlargedHeight);
        Assert.True(control.Bounds.Height >= 48);
        var point = host.PointIn(control, new Point(24, 1));
        using var contact = host.Window.TouchBegin(point);
        host.Window.TouchEnd(contact, point);
        Assert.Equal(example == 0 ? false : true, control.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData("checkbox")]
    [InlineData("radio")]
    [InlineData("switch")]
    public void Host_content_template_is_visible_and_keeps_selection_and_theme_updates(string kind)
    {
        ToggleButton control = kind switch
        {
            "checkbox" => new MaterialCheckBox(),
            "radio" => new MaterialRadioButton(),
            _ => new MaterialSwitch()
        };
        var label = new TextBlock { Text = "自定义 / Custom label", TextWrapping = TextWrapping.Wrap };
        control.Content = "host-value";
        control.ContentTemplate = new FuncDataTemplate<string>((_, _) => label);
        using var host = new SelectionHost(control);
        Assert.True(label.Bounds.Width > 0);
        Assert.True(label.IsEffectivelyVisible);
        host.Click(control, new Point(kind == "switch" ? 54 : 24, 24));
        Assert.True(control.IsChecked);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Capture();
        Assert.Equal(Color.Parse("#D0BCFF"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
        host.Theme.DarkColorScheme = host.Theme.DarkColorScheme with { Primary = Color.Parse("#80CBC4") };
        host.Capture();
        Assert.Equal(Color.Parse("#80CBC4"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Light;
        host.Theme.LightColorScheme = host.Theme.LightColorScheme with { Primary = Color.Parse("#006C4C") };
        host.Capture();
        Assert.Equal(Color.Parse("#006C4C"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
        Assert.Equal("host-value", control.Content);
        Assert.True(control.IsChecked);
    }

    [AvaloniaFact]
    public void Host_can_replace_the_checkbox_template_without_losing_the_public_toggle_contract()
    {
        var label = new TextBlock { Text = "Host checkbox template" };
        var checkbox = new MaterialCheckBox
        {
            Template = new FuncControlTemplate<MaterialCheckBox>((_, _) => new Border { Background = Brushes.Transparent, Child = label })
        };
        using var host = new SelectionHost(checkbox);
        Assert.True(label.Bounds.Width > 0);
        host.Click(checkbox);
        Assert.True(checkbox.IsChecked);
    }
}
