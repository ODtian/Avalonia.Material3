using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Gallery.Pages;
using Xunit;

namespace PackageConsumption.Tests;

public class SliderGalleryScenarioTests
{
    [AvaloniaFact]
    public void Narrow_gallery_with_large_fonts_keeps_the_reopen_action_inside_the_window()
    {
        var path = Path.Combine(Path.GetTempPath(), $"m3-sliders-{Guid.NewGuid():N}.json");
        var theme = new MaterialTheme { Typography = new MaterialTypography { Scale = 1.5 } };
        Application.Current!.Styles.Add(theme);
        var page = new SliderSettingsPage(path);
        var window = new Window { Width = 320, Height = 1800, Content = page };
        try
        {
            window.Show();
            using (window.CaptureRenderedFrame()) { }
            var end = page.ReopenButton.TranslatePoint(new Point(page.ReopenButton.Bounds.Width, page.ReopenButton.Bounds.Height), window)!.Value;
            Assert.True(end.X <= 320, $"Reopen action extends to {end.X}");
            Assert.True(end.Y <= 1800);
            Assert.True(page.Brightness.Bounds.Height >= 48);
            Assert.True(page.Hours.Bounds.Width >= 48);
        }
        finally { window.Close(); Application.Current!.Styles.Remove(theme); }
    }

    [AvaloniaFact]
    public void User_adjusts_live_preview_saves_and_reopens_the_public_package_gallery_page()
    {
        var path = Path.Combine(Path.GetTempPath(), $"m3-sliders-{Guid.NewGuid():N}.json");
        var theme = new MaterialTheme();
        Application.Current!.Styles.Add(theme);
        var window = new Window { Width = 500, Height = 1500 };
        try
        {
            var page = new SliderSettingsPage(path);
            window.Content = page;
            window.Show();
            using (window.CaptureRenderedFrame()) { }
            page.Brightness.Focus();
            window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.End, RawInputModifiers.None);
            Assert.Contains("Brightness 100", page.Preview.Text);
            page.Volume.Focus();
            window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
            Assert.Contains("Volume 0", page.Preview.Text);
            page.Hours.LowerValue = 9;
            page.Hours.UpperValue = 17;
            Assert.Contains("9–17", page.Preview.Text);
            Assert.Equal("Brightness", AutomationProperties.GetName(page.Brightness));
            page.SaveButton.Focus();
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal("Saved settings", page.Status.Text);
            page.Brightness.Value = 15;
            page.ReopenButton.Focus();
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(100, page.Brightness.Value);
            Assert.Equal("Restored saved settings", page.Status.Text);
            var reopened = new SliderSettingsPage(path);
            window.Content = reopened;
            using (window.CaptureRenderedFrame()) { }
            Assert.Equal(100, reopened.Brightness.Value);
            Assert.Equal(0, reopened.Volume.Value);
            Assert.Equal(9, reopened.Hours.LowerValue);
            Assert.Equal(17, reopened.Hours.UpperValue);
            Assert.Equal(page.Preview.Text, reopened.Preview.Text);
        }
        finally
        {
            window.Close();
            Application.Current!.Styles.Remove(theme);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
