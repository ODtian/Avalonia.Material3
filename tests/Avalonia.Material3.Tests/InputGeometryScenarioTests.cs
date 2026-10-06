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
