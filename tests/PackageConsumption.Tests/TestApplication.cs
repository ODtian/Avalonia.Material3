using Avalonia;
using Avalonia.Headless;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;

[assembly: AvaloniaTestApplication(typeof(PackageConsumption.Tests.TestApplication))]

namespace PackageConsumption.Tests;

public class TestApplication : Application
{
    public override void Initialize() => Styles.Add(new MaterialTheme { Motion = new MaterialMotion { ReduceMotion = true } });

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
