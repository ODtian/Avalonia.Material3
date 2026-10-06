using System.Windows.Input;
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
