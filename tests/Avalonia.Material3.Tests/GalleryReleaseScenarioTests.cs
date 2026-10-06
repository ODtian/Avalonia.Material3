using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Gallery.Pages;
using Gallery;
using Xunit;

namespace Avalonia.Material3.Tests;

public class GalleryReleaseScenarioTests
{
    [AvaloniaFact]
    public void Package_gallery_exposes_every_delivered_page_without_discovery()
    {
        var theme = new MaterialTheme();
        Application.Current!.Styles.Add(theme);
        var shell = new GalleryShell(theme);
        Assert.Equal(new[] { "ThemeTokens", "ExpressiveButtons", "FloatingActions", "ButtonGroups", "SelectionForm",
            "TextFields", "SearchChips", "ContentHierarchy", "SliderSettings", "ProgressFeedback",
            "Dialogs", "SecondaryFeedback", "Sheets", "ContentNavigation", "AppChrome", "DateTimePickers",
            "CarouselRefresh" }, shell.Pages.Select(p => p.Id));
        var window = new Window { Width = 1000, Height = 800, Content = shell };
        window.Show();
        foreach (var page in shell.Pages)
        {
            Assert.True(shell.Navigate(page.Id));
            Assert.NotNull(shell.CurrentPage);
            Assert.Equal(page.Id, shell.CurrentPageId);
        }
        window.Close();
        Application.Current.Styles.Remove(theme);
    }

    [AvaloniaFact]
    public async Task Detached_progress_page_can_reactivate_and_complete_a_new_operation()
    {
        var theme = new MaterialTheme(); Application.Current!.Styles.Add(theme);
        var page = new ProgressFeedbackPage(theme, (_, token) => { token.ThrowIfCancellationRequested(); return Task.CompletedTask; });
        var window = new Window { Width = 800, Height = 700, Content = page };
        try
        {
            window.Show(); window.Content = null; window.Content = page;
            await page.RunOperationAsync();
            Assert.Equal(MaterialProgressStatus.Completed, page.Indicators[0].Status);
        }
        finally { window.Close(); Application.Current.Styles.Remove(theme); }
    }

    [AvaloniaTheory]
    [InlineData("Dialogs")]
    [InlineData("SecondaryFeedback")]
    [InlineData("Sheets")]
    [InlineData("AppChrome")]
    [InlineData("DateTimePickers")]
    public void Root_modal_blocks_header_navigation_and_back_never_routes_under_a_veto(string route)
    {
        var theme = new MaterialTheme(); Application.Current!.Styles.Add(theme);
        var shell = new GalleryShell(theme);
        var window = new Window { Width = 1000, Height = 800, Content = shell };
        try
        {
            window.Show(); shell.Navigate(route);
            var dialog = new MaterialDialog { Title = "Root confirmation", Content = "Modal scope covers the WHOLE window." };
            var session = dialog.Show(shell.Overlay);
            session.Closing += (_, e) => e.Cancel = true;
            using var frame = window.CaptureRenderedFrame();
            Assert.False(shell.ActionButton.IsEffectivelyEnabled);
            Assert.False(shell.ThemeButton.IsEffectivelyEnabled);
            Assert.False(shell.Navigate("ThemeTokens"));
            Assert.True(shell.RequestBack());
            Assert.Equal(route, shell.CurrentPageId);
            Assert.Equal(1, shell.Overlay.OpenCount);
        }
        finally { window.Close(); Application.Current.Styles.Remove(theme); }
    }

    [AvaloniaFact]
    public void Root_back_collapses_expanded_bottom_then_closes_without_underlying_navigation()
    {
        var theme = new MaterialTheme { Motion = new MaterialMotion { ReduceMotion = true } };
        Application.Current!.Styles.Add(theme);
        var shell = new GalleryShell(theme);
        var window = new Window { Width = 1000, Height = 800, Content = shell };
        try
        {
            window.Show(); shell.Navigate("Sheets");
            var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Content = "Expanded to partial to hidden" };
            sheet.Show(shell.Overlay); sheet.Expand();
            Assert.True(shell.RequestBack());
            Assert.Equal(MaterialSheetState.PartiallyExpanded, sheet.State);
            Assert.Equal("Sheets", shell.CurrentPageId);
            Assert.Equal(1, shell.Overlay.OpenCount);
            Assert.True(shell.RequestBack());
            Assert.Equal(0, shell.Overlay.OpenCount);
            Assert.Equal("Sheets", shell.CurrentPageId);
        }
        finally { window.Close(); Application.Current.Styles.Remove(theme); }
    }

    [AvaloniaTheory]
    [InlineData(320, 300, 2)]
    [InlineData(1000, 800, 1.5)]
    public void All_pages_remain_bounded_during_live_scheme_font_shape_motion_and_window_changes(int width, int height, double scale)
    {
        var theme = new MaterialTheme(); Application.Current!.Styles.Add(theme);
        var shell = new GalleryShell(theme);
        var window = new Window { Width = width, Height = height, Content = shell };
        try
        {
            window.Show();
            theme.SeedColor = Colors.Teal;
            theme.DynamicColors = new(MaterialColorScheme.FromSeed(Colors.Coral), MaterialColorScheme.FromSeed(Colors.Coral, true));
            theme.Typography = theme.Typography with { Scale = scale };
            theme.Shapes = theme.Shapes with { ButtonCornerRadius = 4 };
            theme.Motion = theme.Motion with { ReduceMotion = true };
            window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            foreach (var page in shell.Pages)
            {
                Assert.True(shell.Navigate(page.Id));
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                Assert.True(shell.CurrentPage!.Bounds.Width > 0);
                Assert.True(double.IsFinite(shell.CurrentPage.Bounds.Height));
            }
        }
        finally { window.Close(); Application.Current.Styles.Remove(theme); }
    }

    [AvaloniaFact]
    public async Task Published_content_automation_can_query_invoke_availability_from_a_native_worker()
    {
        var card = new MaterialCard { IsInteractive = true, IsSelectable = true, Title = "Native card" };
        var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(card)!;
        Assert.True(await Task.Run(() => peer.GetProvider<Avalonia.Automation.Provider.IInvokeProvider>() is not null));
        card.IsSelected = true;
        Assert.Equal(Avalonia.Automation.Provider.ToggleState.On, await Task.Run(() => peer.GetProvider<Avalonia.Automation.Provider.IToggleProvider>()!.ToggleState));
        card.IsInteractive = false;
        Assert.Null(await Task.Run(() => peer.GetProvider<Avalonia.Automation.Provider.IInvokeProvider>()));
    }

    [AvaloniaTheory]
    [InlineData("ThemeTokens")]
    [InlineData("FloatingActions")]
    [InlineData("CarouselRefresh")]
    public void Page_owned_viewport_receives_the_finite_body_instead_of_nested_unbounded_scrolling(string route)
    {
        var theme = new MaterialTheme(); Application.Current!.Styles.Add(theme);
        var shell = new GalleryShell(theme);
        var window = new Window { Width = 320, Height = 500, Content = shell };
        try
        {
            window.Show(); shell.Navigate(route);
            using var frame = window.CaptureRenderedFrame();
            Assert.InRange(shell.CurrentPage!.Bounds.Height, 1, 330);
        }
        finally { window.Close(); Application.Current.Styles.Remove(theme); }
    }
}
