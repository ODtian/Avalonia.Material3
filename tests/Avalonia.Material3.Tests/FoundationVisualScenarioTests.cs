using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Material3.Tests;

public class FoundationVisualScenarioTests
{
    [AvaloniaFact]
    public void Official_add_symbol_paints_the_pinned_design_without_document_font_dependence()
    {
        using var host = new ButtonHost();
        var symbol = new MaterialSymbol { Symbol = "add", Size = 24, Foreground = Brushes.Black };
        host.Window.Background = Brushes.White;
        host.Window.Content = new Border { Padding = new Thickness(24), Child = symbol,
            HorizontalAlignment = Layout.HorizontalAlignment.Left, VerticalAlignment = Layout.VerticalAlignment.Top };
        host.Capture();
        var before = Mask(host.Window, symbol);
        // Independent Google font outlines: UPEM960, add bbox(200,200,760,760).
        // Nominal24: centered ink bbox(5,5,14,14), not a 24-wide tight-ink stretch.
        Assert.Equal(new Rect(5, 5, 14, 14), Ink(before, 24));
        host.Theme.Typography = new MaterialTypography { FontFamily = new FontFamily("Times New Roman"), Scale = 2 };
        host.Capture();
        Assert.Equal(before, Mask(host.Window, symbol));
    }

    [AvaloniaFact]
    public void Split_main_preserves_the_pinned_four_dip_inner_corner_beside_Full_caps()
    {
        using var host = new ButtonHost();
        var split = new MaterialSplitButton { Width = 180 };
        split.MainButton.Content = "Save";
        host.Window.Background = Brushes.White;
        host.Window.Content = new StackPanel { Margin = new Thickness(20), Children = { split } };
        host.Capture();
        var main = split.MainButton;
        Assert.Equal(120, main.Bounds.Width);
        // Pinned CornerBasedShape: Small finite main (20,4,4,20), visual top at5.
        var outside = main.TranslatePoint(new Point(119, 5), host.Window)!.Value;
        Assert.Equal(Colors.White, host.PixelAt(outside));
        Assert.Equal(Color.Parse("#6750A4"), host.PixelAt(main.TranslatePoint(new Point(117, 8), host.Window)!.Value));
    }

    [AvaloniaFact]
    public void Filled_uses_real_official_fill_artwork_in_the_same_fixed_frame()
    {
        using var host = new ButtonHost();
        var symbol = new MaterialSymbol { Symbol = "favorite", Foreground = Brushes.Black };
        host.Window.Background = Brushes.White;
        host.Window.Content = new Border { Padding = new Thickness(24), Child = symbol,
            HorizontalAlignment = Layout.HorizontalAlignment.Left, VerticalAlignment = Layout.VerticalAlignment.Top };
        host.Capture();
        var outline = Mask(host.Window, symbol);
        Assert.Equal(0, outline[12 * 24 + 12]);
        symbol.Filled = true; host.Capture();
        var filled = Mask(host.Window, symbol);
        Assert.Equal(1, filled[12 * 24 + 12]);
        Assert.Equal(new Size(24, 24), symbol.Bounds.Size);
    }

    [AvaloniaFact]
    public void Published_asset_seam_exposes_both_pinned_fonts_and_matching_manifest()
    {
        foreach (var instance in new[] { "Unfilled", "Filled" })
        {
            using var font = AssetLoader.Open(new Uri($"avares://Avalonia.Material3/Assets/Icons/MaterialSymbolsRounded-{instance}.ttf"));
            var signature = new byte[4]; font.ReadExactly(signature);
            Assert.Equal(new byte[] { 0, 1, 0, 0 }, signature);
        }
        using var manifest = AssetLoader.Open(new Uri("avares://Avalonia.Material3/Assets/Icons/manifest.json"));
        using var reader = new StreamReader(manifest);
        Assert.Contains("737e3324305806514d7909874fa1818ae1808232", reader.ReadToEnd());
    }

    private static byte[] Mask(Window window, Control visual)
    {
        using var bitmap = window.CaptureRenderedFrame()!;
        using var frame = bitmap.Lock();
        var origin = visual.TranslatePoint(default, window)!.Value;
        var width = (int)visual.Bounds.Width; var height = (int)visual.Bounds.Height;
        var result = new byte[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var offset = (int)(origin.Y + y) * frame.RowBytes + (int)(origin.X + x) * 4;
                result[y * width + x] = Marshal.ReadByte(frame.Address, offset + 1) < 240 ? (byte)1 : (byte)0;
            }
        return result;
    }

    private static Rect Ink(byte[] mask, int width)
    {
        var indices = Enumerable.Range(0, mask.Length).Where(i => mask[i] != 0).ToArray();
        Assert.NotEmpty(indices);
        var left = indices.Min(i => i % width); var right = indices.Max(i => i % width);
        var top = indices.Min(i => i / width); var bottom = indices.Max(i => i / width);
        return new(left, top, right - left + 1, bottom - top + 1);
    }
}
