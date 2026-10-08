using Avalonia;

namespace Gallery;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new SkiaOptions { UseStencilBuffers = true })
        .LogToTrace()
        .StartWithClassicDesktopLifetime(args);
}
