using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Material3.Tokens;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ModalPaintScenarioTests
{
    [AvaloniaFact]
    public void String_body_content_keeps_its_authored_paint_under_a_nested_modal_gate()
    {
        using var host = new DialogHost(600, 500);
        host.Theme.States = new MaterialStates { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 };
        var outer = new MaterialDialog { Width = 360, Height = 240, Content = "Authored body text / 原始正文" };
        outer.Show(host.Overlay); host.Render();
        var before = Painted();
        var inner = new MaterialDialog { Width = 120, Height = 80 }; inner.Show(host.Overlay); host.Render();
        Assert.False(outer.IsEffectivelyEnabled); Assert.Equal(before, Painted());
        inner.Cancel(); host.Render(); Assert.True(outer.IsEffectivelyEnabled);
        outer.IsEnabled = false; host.Render(); Assert.NotEqual(before, Painted());

        byte[] Painted()
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)outer.Bounds.Width, (int)outer.Bounds.Height), new Vector(96, 96));
            bitmap.Render(outer); using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default); return stream.ToArray();
        }
    }

    [AvaloniaTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void Modal_gate_keeps_each_selection_field_chip_slider_and_icon_family_authored_paint(int family)
    {
        Control control = family switch {
            0 => new MaterialCheckBox { IsChecked = true }, 1 => new MaterialRadioButton { IsChecked = true },
            2 => new MaterialSwitch { IsChecked = true }, 3 => new MaterialTextField { Label = "Value", Text = "Draft" },
            4 => new MaterialChip { ChipVariant = MaterialChipVariant.Filter, IsChecked = true, Content = "Scope" },
            5 => new MaterialSlider { Value = 50 },
            _ => new MaterialIconButton { IconVariant = MaterialIconButtonVariant.Filled, Content = new MaterialSymbol { Symbol = "add" } }
        };
        control.Width = 180; control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        var overlay = new MaterialOverlayHost { Content = new StackPanel { Children = { control } } };
        using var host = new GeometryHost(overlay, 600, 500);
        host.Theme.States = new MaterialStates { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 }; host.Render();
        var before = Painted();
        var dialog = new MaterialDialog { Width = 220, Height = 160 }; dialog.Show(overlay); host.Render();
        Assert.False(control.IsEffectivelyEnabled); Assert.Equal(before, Painted());
        dialog.Cancel(); host.Render(); Assert.True(control.IsEffectivelyEnabled);

        byte[] Painted()
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)control.Bounds.Width, (int)control.Bounds.Height), new Vector(96, 96));
            bitmap.Render(control); using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default); return stream.ToArray();
        }
    }

    [AvaloniaFact]
    public void Background_progress_keeps_its_public_presentation_clock_and_authored_disabling_still_pauses_it()
    {
        var progress = new MaterialLinearProgressIndicator { Width = 160, Height = 48, IsIndeterminate = true,
            AnimationTime = TimeSpan.Zero, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var overlay = new MaterialOverlayHost { Background = Brushes.White, Content = progress };
        using var host = new GeometryHost(overlay, 600, 500);
        new MaterialDialog { Width = 220, Height = 160 }.Show(overlay); host.Render();
        host.Theme.Motion = new MaterialMotion(); host.Render();
        var first = host.Region(0, 0, 160, 48).Cast<Color>().ToArray();
        progress.AnimationTime = TimeSpan.FromMilliseconds(1000); host.Render();
        Assert.False(progress.IsEffectivelyEnabled);
        Assert.False(first.SequenceEqual(host.Region(0, 0, 160, 48).Cast<Color>()));
        progress.IsEnabled = false; host.Render();
        var disabled = host.Region(0, 0, 160, 48).Cast<Color>().ToArray();
        progress.AnimationTime = TimeSpan.FromMilliseconds(2000); host.Render();
        Assert.True(disabled.SequenceEqual(host.Region(0, 0, 160, 48).Cast<Color>()));
    }

    [AvaloniaFact]
    public void Modal_paint_reconciles_authored_ancestors_commands_new_children_and_nested_dialogs()
    {
        var command = new ChangingCommand();
        var button = new MaterialButton { Width = 120, Content = "", Command = command };
        var probe = new MaterialButton { Width = 120, Content = "", IsEnabled = false };
        var group = new StackPanel { Children = { button, probe } };
        var overlay = new MaterialOverlayHost { Background = Brushes.White, Content = group };
        using var host = new GeometryHost(overlay, 600, 500);
        host.Theme.States = new MaterialStates { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 };
        host.Render();
        var point = button.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var disabledPoint = probe.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var enabled = host.Pixel(point.X, point.Y); var disabled = host.Pixel(disabledPoint.X, disabledPoint.Y);
        var outerButton = new MaterialButton { Width = 120, Content = "" };
        var outer = new MaterialDialog { Width = 360, Height = 240, Content = outerButton };
        outer.Show(overlay); host.Render();
        command.Enabled = false; host.Render(); AssertScrim(disabled, host.Pixel(point.X, point.Y));
        command.Enabled = true; host.Render(); AssertScrim(enabled, host.Pixel(point.X, point.Y));
        group.IsEnabled = false; host.Render(); AssertScrim(disabled, host.Pixel(point.X, point.Y));
        group.IsEnabled = true; host.Render(); AssertScrim(enabled, host.Pixel(point.X, point.Y));
        overlay.IsEnabled = false; host.Render(); AssertScrim(disabled, host.Pixel(point.X, point.Y));
        overlay.IsEnabled = true; host.Render(); AssertScrim(enabled, host.Pixel(point.X, point.Y));
        var added = new MaterialButton { Width = 120, Content = "" }; group.Children.Add(added); host.Render();
        var addedPoint = added.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        Assert.False(added.IsEffectivelyEnabled); AssertScrim(enabled, host.Pixel(addedPoint.X, addedPoint.Y));
        var outerPoint = outerButton.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var outerPaint = host.Pixel(outerPoint.X, outerPoint.Y);
        var inner = new MaterialDialog { Width = 100, Height = 80 };
        inner.Show(overlay); host.Render();
        Assert.False(outerButton.IsEffectivelyEnabled); AssertScrim(outerPaint, host.Pixel(outerPoint.X, outerPoint.Y));
        inner.Cancel(); host.Render(); Assert.True(outerButton.IsEffectivelyEnabled);
        AssertScrim(enabled, host.Pixel(point.X, point.Y));
        outer.Cancel(); host.Render(); Assert.True(button.IsEffectivelyEnabled); Assert.True(added.IsEffectivelyEnabled);
        command.Enabled = false; host.Render(); Assert.False(button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Modal_scrim_preserves_authored_enabled_and_disabled_paint_while_cached_actions_remain_blocked()
    {
        var enabled = new MaterialButton { Width = 120, Content = "" };
        var disabled = new MaterialButton { Width = 120, Content = "", IsEnabled = false };
        var stock = new Button { Width = 120, Height = 48, Content = "Stock" };
        var overlay = new MaterialOverlayHost { Background = Brushes.White,
            Content = new StackPanel { Children = { enabled, disabled, stock } } };
        using var host = new GeometryHost(overlay, 600, 500);
        host.Theme.States = new MaterialStates { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 };
        host.Theme.LightColorScheme = host.Theme.LightColorScheme with { Primary = Color.Parse("#006C4C") }; host.Render();
        var enabledPoint = enabled.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var disabledPoint = disabled.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var enabledBefore = host.Pixel(enabledPoint.X, enabledPoint.Y);
        var disabledBefore = host.Pixel(disabledPoint.X, disabledPoint.Y);
        var peer = ControlAutomationPeer.CreatePeerForElement(enabled)!;
        var cached = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetProvider<IInvokeProvider>());
        var stockCached = Assert.IsAssignableFrom<IInvokeProvider>(ControlAutomationPeer.CreatePeerForElement(stock)!.GetProvider<IInvokeProvider>());
        var invoked = 0; enabled.Click += (_, _) => invoked++;
        var dialog = new MaterialDialog { Width = 220, Height = 160, Title = "Dialog" };
        dialog.Show(overlay); host.Render();
        Assert.False(enabled.IsEffectivelyEnabled); Assert.False(peer.IsEnabled());
        Assert.ThrowsAny<Exception>(() => cached.Invoke()); Assert.Equal(0, invoked);
        Assert.False(stock.IsEffectivelyEnabled); Assert.ThrowsAny<Exception>(() => stockCached.Invoke());
        AssertScrim(enabledBefore, host.Pixel(enabledPoint.X, enabledPoint.Y));
        AssertScrim(disabledBefore, host.Pixel(disabledPoint.X, disabledPoint.Y));
        dialog.Cancel(); host.Render();
        Assert.True(enabled.IsEffectivelyEnabled); Assert.False(disabled.IsEffectivelyEnabled);
        Assert.Equal(enabledBefore, host.Pixel(enabledPoint.X, enabledPoint.Y));
    }

    private static void AssertScrim(Color original, Color actual)
    {
        // Pinned Dialog scrim is black at .32 alpha over the authored component paint.
        Assert.InRange(Math.Abs(actual.R - Math.Round(original.R * .68)), 0, 1);
        Assert.InRange(Math.Abs(actual.G - Math.Round(original.G * .68)), 0, 1);
        Assert.InRange(Math.Abs(actual.B - Math.Round(original.B * .68)), 0, 1);
    }
    private sealed class ChangingCommand : ICommand
    {
        private bool _enabled = true;
        public bool Enabled { get => _enabled; set { _enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) { }
        public event EventHandler? CanExecuteChanged;
    }
}
