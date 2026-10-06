using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SheetGalleryScenarioTests
{
    [AvaloniaTheory]
    [InlineData(320, 2, false)]
    [InlineData(1000, 1, true)]
    public void Gallery_both_modal_forms_keep_scaled_actions_reachable_across_resize(int width, int scale, bool dark)
    {
        using var host = new SheetTestHost(width, 500);
        host.Theme.Typography = host.Theme.Typography with { Scale = scale };
        host.Window.RequestedThemeVariant = dark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
        var page = new SheetsPage(host.Theme); host.Overlay.Content = page; host.Render();
        foreach (var name in new[] { "Open modal bottom", "Open modal side" })
        {
            host.Click(Button(page, name)); host.Render();
            var sheet = page.LastModal!;
            host.Window.Height = 300; host.Render();
            Assert.InRange(sheet.VisibleExtent, 0, sheet is MaterialSideSheet ? width : 300);
            var dismiss = Button(sheet, "Dismiss sheet"); dismiss.BringIntoView(); host.Render();
            var point = host.Center(dismiss);
            Assert.InRange(point.Y, 0, 300);
            Save(host, $"m3-14-{(sheet is MaterialSideSheet ? "side" : "bottom")}-{width}-font{scale}00-{(dark ? "dark" : "light")}.png");
            host.Click(dismiss); Assert.Null(page.LastModal);
            host.Window.Height = 500; host.Render();
        }
    }
    [AvaloniaFact]
    public void Gallery_switches_real_standard_layout_then_opens_edits_and_dismisses_modal_content()
    {
        using var host = new SheetTestHost(900, 700);
        var page = new SheetsPage(host.Theme);
        host.Overlay.Content = page; host.Render();
        host.Click(Button(page, "Standard side"));
        Assert.IsType<MaterialSideSheet>(page.Layout.Sheet);
        Assert.Equal(256, page.Layout.Sheet!.VisibleExtent);
        Assert.True(page.Layout.Content is Control { IsEffectivelyEnabled: true });
        host.Click(Button(page, "Open modal bottom"));
        Assert.True(page.LastModal!.IsModal);
        Assert.False(page.Layout.IsEffectivelyEnabled);
        host.Click(Button(page.LastModal, "Expand sheet")); Assert.Equal(MaterialSheetState.Expanded, page.LastModal.State);
        host.Click(Button(page.LastModal, "Dismiss sheet")); Assert.Null(page.LastModal);
        Assert.Contains("Cancelled", page.Result.Text);
        host.Click(Button(page, "Open modal side"));
        Assert.IsType<MaterialSideSheet>(page.LastModal);
        host.Key(Avalonia.Input.Key.Escape); Assert.Null(page.LastModal);
    }
    private static MaterialButton Button(Control root, string text) => root.GetVisualDescendants().OfType<MaterialButton>().Single(button => button.Content as string == text);
    private static void Save(SheetTestHost host, string name)
    {
        if (Environment.GetEnvironmentVariable("M3_ISSUE16_SCREENSHOTS") is not { } directory) return;
        Directory.CreateDirectory(directory);
        using var frame = Avalonia.Headless.HeadlessWindowExtensions.CaptureRenderedFrame(host.Window);
        frame!.Save(Path.Combine(directory, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
