using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class TextFieldsGalleryScenarioTests
{
    [AvaloniaFact]
    public void Packaged_gallery_page_accepts_real_editing_and_recovers_from_form_validation()
    {
        var theme = new MaterialTheme();
        Application.Current!.Styles.Add(theme);
        var page = new TextFieldsPage(theme);
        var window = new Window { Width = 480, Height = 1800, Content = page, RequestedThemeVariant = ThemeVariant.Light };
        try
        {
            window.Show();
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            var fields = page.GetVisualDescendants().OfType<MaterialTextField>().ToArray();
            Assert.Equal(11, fields.Length);
            var name = fields.Single(field => AutomationProperties.GetAutomationId(field) == "TextField.DisplayName");
            var email = fields.Single(field => AutomationProperties.GetAutomationId(field) == "TextField.Email");
            var validate = page.GetVisualDescendants().OfType<MaterialButton>()
                .Single(button => AutomationProperties.GetAutomationId(button) == "TextField.Validate");
            validate.Focus();
            Press(window, PhysicalKey.Enter);
            Assert.True(name.IsFocused);
            Assert.True(name.HasError && email.HasError);
            window.KeyTextInput("中文 Atlas");
            Press(window, PhysicalKey.Tab);
            Press(window, PhysicalKey.Tab); // Clear action is reachable between editable fields.
            Assert.True(email.IsFocused);
            window.KeyTextInput("atlas");
            Assert.True(email.HasError);
            window.KeyTextInput("@example.test");
            Assert.False(email.HasError);
            validate.Focus();
            Press(window, PhysicalKey.Enter);
            using var resultFrame = window.CaptureRenderedFrame();
            var result = page.GetVisualDescendants().OfType<TextBlock>()
                .Single(text => AutomationProperties.GetAutomationId(text) == "TextField.Result");
            Assert.Equal("Form accepted", result.Text);
            Assert.Equal("中文 Atlas", name.Text);
            Assert.Equal("atlas@example.test", email.Text);
            var evidence = Environment.GetEnvironmentVariable("M3_TEXT_FIELD_EVIDENCE");
            if (!string.IsNullOrEmpty(evidence))
            {
                Directory.CreateDirectory(evidence);
                resultFrame!.Save(Path.Combine(evidence, "gallery-text-fields-headless-light.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                window.RequestedThemeVariant = ThemeVariant.Dark;
                using var darkFrame = window.CaptureRenderedFrame();
                darkFrame!.Save(Path.Combine(evidence, "gallery-text-fields-headless-dark.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            }
        }
        finally
        {
            window.Close();
            Application.Current!.Styles.Remove(theme);
        }
    }

    private static void Press(Window window, PhysicalKey key)
    {
        window.KeyPressQwerty(key, RawInputModifiers.None);
        window.KeyReleaseQwerty(key, RawInputModifiers.None);
    }
}
