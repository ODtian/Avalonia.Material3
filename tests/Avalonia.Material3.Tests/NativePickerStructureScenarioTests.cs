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
    public void Range_viewport_consumes_the_finite_body_and_the_date_dialog_owns_its_actions()
    {
        var picker=new MaterialDatePicker {SelectionMode=MaterialDateSelectionMode.Range,DisplayMonth=new(2024,2,1)};
        using(var host=new GeometryHost(picker,600,600))
        {
            var scroll=picker.GetVisualDescendants().OfType<ScrollViewer>().Single(s=>s.IsEffectivelyVisible&&s.Bounds.Height>100&&s.VerticalScrollBarVisibility==Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
            Assert.Equal(600,GeometryHost.Box(scroll,host.Window).Bottom);
        }
        using var dialogHost=new DialogHost(800,800);
        picker=new MaterialDatePicker {SelectionMode=MaterialDateSelectionMode.Range,DisplayMonth=new(2024,2,1)};
        picker.Show(dialogHost.Overlay);dialogHost.Render();
        var dialog=dialogHost.Window.GetVisualDescendants().OfType<MaterialDialog>().Single();
        var body=picker.GetVisualDescendants().OfType<ScrollViewer>().Single(s=>s.IsEffectivelyVisible&&s.Bounds.Height>100&&s.VerticalScrollBarVisibility==Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
        var confirm=dialogHost.Button(dialog,picker.Labels.Confirm);
        var bounds=GeometryHost.Box(dialog,dialogHost.Window);
        Assert.Equal(360,bounds.Width);Assert.Equal(568,bounds.Height);
        Assert.Equal(GeometryHost.Box(confirm,dialogHost.Window).Top,GeometryHost.Box(body,dialogHost.Window).Bottom);
        Assert.Equal(8,bounds.Bottom-GeometryHost.Box(confirm,dialogHost.Window).Bottom);
    }
    [AvaloniaFact]
    public void Range_header_uses_its_own_title_placeholders_and_year_format()
    {
        var picker=new MaterialDatePicker {SelectionMode=MaterialDateSelectionMode.Range,DisplayMonth=new(2024,2,1),Culture=System.Globalization.CultureInfo.GetCultureInfo("en-US")};
        using var host=new GeometryHost(picker,600,800);
        var title=picker.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.IsEffectivelyVisible&&t.Text=="Select dates");
        var box=GeometryHost.Box(title,host.Window);Assert.Equal(64,box.Left);Assert.Equal(0,box.Top);
        Assert.Contains(picker.GetVisualDescendants().OfType<TextBlock>(),t=>t.IsEffectivelyVisible&&t.Text=="Start date");
        Assert.Contains(picker.GetVisualDescendants().OfType<TextBlock>(),t=>t.IsEffectivelyVisible&&t.Text=="End date");
        picker.SelectDate(new(2024,2,9));picker.SelectDate(new(2024,2,16));host.Render();
        var start=picker.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.IsEffectivelyVisible&&t.Text=="Feb 9, 2024");
        var delimiter=picker.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.IsEffectivelyVisible&&t.Text=="-");
        Assert.Equal(4,GeometryHost.Box(delimiter,host.Window).Left-GeometryHost.Box(start,host.Window).Right);
        picker.Mode=MaterialDatePickerMode.Input;host.Render();Assert.Equal("Enter dates",title.Text);
        picker.Labels=picker.Labels with {Title="Select date"};host.Render();Assert.Equal("Select date",title.Text);
    }
    [AvaloniaTheory]
    [InlineData(360, false, 252, 266, 286)]
    [InlineData(600, false, 432, 463, 466)]
    [InlineData(600, true, 120, 136, 133)]
    public void Range_wide_calendar_keeps_48_dip_targets_and_the_literal_range_background_spacing(double width,bool rtl,double dayLeft,double outside,double inside)
    {
        var picker=new MaterialDatePicker {SelectionMode=MaterialDateSelectionMode.Range,Width=width,DisplayMonth=new(2024,2,1),
            Culture=System.Globalization.CultureInfo.GetCultureInfo("en-US"),SelectedDate=new(2024,2,9),RangeEnd=new(2024,2,16),
            FlowDirection=rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
        using var host=new GeometryHost(picker,width,800);
        var day=picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d=>d.Date==new DateOnly(2024,2,9));
        var box=GeometryHost.Box(day,host.Window);
        Assert.Equal(dayLeft,box.Left);Assert.Equal(new Size(48,48),box.Size);
        Assert.Equal(Color.Parse("#ECE6F0"),host.Pixel(outside,box.Top+4));
        Assert.Equal(Color.Parse("#E8DEF8"),host.Pixel(inside,box.Top+4));
    }
    [AvaloniaFact]
    public void Single_calendar_reserves_six_rows_when_the_last_week_is_empty()
    {
        var picker=new MaterialDatePicker {DisplayMonth=new(2024,2,1),SelectedDate=new(2024,2,7)};
        using var host=new GeometryHost(picker,360,720);
        var before=picker.Surface.DesiredSize.Height;
        var first=picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d=>d.Date==new DateOnly(2024,2,1));
        Assert.Equal(48,first.Bounds.Width);
        picker.DisplayMonth=new(2024,3,1);host.Render();
        Assert.Equal(before,picker.Surface.DesiredSize.Height);
        var march31=picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d=>d.Date==new DateOnly(2024,3,31));
        Assert.Equal(48,march31.Bounds.Height);
    }

    [AvaloniaFact]
    public void Range_calendar_scrolls_continuous_months_and_inputs_share_a_row()
    {
        var picker=new MaterialDatePicker {SelectionMode=MaterialDateSelectionMode.Range,DisplayMonth=new(2024,2,1)};
        using var host=new GeometryHost(picker,600,800);
        Assert.Equal(600,picker.Bounds.Width);
        var days=picker.GetVisualDescendants().OfType<MaterialCalendarDay>().ToArray();
        Assert.True(days.Length<160);
        Assert.Contains(days,d=>d.Date==new DateOnly(2024,3,1));
        var start=days.Single(d=>d.Date==new DateOnly(2024,2,29));
        var marchFirst=days.Single(d=>d.Date==new DateOnly(2024,3,1));
        Assert.Equal(96,GeometryHost.Box(marchFirst,host.Window).Top-GeometryHost.Box(start,host.Window).Bottom);
        var end=days.Single(d=>d.Date==new DateOnly(2024,3,2));
        foreach(var day in new[]{start,end})
        {
            var point=GeometryHost.Box(day,host.Window).Center;
            host.Window.MouseDown(point,MouseButton.Left);host.Window.MouseUp(point,MouseButton.Left);host.Render();
        }
        Assert.Equal(new DateOnly(2024,2,29),picker.SelectedDate);Assert.Equal(new DateOnly(2024,3,2),picker.RangeEnd);
        var scroll=picker.GetVisualDescendants().OfType<ScrollViewer>().Single(s=>s.IsEffectivelyVisible&&s.Bounds.Height>100&&s.VerticalScrollBarVisibility==Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
        scroll.Offset+=new Vector(0,336);host.Render();
        Assert.Equal(new DateOnly(2024,3,1),picker.DisplayMonth);
        picker.MinimumDate=new(2024,2,1);host.Render();Assert.Equal(new DateOnly(2024,3,1),picker.DisplayMonth);
        Assert.True(GeometryHost.Box(picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d=>d.Date==new DateOnly(2024,3,2)),host.Window).Top<400);
        picker.Mode=MaterialDatePickerMode.Input;host.Render();
        var a=GeometryHost.Box(picker.StartInput,host.Window);var b=GeometryHost.Box(picker.EndInput,host.Window);
        Assert.Equal(a.Top,b.Top);Assert.Equal(a.Width,b.Width);Assert.Equal(8,b.Left-a.Right);
    }

    [AvaloniaFact]
    public void Time_dialog_title_uses_its_slot_and_disabled_picker_keeps_cancel_available()
    {
        using var host=new DialogHost();var picker=new MaterialTimePicker {SelectedTime=new(7,7),Layout=MaterialTimePickerLayout.Vertical};
        picker.Show(host.Overlay);host.Render();
        var dialog=host.Window.GetVisualDescendants().OfType<MaterialDialog>().Single();
        var title=host.Window.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.IsEffectivelyVisible&&t.Text==picker.Labels.Title);
        var dialogBox=GeometryHost.Box(dialog,host.Window);var titleBox=GeometryHost.Box(title,host.Window);
        Assert.Equal(24,titleBox.Left-dialogBox.Left);Assert.Equal(24,titleBox.Top-dialogBox.Top);Assert.Equal(12,title.FontSize);
        var hour=picker.GetVisualDescendants().OfType<MaterialTimeSelector>().First();
        Assert.Equal(20,GeometryHost.Box(hour,host.Window).Top-titleBox.Bottom);
        Assert.Equal(Color.Parse("#1D1B20"),((ISolidColorBrush)title.Foreground!).Color);
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
