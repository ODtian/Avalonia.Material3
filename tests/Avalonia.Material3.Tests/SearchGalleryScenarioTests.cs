using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SearchGalleryScenarioTests
{
    [AvaloniaTheory]
    [InlineData(320, 2)]
    [InlineData(1100, 1)]
    public void Gallery_user_edits_accepts_filters_removes_and_explicitly_submits_complete_query(int width, int scale)
    {
        using var host = new SearchScenarioHost(new StackPanel(), width, 900);
        host.Theme.Typography = host.Theme.Typography with { Scale = scale };
        var page = new SearchChipsPage(host.Theme);
        var viewer = new ScrollViewer { Content = page };
        host.Window.Content = viewer;
        host.Capture();
        page.QuerySearch.Editor!.Focus();
        host.Window.KeyTextInput("Atlas");
        host.Press(PhysicalKey.ArrowDown);
        host.Press(PhysicalKey.Enter);
        Assert.Equal("Atlas / 图集", Assert.Single(page.QuerySearch.Tokens).Label);
        host.Click(page.OpenAccessFilter);
        Assert.Equal(2, page.QuerySearch.Tokens.Count);
        Assert.Equal(0, page.SubmissionCount);
        var first = page.QuerySearch.GetVisualDescendants().OfType<MaterialChip>().First();
        var remove = first.GetVisualDescendants().OfType<Button>().Single(b => b is MaterialIconButton);
        host.Click(remove);
        Assert.Single(page.QuerySearch.Tokens);
        page.QuerySearch.Editor.Focus();
        host.Window.KeyTextInput("中文 final");
        page.QuerySearch.Close();
        host.Press(PhysicalKey.Enter);
        Assert.Equal(1, page.SubmissionCount);
        Assert.Equal("中文 final", page.LastQuery!.Text);
        Assert.Equal("Open access / 开放", Assert.Single(page.LastQuery.Tokens).Label);
        Assert.Contains("Keyboard", page.Result.Text);
        Assert.True(page.QuerySearch.Bounds.Width <= width - 24);
        Save(host.Window, $"m3-08-gallery-{width}-font{scale}00.png");
        viewer.ScrollToEnd();
        host.Capture();
        Assert.True(viewer.Offset.Y > 0);
        Save(host.Window, $"m3-08-gallery-bottom-{width}.png");
        Assert.Equal(5, page.SearchForms.Count);
        foreach (var search in page.SearchForms) Assert.True(search.Bounds.Width <= width - 24);
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Fresh_public_host_renders_all_search_forms_and_chip_purposes_with_visible_candidates(bool dark)
    {
        var panel = new StackPanel { Spacing = 8 };
        using var host = new SearchScenarioHost(panel, 800, 1500);
        host.Window.RequestedThemeVariant = dark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
        var searches = new List<MaterialSearch>();
        foreach (var mode in Enum.GetValues<MaterialSearchMode>())
        {
            panel.Children.Add(new TextBlock { Text = mode.ToString() });
            var search = new MaterialSearch { Mode = mode, IsOpen = true, Label = "Find / 查找", Candidates = new[] { new MaterialSearchToken("a", "Atlas / 图集 candidate") } };
            searches.Add(search);
            panel.Children.Add(search);
        }
        panel.Children.Add(new TextBlock { Text = "Full-screen view / 全屏搜索" });
        var full = new MaterialSearch { Mode = MaterialSearchMode.View, ViewPresentation = MaterialSearchViewPresentation.FullScreen, Height = 240, Candidates = new[] { new MaterialSearchToken("a", "Atlas / 图集") } };
        panel.Children.Add(full);
        foreach (var variant in Enum.GetValues<MaterialChipVariant>())
        {
            var row = new WrapPanel();
            foreach (var state in new[] { 0, 1, 2 }) row.Children.Add(new MaterialChip { ChipVariant = variant, Content = variant + " / 中文 Atlas", IsChecked = state == 1 && variant is MaterialChipVariant.Input or MaterialChipVariant.Filter, IsEnabled = state != 2 });
            panel.Children.Add(row);
        }
        host.Capture();
        foreach (var search in searches) Assert.Contains(search.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Atlas / 图集 candidate" && text.IsEffectivelyVisible);
        Assert.Equal(new CornerRadius(0), full.CornerRadius);
        Save(host.Window, dark ? "m3-08-pinned-forms-dark.png" : "m3-08-pinned-forms-light.png");
    }

    private static void Save(Window window, string name)
    {
        var folder = Environment.GetEnvironmentVariable("M3_ISSUE11_SCREENSHOTS");
        if (folder is null) return;
        Directory.CreateDirectory(folder);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(folder, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
