using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class AppChromeGalleryScenarioTests
{
    [AvaloniaFact]
    public void Gallery_destination_page_actions_and_return_keep_title_result_and_drawer_selection_consistent()
    {
        using var host = new ChromeHost(new Border(), 600);
        var gallery = new AppChromePage(host.Theme);
        host.Window.Content = gallery;
        host.Layout();
        gallery.OpenNavigation();
        host.Layout();
        host.Click((MaterialNavigationItem)gallery.Drawer.Items[1]!);
        Assert.Equal("Library", gallery.CurrentPage);
        Assert.Contains("Library", gallery.TopBar.Title);
        host.Click(gallery.SaveButton);
        Assert.Contains("Library saved", gallery.Status.Text);
        host.Click(gallery.DetailsButton);
        Assert.Equal("Details", gallery.CurrentPage);
        Assert.Equal(1, gallery.Drawer.SelectedIndex);
        host.Click(gallery.NavigationButton);
        Assert.Equal("Library", gallery.CurrentPage);
        Assert.Contains("Library saved", gallery.Status.Text);
        gallery.OpenNavigation(); host.Layout();
        Assert.True(gallery.RequestBack());
        Assert.False(gallery.Drawer.IsOpen);
        Assert.Equal("Library", gallery.CurrentPage);
    }

    [AvaloniaTheory]
    [InlineData(320, 300, 2)]
    [InlineData(320, 700, 2)]
    [InlineData(1000, 700, 1)]
    [InlineData(1000, 700, 2)]
    public void All_eight_gallery_forms_keep_page_and_actions_reachable_with_long_titles_and_fonts(double width, double height, double scale)
    {
        using var host = new ChromeHost(new Border(), width, height);
        var gallery = new AppChromePage(host.Theme);
        host.Window.Content = gallery; host.Layout();
        host.Theme.Typography = new MaterialTypography { Scale = scale };
        for (var i = 0; i < 8; i++)
        {
            gallery.TopBar.Title = "Collection 收藏 — bilingual collection with host-owned page operations";
            host.Layout();
            Assert.True(gallery.PageScroll.Viewport.Height >= 48, $"form={gallery.TopBar.Variant}, top={gallery.TopBar.Bounds.Height}, viewport={gallery.PageScroll.Viewport}");
            Assert.True(gallery.SaveButton.Bounds.Width >= 48 && gallery.SaveButton.Bounds.Height >= 48);
            Assert.Contains(gallery.TopBar.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == gallery.TopBar.Title);
            gallery.RecipeButton.BringIntoView(); host.Layout(); host.Click(gallery.RecipeButton);
        }
        host.Window.RequestedThemeVariant = ThemeVariant.Dark; host.Layout();
        Assert.Equal(ThemeVariant.Dark, gallery.TopBar.ActualThemeVariant);
    }

    [AvaloniaFact]
    public void Borrowed_window_root_overlay_host_gates_aggregate_header_and_restores_its_focus()
    {
        using var host = new ChromeHost(new Border(), 600);
        var aggregateAction = new MaterialButton { Content = "Aggregate shell action" };
        var overlays = new MaterialOverlayHost();
        var gallery = new AppChromePage(host.Theme, overlays);
        var shell = new DockPanel();
        DockPanel.SetDock(aggregateAction, Dock.Top);
        shell.Children.Add(aggregateAction); shell.Children.Add(gallery);
        overlays.Content = shell; host.Window.Content = overlays; host.Layout();
        aggregateAction.Focus();
        gallery.OpenNavigation(); host.Layout();
        Assert.Equal(1, overlays.OpenCount);
        Assert.True(aggregateAction.IsEnabled);
        Assert.False(aggregateAction.IsEffectivelyEnabled);
        var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(aggregateAction)!;
        Assert.ThrowsAny<Exception>(() => ((Avalonia.Automation.Provider.IInvokeProvider)peer).Invoke());
        Assert.True(gallery.RequestBack()); host.Layout();
        Assert.True(aggregateAction.IsEffectivelyEnabled);
        Assert.True(aggregateAction.IsFocused);
        Assert.Equal(0, overlays.OpenCount);
    }
}
