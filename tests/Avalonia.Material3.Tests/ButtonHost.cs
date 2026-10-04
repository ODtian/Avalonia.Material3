using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;

namespace Avalonia.Material3.Tests;

internal sealed class ButtonHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new Tokens.MaterialMotion { ReduceMotion = true } };
    public MaterialButton Button { get; } = new() { Content = "Continue" };
    public TextBlock Result { get; } = new() { Text = "Waiting" };
    public Window Window { get; } = new() { Width = 320, Height = 240, RequestedThemeVariant = ThemeVariant.Light };

    public ButtonHost()
    {
        Window.Styles.Add(Theme);
        Window.Content = new StackPanel { Margin = new Thickness(24), Children = { Button, Result } };
        Button.Click += (_, _) => Result.Text = "Action completed";
        Window.Show();
    }

    public Point Center => Button.TranslatePoint(new Point(Button.Bounds.Width / 2, Button.Bounds.Height / 2), Window)!.Value;

    public byte[] Capture()
    {
        using var bitmap = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
        using var stream = new MemoryStream();
        bitmap.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    public void Dispose() => Window.Close();
}
