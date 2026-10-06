extern alias standalone;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace PackageConsumption.Tests;

public class SampleScenarioTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Package_consumer_starts_runs_action_and_switches_theme_with_keyboard(bool gallery)
    {
        Window window = gallery ? new Gallery.MainWindow() : new standalone::StandaloneHost.MainWindow();
        window.Show();
        try
        {
            var action = window.FindControl<MaterialButton>("ActionButton")!;
            var theme = window.FindControl<MaterialButton>("ThemeButton")!;
            var result = window.FindControl<TextBlock>("ResultText")!;
            var center = action.TranslatePoint(new Point(action.Bounds.Width / 2, action.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
            Assert.Equal("Action completed (1)", result.Text);
            Assert.Equal(Color.Parse("#6750A4"), Assert.IsAssignableFrom<ISolidColorBrush>(action.Background).Color);
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            Assert.True(theme.IsFocused);
            window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
            Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
            Assert.Equal(Color.Parse("#D0BCFF"), Assert.IsAssignableFrom<ISolidColorBrush>(action.Background).Color);
            Assert.Equal("Use light theme", theme.Content);
            Assert.Equal("Action completed (1)", result.Text);
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
        }
        finally
        {
            window.Close();
        }
    }
}
