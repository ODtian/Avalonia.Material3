using System.Windows.Input;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Tokens;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ContractScenarioTests
{
    [AvaloniaFact]
    public void Host_content_template_and_command_work_without_replacing_input_behavior()
    {
        using var host = new ButtonHost();
        var customLabel = new TextBlock { Text = "提交 / Continue" };
        host.Button.ContentTemplate = new FuncDataTemplate<string>((_, _) => customLabel);
        host.Button.CommandParameter = "confirmed";
        host.Button.Command = new HostCommand(parameter => host.Result.Text = (string)parameter!);
        host.Capture();
        Assert.True(customLabel.IsEffectivelyVisible);
        Assert.True(customLabel.Bounds.Width > 0);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal("confirmed", host.Result.Text);
    }

    [AvaloniaFact]
    public void Touch_user_can_activate_the_invisible_part_of_the_48_unit_target()
    {
        using var host = new ButtonHost();
        var point = host.Button.TranslatePoint(new Point(host.Button.Bounds.Width / 2, 1), host.Window)!.Value;
        using var contact = host.Window.TouchBegin(point);
        host.Window.TouchEnd(contact, point);
        Assert.Equal("Action completed", host.Result.Text);
    }

    [AvaloniaFact]
    public void Releasing_pointer_outside_the_button_cancels_the_action()
    {
        using var host = new ButtonHost();
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseMove(new Point(300, 220), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(new Point(300, 220), MouseButton.Left);
        Assert.False(host.Button.IsPressed);
        Assert.Equal("Waiting", host.Result.Text);
    }

    [AvaloniaFact]
    public void Automation_consumer_observes_button_name_role_and_disabled_state()
    {
        using var host = new ButtonHost();
        AutomationProperties.SetName(host.Button, "Commit changes");
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Button)!;
        Assert.Equal("Commit changes", peer.GetName());
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.True(peer.IsEnabled());
        host.Button.IsEnabled = false;
        Assert.False(peer.IsEnabled());
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.MaxValue)]
    public void Invalid_font_scale_cannot_replace_the_host_theme(double scale)
    {
        using var host = new ButtonHost();
        Assert.Throws<ArgumentException>(() => host.Theme.Typography = new MaterialTypography { Scale = scale });
        Assert.Equal(14, host.Button.FontSize);
    }

    private sealed class HostCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
