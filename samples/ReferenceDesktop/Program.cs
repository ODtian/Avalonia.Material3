using Avalonia;
using Material3.ReferenceUi;

namespace Material3.ReferenceDesktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ReferenceApp.Configuration = ReferenceConfiguration.FromArguments(args);
        ReferenceApp.Configuration.Validate();
        AppBuilder.Configure<ReferenceApp>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
    }
}
