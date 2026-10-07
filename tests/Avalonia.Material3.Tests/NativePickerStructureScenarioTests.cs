using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Automation;
using Avalonia.Headless;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NativePickerStructureScenarioTests
{
    [AvaloniaFact]
    public void Time_dialog_title_uses_its_slot_and_disabled_picker_keeps_cancel_available()
    {
        using var host=new DialogHost();var picker=new MaterialTimePicker {SelectedTime=new(7,7),Layout=MaterialTimePickerLayout.Vertical};
        picker.Show(host.Overlay);host.Render();
        var dialog=host.Window.GetVisualDescendants().OfType<MaterialDialog>().Single();
        var title=host.Window.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.IsEffectivelyVisible&&t.Text==picker.Labels.Title);
        var dialogBox=GeometryHost.Box(dialog,host.Window);var titleBox=GeometryHost.Box(title,host.Window);
        Assert.Equal(24,titleBox.Left-dialogBox.Left);Assert.Equal(24,titleBox.Top-dialogBox.Top);Assert.Equal(12,title.FontSize);
        picker.Labels=picker.Labels with {Title="Choose delivery time"};host.Render();Assert.Equal(picker.Labels.Title,title.Text);
        var mode=host.Window.GetVisualDescendants().OfType<MaterialIconButton>().Single(b=>AutomationProperties.GetName(b)==picker.Labels.InputMode);
        var confirm=host.Window.GetVisualDescendants().OfType<MaterialButton>().Single(b=>b.IsEffectivelyVisible&&AutomationProperties.GetName(b)==picker.Labels.Confirm);
        var cancel=host.Window.GetVisualDescendants().OfType<MaterialButton>().Single(b=>b.IsEffectivelyVisible&&AutomationProperties.GetName(b)==picker.Labels.Cancel);
        picker.IsEnabled=false;host.Render();Assert.False(mode.IsEffectivelyEnabled);Assert.False(confirm.IsEffectivelyEnabled);Assert.True(cancel.IsEffectivelyEnabled);
        var modeBox=GeometryHost.Box(mode,host.Window);host.Window.MouseDown(modeBox.Center,MouseButton.Left);host.Window.MouseUp(modeBox.Center,MouseButton.Left);
        Assert.Equal(MaterialTimePickerMode.Clock,picker.Mode);Assert.True(picker.Cancel());
    }
    [AvaloniaFact]
    public void Time_dialog_footer_keeps_mode_cancel_confirm_together_and_focuses_native_input()
    {
        using var host=new DialogHost();
        var picker=new MaterialTimePicker {SelectedTime=new(7,7),Layout=MaterialTimePickerLayout.Vertical};
        picker.Show(host.Overlay);host.Render();
        var mode=host.Window.GetVisualDescendants().OfType<MaterialIconButton>().Single(b=>AutomationProperties.GetName(b)==picker.Labels.InputMode);
        var cancel=host.Window.GetVisualDescendants().OfType<MaterialButton>().Single(b=>b.IsEffectivelyVisible && AutomationProperties.GetName(b)==picker.Labels.Cancel);
        var confirm=host.Window.GetVisualDescendants().OfType<MaterialButton>().Single(b=>b.IsEffectivelyVisible && AutomationProperties.GetName(b)==picker.Labels.Confirm);
        var modeBox=GeometryHost.Box(mode,host.Window);var cancelBox=GeometryHost.Box(cancel,host.Window);var confirmBox=GeometryHost.Box(confirm,host.Window);
        Assert.Equal(modeBox.Center.Y,cancelBox.Center.Y);Assert.Equal(modeBox.Center.Y,confirmBox.Center.Y);
        Assert.True(modeBox.Right<cancelBox.Left);
        host.Window.MouseDown(modeBox.Center,MouseButton.Left);host.Window.MouseUp(modeBox.Center,MouseButton.Left);host.Render();
        Assert.Equal(MaterialTimePickerMode.Input,picker.Mode);Assert.True(picker.HourInput.IsFocused);
        picker.HourInput.SelectAll();host.Window.KeyTextInput("11");host.Render();Assert.Equal(11,picker.SelectedTime?.Hour);
        Assert.True(picker.Confirm());
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Clock_surface_has_two_96_dip_fields_and_the_standard_display_to_dial_geometry(bool is24Hour)
    {
        var picker=new MaterialTimePicker {Is24Hour=is24Hour,SelectedTime=new(19,7),Layout=MaterialTimePickerLayout.Vertical};
        using var host=new GeometryHost(picker,400,640);
        var selectors=picker.GetVisualDescendants().OfType<MaterialTimeSelector>().ToArray();
        Assert.All(selectors,s=>Assert.Equal(96,s.Bounds.Width));
        var hour=GeometryHost.Box(selectors[0],host.Window);
        var dial=GeometryHost.Box(picker.GetVisualDescendants().OfType<MaterialClockDial>().Single(),host.Window);
        Assert.Equal(36,dial.Top-hour.Bottom);
        Assert.Equal(80+36+256+24,picker.Surface.Bounds.Height);
        Assert.Equal(is24Hour?256:272,picker.Surface.DesiredSize.Width);
        Assert.Equal(0,picker.Surface.GetVisualDescendants().OfType<Button>().Count(b=>b.Content is "−" or "+" or "Enter time"));
    }

    [AvaloniaFact]
    public void Time_input_supporting_replaces_its_label_with_error_in_the_same_two_line_region()
    {
        var picker=new MaterialTimePicker {Mode=MaterialTimePickerMode.Input,SelectedTime=new(7,7)};
        using var host=new GeometryHost(picker,400,640);
        var before=picker.HourInput.Bounds.Height;
        picker.HourInput.Text="invalid";host.Render();
        Assert.Equal(before,picker.HourInput.Bounds.Height);
        Assert.Single(picker.GetVisualDescendants().OfType<TextBlock>(),t=>t.IsEffectivelyVisible && t.Text==picker.Labels.InvalidHour);
        Assert.Equal(0,picker.HourInput.GetVisualDescendants().OfType<TextBlock>().Count(t=>t.IsEffectivelyVisible && t.Text==picker.Labels.Hour));
    }

    [AvaloniaFact]
    public void Default_time_layout_tracks_window_orientation_and_explicit_layout_wins()
    {
        var picker=new MaterialTimePicker {SelectedTime=new(7,7)};
        using var host=new GeometryHost(picker,800,350);
        Rect Dial()=>GeometryHost.Box(picker.GetVisualDescendants().OfType<MaterialClockDial>().Single(),host.Window);
        var selector=picker.GetVisualDescendants().OfType<MaterialTimeSelector>().First();
        Assert.True(Dial().Left>GeometryHost.Box(selector,host.Window).Right);
        Assert.Equal(238,Dial().Width);
        picker.Layout=MaterialTimePickerLayout.Vertical;host.Render();
        Assert.Equal(256,Dial().Width);
        Assert.True(Dial().Top>GeometryHost.Box(selector,host.Window).Bottom);
    }
    [AvaloniaFact]
    public void Horizontal_dial_uses_its_finite_host_height_inside_a_taller_window()
    {
        var picker=new MaterialTimePicker {Height=230,Layout=MaterialTimePickerLayout.Horizontal,SelectedTime=new(7,7)};
        using var host=new GeometryHost(picker,800,600);
        var dial=picker.GetVisualDescendants().OfType<MaterialClockDial>().Single();
        Assert.Equal(200,dial.Bounds.Width);
        Assert.All(dial.GetVisualDescendants().OfType<MaterialClockNumber>(),mark=>Assert.True(mark.Bounds.Height>=48));
    }
}
