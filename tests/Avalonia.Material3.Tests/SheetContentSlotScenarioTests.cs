using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Tests;

public class SheetContentSlotScenarioTests
{
    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(24)]
    public void Bottom_sheet_content_starts_after_the_native_handle_with_only_the_callers_padding(double padding)
    {
        var body = new Border { Height = 80, Background = Brushes.Magenta };
        var sheet = new MaterialBottomSheet { Content = body, Padding = new Thickness(padding), ExpandedExtent = 240, IsPartialEnabled = false };
        using var host = new GeometryHost(new MaterialSheetHost { Sheet = sheet }, 400, 500);
        sheet.Expand(); host.Render();
        var start = body.TranslatePoint(default, sheet)!.Value;
        Assert.True(start == new Point(padding, 48 + padding), $"Start {start}; " + string.Join("; ", body.GetVisualAncestors().OfType<Control>().Select(control => $"{control.GetType().Name}: {control.Bounds}, margin {control.Margin}")));
        sheet.Title = "Information"; host.Render();
        Assert.True(body.TranslatePoint(default, sheet)!.Value.Y > 48 + padding);
        sheet.Title = null; host.Render();
        Assert.Equal(new Point(padding, 48 + padding), body.TranslatePoint(default, sheet)!.Value);
    }
}
