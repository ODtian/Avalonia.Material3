using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avalonia.Material3.Tests.TestApplication))]

namespace Avalonia.Material3.Tests;

public class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
