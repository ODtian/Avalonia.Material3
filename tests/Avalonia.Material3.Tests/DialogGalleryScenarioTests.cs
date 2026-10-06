using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class DialogGalleryScenarioTests
{
    [AvaloniaTheory]
    [InlineData(320, 2, false)]
    [InlineData(900, 1, true)]
    public void Gallery_edits_confirms_and_cancels_both_forms_through_the_published_contract(int width, int scale, bool dark)
    {
        var theme = new MaterialTheme { Typography = new Tokens.MaterialTypography { Scale = scale } };
        Application.Current!.Styles.Add(theme);
        var page = new DialogsPage(theme);
        var window = new Window { Width = width, Height = 700, Content = page, RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light };
        window.Show();
        try
        {
            Click(window, page.BasicButton);
            Assert.True(page.Editor!.IsFocused);
            page.Editor.SelectAll(); window.KeyTextInput("Package edited / 中文");
            Capture(window);
            Assert.Equal("Package edited / 中文", page.Editor.Text);
            Save(window, $"m3-12-basic-{width}-font{scale}00-{(dark ? "dark" : "light")}.png");
            Click(window, FindAction(page.ActiveDialog!, "Save"));
            Assert.Equal("Confirmed: Package edited / 中文", page.Result.Text);
            Assert.True(page.BasicButton.IsFocused);
            Click(window, page.FullScreenButton);
            Assert.Equal(width, page.ActiveDialog!.Bounds.Width);
            Assert.Equal(700, page.ActiveDialog.Bounds.Height);
            Save(window, $"m3-12-fullscreen-{width}-font{scale}00-{(dark ? "dark" : "light")}.png");
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            Capture(window);
            Assert.Equal("Escape", page.Result.Text);
            Assert.True(page.FullScreenButton.IsFocused);
            Click(window, page.LongButton);
            var viewer = page.ActiveDialog!.GetVisualDescendants().OfType<ScrollViewer>().Single(scroll => scroll.Extent.Height > scroll.Viewport.Height);
            window.MouseWheel(Center(window, viewer), new Vector(0, -4)); Capture(window);
            Assert.True(viewer.Offset.Y > 0);
            Save(window, $"m3-12-long-{width}-font{scale}00.png");
            Click(window, FindAction(page.ActiveDialog, "Cancel"));
            Assert.Equal("Cancelled", page.Result.Text);
            Assert.Equal(0, page.Overlay.OpenCount);
        }
        finally { window.Close(); Application.Current.Styles.Remove(theme); }
    }
    [AvaloniaFact]
    public void Gallery_nested_and_anchored_presentations_use_the_same_stack_and_results()
    {
        var theme = new MaterialTheme(); Application.Current!.Styles.Add(theme);
        var page = new DialogsPage(theme);
        var window = new Window { Width = 900, Height = 700, Content = page }; window.Show();
        try
        {
            Click(window, page.NestedButton);
            var outer = page.ActiveDialog!;
            Click(window, outer.GetVisualDescendants().OfType<MaterialButton>().Single(button => button.Name == "NestedLauncher"));
            Assert.Equal(2, page.Overlay.OpenCount);
            Click(window, FindAction(page.ActiveDialog!, "Confirm"));
            Assert.Equal("Confirmed: Nested confirmed", page.Result.Text);
            Assert.Equal(1, page.Overlay.OpenCount);
            Click(window, FindAction(outer, "Host back"));
            Assert.Equal("Back", page.Result.Text);
            Click(window, page.PopupButton);
            Assert.True(page.BasicButton.IsEffectivelyEnabled);
            Click(window, page.Overlay.GetVisualDescendants().OfType<MaterialButton>().Single(button => button.Name == "PopupAction"));
            Assert.Equal("Confirmed: Sample selected", page.Result.Text);
            Assert.True(page.PopupButton.IsFocused);
        }
        finally { window.Close(); Application.Current.Styles.Remove(theme); }
    }
    private static MaterialButton FindAction(Control dialog, string label) => dialog.GetVisualDescendants().OfType<MaterialButton>()
        .Single(button => button.IsEffectivelyVisible && (button.Content is TextBlock text ? text.Text : button.Content?.ToString()) == label);
    private static Point Center(Window window, Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
    private static void Capture(Window window) { using var frame = window.CaptureRenderedFrame(); }
    private static void Click(Window window, Control control) { control.BringIntoView(); Capture(window); var point = Center(window, control); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Capture(window); }
    private static void Save(Window window, string name)
    {
        if (Environment.GetEnvironmentVariable("M3_ISSUE13_SCREENSHOTS") is not { } directory) return;
        Directory.CreateDirectory(directory);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(directory, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
