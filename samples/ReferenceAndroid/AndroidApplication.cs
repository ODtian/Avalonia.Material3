using Android.App;
using Android.Runtime;
using Avalonia.Android;
using Material3.ReferenceUi;

namespace Material3.ReferenceAndroid;

[Application]
public sealed class AndroidApplication(nint reference, JniHandleOwnership ownership) : AvaloniaAndroidApplication<ReferenceApp>(reference, ownership);
