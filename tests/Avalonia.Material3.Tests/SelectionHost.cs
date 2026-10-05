using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Material3.Tests;

internal sealed class SelectionHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public Window Window { get; }

    public SelectionHost(params Control[] controls)
    {
        Application.Current!.Styles.Add(Theme);
        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 8 };
        foreach (var control in controls)
            panel.Children.Add(control);
        Window = new Window { Width = 400, Height = 700, RequestedThemeVariant = ThemeVariant.Light, Content = panel };
        Window.Show();
        Capture();
    }

    public Point PointIn(Control control, Point local) => control.TranslatePoint(local, Window)!.Value;

    public void Click(Control control, Point? local = null)
    {
        var point = PointIn(control, local ?? new Point(24, 24));
        Window.MouseDown(point, MouseButton.Left);
        Window.MouseUp(point, MouseButton.Left);
    }

    public byte[] Capture()
    {
        using var bitmap = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
        using var stream = new MemoryStream();
        bitmap.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    public Color PixelAt(Control control, Point local)
    {
        var point = PointIn(control, local);
        using var bitmap = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
        using var frame = bitmap.Lock();
        var offset = (int)(point.Y * Window.RenderScaling) * frame.RowBytes + (int)(point.X * Window.RenderScaling) * 4;
        var first = Marshal.ReadByte(frame.Address, offset);
        var green = Marshal.ReadByte(frame.Address, offset + 1);
        var third = Marshal.ReadByte(frame.Address, offset + 2);
        var alpha = Marshal.ReadByte(frame.Address, offset + 3);
        return frame.Format == PixelFormat.Bgra8888
            ? Color.FromArgb(alpha, third, green, first)
            : Color.FromArgb(alpha, first, green, third);
    }

    public void Dispose()
    {
        Window.Close();
        Application.Current!.Styles.Remove(Theme);
    }
}
