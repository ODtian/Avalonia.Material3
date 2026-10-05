using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SelectionFormScenarioTests
{
    [AvaloniaFact]
    public void Gallery_exhibits_each_standard_value_disabled_and_error_combination()
    {
        var page = new SelectionFormPage();
        using var host = new SelectionHost(page);
        foreach (var type in new[] { typeof(MaterialCheckBox), typeof(MaterialRadioButton), typeof(MaterialSwitch) })
        {
            var examples = page.StateExamples.Where(control => control.GetType() == type).ToArray();
            Assert.Contains(examples, control => control.IsEnabled && control.IsChecked == false);
            Assert.Contains(examples, control => control.IsEnabled && control.IsChecked == true);
            Assert.Contains(examples, control => !control.IsEnabled && control.IsChecked == false);
            Assert.Contains(examples, control => !control.IsEnabled && control.IsChecked == true);
            Assert.Contains(examples, control => IsError(control) && control.IsChecked == false);
            Assert.Contains(examples, control => IsError(control) && control.IsChecked == true);
        }
        Assert.Contains(page.StateExamples.OfType<MaterialCheckBox>(), control => control.IsChecked is null && control.IsThreeState);
        Assert.Contains(page.StateExamples.OfType<MaterialCheckBox>(), control => !control.IsEnabled && control.IsChecked is null);
        Assert.Contains(page.StateExamples.OfType<MaterialCheckBox>(), control => control.IsError && control.IsChecked is null);
        Assert.Equal(3, page.LongLabelExamples.Count);
        Assert.All(page.StateExamples, control => Assert.True(control.Bounds.Height >= 48));
    }

    [AvaloniaFact]
    public void Gallery_theme_and_font_actions_update_the_existing_form_without_resetting_values()
    {
        var page = new SelectionFormPage();
        using var host = new SelectionHost(page);
        host.Window.Height = 1200;
        host.Capture();
        host.Click(page.ThemeButton);
        host.Capture();
        Assert.Equal(ThemeVariant.Dark, host.Window.ActualThemeVariant);
        Assert.Equal(Color.Parse("#D0BCFF"), Assert.IsAssignableFrom<ISolidColorBrush>(page.Notifications.BorderBrush).Color);
        Assert.Equal(Color.Parse("#36343B"), Assert.IsAssignableFrom<ISolidColorBrush>(page.AutoSave.Background).Color);
        host.Click(page.FontScaleButton);
        host.Capture();
        Assert.Equal(32, page.Notifications.FontSize);
        Assert.Equal(32, page.Email.FontSize);
        Assert.Equal(32, page.AutoSave.FontSize);
        Assert.Null(page.Notifications.IsChecked);
        Assert.True(page.Email.IsChecked);
        Assert.False(page.AutoSave.IsChecked);
        host.Click(page.ThemeButton);
        host.Capture();
        Assert.Equal(ThemeVariant.Light, host.Window.ActualThemeVariant);
        Assert.Equal(Color.Parse("#6750A4"), Assert.IsAssignableFrom<ISolidColorBrush>(page.Notifications.BorderBrush).Color);
        host.Click(page.FontScaleButton);
        host.Capture();
        Assert.Equal(16, page.Notifications.FontSize);
    }

    [AvaloniaFact]
    public void Automation_selection_updates_form_data_and_bindings_survive_a_later_restore()
    {
        var page = new SelectionFormPage();
        using var host = new SelectionHost(page);
        var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(ControlAutomationPeer.CreatePeerForElement(page.Post));
        selection.Select();
        Assert.Equal("Post", page.Form.Delivery);
        page.Restore("{\"Notifications\":true,\"Delivery\":\"Email\",\"AutoSave\":true}");
        host.Capture();
        Assert.False(page.Post.IsChecked);
        Assert.True(page.Email.IsChecked);
        selection.Select();
        Assert.Equal("Post", page.Form.Delivery);
        Assert.False(page.Email.IsChecked);
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(ControlAutomationPeer.CreatePeerForElement(page.AutoSave));
        toggle.Toggle();
        Assert.False(page.Form.AutoSave);
        page.Form.AutoSave = true;
        Assert.True(page.AutoSave.IsChecked);
    }

    private static bool IsError(ToggleButton control) => control switch
    {
        MaterialCheckBox checkbox => checkbox.IsError,
        MaterialRadioButton radio => radio.IsError,
        MaterialSwitch toggle => toggle.IsError,
        _ => false
    };

    [AvaloniaFact]
    public void User_saves_and_restores_mixed_checkbox_radio_value_and_switch_through_the_gallery_form()
    {
        var page = new SelectionFormPage();
        using var host = new SelectionHost(page);
        Assert.Null(page.Notifications.IsChecked);
        host.Click(page.Post);
        host.Click(page.AutoSave, new Point(54, 24));
        host.Click(page.SaveButton);
        Assert.Equal("{\"Notifications\":null,\"Delivery\":\"Post\",\"AutoSave\":true}", page.SavedJson);
        host.Click(page.ResetButton);
        host.Capture();
        Assert.False(page.Notifications.IsChecked);
        Assert.True(page.Email.IsChecked);
        Assert.False(page.AutoSave.IsChecked);
        host.Click(page.RestoreButton);
        host.Capture();
        Assert.Null(page.Notifications.IsChecked);
        Assert.True(page.Post.IsChecked);
        Assert.False(page.Email.IsChecked);
        Assert.True(page.AutoSave.IsChecked);
        Assert.Equal(new SelectionFormData(null, "Post", true), page.Form.Snapshot);
        host.Click(page.Notifications);
        Assert.False(page.Form.Notifications);
        host.Click(page.Notifications);
        Assert.True(page.Form.Notifications);
    }
}
