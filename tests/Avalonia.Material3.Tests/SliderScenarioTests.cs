using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SliderScenarioTests
{
    [AvaloniaFact]
    public void Track_corner_next_to_the_handle_uses_the_pinned_two_dip_inside_radius()
    {
        using var host = new SliderHost(new MaterialSlider { Value = 50 });
        var point = host.Slider.TranslatePoint(new Point(host.Slider.Bounds.Width / 2 - 11, host.Slider.Bounds.Height - 39), host.Window)!.Value;
        Assert.Equal(Color.Parse("#6750A4"), host.PixelAt(point));
    }

    [AvaloniaFact]
    public void Automation_clients_receive_value_and_readonly_notifications_and_formatted_units()
    {
        using var host = new SliderHost(new MaterialSlider { Value = 40, LabelFormat = "0'%'" });
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Slider);
        var changes = new List<AutomationPropertyChangedEventArgs>();
        peer.PropertyChanged += (_, e) => changes.Add(e);
        host.Slider.Value = 60;
        Assert.Equal("60%", peer.GetItemStatus());
        Assert.Contains(changes, e => e.Property == RangeValuePatternIdentifiers.ValueProperty && Equals(e.NewValue, 60d));
        host.Slider.IsEnabled = false;
        Assert.Contains(changes, e => e.Property == RangeValuePatternIdentifiers.IsReadOnlyProperty && Equals(e.NewValue, true));
    }

    [AvaloniaFact]
    public void Touch_can_separate_coincident_range_endpoints_in_both_directions()
    {
        var slider = new MaterialRangeSlider { LowerValue = 50, UpperValue = 50, Step = 10 };
        using var host = new SliderHost(slider);
        using (var contact = host.Window.TouchBegin(host.At(0.2)))
            host.Window.TouchEnd(contact, host.At(0.2));
        Assert.Equal(20, slider.LowerValue);
        Assert.Equal(50, slider.UpperValue);
        slider.LowerValue = 50;
        using (var contact = host.Window.TouchBegin(host.At(0.8)))
            host.Window.TouchEnd(contact, host.At(0.8));
        Assert.Equal(50, slider.LowerValue);
        Assert.Equal(80, slider.UpperValue);
    }

    [AvaloniaTheory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Invalid_values_do_not_replace_a_valid_setting(double invalid)
    {
        var slider = new MaterialRangeSlider { LowerValue = 20, UpperValue = 80 };
        Assert.Throws<ArgumentException>(() => slider.Minimum = invalid);
        Assert.Throws<ArgumentException>(() => slider.Maximum = invalid);
        Assert.Throws<ArgumentException>(() => slider.Step = invalid);
        Assert.Throws<ArgumentException>(() => slider.LowerValue = invalid);
        Assert.Throws<ArgumentException>(() => slider.UpperValue = invalid);
        Assert.Equal(20, slider.LowerValue);
        Assert.Equal(80, slider.UpperValue);
    }

    [AvaloniaFact]
    public void Collapsed_range_is_stable_and_invalid_steps_or_formats_are_rejected()
    {
        using var host = new SliderHost(new MaterialRangeSlider { Minimum = 20, Maximum = 20, Step = 10, ShowMarks = true });
        var range = (MaterialRangeSlider)host.Slider;
        host.Window.MouseDown(host.At(0.5), MouseButton.Left);
        host.Window.MouseUp(host.At(0.5), MouseButton.Left);
        host.Window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
        Assert.Equal(20, range.LowerValue);
        Assert.Equal(20, range.UpperValue);
        Assert.Throws<ArgumentException>(() => range.Step = -1);
        Assert.Throws<ArgumentException>(() => range.LabelFormat = "Q");
        Assert.NotEmpty(host.Capture());
    }

    [AvaloniaFact]
    public void Custom_template_keeps_range_focus_input_and_automation_and_rtl_reverses_horizontal_input()
    {
        var slider = new MaterialRangeSlider { LowerValue = 20, UpperValue = 80, Step = 10 };
        using var host = new SliderHost(slider);
        var peer = ControlAutomationPeer.CreatePeerForElement(slider);
        var originalEndpoints = peer.GetChildren();
        slider.Template = new FuncControlTemplate<MaterialRangeSlider>((_, scope) =>
        {
            var surface = new MaterialSliderPresenter();
            scope.Register("PART_Surface", surface);
            return new Border { Child = surface };
        });
        host.Capture();
        var endpoints = peer.GetChildren();
        Assert.Equal(2, endpoints.Count);
        Assert.Same(originalEndpoints[0], endpoints[0]);
        Assert.Same(originalEndpoints[1], endpoints[1]);
        endpoints[0].SetFocus();
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(30, slider.LowerValue);
        slider.FlowDirection = FlowDirection.RightToLeft;
        host.Capture();
        host.Window.MouseDown(host.At(0.1), MouseButton.Left);
        host.Window.MouseUp(host.At(0.1), MouseButton.Left);
        Assert.Equal(90, slider.UpperValue);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(80, slider.UpperValue);
    }

    [AvaloniaFact]
    public void Standard_track_and_value_indicator_use_the_pinned_semantic_color_roles()
    {
        using var host = new SliderHost(new MaterialSlider());
        Assert.Equal(Color.Parse("#E8DEF8"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Slider.Background).Color);
        Assert.Equal(Color.Parse("#322F35"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Slider.ValueIndicatorBrush).Color);
        Assert.Equal(Color.Parse("#F5EFF7"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Slider.ValueIndicatorForeground).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Capture();
        Assert.Equal(Color.Parse("#4A4458"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Slider.Background).Color);
        Assert.Equal(Color.Parse("#E6E0E9"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Slider.ValueIndicatorBrush).Color);
    }

    [AvaloniaFact]
    public void Disabling_during_drag_cancels_capture_and_later_input_cannot_change_the_last_setting()
    {
        using var host = new SliderHost(new MaterialSlider { Step = 10 });
        host.Window.MouseDown(host.At(0.4), MouseButton.Left);
        Assert.Equal(40, host.Slider.Value);
        host.Slider.IsEnabled = false;
        var disabled = host.Capture();
        host.Window.MouseMove(host.At(0.9), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(host.At(0.9), MouseButton.Left);
        Assert.Equal(40, host.Slider.Value);
        Assert.Equal(disabled, host.Capture());
        host.Slider.IsEnabled = true;
        host.Window.MouseDown(host.At(0.2), MouseButton.Left);
        host.Window.MouseUp(host.At(0.2), MouseButton.Left);
        Assert.Equal(20, host.Slider.Value);
    }

    [AvaloniaFact]
    public void Host_binding_to_lower_value_observes_changes_without_losing_the_binding_on_input_or_theme_switch()
    {
        var range = new MaterialRangeSlider { LowerValue = 20, UpperValue = 80, Step = 10 };
        var mirror = new MaterialSlider();
        using var binding = mirror.Bind(MaterialSlider.ValueProperty,
            new Binding(nameof(MaterialRangeSlider.LowerValue)) { Source = range, Mode = BindingMode.TwoWay });
        using var host = new SliderHost(range);
        Assert.Equal(20, mirror.Value);
        host.Window.MouseDown(host.At(0.3), MouseButton.Left);
        host.Window.MouseUp(host.At(0.3), MouseButton.Left);
        Assert.Equal(30, mirror.Value);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Capture();
        mirror.Value = 40;
        Assert.Equal(40, range.LowerValue);
        host.Window.MouseDown(host.At(0.2), MouseButton.Left);
        host.Window.MouseUp(host.At(0.2), MouseButton.Left);
        Assert.Equal(20, mirror.Value);
    }

    [AvaloniaFact]
    public void Finite_extreme_bounds_and_dense_marks_do_not_produce_nan_or_unbounded_render_work()
    {
        using var host = new SliderHost(new MaterialSlider { Minimum = -double.MaxValue, Maximum = double.MaxValue,
            ValueLabelVisibility = SliderValueLabelVisibility.Never });
        host.Window.MouseDown(host.At(0.5), MouseButton.Left);
        host.Window.MouseUp(host.At(0.5), MouseButton.Left);
        Assert.Equal(0, host.Slider.Value);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.True(double.IsFinite(host.Slider.Value));
        Assert.True(host.Slider.Value > 0);
        host.Slider.Minimum = -1e308;
        host.Slider.Maximum = 1e308;
        host.Slider.Step = 1e308;
        host.Slider.Value = 8e307;
        Assert.Equal(1e308, host.Slider.Value);
        host.Slider.Step = double.Epsilon;
        host.Slider.ShowMarks = true;
        Assert.NotEmpty(host.Capture());
    }

    [AvaloniaFact]
    public void Arrow_keys_reach_the_previous_stop_from_a_short_final_interval()
    {
        using var host = new SliderHost(new MaterialSlider { Minimum = 10, Maximum = 95, Step = 20, Value = 95 });
        host.Slider.Focus();
        host.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(90, host.Slider.Value);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(95, host.Slider.Value);
    }

    [AvaloniaFact]
    public void Vertical_reversed_and_centered_forms_keep_pointer_and_keyboard_value_semantics()
    {
        var slider = new MaterialSlider { Minimum = -100, Maximum = 100, Step = 10, Value = 0,
            Orientation = Orientation.Vertical, CenteredTrack = true, Height = 160, Width = 120,
            ValueLabelVisibility = SliderValueLabelVisibility.Never };
        using var host = new SliderHost(slider);
        var centered = host.Capture();
        var top = slider.TranslatePoint(new Point(slider.Bounds.Width - 32, 24), host.Window)!.Value;
        host.Window.MouseDown(top, MouseButton.Left);
        host.Window.MouseUp(top, MouseButton.Left);
        Assert.Equal(100, slider.Value);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Equal(90, slider.Value);
        slider.ReverseDirection = true;
        host.Window.MouseDown(top, MouseButton.Left);
        host.Window.MouseUp(top, MouseButton.Left);
        Assert.Equal(-100, slider.Value);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Equal(-90, slider.Value);
        slider.Value = 0;
        slider.CenteredTrack = false;
        Assert.NotEqual(centered, host.Capture());
    }

    [AvaloniaFact]
    public void Marks_and_formatted_labels_remain_readable_after_live_theme_and_font_changes()
    {
        var slider = new MaterialSlider { Value = 50, Step = 10, ShowMarks = true,
            ValueLabelVisibility = SliderValueLabelVisibility.Always, LabelFormat = "0.0" };
        using var host = new SliderHost(slider);
        Assert.Equal("50.0", slider.ValueText);
        var marked = host.Capture();
        slider.ShowMarks = false;
        Assert.NotEqual(marked, host.Capture());
        slider.Marks = new double[] { 0, 25, 75, 100 };
        slider.ShowMarks = true;
        Assert.NotEqual(marked, host.Capture());
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.NotEqual(marked, host.Capture());
        Assert.Equal(Color.Parse("#D0BCFF"), Assert.IsAssignableFrom<ISolidColorBrush>(slider.Foreground).Color);
        host.Theme.Typography = new MaterialTypography { Scale = 2 };
        host.Capture();
        Assert.Equal(28, slider.FontSize);
        Assert.True(slider.Bounds.Height >= 100);
        host.Theme.DarkColorScheme = MaterialColorScheme.Dark with { Primary = Color.Parse("#00FFAA") };
        host.Capture();
        Assert.Equal(Color.Parse("#00FFAA"), Assert.IsAssignableFrom<ISolidColorBrush>(slider.Foreground).Color);
        Assert.Equal(50, slider.Value);
    }

    [AvaloniaFact]
    public void Range_has_two_named_focusable_automation_endpoints_and_pointer_selects_the_nearest()
    {
        var slider = new MaterialRangeSlider { LowerValue = 20, UpperValue = 80, Step = 10 };
        using var host = new SliderHost(slider);
        AutomationProperties.SetName(slider, "Price");
        var peer = ControlAutomationPeer.CreatePeerForElement(slider);
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        var endpoints = peer.GetChildren();
        Assert.Equal(2, endpoints.Count);
        Assert.Equal("Price Lower value", endpoints[0].GetName());
        Assert.Equal("Price Upper value", endpoints[1].GetName());
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(endpoints[0].HasKeyboardFocus());
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(30, slider.LowerValue);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(endpoints[1].HasKeyboardFocus());
        host.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(70, slider.UpperValue);
        var upper = Assert.IsAssignableFrom<IRangeValueProvider>(endpoints[1].GetProvider<IRangeValueProvider>());
        Assert.Equal(30, upper.Minimum);
        upper.SetValue(20);
        Assert.Equal(30, slider.UpperValue);
        host.Window.MouseDown(host.At(0.9), MouseButton.Left);
        host.Window.MouseUp(host.At(0.9), MouseButton.Left);
        Assert.Equal(90, slider.UpperValue);
        Assert.Equal(30, slider.LowerValue);
    }

    [AvaloniaFact]
    public void Automation_can_name_read_and_adjust_a_setting_and_respects_disabled_state()
    {
        using var host = new SliderHost(new MaterialSlider { Minimum = 10, Maximum = 90, Step = 10 });
        AutomationProperties.SetName(host.Slider, "Brightness / 亮度");
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Slider);
        Assert.Equal("Brightness / 亮度", peer.GetName());
        Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());
        var provider = Assert.IsAssignableFrom<IRangeValueProvider>(peer.GetProvider<IRangeValueProvider>());
        Assert.Equal(10, provider.Minimum);
        Assert.Equal(90, provider.Maximum);
        Assert.Equal(10, provider.SmallChange);
        provider.SetValue(46);
        Assert.Equal(50, host.Slider.Value);
        Assert.Equal(50, provider.Value);
        host.Slider.IsEnabled = false;
        Assert.False(peer.IsEnabled());
        Assert.True(provider.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => provider.SetValue(70));
        host.Window.MouseDown(host.At(0.9), MouseButton.Left);
        host.Window.MouseUp(host.At(0.9), MouseButton.Left);
        Assert.Equal(50, host.Slider.Value);
    }

    [AvaloniaFact]
    public void Pointer_touch_and_keyboard_update_the_same_discrete_setting_with_visible_feedback()
    {
        using var host = new SliderHost(new MaterialSlider { Step = 10 });
        var slider = host.Slider;
        Assert.True(slider.Bounds.Height >= 48);
        var idle = host.Capture();
        host.Window.MouseDown(host.At(0.5), MouseButton.Left);
        Assert.Equal(50, slider.Value);
        Assert.NotEqual(idle, host.Capture());
        host.Window.MouseMove(host.At(0.8), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(host.At(0.8), MouseButton.Left);
        Assert.Equal(80, slider.Value);
        using var contact = host.Window.TouchBegin(host.At(0.5));
        host.Window.TouchEnd(contact, host.At(0.5));
        Assert.Equal(50, slider.Value);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(60, slider.Value);
        host.Window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
        Assert.Equal(100, slider.Value);
        host.Window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
        Assert.Equal(0, slider.Value);
    }

    [AvaloniaFact]
    public void Range_endpoints_never_cross_when_values_steps_or_bounds_change()
    {
        var slider = new MaterialRangeSlider { LowerValue = 20, UpperValue = 80, Step = 10 };
        slider.LowerValue = 90;
        Assert.Equal(80, slider.LowerValue);
        slider.UpperValue = 10;
        Assert.Equal(80, slider.UpperValue);
        slider.Maximum = 50;
        Assert.Equal(50, slider.LowerValue);
        Assert.Equal(50, slider.UpperValue);
        slider.Minimum = 60;
        Assert.Equal(60, slider.Maximum);
        Assert.Equal(60, slider.LowerValue);
        Assert.Equal(60, slider.UpperValue);
        slider.Maximum = 40;
        Assert.Equal(40, slider.Minimum);
        Assert.Equal(40, slider.UpperValue);
    }

    private sealed class SliderHost : IDisposable
    {
        public MaterialSlider Slider { get; }
        public MaterialTheme Theme { get; } = new();
        public Window Window { get; }
        public SliderHost(MaterialSlider slider)
        {
            Slider = slider;
            Application.Current!.Styles.Add(Theme);
            Window = new Window { Width = 360, Height = 220, RequestedThemeVariant = ThemeVariant.Light,
                Content = new StackPanel { Margin = new Thickness(24), Children = { slider } } };
            Window.Show();
            Capture();
        }
        public Point At(double fraction) => Slider.TranslatePoint(new Point(24 + fraction * (Slider.Bounds.Width - 48), Slider.Bounds.Height / 2), Window)!.Value;
        public byte[] Capture()
        {
            using var frame = Window.CaptureRenderedFrame()!;
            using var stream = new MemoryStream();
            frame.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            return stream.ToArray();
        }
        public Color PixelAt(Point point)
        {
            using var bitmap = Window.CaptureRenderedFrame()!;
            using var frame = bitmap.Lock();
            var offset = (int)(point.Y * Window.RenderScaling) * frame.RowBytes + (int)(point.X * Window.RenderScaling) * 4;
            var first = Marshal.ReadByte(frame.Address, offset);
            var green = Marshal.ReadByte(frame.Address, offset + 1);
            var third = Marshal.ReadByte(frame.Address, offset + 2);
            var alpha = Marshal.ReadByte(frame.Address, offset + 3);
            return frame.Format == PixelFormat.Bgra8888 ? Color.FromArgb(alpha, third, green, first) : Color.FromArgb(alpha, first, green, third);
        }
        public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
    }

    [AvaloniaFact]
    public void Settings_values_snap_from_minimum_and_keep_both_endpoints_reachable()
    {
        var slider = new MaterialSlider { Minimum = 10, Maximum = 95, Step = 20 };
        slider.Value = 39;
        Assert.Equal(30, slider.Value);
        slider.Value = 40;
        Assert.Equal(50, slider.Value);
        slider.Value = 94;
        Assert.Equal(95, slider.Value);
        slider.Value = -100;
        Assert.Equal(10, slider.Value);
        slider.Step = 0;
        slider.Value = 42.125;
        Assert.Equal(42.125, slider.Value);
    }
}
