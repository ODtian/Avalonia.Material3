using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Material3.Tests;

internal sealed class ButtonHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new Tokens.MaterialMotion { ReduceMotion = true } };
    public MaterialButton Button { get; } = new() { Content = "Continue" };
    public TextBlock Result { get; } = new() { Text = "Waiting" };
    public Window Window { get; }

    public ButtonHost()
    {
        Application.Current!.Styles.Add(Theme);
        Window = new Window { Width = 320, Height = 240, RequestedThemeVariant = ThemeVariant.Light };
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

    public Color PixelAt(Point point)
    {
        using var bitmap = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
        using var frame = bitmap.Lock();
        var offset = (int)(point.Y * Window.RenderScaling) * frame.RowBytes + (int)(point.X * Window.RenderScaling) * 4;
        var first = Marshal.ReadByte(frame.Address, offset);
        var green = Marshal.ReadByte(frame.Address, offset + 1);
        var third = Marshal.ReadByte(frame.Address, offset + 2);
        var alpha = Marshal.ReadByte(frame.Address, offset + 3);
        if (frame.Format == PixelFormat.Bgra8888)
            return Color.FromArgb(alpha, third, green, first);
        if (frame.Format == PixelFormat.Rgba8888)
            return Color.FromArgb(alpha, first, green, third);
        throw new InvalidOperationException($"Unexpected frame format: {frame.Format}");
    }

    public void Dispose()
    {
        Window.Close();
        Application.Current!.Styles.Remove(Theme);
    }
}
