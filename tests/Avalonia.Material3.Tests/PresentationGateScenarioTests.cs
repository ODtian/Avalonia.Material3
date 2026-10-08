using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Avalonia.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Material3.Tests;

public class PresentationGateScenarioTests
{
    [AvaloniaFact]
    public void Retiring_toolbar_under_modal_keeps_authored_paint_and_reverses_and_reattaches_with_correct_input()
    {
        var action = new MaterialButton { Content = "Action", Background = Brushes.Magenta };
        var disabled = new MaterialButton { Content = "Disabled", IsEnabled = false };
        var coreDisabled = new CoreDisabledButton { Content = "Core disabled" };
        var toolbar = new MaterialToolbar { Anchor = MaterialActionAnchor.TopStart, LeadingItems = { action, disabled, coreDisabled } };
        var overlay = new MaterialOverlayHost { Content = toolbar };
        using var host = new GeometryHost(overlay, 600, 300);
        host.Theme.States = host.Theme.States with { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 };
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastEffects = new(1, 1), FastSpatial = new(1, 1) } }; host.Render();
        var enabledInk = Painted(action); var disabledInk = Painted(disabled); var coreInk = Painted(coreDisabled);
        var cached = ControlAutomationPeer.CreatePeerForElement(action)!.GetProvider<IInvokeProvider>()!;
        var dialog = new MaterialDialog { Width = 160, Height = 100 }; dialog.Show(overlay); host.Render();
        toolbar.IsExpanded = false; host.Window.UpdateLayout();
        Assert.False(action.IsEffectivelyEnabled); Assert.ThrowsAny<Exception>(() => cached.Invoke());
        Assert.Equal(enabledInk, Painted(action)); Assert.Equal(disabledInk, Painted(disabled)); Assert.Equal(coreInk, Painted(coreDisabled));
        dialog.Cancel(); host.Window.UpdateLayout(); Assert.False(action.IsEffectivelyEnabled);
        toolbar.IsExpanded = true; host.Window.UpdateLayout(); Assert.True(action.IsEffectivelyEnabled);
        Assert.False(disabled.IsEffectivelyEnabled); Assert.False(coreDisabled.IsEffectivelyEnabled);
        toolbar.IsExpanded = false; overlay.Content = null; host.Render(); overlay.Content = toolbar; host.Render();
        Assert.False(action.IsEffectivelyEnabled);
        toolbar.IsExpanded = true; host.Render(); Assert.True(action.IsEffectivelyEnabled);
        Assert.False(disabled.IsEffectivelyEnabled); Assert.False(coreDisabled.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Modal_gate_preserves_a_host_presenter_enabled_binding_and_its_live_authored_disabled_state()
    {
        var state = new MaterialCheckBox { IsChecked = true };
        var button = new MaterialButton { Content = "Action" };
        var overlay = new MaterialOverlayHost { Content = button };
        using var host = new GeometryHost(overlay, 400, 300);
        var presenter = overlay.GetVisualDescendants().OfType<MaterialOverlayContentPresenter>().Single();
        presenter.Bind(Avalonia.Input.InputElement.IsEnabledProperty, new Binding(nameof(state.IsChecked)) { Source = state });
        var dialog = new MaterialDialog { Width = 120, Height = 80 }; dialog.Show(overlay); host.Render();
        state.IsChecked = false; host.Render();
        Assert.False(button.IsEffectivelyEnabled);
        dialog.Cancel(); host.Render();
        Assert.False(button.IsEffectivelyEnabled);
        state.IsChecked = true; host.Render(); Assert.True(button.IsEffectivelyEnabled);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retiring_calendar_keeps_selected_ink_while_real_and_cached_day_input_is_blocked(bool yearMenu)
    {
        var picker = new MaterialDatePicker { Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 9),
            MinimumDate = new(2024, 1, 1), MaximumDate = new(2024, 12, 31), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        using var host = new GeometryHost(picker, 360, 720);
        host.Theme.States = host.Theme.States with { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 };
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastEffects = new(1, 1), DefaultEffects = new(1, 1), DefaultSpatial = new(1, 1) } }; host.Render();
        var day = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(control => control.Date == new DateOnly(2024, 2, 9));
        var point = day.TranslatePoint(new Point(12, 24), picker)!.Value;
        var cached = ControlAutomationPeer.CreatePeerForElement(day)!.GetProvider<IInvokeProvider>()!;
        var before = Pixel(picker, point);
        if (yearMenu)
        {
            var menu = picker.GetVisualDescendants().OfType<MaterialButton>().Single(button =>
                ControlAutomationPeer.CreatePeerForElement(button)!.GetName()?.StartsWith(picker.Labels.ChooseYear + ":", StringComparison.Ordinal) == true);
            ControlAutomationPeer.CreatePeerForElement(menu)!.GetProvider<IInvokeProvider>()!.Invoke();
        }
        else picker.Mode = MaterialDatePickerMode.Input;
        host.Window.UpdateLayout();
        Assert.False(day.IsEffectivelyEnabled); Assert.ThrowsAny<Exception>(() => cached.Invoke());
        var during = Pixel(picker, point);
        Assert.InRange(Math.Abs(before.R - during.R), 0, 2); Assert.InRange(Math.Abs(before.G - during.G), 0, 2); Assert.InRange(Math.Abs(before.B - during.B), 0, 2);
    }

    private static Color Pixel(Control control, Point point)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(control);
        using var pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var frame = pixels.Lock(); bitmap.CopyPixels(frame);
        var offset = (int)point.Y * frame.RowBytes + (int)point.X * 4;
        return Color.FromRgb(Marshal.ReadByte(frame.Address, offset + 2), Marshal.ReadByte(frame.Address, offset + 1), Marshal.ReadByte(frame.Address, offset));
    }
    private static byte[] Painted(Control control)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(control); using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default); return stream.ToArray();
    }
    private sealed class CoreDisabledButton : MaterialButton { protected override bool IsEnabledCore => false; }
}
