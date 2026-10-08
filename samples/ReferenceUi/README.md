# Paired Material reference apps

`ReferenceUi` is shared by `ReferenceDesktop` and `ReferenceAndroid`. Both consume the exact `$(Material3Version)` NuGet package. The scene IDs, labels, initial state, callbacks, host padding, spacing and actions port the pinned official `AndroidReference` sources. The actual library components render every standard control.

Default comparison: `home`, light, `palette=classic`, `locale=en-US`, `checkboxM3=true`, `expressiveButtons=true`. The native Android reference uses these same manual defaults. [scene-parity.json](scene-parity.json) records all 15 scenes, actions, stable IDs and visible component differences. Legacy checkbox paint and stable button overloads remain native research branches; the Avalonia entry validates and rejects those unsupported alternatives with an explicit error.

Build the versioned library package into `artifacts/packages` first, then:

```powershell
dotnet run --project samples/ReferenceDesktop -- --scene=selection --palette=classic --locale=en-US
dotnet publish samples/ReferenceAndroid -c Release -p:AndroidSdkDirectory=C:/Users/boqi/Android/Sdk '-p:JavaSdkDirectory=C:/Program Files/Eclipse Adoptium/jdk-17.0.20.101-hotspot'
```

Avalonia Android package/activity: `org.pixivyou.m3avaloniareference/.MainActivity`. It accepts the same `scene`, `dark`, `palette`, `locale`, `checkboxM3`, `expressiveButtons` intent extras. The APK is x64 for the reference emulator. Windows uses the same content width and usable height in DIP; Android reads actual density, font scale and safe-area insets from the platform. Insets are reapplied on the framework's scaling change; overlays cover the whole window.

`M3AvaloniaReference` logcat records launch inputs, received DOWN/UP/CANCEL event uptimes, render readiness, actual scene-tag bounds in DIP/physical pixels, density, insets and chosen font weights. The IDs map directly to the native test tags. Source input tests cover scene navigation, shared fields/switches/radio, mixed-checkbox cycling and cancellation retaining shared picker drafts.

Roboto is the exact variable font pulled from the reference emulator, licensed Apache2.0; its hash/provenance and license accompany the font assets. The carousel landscape asset ports the native XML vector coordinates, colors and256×256 viewport.
