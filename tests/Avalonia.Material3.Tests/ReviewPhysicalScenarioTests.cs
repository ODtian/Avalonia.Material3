using System.Runtime.InteropServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

// All positions are PHYSICAL WINDOW offsets from transformed AABBs, never owner-local expected coordinates.
public class ReviewPhysicalScenarioTests
{
    [AvaloniaTheory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Always_range_value_bubbles_stay_with_physical_endpoints_in_LTR_RTL_and_reverse(bool rtl, bool inherited, bool reverse)
    {
        var range = new MaterialRangeSlider { Width = 360, Height = 100, Step = 10, LowerValue = 20, UpperValue = 80,
            ReverseDirection = reverse, ValueLabelVisibility = SliderValueLabelVisibility.Always };
        using var host = new BrowseHost(range, 400, 200);
        host.Window.Background = new SolidColorBrush(Color.Parse("#FEF7FF"));
        if (rtl && inherited) host.Window.FlowDirection = FlowDirection.RightToLeft;
        else if (rtl) range.FlowDirection = FlowDirection.RightToLeft;
        host.Render();
        var box = Physical(range, host.Window);
        // Short labels have ample space: physical centers86.4/273.6, not two central/swapped bubbles.
        var lowerFraction = rtl ^ reverse ? .8 : .2;
        var upperFraction = rtl ^ reverse ? .2 : .8;
        var lowerX = box.Left + 24 + lowerFraction * (box.Width - 48);
        var upperX = box.Left + 24 + upperFraction * (box.Width - 48);
        Assert.Equal(Color.Parse("#322F35"), Pixel(host.Window, new(lowerX, box.Bottom - 70)));
        Assert.Equal(Color.Parse("#322F35"), Pixel(host.Window, new(upperX, box.Bottom - 70)));
        Assert.Equal(Color.Parse("#FEF7FF"), Pixel(host.Window, new(box.Center.X, box.Bottom - 70)));
        // Close/coincident endpoints still show TWO separated bubbles instead of painting one over the other.
        range.LowerValue = range.UpperValue = 50; host.Render();
        Assert.Equal(Color.Parse("#322F35"), Pixel(host.Window, new(box.Center.X - 12, box.Bottom - 70)));
        Assert.Equal(Color.Parse("#322F35"), Pixel(host.Window, new(box.Center.X + 12, box.Bottom - 70)));
        Assert.Equal(50, range.LowerValue); Assert.Equal(50, range.UpperValue);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Physical_RTL_dialog_actions_confirm_left_of_cancel_and_align_to_logical_end(bool inherited)
    {
        using var host = new DialogHost();
        if (inherited) host.Window.FlowDirection = FlowDirection.RightToLeft;
        var dialog = new MaterialDialog { Title = "Confirm", Content = "Action order", ConfirmText = "OK", CancelText = "Cancel" };
        if (!inherited) dialog.FlowDirection = FlowDirection.RightToLeft;
        dialog.Show(host.Overlay); host.Render();
        var confirm = host.Button(dialog, "OK"); var cancel = host.Button(dialog, "Cancel");
        var confirmBox = Physical(confirm, host.Window); var cancelBox = Physical(cancel, host.Window);
        Assert.Equal(confirmBox.Top, cancelBox.Top);
        Assert.True(confirmBox.Center.X < cancelBox.Center.X, $"confirm={confirmBox}; cancel={cancelBox}");
        Assert.InRange(confirmBox.Left - Physical(dialog, host.Window).Left, 20, 28);
    }

    [AvaloniaTheory]
    [InlineData(false, MaterialCarouselLayout.MultiBrowse, 157, 243, 56, 93)]
    [InlineData(true, MaterialCarouselLayout.MultiBrowse, 157, 243, 56, 93)]
    [InlineData(false, MaterialCarouselLayout.Hero, 56, 344, 0, 48)]
    [InlineData(true, MaterialCarouselLayout.Hero, 56, 344, 0, 48)]
    [InlineData(false, MaterialCarouselLayout.Uncontained, 214, 186, 20, 186)]
    [InlineData(true, MaterialCarouselLayout.Uncontained, 214, 186, 20, 186)]
    public void Physical_RTL_carousel_keeps_original_image_order_positions_and_rightward_drag_advances(bool inherited,
        MaterialCarouselLayout layout, double firstX, double firstWidth, double secondX, double secondWidth)
    {
        var items = Enumerable.Range(0, 6).Select(i => new MaterialCarouselItem { Title = "Photo " + i,
            Image = CarouselRefreshScenarioTests.Picture(Brushes.Green) }).ToArray();
        var carousel = new MaterialCarousel { ItemsSource = items, Layout = layout, PreferredItemWidth = 186,
            ItemSpacing = 8, Height = 200, MotionDuration = TimeSpan.Zero };
        using var host = new BrowseHost(carousel, 400, 220);
        if (inherited) host.Window.FlowDirection = FlowDirection.RightToLeft;
        else carousel.FlowDirection = FlowDirection.RightToLeft;
        host.Render();
        var box = Physical(carousel, host.Window);
        Rect Mask(int index) => Physical(carousel.GetVisualDescendants().OfType<Image>().Single(i => i.Source == items[index].Image)
            .GetVisualAncestors().OfType<Border>().First(), host.Window);
        Assert.Equal(firstX, Mask(0).Left - box.Left); Assert.Equal(firstWidth, Mask(0).Width);
        Assert.Equal(secondX, Mask(1).Left - box.Left); Assert.Equal(secondWidth, Mask(1).Width);
        var start = new Point(box.Left + 220, box.Top + 100);
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start + new Vector(100, 0));
        host.Window.MouseUp(start + new Vector(100, 0), MouseButton.Left);
        host.Render();
        Assert.Equal(1, carousel.CurrentIndex); Assert.Same(items[1], carousel.CurrentItem);
        carousel.Focus(); host.Key(PhysicalKey.ArrowLeft);
        Assert.Equal(2, carousel.CurrentIndex); Assert.Same(items[2], carousel.CurrentItem);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Physical_RTL_linear_progress_places_active_gap_track_and_stop_once(bool inherited)
    {
        var indicator = new MaterialLinearProgressIndicator { Value = .25, Width = 240, Height = 4 };
        using var host = new BrowseHost(indicator, 400, 200);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        host.Window.Background = new SolidColorBrush(Color.Parse("#FEF7FF"));
        if (inherited) host.Window.FlowDirection = FlowDirection.RightToLeft;
        else indicator.FlowDirection = FlowDirection.RightToLeft;
        host.Render();
        var graphic = Physical(indicator, host.Window);
        Assert.Equal(240, graphic.Width);
        Assert.Equal(Color.Parse("#6750A4"), Pixel(host.Window, new(graphic.Left + 220, graphic.Center.Y)));
        Assert.Equal(Color.Parse("#FEF7FF"), Pixel(host.Window, new(graphic.Left + 176, graphic.Center.Y)));
        Assert.Equal(Color.Parse("#E8DEF8"), Pixel(host.Window, new(graphic.Left + 100, graphic.Center.Y)));
        Assert.Equal(Color.Parse("#6750A4"), Pixel(host.Window, new(graphic.Left + 2, graphic.Center.Y)));
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Physical_RTL_range_endpoints_keep_numeric_identity_render_and_input(bool inherited, bool reverse)
    {
        var range = new MaterialRangeSlider { Width = 360, Height = 100, Step = 10, ReverseDirection = reverse,
            LowerValue = 20, UpperValue = 80, ValueLabelVisibility = SliderValueLabelVisibility.Never };
        using var host = new BrowseHost(range, 400, 200);
        if (inherited) host.Window.FlowDirection = FlowDirection.RightToLeft;
        else range.FlowDirection = FlowDirection.RightToLeft;
        host.Render();
        var box = Physical(range, host.Window);
        var endpoints = range.GetVisualDescendants().OfType<Control>().Where(c => c.Focusable).ToArray();
        Assert.Equal(2, endpoints.Length);
        var lower = endpoints.Single(c => ControlAutomationPeer.CreatePeerForElement(c)!.GetName() == "Lower value");
        var upper = endpoints.Single(c => ControlAutomationPeer.CreatePeerForElement(c)!.GetName() == "Upper value");
        // Standard layout rounding may move a center by at most half a DIP at scale1.
        Assert.InRange(Physical(lower, host.Window).Center.X, box.Left + 24 + (reverse ? .2 : .8) * (box.Width - 48) - .5, box.Left + 24 + (reverse ? .2 : .8) * (box.Width - 48) + .5);
        Assert.InRange(Physical(upper, host.Window).Center.X, box.Left + 24 + (reverse ? .8 : .2) * (box.Width - 48) - .5, box.Left + 24 + (reverse ? .8 : .2) * (box.Width - 48) + .5);
        var left = new Point(box.Left + 24 + .1 * (box.Width - 48), box.Bottom - 32);
        var right = new Point(box.Right - 24 - .1 * (box.Width - 48), box.Bottom - 32);
        host.Window.MouseDown(left, MouseButton.Left); host.Window.MouseUp(left, MouseButton.Left);
        Assert.Equal(reverse ? 10 : 20, range.LowerValue);
        Assert.Equal(reverse ? 80 : 90, range.UpperValue);
        host.Window.MouseDown(right, MouseButton.Left); host.Window.MouseUp(right, MouseButton.Left);
        Assert.Equal(10, range.LowerValue);
        Assert.Equal(90, range.UpperValue);
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Physical_RTL_slider_click_handle_and_keyboard_mirror_exactly_once(bool inherited, bool reverse)
    {
        var slider = new MaterialSlider { Width = 360, Height = 100, Step = 10, ReverseDirection = reverse,
            ValueLabelVisibility = SliderValueLabelVisibility.Never };
        using var host = new BrowseHost(slider, 400, 200);
        if (inherited) host.Window.FlowDirection = FlowDirection.RightToLeft;
        else slider.FlowDirection = FlowDirection.RightToLeft;
        host.Render();
        var box = Physical(slider, host.Window);
        var left = new Point(box.Left + 24 + .1 * (box.Width - 48), box.Bottom - 32);
        var right = new Point(box.Right - 24 - .1 * (box.Width - 48), box.Bottom - 32);
        host.Window.MouseDown(left, MouseButton.Left); host.Window.MouseUp(left, MouseButton.Left);
        Assert.Equal(reverse ? 10 : 90, slider.Value);
        host.Window.MouseDown(right, MouseButton.Left); host.Window.MouseUp(right, MouseButton.Left);
        Assert.Equal(reverse ? 90 : 10, slider.Value);
        slider.Value = 70; host.Render();
        var handleX = box.Left + 24 + (reverse ? .7 : .3) * (box.Width - 48);
        Assert.Equal(Color.Parse("#6750A4"), Pixel(host.Window, new(handleX, box.Bottom - 48)));
        slider.Focus(); host.Key(PhysicalKey.ArrowRight);
        Assert.Equal(reverse ? 80 : 60, slider.Value);
        var movedX = box.Left + 24 + (reverse ? .8 : .4) * (box.Width - 48);
        Assert.True(movedX > handleX);
        Assert.Equal(Color.Parse("#6750A4"), Pixel(host.Window, new(movedX, box.Bottom - 48)));
    }

    public static Rect Physical(Visual control, Window window) => new Rect(control.Bounds.Size)
        .TransformToAABB(control.TransformToVisual(window)!.Value);

    public static Color Pixel(Window window, Point physical)
    {
        using var bitmap = window.CaptureRenderedFrame()!;
        using var frame = bitmap.Lock();
        var offset = (int)(physical.Y * window.RenderScaling) * frame.RowBytes + (int)(physical.X * window.RenderScaling) * 4;
        var first = Marshal.ReadByte(frame.Address, offset);
        var green = Marshal.ReadByte(frame.Address, offset + 1);
        var third = Marshal.ReadByte(frame.Address, offset + 2);
        return frame.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third);
    }

    [AvaloniaTheory]
    [InlineData(12, 14, 1)]
    [InlineData(12, 14, 2)]
    [InlineData(10, 14, 1)]
    [InlineData(12, 12, 1)]
    public void Range_calendar_paints_40_dip_band_inside_48_dip_targets_including_endpoints_rows_and_fonts(int start, int end, double scale)
    {
        using var host = new FeedbackHost(800, 800);
        host.Theme.Typography = host.Theme.Typography with { Scale = scale };
        var picker = new MaterialDatePicker { SelectionMode = MaterialDateSelectionMode.Range,
            DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, start), RangeEnd = new(2024, 2, end) };
        picker.Show(host.Overlay); host.Render();
        foreach (var day in picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Where(d => d.IsInRange))
        {
            var box = Physical(day, host.Window);
            Assert.True(box.Height >= 48 && box.Width >= 48);
            Assert.Equal(Color.Parse("#ECE6F0"), Pixel(host.Window, new(box.Left + 12, box.Top + 2)));
            Assert.Equal(Color.Parse("#ECE6F0"), Pixel(host.Window, new(box.Left + 12, box.Bottom - 2)));
            if (day.Date.Day != start && day.Date.Day != end)
            {
                Assert.Equal(Color.Parse("#E8DEF8"), Pixel(host.Window, new(box.Left + 2, box.Center.Y)));
                Assert.Equal(Color.Parse("#ECE6F0"), Pixel(host.Window, new(box.Left + 2, box.Center.Y - 21)));
            }
        }
    }
}
