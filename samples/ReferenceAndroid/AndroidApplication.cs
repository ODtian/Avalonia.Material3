using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Material3.ReferenceUi;

namespace Material3.ReferenceAndroid;

[Application]
public sealed class AndroidApplication(nint reference, JniHandleOwnership ownership) : AvaloniaAndroidApplication<ReferenceApp>(reference, ownership)
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) => base.CustomizeAppBuilder(builder)
        .With(new SkiaOptions { UseStencilBuffers = true });
}
