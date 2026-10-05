using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class FloatingActionsGalleryTests
{
    [AvaloniaTheory]
    [InlineData(320)]
    [InlineData(1100)]
    public void Package_gallery_demonstrates_every_recipe_and_a_complete_action_chain_at_both_window_widths(int width)
    {
        using var host = new ButtonHost();
        var page = new Gallery.Pages.FloatingActionsPage(host.Theme);
        host.Window.Width = width;
        host.Window.Height = 800;
        host.Window.Content = page;
        host.Capture();
        Assert.Equal(4, page.FabSamples.Count);
        Assert.Equal(5, page.ExtendedSamples.Count);
        Assert.Equal(8, page.Toolbars.Count);
        Assert.Equal(3, page.PreviewMenu.Items.Count);
        Click(page, "floating-font", host);
        Assert.Equal(2, host.Theme.Typography.Scale);
        Click(page, "floating-mode", host);
        Assert.Equal(Avalonia.Styling.ThemeVariant.Dark, host.Window.RequestedThemeVariant);
        Click(page, "floating-motion", host);
        Assert.False(host.Theme.Motion.ReduceMotion);
        Click(page, "floating-motion", host);
        Assert.True(host.Theme.Motion.ReduceMotion);
        page.PreviewMenu.BringIntoView();
        host.Capture();
        var toggle = page.PreviewMenu.GetVisualDescendants().OfType<MaterialFab>().Single();
        Click(toggle, host);
        host.Capture();
        Assert.True(page.PreviewMenu.IsExpanded);
        var item = page.PreviewMenu.Items[0];
        item.BringIntoView();
        host.Capture();
        Click(item, host);
        Assert.False(page.PreviewMenu.IsExpanded);
        Assert.Contains("Document created", page.Result.Text);
        Assert.True(toggle.IsFocused);
        var toolbar = page.Toolbars.Last();
        toolbar.BringIntoView();
        host.Capture();
        var action = toolbar.Items.OfType<MaterialIconButton>().First();
        action.BringIntoView();
        host.Capture();
        action.Focus(NavigationMethod.Tab);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Contains("Tool selected", page.Result.Text);
        host.Window.Width = width == 320 ? 1100 : 320;
        host.Capture();
        Assert.True(page.Toolbars.All(value => value.Bounds.Width <= host.Window.Width));
    }

    private static void Click(Control page, string id, ButtonHost host) => Click(page.GetVisualDescendants().OfType<MaterialButton>().Single(value => AutomationProperties.GetAutomationId(value) == id), host);
    private static void Click(Control action, ButtonHost host)
    {
        action.BringIntoView();
        host.Capture();
        var center = action.TranslatePoint(new Point(action.Bounds.Width / 2, action.Bounds.Height / 2), host.Window)!.Value;
        Assert.InRange(center.X, 0, host.Window.Width);
        Assert.InRange(center.Y, 0, host.Window.Height);
        host.Window.MouseDown(center, MouseButton.Left);
        host.Window.MouseUp(center, MouseButton.Left);
    }
}
