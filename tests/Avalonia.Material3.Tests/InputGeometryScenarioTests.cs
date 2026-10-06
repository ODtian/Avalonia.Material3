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
