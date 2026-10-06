using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Xunit;

namespace Avalonia.Material3.Tests;

// Confirmed seam: public host layout/input, native IME text-view/caret and rendered output.
public class InputGeometryScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Outlined_label_removes_the_stroke_without_painting_a_surface_rectangle(double scale, bool dark)
    {
        using var host = new TextFieldHost(new MaterialTextField { Variant = MaterialTextFieldVariant.Outlined, Label = "Name" });
        host.Theme.Typography = new MaterialTypography { Scale = scale };
        host.Window.Background = Avalonia.Media.Brush.Parse("#27567A");
        host.Window.RequestedThemeVariant = dark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
        host.Field.Focus();
        host.Capture();
        // Independent notch recipe: text x16, four-DIP notch padding, top stroke on y8/16.
        // x13 is padding with no glyph; it MUST expose the actual blue host, not theme Surface.
        Assert.Equal(Avalonia.Media.Color.Parse("#27567A"),
            host.PixelAt(host.Field.TranslatePoint(new Point(13, 8 * scale), host.Window)!.Value));
        Assert.Equal(dark ? Avalonia.Media.Color.Parse("#D0BCFF") : Avalonia.Media.Color.Parse("#6750A4"),
            host.PixelAt(host.Field.TranslatePoint(new Point(160, 8 * scale + 1), host.Window)!.Value));
    }

    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled, 8)]
    [InlineData(MaterialTextFieldVariant.Outlined, 0)]
    public async Task Floating_label_has_an_intermediate_visual_but_native_editor_stays_registered(MaterialTextFieldVariant variant, double finalTop)
    {
        using var host = new TextFieldHost(new MaterialTextField { Variant = variant, Label = "Name" });
        host.Theme.Motion = new MaterialMotion { DurationShort4 = TimeSpan.FromMilliseconds(400) };
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        host.Capture();
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        host.Field.RaiseEvent(request);
        var editor = request.Client!.TextViewVisual;
        var origin = editor.TranslatePoint(default, host.Field);
        var size = host.Field.Bounds.Size;
        host.Field.Focus();
        await Task.Delay(80);
        host.Capture();
        var label = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(host.Field).OfType<TextBlock>()
            .Single(text => text.Text == "Name" && text.IsEffectivelyVisible);
        var rect = new Rect(label.Bounds.Size).TransformToAABB(label.TransformToVisual(host.Field)!.Value);
        Assert.True(rect.Top > finalTop + 0.1, $"The label jumped to its endpoint: {rect}.");
        Assert.True(rect.Top < 24, $"The label did not leave its resting position: {rect}.");
        Assert.Equal(origin, editor.TranslatePoint(default, host.Field));
        Assert.Equal(size, host.Field.Bounds.Size);
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true };
        host.Capture();
        rect = new Rect(label.Bounds.Size).TransformToAABB(label.TransformToVisual(host.Field)!.Value);
        Assert.Equal(finalTop, rect.Top);
    }

    [AvaloniaFact]
    public async Task Switch_travels_through_an_intermediate_painted_thumb_without_moving_its_envelope()
    {
        var toggle = new MaterialSwitch
        {
            OnIcon = new Border { Width = 4, Height = 4, Background = Avalonia.Media.Brushes.Red },
            OffIcon = new Border { Width = 4, Height = 4, Background = Avalonia.Media.Brushes.Red }
        };
        using var host = new SelectionHost(toggle);
        host.Theme.Motion = new MaterialMotion { DurationShort4 = TimeSpan.FromMilliseconds(400) };
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        host.Capture();
        var bounds = toggle.Bounds;
        toggle.IsChecked = true;
        await Task.Delay(80);
        host.Capture();
        Assert.Equal(bounds, toggle.Bounds);
        // Independent track recipe: centres22->42. A caller-owned red4-square icon must move
        // with the thumb through intermediate pixels, never jump directly to the endpoint.
        using var bitmap = host.Window.CaptureRenderedFrame()!;
        using var frame = bitmap.Lock();
        var origin = toggle.TranslatePoint(default, host.Window)!.Value;
        var pixels = Enumerable.Range(6, 52).Where(x =>
        {
            var offset = (int)((origin.Y + 24) * host.Window.RenderScaling) * frame.RowBytes +
                (int)((origin.X + x) * host.Window.RenderScaling) * 4;
            var first = System.Runtime.InteropServices.Marshal.ReadByte(frame.Address, offset);
            var g = System.Runtime.InteropServices.Marshal.ReadByte(frame.Address, offset + 1);
            var third = System.Runtime.InteropServices.Marshal.ReadByte(frame.Address, offset + 2);
            return g == 0 && (frame.Format == Avalonia.Platform.PixelFormat.Bgra8888 ? third == 255 && first == 0 : first == 255 && third == 0);
        }).ToArray();
        Assert.NotEmpty(pixels);
        var centre = (pixels[0] + pixels[^1] + 1) / 2d;
        Assert.InRange(centre, 23, 41);
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true };
        host.Capture();
        Assert.Equal(Avalonia.Media.Colors.Red, host.PixelAt(toggle, new Point(42, 24)));
        toggle.IsChecked = false;
        host.Capture();
        Assert.Equal(Avalonia.Media.Colors.Red, host.PixelAt(toggle, new Point(22, 24)));
    }

    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled, 1)]
    [InlineData(MaterialTextFieldVariant.Outlined, 1)]
    [InlineData(MaterialTextFieldVariant.Filled, 2)]
    [InlineData(MaterialTextFieldVariant.Outlined, 2)]
    public void Focus_and_error_do_not_reflow_single_line_editor_or_next_control(MaterialTextFieldVariant variant, double scale)
    {
        using var host = new TextFieldHost(new MaterialTextField
        {
            Variant = variant, Label = "Amount / 金额", PrefixText = "¥", SuffixText = "CNY", ShowClearButton = true
        });
        host.Theme.Typography = new MaterialTypography { Scale = scale };
        host.Window.Width = 320;
        host.Capture();
        var size = host.Field.Bounds.Size;
        var next = host.Next.Bounds;
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        host.Field.RaiseEvent(request);
        var textView = request.Client!.TextViewVisual;
        Point EditorOrigin() => textView.TranslatePoint(default, host.Field)!.Value;
        var editor = EditorOrigin();
        for (var i = 0; i < 50; i++)
        {
            host.Field.Focus();
            host.Field.IsError = true;
            host.Capture();
            Assert.Equal(size, host.Field.Bounds.Size);
            Assert.Equal(next, host.Next.Bounds);
            Assert.Equal(editor, EditorOrigin());
            host.Field.IsError = false;
            host.Next.Focus();
            host.Capture();
            Assert.Equal(size, host.Field.Bounds.Size);
            Assert.Equal(next, host.Next.Bounds);
            Assert.Equal(editor, EditorOrigin());
        }
    }

}
