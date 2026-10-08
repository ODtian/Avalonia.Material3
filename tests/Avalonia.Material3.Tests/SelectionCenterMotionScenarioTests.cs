using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

// Public selection, fractional-DPI host and painted-frame seams (docs/testing.md).
public class SelectionCenterMotionScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public async Task Switch_thumb_size_motion_keeps_its_center_when_off_content_changes(double scale)
    {
        using var host = new ButtonHost();
        host.Window.SetRenderScaling(scale);
        host.Theme.LightColorScheme = host.Theme.LightColorScheme with
        { Outline = Colors.Black, OnSurfaceVariant = Colors.Black, SurfaceContainerHighest = Colors.White };
        var toggle = new MaterialSwitch { Width = 64, Height = 48, BorderBrush = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left };
        host.Window.Content = new StackPanel { Margin = new Thickness(24.3, 24.65, 0, 0), Children = { toggle } };
        host.Capture();
        var center = toggle.TranslatePoint(new Point(16, 24), host.Window)!.Value * scale;
        var rest = Centroid(ReadFrame(host.Window), center, 15 * scale, background: 765);
        host.Theme.Motion = new MaterialMotion
        { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 100) } };
        var widths = new List<double>();
        foreach (var content in new Control?[] { new Border { Width = 16, Height = 16 }, null })
        {
            toggle.OffIcon = content;
            for (var frame = 0; frame < 45; frame++)
            {
                await Task.Delay(8);
                host.Capture();
                var painted = Centroid(ReadFrame(host.Window), center, 15 * scale, background: 765);
                widths.Add(painted.Weight);
                Assert.True(Math.Abs(painted.X - rest.X) < .16 && Math.Abs(painted.Y - rest.Y) < .16,
                    $"DPI {scale}, has icon {content is not null}, frame {frame}: rest ({rest.X:F3}, {rest.Y:F3}), thumb ({painted.X:F3}, {painted.Y:F3}).");
            }
        }
        Assert.True(widths.Max() > widths.Min() * 1.8, "The rendered thumb must visibly grow and shrink.");
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public async Task Radio_dot_grows_and_shrinks_about_the_visible_ring_center_at_every_frame(double scale)
    {
        using var host = new ButtonHost();
        host.Window.SetRenderScaling(scale);
        var radio = new MaterialRadioButton { Width = 48, Height = 48, IsEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Left };
        host.Window.Content = new StackPanel { Margin = new Thickness(24.3, 24.65, 0, 0), Children = { radio } };
        host.Capture();
        var center = radio.TranslatePoint(new Point(24, 24), host.Window)!.Value * scale;
        var baseline = ReadFrame(host.Window);
        var ringCenter = Centroid(baseline, center, 10.5 * scale, background: baseline.Pixel((int)center.X, (int)center.Y));
        host.Theme.Motion = new MaterialMotion
        {
            Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 100) }
        };
        var growingFrames = 0;
        var shrinkingFrames = 0;
        var enteringInk = new List<double>();
        var exitingInk = new List<double>();
        foreach (var selected in new[] { true, false })
        {
            radio.IsChecked = selected;
            for (var frame = 0; frame < 55; frame++)
            {
                await Task.Delay(8);
                host.Capture();
                var painted = Centroid(ReadFrame(host.Window), ringCenter, 7 * scale, baseline: baseline);
                if (painted.Weight < 5) continue;
                if (selected) { growingFrames++; enteringInk.Add(painted.Weight); }
                else { shrinkingFrames++; exitingInk.Add(painted.Weight); }
                Assert.True(Math.Abs(painted.X - ringCenter.X) < .16 && Math.Abs(painted.Y - ringCenter.Y) < .16,
                    $"DPI {scale}, selected {selected}, frame {frame}: ring ({ringCenter.X:F3}, {ringCenter.Y:F3}), dot ({painted.X:F3}, {painted.Y:F3}).");
            }
        }
        Assert.True(growingFrames >= 3 && shrinkingFrames >= 3,
            $"Both transitions must contain visible intermediate dot frames; grow {growingFrames}, shrink {shrinkingFrames}.");
        Assert.True(enteringInk.Max() > enteringInk.Min() * 3 && exitingInk.Max() > exitingInk.Min() * 3,
            "Each transition must present a changing painted dot area.");
    }

    private sealed record Frame(int Stride, byte[] Bytes)
    {
        public double Pixel(int x, int y)
        {
            var offset = y * Stride + x * 4;
            return Bytes[offset] + Bytes[offset + 1] + Bytes[offset + 2];
        }
    }

    private readonly record struct InkCenter(double X, double Y, double Weight)
    {
        public static implicit operator Point(InkCenter value) => new(value.X, value.Y);
    }

    private static Frame ReadFrame(Window window)
    {
        using var bitmap = window.CaptureRenderedFrame()!;
        using var pixels = bitmap.Lock();
        var bytes = new byte[pixels.RowBytes * pixels.Size.Height];
        Marshal.Copy(pixels.Address, bytes, 0, bytes.Length);
        return new(pixels.RowBytes, bytes);
    }

    private static InkCenter Centroid(Frame frame, Point center, double radius, double? background = null, Frame? baseline = null)
    {
        double weight = 0, horizontal = 0, vertical = 0;
        for (var y = (int)Math.Floor(center.Y - radius); y < center.Y + radius; y++)
            for (var x = (int)Math.Floor(center.X - radius); x < center.X + radius; x++)
            {
                var dx = x + .5 - center.X; var dy = y + .5 - center.Y;
                if (dx * dx + dy * dy > radius * radius) continue;
                var ink = Math.Max(0, (baseline?.Pixel(x, y) ?? background!.Value) - frame.Pixel(x, y));
                weight += ink; horizontal += ink * (x + .5); vertical += ink * (y + .5);
            }
        return new(horizontal / weight, vertical / weight, weight);
    }
}
