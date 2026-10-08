using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.VisualTree;
using Material3.ReferenceUi;
using Xunit;
using Avalonia.Media;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace Avalonia.Material3.Tests;

public class ReferenceParityScenarioTests
{
    [AvaloniaFact]
    public void Reference_Roboto_assets_match_400_500_and_700_without_font_simulation()
    {
        var family = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto");
        foreach (var weight in new[] { FontWeight.Normal, FontWeight.Medium, FontWeight.Bold })
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, weight), out var face));
            Assert.Equal(weight, face.Weight); Assert.Equal(FontSimulations.None, face.FontSimulations);
            Assert.Equal("Roboto", face.FamilyName);
        }
    }

    [AvaloniaFact]
    public void Reference_navigation_detaches_the_departing_scene_before_releasing_its_tooltip_description()
    {
        using var host = new ReferenceHost("overlays");
        var oldScene = host.Shell.CurrentScene!;
        var anchor = host.Find<MaterialIconButton>("tooltip");
        Assert.Equal("Save draft", AutomationProperties.GetHelpText(anchor));
        string? descriptionAtDetach = null;
        oldScene.PropertyChanged += (_, change) =>
        {
            if (change.Property == StyledElement.ParentProperty && change.NewValue is null)
                descriptionAtDetach = AutomationProperties.GetHelpText(anchor);
        };
        Assert.True(host.Shell.Navigate("home")); host.Render();
        Assert.Equal("Save draft", descriptionAtDetach);
        Assert.Null(oldScene.Parent); Assert.Null(AutomationProperties.GetHelpText(anchor));
    }

    [AvaloniaFact]
    public void Reference_sliders_use_the_native_48_dip_no_value_label_recipe()
    {
        using var host = new ReferenceHost("slider");
        foreach (var id in new[] { "slider", "slider-discrete", "slider-range" })
        {
            var slider = host.Find<MaterialSlider>(id);
            Assert.Equal(48, slider.Bounds.Height);
            Assert.Equal(SliderValueLabelVisibility.Never, slider.ValueLabelVisibility);
        }
        var continuous = host.Find<MaterialSlider>("slider");
        var center = continuous.TranslatePoint(new Point(40, 24), host.Window)!.Value;
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var offset = (int)(center.Y * host.Window.RenderScaling) * pixels.RowBytes + (int)(center.X * host.Window.RenderScaling) * 4;
        var first = Marshal.ReadByte(pixels.Address, offset); var green = Marshal.ReadByte(pixels.Address, offset + 1); var third = Marshal.ReadByte(pixels.Address, offset + 2);
        Assert.Equal(Color.Parse("#6750A4"), pixels.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third));
    }

    [AvaloniaFact]
    public void Reference_topics_then_activity_tab_returns_both_navigation_controls_to_activity()
    {
        using var host = new ReferenceHost("navigation");
        host.Click("navigation-3");
        Assert.Equal(3, host.Find<MaterialNavigationBar>("navigation").SelectedIndex);
        var tabs = host.Find<MaterialTabs>("tabs");
        var activity = (MaterialNavigationItem)tabs.Items[2]!;
        var point = activity.TranslatePoint(new Point(activity.Bounds.Width / 2, activity.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left); host.Render();
        Assert.Equal(2, host.Find<MaterialNavigationBar>("navigation").SelectedIndex);
        Assert.Equal(2, tabs.SelectedIndex);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reference_range_dialog_OK_closes_incomplete_or_invalid_shared_drafts(bool invalidInput)
    {
        using var host = new ReferenceHost("date-range-9-16"); host.Click("open-date-dialog");
        var picker = host.Find<MaterialDatePicker>("date-range-dialog-picker");
        if (invalidInput)
        {
            picker.Mode = MaterialDatePickerMode.Input; host.Render();
            picker.StartInput.Focus(); picker.StartInput.SelectAll(); host.Window.KeyTextInput("02312024"); host.Render();
        }
        else { picker.SelectedDate = new DateOnly(2024, 2, 10); picker.RangeEnd = null; host.Render(); }
        Assert.False(picker.IsValid);
        var confirm = host.Shell.Overlay.GetVisualDescendants().OfType<MaterialButton>().Single(button =>
            button.IsEffectivelyVisible && Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(button)!.GetName() == "OK");
        Assert.True(confirm.IsEffectivelyEnabled);
        var point = confirm.TranslatePoint(new Point(confirm.Bounds.Width / 2, confirm.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left); host.Render();
        Assert.Equal(0, host.Shell.Overlay.OpenCount);
        var inline = host.Find<MaterialDatePicker>("date-range-picker");
        if (!invalidInput) Assert.Null(inline.RangeEnd);
        else { Assert.Null(inline.SelectedDate); Assert.Null(inline.RangeEnd); Assert.Equal(MaterialDatePickerMode.Input, inline.Mode); }
    }

    [AvaloniaFact]
    public void Reference_scene_manifest_matches_the_fifteen_native_routes_and_home_navigation()
    {
        using var host = new ReferenceHost();
        Assert.Equal(new[] { "date-range", "date-range-7-24", "date-range-9-16", "date-single", "time", "selection", "slider", "fields", "buttons", "ripple", "fab", "progress", "carousel", "navigation", "overlays" }, ReferenceShell.SceneIds);
        foreach (var scene in ReferenceShell.SceneIds)
        {
            Assert.True(host.Shell.Navigate(scene)); host.Render();
            Assert.Equal(scene, host.Shell.Scene);
            Assert.Equal("M3 · " + scene, host.Shell.TopBar.Title);
        }
        host.Shell.Navigate("home"); host.Render(); host.Click("scene-selection");
        Assert.Equal("selection", host.Shell.Scene); host.Click("home"); Assert.Equal("home", host.Shell.Scene);
    }

    [AvaloniaFact]
    public void Reference_selection_preserves_native_initial_state_and_shared_switch_value_through_input()
    {
        using var host = new ReferenceHost("selection");
        Assert.False(host.Find<MaterialCheckBox>("checkbox").IsChecked);
        Assert.Null(host.Find<MaterialCheckBox>("checkbox-mixed").IsChecked);
        Assert.True(host.Find<MaterialRadioButton>("radio-0").IsChecked);
        Assert.True(host.Find<MaterialSwitch>("switch").IsChecked);
        host.Click("checkbox-mixed"); Assert.True(host.Find<MaterialCheckBox>("checkbox-mixed").IsChecked);
        host.Click("radio-1"); Assert.True(host.Find<MaterialRadioButton>("radio-1").IsChecked);
        Assert.False(host.Find<MaterialRadioButton>("radio-0").IsChecked);
        host.Click("switch"); Assert.False(host.Find<MaterialSwitch>("switch-icons").IsChecked);
    }

    [AvaloniaFact]
    public void Reference_fields_share_one_native_value_and_ripple_actions_increment_the_host_counter()
    {
        using var host = new ReferenceHost("fields");
        var outlined = host.Find<MaterialTextField>("field-outlined");
        outlined.Text = "Draft"; host.Render();
        Assert.Equal("Draft", host.Find<MaterialTextField>("field-amount").Text);
        Assert.Equal("Draft", host.Find<MaterialTextField>("field-filled").Text);
        host.Shell.Navigate("ripple"); host.Render(); host.Click("ripple-filled");
        Assert.Equal("Pressed 1", host.Find<TextBlock>("ripple-count").Text);
    }

    [AvaloniaFact]
    public void Reference_date_dialog_edits_the_same_native_state_and_cancel_retains_the_edit()
    {
        using var host = new ReferenceHost("date-range-9-16");
        var inline = host.Find<MaterialDatePicker>("date-range-picker");
        Assert.Equal(new DateOnly(2024, 2, 9), inline.SelectedDate); Assert.Equal(new DateOnly(2024, 2, 16), inline.RangeEnd);
        host.Click("open-date-dialog");
        var dialog = host.Find<MaterialDatePicker>("date-range-dialog-picker");
        dialog.SelectedDate = new DateOnly(2024, 2, 10); dialog.RangeEnd = new DateOnly(2024, 2, 12); host.Render();
        Assert.Equal(dialog.SelectedDate, inline.SelectedDate); Assert.Equal(dialog.RangeEnd, inline.RangeEnd);
        Assert.True(host.Shell.Overlay.RequestBack()); host.Render();
        Assert.Equal(new DateOnly(2024, 2, 10), inline.SelectedDate); Assert.Equal(new DateOnly(2024, 2, 12), inline.RangeEnd);
    }

    [AvaloniaFact]
    public void Reference_default_launch_configuration_matches_the_manual_native_comparison()
    {
        var defaults = new ReferenceConfiguration();
        Assert.Equal("home", defaults.Scene); Assert.Equal("classic", defaults.Palette); Assert.Equal("en-US", defaults.Locale);
        Assert.True(defaults.CheckboxM3); Assert.True(defaults.ExpressiveButtons); Assert.False(defaults.Dark);
        var custom = ReferenceConfiguration.FromArguments(["--scene=selection", "--dark=true", "--palette=expressive", "--locale=zh-CN", "--checkboxM3=false", "--expressiveButtons=false"]);
        Assert.Equal("selection", custom.Scene); Assert.True(custom.Dark); Assert.Equal("expressive", custom.Palette);
        Assert.Equal("zh-CN", custom.Locale); Assert.False(custom.CheckboxM3); Assert.False(custom.ExpressiveButtons);
        Assert.Equal(2, custom.UnsupportedVariants.Count); Assert.Empty(defaults.UnsupportedVariants);
        Assert.Throws<ArgumentException>(custom.Validate); defaults.Validate();
    }

    private sealed class ReferenceHost : IDisposable
    {
        private readonly MaterialTheme _theme = new();
        public ReferenceShell Shell { get; }
        public Window Window { get; }
        public ReferenceHost(string scene = "home")
        {
            _theme.Motion = _theme.Motion with { ReduceMotion = true };
            Application.Current!.Styles.Add(_theme);
            Shell = new ReferenceShell(_theme, new ReferenceConfiguration { Scene = scene, Palette = "classic", Locale = "en-US", CheckboxM3 = true });
            Window = new Window { Width = 354.285714, Height = 744, Content = Shell }; Window.Show(); Render();
        }
        public T Find<T>(string id) where T : Control => Shell.GetVisualDescendants().OfType<T>().Single(control => AutomationProperties.GetAutomationId(control) == id);
        public void Render() { Window.UpdateLayout(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); Window.CaptureRenderedFrame()?.Dispose(); }
        public void Click(string id)
        {
            var control = Find<Control>(id); control.BringIntoView(); Render();
            var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
            Window.MouseDown(point, MouseButton.Left); Window.MouseUp(point, MouseButton.Left); Render();
        }
        public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(_theme); }
    }
}
