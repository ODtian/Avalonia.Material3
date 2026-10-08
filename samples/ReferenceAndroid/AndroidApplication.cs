using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Material3.ReferenceUi;

namespace Material3.ReferenceAndroid;

[Application]
public sealed class AndroidApplication(nint reference, JniHandleOwnership ownership) : AvaloniaAndroidApplication<ReferenceApp>(reference, ownership)
{
    public override void OnCreate()
    {
        // The locked Android15 HWUI font engine uses TrueType interpreter35.
        Android.Systems.Os.Setenv("FREETYPE_PROPERTIES", "truetype:interpreter-version=35", true);
        Android.Util.Log.Info("M3AvaloniaReference", "ft-profile=" + Android.Systems.Os.Getenv("FREETYPE_PROPERTIES"));
        base.OnCreate();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) => base.CustomizeAppBuilder(builder)
        .With(new SkiaOptions { UseStencilBuffers = true });
}
