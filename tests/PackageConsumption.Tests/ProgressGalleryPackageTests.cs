using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;
using Gallery.Pages;
using Xunit;

namespace PackageConsumption.Tests;

public class ProgressGalleryPackageTests
{
    [AvaloniaTheory]
    [InlineData(320, 2)]
    [InlineData(1000, 1)]
    public async Task Fresh_package_gallery_keeps_real_feedback_and_controls_reachable_in_narrow_scaled_and_wide_hosts(int width, int scale)
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var motion = theme.Motion;
        theme.Motion = motion with { ReduceMotion = false };
        var original = theme.Typography;
        theme.Typography = original with { Scale = scale };
        var outcome = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IProgress<double>? reporter = null;
        var page = new ProgressFeedbackPage(theme, (progress, _) => { reporter = progress; return outcome.Task; });
        var viewer = new ScrollViewer { Content = page };
        var window = new Window { Width = width, Height = 900, RequestedThemeVariant = ThemeVariant.Light, Content = viewer };
        window.Show();
        try
        {
            Click(window, page.StartButton);
            reporter!.Report(.5);
            foreach (var indicator in page.Indicators) indicator.AnimationTime = TimeSpan.Zero;
            foreach (var indicator in page.Indicators) indicator.AnimationTime = TimeSpan.FromMilliseconds(800);
            Assert.Equal("Running, 50%", page.Result.Text);
            using var frame = window.CaptureRenderedFrame();
            foreach (var indicator in page.Indicators) Assert.InRange(indicator.Bounds.Width, 4, width - 48);
            Save(window, $"m3-11-gallery-{width}-font{scale}00.png");
            window.MouseWheel(new Point(width / 2d, 800), new Vector(0, -12));
            using var scroll = window.CaptureRenderedFrame();
            Assert.True(viewer.Offset.Y > 0);
            viewer.ScrollToEnd();
            using var end = window.CaptureRenderedFrame();
            var final = page.Indicators[^1];
            var location = final.TranslatePoint(new Point(0, final.Bounds.Height / 2), window)!.Value;
            Assert.InRange(location.Y, 0, 900);
            Save(window, $"m3-11-gallery-bottom-{width}.png");
            outcome.SetResult();
            await page.CurrentOperation;
            Assert.Equal("Completed: 12 documents imported", page.Result.Text);
            viewer.ScrollToHome();
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Save(window, $"m3-11-gallery-completed-dark-{width}.png");
        }
        finally { window.Close(); theme.Typography = original; theme.Motion = motion; }
    }

    [AvaloniaFact]
    public void Fresh_package_renders_the_pinned_shape_sequence_and_all_standard_and_wave_forms()
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var motion = theme.Motion;
        theme.Motion = motion with { ReduceMotion = false };
        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
        var shapes = new WrapPanel();
        var loading = new List<MaterialLoadingIndicator>();
        var names = new[] { "Soft burst", "Cookie 9", "Pentagon", "Pill", "Sunny", "Cookie 4", "Oval" };
        for (var i = 0; i < 7; i++)
        {
            var control = new MaterialLoadingIndicator { AnimationTime = TimeSpan.Zero, Width = 80 };
            loading.Add(control);
            shapes.Children.Add(new StackPanel { Width = 100, Children = { control, new TextBlock { Text = names[i] } } });
        }
        panel.Children.Add(shapes);
        var progress = new List<MaterialProgressIndicator>();
        foreach (var expressive in new[] { false, true })
        foreach (var unknown in new[] { false, true })
        {
            var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 24 };
            row.Children.Add(new TextBlock { Width = 180, Text = (expressive ? "Wave" : "Standard") + (unknown ? " indeterminate" : " determinate") });
            var linear = new MaterialLinearProgressIndicator { IsExpressive = expressive, IsIndeterminate = unknown, Value = .5, AnimationTime = TimeSpan.Zero };
            var circular = new MaterialCircularProgressIndicator { IsExpressive = expressive, IsIndeterminate = unknown, Value = .5, AnimationTime = TimeSpan.Zero };
            row.Children.Add(linear); row.Children.Add(circular);
            progress.Add(linear); progress.Add(circular);
            panel.Children.Add(row);
        }
        var window = new Window { Width = 800, Height = 420, RequestedThemeVariant = ThemeVariant.Light, Content = panel };
        window.Show();
        try
        {
            for (var i = 0; i < 7; i++) loading[i].AnimationTime = TimeSpan.FromMilliseconds(650 * i);
            foreach (var control in progress) control.AnimationTime = TimeSpan.FromMilliseconds(800);
            Save(window, "m3-11-pinned-forms-light.png");
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Save(window, "m3-11-pinned-forms-dark.png");
            foreach (var control in progress) Assert.True(control.Bounds.Width > 0 && control.Bounds.Height > 0);
        }
        finally { window.Close(); theme.Motion = motion; }
    }
    private static void Click(Window window, Control control)
    {
        using var frame = window.CaptureRenderedFrame();
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }
    private static void Save(Window window, string name)
    {
        var directory = Environment.GetEnvironmentVariable("M3_ISSUE12_SCREENSHOTS");
        if (directory is null) return;
        Directory.CreateDirectory(directory);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(directory, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
