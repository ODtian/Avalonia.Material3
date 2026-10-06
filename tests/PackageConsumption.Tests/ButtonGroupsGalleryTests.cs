using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Gallery.Pages;
using Xunit;

namespace PackageConsumption.Tests;

public class ButtonGroupsGalleryTests
{
    [AvaloniaFact]
    public void Published_gallery_commits_options_and_separate_split_actions_with_real_input_and_mode_changes()
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var typography = theme.Typography; var motion = theme.Motion;
        var page = new ButtonGroupsPage(theme);
        var window = new Window { Width = 1100, Height = 850, RequestedThemeVariant = ThemeVariant.Light, Content = new ScrollViewer { Content = page } };
        window.Show();
        try
        {
            Save(window, "m3-05-gallery-light.png");
            Click(window, page.TimeRange.Children.OfType<MaterialGroupButton>().ElementAt(1));
            Assert.Equal("Range: Week", page.SelectionResult.Text);
            Click(window, page.MediaTypes.Children.OfType<MaterialGroupButton>().First());
            Assert.Single(page.MediaTypes.SelectedItems);
            Click(window, page.PrimarySplit.SecondaryButton);
            Assert.True(page.PrimarySplit.IsExpanded); Assert.Equal("Saved: 0", page.ActionResult.Text);
            Click(window, page.PrimarySplit.MainButton); Assert.Equal("Saved: 1", page.ActionResult.Text);
            Click(window, page.ModeButton); Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
            Save(window, "m3-05-gallery-dark-selected.png");
            Click(window, page.FontButton); Assert.Equal(2, theme.Typography.Scale);
            Assert.Equal(20, page.SplitVariants.Count);
            window.MouseWheel(new Point(500, 700), new Vector(0, -10));
            using var frame = window.CaptureRenderedFrame(); Assert.NotNull(frame);
        }
        finally { window.Close(); theme.Typography = typography; theme.Motion = motion; }
    }

    [AvaloniaTheory]
    [InlineData(320)]
    [InlineData(1100)]
    public void All_twenty_published_split_variants_remain_inside_the_scrolling_gallery_at_200_percent(int width)
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var original = theme.Typography;
        theme.Typography = original with { Scale = 2 };
        var page = new ButtonGroupsPage(theme);
        var viewer = new ScrollViewer { Content = page };
        var window = new Window { Width = width, Height = 850, RequestedThemeVariant = ThemeVariant.Light, Content = viewer };
        window.Show();
        try
        {
            Save(window, $"m3-05-gallery-{width}-font200.png");
            var actions = 0;
            foreach (var split in page.SplitVariants)
            {
                Assert.True(split.MainButton.Bounds.Width >= 48);
                Assert.True(split.SecondaryButton.Bounds.Width >= 48);
                Assert.InRange(split.SecondaryButton.Bounds.Right, 48, width - 32);
                Assert.Equal(split.MainButton.SharedContainerHeight, split.SecondaryButton.SharedContainerHeight);
                var expand = ControlAutomationPeer.CreatePeerForElement(split.SecondaryButton)!.GetProvider<IExpandCollapseProvider>()!;
                expand.Expand(); Assert.True(split.IsExpanded);
                ControlAutomationPeer.CreatePeerForElement(split.MainButton)!.GetProvider<IInvokeProvider>()!.Invoke();
                Assert.Equal($"Saved: {++actions}", page.ActionResult.Text);
                expand.Collapse(); Assert.False(split.IsExpanded);
            }
            viewer.ScrollToEnd(); Save(window, $"m3-05-gallery-{width}-bottom.png");
            Assert.True(viewer.Offset.Y > 0);
            if (width == 320)
            {
                Assert.NotEmpty(page.OverflowActions.OverflowItems);
                Assert.True(page.OverflowActions.OverflowButton.IsVisible);
                Assert.True(page.OverflowActions.Bounds.Height >= 48);
            }
        }
        finally { window.Close(); theme.Typography = original; }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Published_pinned_matrix_renders_all_four_recipes_and_five_sizes_together(bool dark)
    {
        var columns = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 12, Margin = new Thickness(16) };
        foreach (var variant in new[] { MaterialButtonVariant.Filled, MaterialButtonVariant.Tonal, MaterialButtonVariant.Elevated, MaterialButtonVariant.Outlined })
        {
            var column = new StackPanel { Width = 350, Spacing = 8 };
            column.Children.Add(new TextBlock { Text = variant.ToString() });
            foreach (var size in Enum.GetValues<MaterialButtonSize>())
            {
                var split = new MaterialSplitButton { Size = size, Variant = variant };
                split.MainButton.Content = size switch { MaterialButtonSize.ExtraSmall => "XS", MaterialButtonSize.Small => "S", MaterialButtonSize.Medium => "M", MaterialButtonSize.Large => "L", _ => "XL" };
                split.MainButton.LeadingIcon = "+";
                column.Children.Add(split);
            }
            columns.Children.Add(column);
        }
        var window = new Window { Width = 1480, Height = 570, RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light, Content = columns };
        window.Show();
        try
        {
            Save(window, dark ? "m3-05-split-matrix-dark.png" : "m3-05-split-matrix-light.png");
            foreach (var column in columns.Children.OfType<StackPanel>())
                foreach (var split in column.Children.OfType<MaterialSplitButton>())
                {
                    Assert.True(split.SecondaryButton.Bounds.Width >= 48);
                    Assert.Equal(2, split.SecondaryButton.Bounds.Left - split.MainButton.Bounds.Right);
                }
        }
        finally { window.Close(); }
    }

    private static void Click(Window window, Control control)
    {
        using var frame = window.CaptureRenderedFrame();
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
        using var after = window.CaptureRenderedFrame();
    }
    private static void Save(Window window, string name)
    {
        var path = Environment.GetEnvironmentVariable("M3_ISSUE10_SCREENSHOTS");
        if (string.IsNullOrEmpty(path)) return;
        Directory.CreateDirectory(path);
        using var frame = window.CaptureRenderedFrame(); frame!.Save(Path.Combine(path, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
