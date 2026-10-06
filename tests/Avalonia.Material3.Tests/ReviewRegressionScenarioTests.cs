using System.Windows.Input;
using System.Globalization;
using Gallery;
using Gallery.Pages;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

// Confirmed SPEC/testing seams: themed public host, routed input, public state/peers and physical visuals.
// Automatically shared with fresh package consumption by ScenarioInventory.props.
public class ReviewRegressionScenarioTests
{
    [AvaloniaFact]
    public void Culture_revalidates_retained_native_date_and_preserves_caret_and_undo()
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { Mode = MaterialDatePickerMode.Input, Culture = CultureInfo.GetCultureInfo("en-US") };
        picker.Show(host.Overlay); host.Render();
        picker.StartInput.Focus(); host.Window.KeyTextInput("31/12/2023"); host.Render();
        picker.StartInput.CaretIndex = 10;
        host.Key(PhysicalKey.Backspace); host.Window.KeyTextInput("4"); host.Render();
        Assert.False(picker.IsValid); Assert.Null(picker.SelectedDate);
        Assert.True(picker.StartInput.CanUndo, "Native input did not establish an initial Undo snapshot.");
        picker.StartInput.CaretIndex = 5;
        picker.Culture = CultureInfo.GetCultureInfo("en-GB"); host.Render();
        Assert.Equal("31/12/2024", picker.StartInput.Text);
        Assert.Equal(5, picker.StartInput.CaretIndex);
        Assert.Equal(new DateOnly(2024, 12, 31), picker.SelectedDate); Assert.True(picker.IsValid);
        Assert.True(picker.StartInput.CanUndo);
        picker.StartInput.Focus();
        host.Key(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.Equal("31/12/202", picker.StartInput.Text); Assert.Null(picker.SelectedDate);
        host.Key(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.Equal("31/12/2023", picker.StartInput.Text); Assert.Equal(new DateOnly(2023, 12, 31), picker.SelectedDate);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Material_setting_in_carousel_item_keeps_its_horizontal_drag_without_browsing(bool range)
    {
        var carousel = new MaterialCarousel { Height = 160, ItemsSource = Enumerable.Range(0, 4).Select(i => new MaterialCarouselItem { Title = "Setting " + i }),
            ItemTemplate = new FuncDataTemplate<MaterialCarouselItem>((_, _) => range
                ? new MaterialRangeSlider { LowerValue = 20, UpperValue = 80, ValueLabelVisibility = SliderValueLabelVisibility.Never }
                : new MaterialSlider { Value = 20, ValueLabelVisibility = SliderValueLabelVisibility.Never }) };
        using var host = new BrowseHost(carousel);
        var slider = carousel.GetVisualDescendants().OfType<MaterialSlider>().First();
        var box = ReviewPhysicalScenarioTests.Physical(slider, host.Window);
        var start = new Point(box.Left + 24 + .2 * (box.Width - 48), box.Bottom - 32);
        var end = new Point(box.Left + 24 + .6 * (box.Width - 48), box.Bottom - 32);
        host.Window.MouseDown(start, MouseButton.Left); host.Window.MouseMove(end); host.Window.MouseUp(end, MouseButton.Left);
        Assert.Equal(0, carousel.CurrentIndex);
        Assert.InRange(range ? ((MaterialRangeSlider)slider).LowerValue : slider.Value, 59, 61);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Closing_or_detaching_gallery_cancels_current_page_with_another_window_alive_and_reattach_is_fresh(bool close)
    {
        using var host = new BrowseHost(new Border(), 1000, 800);
        var shell = new GalleryShell(host.Theme);
        var other = new Window { Content = new Border(), Width = 200, Height = 200 };
        other.Show();
        try
        {
            host.Window.Content = shell; host.Render(); shell.Navigate("CarouselRefresh"); host.Render();
            var old = Assert.IsType<CarouselRefreshPage>(shell.CurrentPage);
            Assert.True(old.Refresh.RequestRefresh());
            if (close) host.Window.Close(); else host.Window.Content = null;
            await old.CurrentOperation.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.NotEqual(MaterialProgressStatus.Completed, old.Refresh.Status);
            Assert.DoesNotContain("updated", old.Result.Text!);
            if (!close)
            {
                host.Window.Content = shell; host.Render();
                var fresh = Assert.IsType<CarouselRefreshPage>(shell.CurrentPage);
                Assert.NotSame(old, fresh);
                Assert.True(fresh.Refresh.RequestRefresh());
                await fresh.CurrentOperation.WaitAsync(TimeSpan.FromSeconds(3));
                Assert.Equal(MaterialProgressStatus.Completed, fresh.Refresh.Status);
            }
        }
        finally { other.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Clock_part_activation_retains_required_selection_and_matching_style_and_Toggle(bool hour24)
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { SelectedTime = new(14, 7), Is24Hour = hour24 };
        picker.Show(host.Overlay); host.Render();
        var selectors = picker.GetVisualDescendants().OfType<MaterialTimeSelector>().ToArray();
        var hour = selectors.Single(s => AutomationProperties.GetName(s)!.StartsWith("Hour"));
        var minute = selectors.Single(s => AutomationProperties.GetName(s)!.StartsWith("Minute"));
        foreach (var selector in new[] { hour, minute, minute, hour, hour })
        {
            ControlAutomationPeer.CreatePeerForElement(selector)!.GetProvider<IInvokeProvider>()!.Invoke();
            host.Render();
            Assert.True(selector.IsChecked);
            Assert.Equal(ToggleState.On, ControlAutomationPeer.CreatePeerForElement(selector)!.GetProvider<IToggleProvider>()!.ToggleState);
            Assert.Equal(selector == hour ? MaterialTimePickerPart.Hour : MaterialTimePickerPart.Minute, picker.ActivePart);
            Assert.Equal(Color.Parse("#EADDFF"), ((ISolidColorBrush)selector.Background!).Color);
            Assert.Equal(Color.Parse("#21005D"), ((ISolidColorBrush)selector.Foreground!).Color);
            Assert.False((selector == hour ? minute : hour).IsChecked);
            Assert.Equal(new TimeOnly(14, 7), picker.SelectedTime);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retained_unavailable_date_drafts_reparse_current_grammar_and_confirm_current_meaning(bool range)
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { Mode = MaterialDatePickerMode.Input, Culture = CultureInfo.GetCultureInfo("en-US"),
            InputFormat = "MM/dd/yyyy", SelectionMode = range ? MaterialDateSelectionMode.Range : MaterialDateSelectionMode.Single,
            MinimumDate = new(2024, 1, 1), MaximumDate = new(2024, 2, 29) };
        var session = picker.Show(host.Overlay); host.Render();
        picker.StartInput.Focus(); host.Window.KeyTextInput("03/04/2024"); host.Render();
        if (range) { picker.EndInput.Focus(); host.Window.KeyTextInput("04/04/2024"); host.Render(); }
        picker.StartInput.CaretIndex = 3;
        Assert.False(picker.IsValid);
        picker.InputFormat = "dd/MM/yyyy"; host.Render();
        Assert.Equal("03/04/2024", picker.StartInput.Text);
        Assert.Equal(3, picker.StartInput.CaretIndex);
        Assert.Equal(new DateOnly(2024, 4, 3), picker.SelectedDate);
        if (range) Assert.Equal(new DateOnly(2024, 4, 4), picker.RangeEnd);
        picker.MaximumDate = new(2024, 4, 30); host.Render();
        Assert.True(picker.IsValid);
        Assert.True(picker.Confirm());
        Assert.Equal(range ? new MaterialDateRange(new(2024, 4, 3), new(2024, 4, 4)) : (object)new DateOnly(2024, 4, 3), session.Completion.Result.Value);
    }

    [AvaloniaTheory]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 3)]
    [InlineData(true, false, 3)]
    [InlineData(true, true, 2)]
    public void Submenu_Tab_dismisses_whole_chain_returns_root_focus_without_command(bool rtl, bool shift, int depth)
    {
        using var host = new FeedbackHost();
        host.Overlay.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var command = new AvailabilityCommand { Available = true };
        var menu = new MaterialMenu { Items = { new MaterialMenuItem { Content = "PDF", Command = command } } };
        var menus = new List<MaterialMenu> { menu };
        for (var i = 1; i < depth; i++)
        {
            menu = new MaterialMenu { Items = { new MaterialMenuItem { Content = "Export " + i, Submenu = menu } } };
            menus.Add(menu);
        }
        var root = menu.Show(host.Overlay, host.Entry);
        host.Render();
        for (var i = 1; i < depth; i++) host.Key(rtl ? Key.Left : Key.Right);
        Assert.Equal(depth, host.Overlay.OpenCount);
        var modifiers = shift ? RawInputModifiers.Shift : RawInputModifiers.None;
        host.Window.KeyPressQwerty(PhysicalKey.Tab, modifiers);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, modifiers);
        host.Render();
        Assert.Equal(0, host.Overlay.OpenCount);
        Assert.All(menus, m => Assert.False(m.IsOpen));
        Assert.True(host.Entry.IsFocused);
        Assert.Equal(0, command.Executions);
        Assert.Null(root.Completion.Result.Value);
    }

    [AvaloniaTheory]
    [InlineData(MaterialTabVariant.Primary, 284)]
    [InlineData(MaterialTabVariant.Secondary, 204)]
    public void Scrollable_tabs_have_natural_extent_without_blank_tail_after_resize(MaterialTabVariant variant, double intrinsic)
    {
        var tabs = new MaterialTabs { Variant = variant, Layout = MaterialTabLayout.Scrollable,
            Items = { new MaterialNavigationItem { Content = "A" }, new MaterialNavigationItem { Content = "B" } } };
        using var host = new BrowseHost(tabs);
        var scroll = tabs.GetVisualDescendants().OfType<ScrollViewer>().Single();
        foreach (var width in new[] { 400, 320, 1000, 400 })
        {
            host.Window.Width = width;
            host.Render();
            Assert.Equal(180, tabs.ItemsPanelRoot!.DesiredSize.Width);
            Assert.InRange(scroll.Extent.Width, intrinsic, width);
            scroll.Offset = new Vector(double.MaxValue, 0);
            host.Render();
            Assert.Equal(0, scroll.Offset.X);
        }
    }

    [AvaloniaFact]
    public async Task List_row_native_worker_discovers_dynamic_expansion_and_reads_state_without_UI_affinity()
    {
        var row = new MaterialListItem { Title = "Details", IsExpandable = false };
        using var host = new BrowseHost(row);
        var peer = ControlAutomationPeer.CreatePeerForElement(row)!;
        Assert.Null(await Task.Run(() => peer.GetProvider<IExpandCollapseProvider>()));
        row.IsExpandable = true;
        var provider = await Task.Run(() => peer.GetProvider<IExpandCollapseProvider>());
        Assert.NotNull(provider);
        Assert.Equal(ExpandCollapseState.Collapsed, await Task.Run(() => provider.ExpandCollapseState));
        var changes = new List<AutomationPropertyChangedEventArgs>();
        peer.PropertyChanged += (_, e) => changes.Add(e);
        provider.Expand();
        Assert.True(row.IsExpanded);
        Assert.Equal(ExpandCollapseState.Expanded, await Task.Run(() => provider.ExpandCollapseState));
        Assert.Contains(changes, e => e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty && Equals(e.NewValue, ExpandCollapseState.Expanded));
        row.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(provider.Collapse);
        Assert.True(row.IsExpanded);
        row.IsExpandable = false;
        Assert.Null(await Task.Run(() => peer.GetProvider<IExpandCollapseProvider>()));
    }

    [AvaloniaTheory]
    [InlineData("direct")]
    [InlineData("command")]
    [InlineData("parent")]
    public void Required_single_group_establishes_available_choice_without_activating_it(string availability)
    {
        var command = new AvailabilityCommand { Available = availability != "command" };
        var choice = new MaterialGroupButton { Content = "Choice", Command = command, IsEnabled = false };
        var group = new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Single, IsEnabled = availability != "parent" };
        group.Children.Add(choice);
        using var host = new BrowseHost(group);
        if (availability != "direct") choice.IsEnabled = true;
        host.Render();
        Assert.Null(group.SelectedItem);
        var changes = 0;
        var clicks = 0;
        group.SelectionChanged += (_, _) => changes++;
        choice.Click += (_, _) => clicks++;
        var selection = ControlAutomationPeer.CreatePeerForElement(group)!.GetProvider<ISelectionProvider>()!;
        if (availability == "direct") choice.IsEnabled = true;
        if (availability == "command") command.SetAvailable(true);
        if (availability == "parent") group.IsEnabled = true;
        host.Render();
        Assert.Same(choice, group.SelectedItem);
        Assert.Single(group.SelectedItems);
        Assert.Single(selection.GetSelection());
        Assert.Equal(1, changes);
        Assert.Equal(0, clicks);
        Assert.Equal(0, command.Executions);
        choice.IsEnabled = false;
        Assert.Same(choice, group.SelectedItem);
        Assert.Equal(1, changes);
    }

    private sealed class AvailabilityCommand : ICommand
    {
        public bool Available { get; set; }
        public int Executions { get; private set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Available;
        public void Execute(object? parameter) => Executions++;
        public void SetAvailable(bool value) { Available = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nested_material_slider_owns_vertical_setting_drag_without_refresh(bool range)
    {
        MaterialSlider slider = range
            ? new MaterialRangeSlider { LowerValue = 30, UpperValue = 80 }
            : new MaterialSlider { Value = 80 };
        slider.Orientation = Orientation.Vertical;
        slider.Height = 300;
        var refresh = new MaterialPullToRefresh { Content = slider };
        using var host = new BrowseHost(refresh, 400, 320);
        var requests = 0;
        refresh.RefreshRequested += (_, _) => requests++;
        var start = host.At(slider, new Point(slider.Bounds.Width - 32, 60));
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start + new Vector(0, 180));
        host.Window.MouseUp(start + new Vector(0, 180), MouseButton.Left);
        Assert.Equal(0, requests);
        Assert.Equal(0, refresh.DistanceFraction);
        Assert.False(refresh.IsPulling);
        Assert.Equal(MaterialProgressStatus.Idle, refresh.Status);
        Assert.True(range ? ((MaterialRangeSlider)slider).UpperValue < 50 : slider.Value < 50);
    }
}
