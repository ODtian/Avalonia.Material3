using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SelectionScenarioTests
{
    [AvaloniaTheory]
    [InlineData(true, true)] [InlineData(true, false)]
    [InlineData(null, true)] [InlineData(null, false)]
    [InlineData(false, true)] [InlineData(false, false)]
    public void Checkbox_box_uses_one_flat_fill_when_its_colours_match_and_an_independent_outline_otherwise(bool? selected, bool enabled)
    {
        var checkbox = new MaterialCheckBox { IsThreeState = true, IsChecked = selected, IsEnabled = enabled };
        using var host = new SelectionHost(checkbox);
        var edge = host.PixelAt(checkbox, new Point(24, 16));
        var interior = host.PixelAt(checkbox, new Point(24, 19));
        if (selected != false)
        {
            Assert.Equal(interior, edge);
            if (!enabled)
            {
                // Native single OnSurface fill at .38 alpha over light Surface.
                var expected = Color.Parse("#A9A3AA");
                Assert.InRange(Math.Abs(edge.R - expected.R), 0, 1);
                Assert.InRange(Math.Abs(edge.G - expected.G), 0, 1);
                Assert.InRange(Math.Abs(edge.B - expected.B), 0, 1);
            }
        }
        else { Assert.NotEqual(interior, edge); Assert.Equal(Color.Parse("#FEF7FF"), interior); }
    }

    [AvaloniaTheory]
    [InlineData("checkbox")]
    [InlineData("radio")]
    [InlineData("switch")]
    public void Pointer_states_are_visible_and_release_outside_cancels_selection(string kind)
    {
        var control = Create(kind);
        using var host = new SelectionHost(control);
        var idle = host.Capture();
        // Start outside the switch thumb: dragging the thumb intentionally selects its endpoint.
        var point = host.PointIn(control, new Point(kind == "switch" ? 48 : 24, 24));
        host.Window.MouseMove(point);
        var hover = host.Capture();
        Assert.NotEqual(idle, hover);
        host.Window.MouseDown(point, MouseButton.Left);
        Assert.True(control.IsPressed);
        Assert.NotEqual(hover, host.Capture());
        Assert.False(control.IsChecked);
        host.Window.MouseMove(new Point(390, 690), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(new Point(390, 690), MouseButton.Left);
        Assert.False(control.IsPressed);
        Assert.False(control.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData("checkbox")]
    [InlineData("radio")]
    [InlineData("switch")]
    public void Keyboard_user_sees_focus_and_space_selects_on_release(string kind)
    {
        var control = Create(kind);
        using var host = new SelectionHost(control);
        var idle = host.Capture();
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(control.IsFocused);
        Assert.Contains(":focus-visible", control.Classes);
        Assert.NotEqual(idle, host.Capture());
        // Pinned Ripple.kt defaults to opacity focus: 10% state ink over the light surface.
        var focus = host.PixelAt(control, new Point(kind == "switch" ? 3 : 4, 24));
        var expectedFocus = Color.Parse(kind == "switch" ? "#DBD5DE" : "#E8E2EA");
        Assert.InRange(Math.Abs(focus.R - expectedFocus.R), 0, 1);
        Assert.InRange(Math.Abs(focus.G - expectedFocus.G), 0, 1);
        Assert.InRange(Math.Abs(focus.B - expectedFocus.B), 0, 1);
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(control.IsPressed);
        Assert.False(control.IsChecked);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(control.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData("checkbox", false)]
    [InlineData("checkbox", true)]
    [InlineData("radio", false)]
    [InlineData("radio", true)]
    [InlineData("switch", false)]
    [InlineData("switch", true)]
    public void Disabled_selection_is_distinct_and_blocks_touch_mouse_and_keyboard(string kind, bool selected)
    {
        var control = Create(kind);
        control.IsChecked = selected;
        using var host = new SelectionHost(control);
        var enabled = host.Capture();
        control.IsEnabled = false;
        Assert.NotEqual(enabled, host.Capture());
        Assert.Equal(Color.Parse(kind == "radio" ? "#611D1B20" : kind == "switch" ? "#E3DCE4" : "#1D1B20"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
        var point = host.PointIn(control, new Point(24, 1));
        using (var contact = host.Window.TouchBegin(point))
            host.Window.TouchEnd(contact, point);
        host.Click(control);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.False(control.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal(selected, control.IsChecked);
    }

    [AvaloniaFact]
    public void Radio_arrow_navigation_wraps_skips_disabled_and_does_not_cross_groups()
    {
        var first = new MaterialRadioButton { GroupName = "delivery", Content = "Email" };
        var disabled = new MaterialRadioButton { GroupName = "delivery", Content = "Courier", IsEnabled = false };
        var last = new MaterialRadioButton { GroupName = "delivery", Content = "Post" };
        var other = new MaterialRadioButton { GroupName = "other", Content = "Other" };
        using var host = new SelectionHost(first, disabled, last, other);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.True(last.IsFocused);
        Assert.True(last.IsChecked);
        Assert.False(first.IsChecked);
        Assert.False(other.IsChecked);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.True(first.IsFocused);
        Assert.True(first.IsChecked);
        Assert.False(last.IsChecked);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        Assert.True(last.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData("checkbox")]
    [InlineData("radio")]
    [InlineData("switch")]
    public void Error_state_has_visible_text_accessible_help_and_does_not_change_the_value(string kind)
    {
        var control = Create(kind);
        using var host = new SelectionHost(control);
        var normal = host.Capture();
        SetError(control, true, "Choose an option / 请选择");
        Assert.NotEqual(normal, host.Capture());
        Assert.Equal("Choose an option / 请选择", ControlAutomationPeer.CreatePeerForElement(control)!.GetHelpText());
        Assert.False(control.IsChecked);
        host.Click(control, new Point(kind == "switch" ? 48 : 24, 24));
        Assert.True(control.IsChecked);
        Assert.Equal(Color.Parse("#B3261E"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
        control.IsEnabled = false;
        host.Capture();
        Assert.Equal(Color.Parse(kind == "radio" ? "#611D1B20" : kind == "switch" ? "#E3DCE4" : "#1D1B20"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
        control.IsEnabled = true;
        SetError(control, false, "Choose an option / 请选择");
        Assert.Equal("", ControlAutomationPeer.CreatePeerForElement(control)!.GetHelpText());
    }

    [AvaloniaFact]
    public void Standard_radio_and_switch_never_expose_a_mixed_state()
    {
        var radio = new MaterialRadioButton { IsChecked = null, IsThreeState = true };
        var toggle = new MaterialSwitch { IsChecked = null, IsThreeState = true };
        using var host = new SelectionHost(radio, toggle);
        Assert.False(radio.IsChecked);
        Assert.False(toggle.IsChecked);
        Assert.False(radio.IsThreeState);
        Assert.False(toggle.IsThreeState);
        host.Click(toggle, new Point(48, 24));
        host.Click(toggle, new Point(48, 24));
        Assert.False(toggle.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData("checkbox", AutomationControlType.CheckBox)]
    [InlineData("switch", AutomationControlType.CheckBox)]
    [InlineData("radio", AutomationControlType.RadioButton)]
    public void Automation_reports_name_role_value_and_supports_actions_but_not_when_disabled(string kind, AutomationControlType role)
    {
        var control = Create(kind);
        using var host = new SelectionHost(control);
        AutomationProperties.SetName(control, "Delivery preference");
        var peer = ControlAutomationPeer.CreatePeerForElement(control)!;
        Assert.Equal("Delivery preference", peer.GetName());
        Assert.Equal(role, peer.GetAutomationControlType());
        Assert.True(peer.IsEnabled());
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer);
        Assert.Equal(ToggleState.Off, toggle.ToggleState);
        if (kind == "radio")
        {
            var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
            selection.Select();
            Assert.True(selection.IsSelected);
        }
        else
            toggle.Toggle();
        Assert.True(control.IsChecked);
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        if (control is MaterialCheckBox checkbox)
        {
            checkbox.IsChecked = null;
            Assert.Equal(ToggleState.Indeterminate, toggle.ToggleState);
        }
        control.IsEnabled = false;
        Assert.False(peer.IsEnabled());
        var value = control.IsChecked;
        Assert.ThrowsAny<Exception>(() => toggle.Toggle());
        if (peer is ISelectionItemProvider radio)
            Assert.ThrowsAny<Exception>(() => radio.Select());
        Assert.Equal(value, control.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData("checkbox")]
    [InlineData("radio")]
    [InlineData("switch")]
    public void Touch_activates_the_transparent_edge_of_the_48_dip_target(string kind)
    {
        var control = Create(kind);
        using var host = new SelectionHost(control);
        var point = host.PointIn(control, new Point(24, 1));
        using var contact = host.Window.TouchBegin(point);
        host.Window.TouchEnd(contact, point);
        Assert.True(control.IsChecked);
    }

    [AvaloniaFact]
    public void Switch_thumb_drag_selects_the_endpoint_in_both_directions()
    {
        var control = new MaterialSwitch();
        using var host = new SelectionHost(control);
        var off = host.PointIn(control, new Point(16, 24));
        var on = host.PointIn(control, new Point(36, 24));
        host.Window.MouseDown(off, MouseButton.Left);
        host.Window.MouseMove(on, RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(on, MouseButton.Left);
        Assert.True(control.IsChecked);
        host.Window.MouseDown(on, MouseButton.Left);
        host.Window.MouseMove(off, RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(off, MouseButton.Left);
        Assert.False(control.IsChecked);
    }

    [AvaloniaFact]
    public void Switch_icon_and_state_content_slots_follow_the_value_without_replacing_input()
    {
        var offIcon = new TextBlock { Text = "×" };
        var onIcon = new TextBlock { Text = "✓" };
        var offText = new TextBlock { Text = "Not saving" };
        var onText = new TextBlock { Text = "Saving" };
        var control = new MaterialSwitch { Content = "Auto save", OffIcon = offIcon, OnIcon = onIcon, OffContent = offText, OnContent = onText };
        using var host = new SelectionHost(control);
        Assert.True(IsDisplayed(offIcon));
        Assert.False(IsDisplayed(onIcon));
        Assert.True(IsDisplayed(offText));
        Assert.False(IsDisplayed(onText));
        Assert.True(offIcon.Bounds.Width > 0);
        host.Click(control, new Point(48, 24));
        host.Capture();
        Assert.True(control.IsChecked);
        Assert.False(IsDisplayed(offIcon));
        Assert.True(IsDisplayed(onIcon));
        Assert.False(IsDisplayed(offText));
        Assert.True(IsDisplayed(onText));
    }

    [AvaloniaFact]
    public void Disabled_off_switch_retains_the_surface_container_track_fill()
    {
        var control = new MaterialSwitch { IsEnabled = false };
        using var host = new SelectionHost(control);
        Assert.Equal(Color.Parse("#FBF4FC"), host.PixelAt(control, new Point(4, 24)));
    }

    [AvaloniaFact]
    public void Unselected_switch_keeps_the_outline_role_during_hover_and_keyboard_focus()
    {
        var control = new MaterialSwitch();
        using var host = new SelectionHost(control);
        host.Window.MouseMove(host.PointIn(control, new Point(48, 24)));
        host.Capture();
        Assert.Equal(Color.Parse("#79747E"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
        host.Window.MouseMove(new Point(390, 690));
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Capture();
        Assert.Equal(Color.Parse("#79747E"), Assert.IsAssignableFrom<ISolidColorBrush>(control.BorderBrush).Color);
    }

    private static bool IsDisplayed(Control content) => content.GetVisualParent() is not null && content.IsEffectivelyVisible;

    private static void SetError(ToggleButton control, bool error, string text)
    {
        switch (control)
        {
            case MaterialCheckBox checkbox: checkbox.IsError = error; checkbox.ErrorText = text; break;
            case MaterialRadioButton radio: radio.IsError = error; radio.ErrorText = text; break;
            case MaterialSwitch toggle: toggle.IsError = error; toggle.ErrorText = text; break;
        }
    }

    private static ToggleButton Create(string kind) => kind switch
    {
        "checkbox" => new MaterialCheckBox { Content = "Notifications" },
        "radio" => new MaterialRadioButton { Content = "Email", GroupName = "delivery" },
        "switch" => new MaterialSwitch { Content = "Auto save" },
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    [AvaloniaFact]
    public void Switch_has_a_52_by_32_track_and_a_thumb_that_moves_when_toggled()
    {
        var control = new MaterialSwitch { Content = "Auto save" };
        using var host = new SelectionHost(control);
        Assert.True(control.Bounds.Height >= 48);
        Assert.Equal(Color.Parse("#79747E"), host.PixelAt(control, new Point(16, 24)));
        var off = host.Capture();
        host.Click(control, new Point(36, 24));
        Assert.True(control.IsChecked);
        Assert.NotEqual(off, host.Capture());
        host.Window.MouseMove(new Point(390, 690));
        Assert.Equal(Colors.White, host.PixelAt(control, new Point(36, 24)));
        Assert.Equal(Color.Parse("#6750A4"), host.PixelAt(control, new Point(16, 24)));
    }

    [AvaloniaFact]
    public void Radio_group_shows_a_20_dip_ring_and_exclusively_selects_an_option()
    {
        var first = new MaterialRadioButton { GroupName = "delivery", Content = "Email" };
        var second = new MaterialRadioButton { GroupName = "delivery", Content = "Post" };
        using var host = new SelectionHost(first, second);
        Assert.True(first.Bounds.Height >= 48);
        Assert.Equal(Color.Parse("#49454F"), host.PixelAt(first, new Point(15, 24)));
        host.Click(first);
        Assert.True(first.IsChecked);
        Assert.Equal(Color.Parse("#6750A4"), host.PixelAt(first, new Point(24, 24)));
        host.Click(second);
        Assert.False(first.IsChecked);
        Assert.True(second.IsChecked);
        host.Click(second);
        Assert.True(second.IsChecked);
    }

    [AvaloniaFact]
    public void Three_state_checkbox_cycles_false_true_mixed_false_with_a_visible_mixed_mark()
    {
        var checkbox = new MaterialCheckBox { IsThreeState = true };
        using var host = new SelectionHost(checkbox);
        host.Click(checkbox);
        Assert.True(checkbox.IsChecked);
        host.Click(checkbox);
        Assert.Null(checkbox.IsChecked);
        Assert.Equal(Colors.White, host.PixelAt(checkbox, new Point(24, 24)));
        Assert.Equal(Color.Parse("#6750A4"), host.PixelAt(checkbox, new Point(17, 18)));
        host.Click(checkbox);
        Assert.False(checkbox.IsChecked);
    }

    [AvaloniaFact]
    public void Checkbox_has_an_18_dip_Material_outline_and_a_48_dip_touch_target()
    {
        var checkbox = new MaterialCheckBox();
        using var host = new SelectionHost(checkbox);
        Assert.True(checkbox.Bounds.Width >= 48);
        Assert.True(checkbox.Bounds.Height >= 48);
        Assert.Equal(Color.Parse("#49454F"), host.PixelAt(checkbox, new Point(15, 24)));
        var uncheckedFrame = host.Capture();
        host.Click(checkbox);
        Assert.True(checkbox.IsChecked);
        Assert.NotEqual(uncheckedFrame, host.Capture());
        Assert.Equal(Color.Parse("#6750A4"), host.PixelAt(checkbox, new Point(17, 18)));
    }
}
