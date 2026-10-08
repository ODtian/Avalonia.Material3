# Native Material 3 Expressive reference

This Android application calls the published official `androidx.compose.material3:material3:1.5.0-beta01` components inside `MaterialExpressiveTheme`. The theme supplies the official expressive motion scheme and shapes. Scene content and icon labels belong to the sample application. The carousel landscape vector is sample content; the carousel masks, layout, gestures and motion come from `HorizontalMultiBrowseCarousel`.

The release is documented in [AndroidX Material 3 release notes](https://developer.android.com/jetpack/androidx/releases/compose-material3#1.5.0-beta01). Runtime dependencies resolve Compose foundation, UI, animation and runtime to `1.12.0`. Icons come from the official Compose Material icon library `1.7.8`. Side sheet uses the official MDC Android `SideSheetDialog` from `com.google.android.material:material:1.14.0`, with application-owned text content.

Build with JDK 17, Android SDK platform `37.1`, build tools `37.0.0`, AGP `9.4.0` and the checked-in Gradle `9.6.0` wrapper:

```powershell
$env:JAVA_HOME = 'C:/Program Files/Eclipse Adoptium/jdk-17.0.20.101-hotspot'
$env:ANDROID_HOME = 'C:/Users/boqi/Android/Sdk'
./gradlew.bat :app:assembleDebug
```

APK: `app/build/outputs/apk/debug/app-debug.apk`. Application: `org.pixivyou.m3reference/.MainActivity`.

The manual comparison defaults are `scene=home`, `dark=false`, `palette=classic`, `locale=en-US`, `checkboxM3=true`, `expressiveButtons=true`, matching the Avalonia reference. `palette=classic` chooses the official default `lightColorScheme()` palette; `palette=expressive` selects `expressiveLightColorScheme()`. Shapes and motion remain expressive. The locale extra supplies the app-process default Locale together with a localized Android configuration context and Compose locale/resources. Device density and font scale flow through Android configuration. `M3Reference` logcat records the runtime palette, locale, density, flags and hardware acceleration.

| Scene | Official components and initial state |
| --- | --- |
| `home` | Scene selector |
| `date-range` | DateRangePicker, February 2024, initial 10–12; tap 7 then 24 or 9 then 16 for live selection |
| `date-range-7-24` | DateRangePicker initial 7–24 |
| `date-range-9-16` | DateRangePicker initial 9–16 |
| `date-single` | DatePicker initial February 9, 2024 |
| `time` | TimePicker / TimeInput / TimePickerDialog, 07:07 PM; tap the minute field to inspect the minute dial |
| `selection` | Checkbox, TriStateCheckbox, RadioButton, Switch and official thumb icons |
| `slider` | Stateful Slider at 27%, discrete Slider at 70%, RangeSlider at 8–20 |
| `fields` | OutlinedTextField and TextField with labels, icons and affixes |
| `buttons` | ButtonGroup, connected ToggleButton shapes, five official elevated split sizes, filled split, ElevatedButton and ElevatedCard |
| `ripple` | Default filled/elevated/tonal/outlined/text/icon buttons and FAB for sustained press and release capture |
| `fab` | Small/regular/large/extended FAB, ToggleFloatingActionButton menu and HorizontalFloatingToolbar |
| `progress` | Linear/circular progress, wavy progress, LoadingIndicator and ContainedLoadingIndicator |
| `carousel` | HorizontalMultiBrowseCarousel with six items and 260 DIP / available width toggle |
| `navigation` | NavigationBarItem with count and dot badges, PrimaryTabRow |
| `overlays` | DropdownMenu, AlertDialog, ModalBottomSheet, tooltip, MDC Android SideSheetDialog |

Semantics enables `testTagsAsResourceId` on the root; stable tags expose triggers and component containers to UIAutomator.
The root uses the platform `motionEventSpy` observer to record actual DOWN/UP/CANCEL event uptime and coordinates under `M3Reference`, preserving the official components' pointer handling. Captures can correlate frame time with the received input event.
The time dialog uses the official `TimePickerDialogDefaults.Title` and `DisplayModeToggle` slots, including the title's 20 DIP bottom padding and mode-specific text.
The ripple and elevated button reference defaults to each complete expressive `shapes` overload: filled/elevated/tonal/outlined/text use `ButtonDefaults.shapes()`, and the icon button uses `IconButtonDefaults.shapes()`. This selects the official press shape morph as well as the official component's own ripple, colors, elevation and motion. The FAB uses its complete regular official overload. Set `--ez expressiveButtons false` to compare the complete stable button overloads; logcat records the branch. Button groups and split buttons retain their own official default expressive recipes.

```powershell
adb -s 127.0.0.1:16416 install -r app/build/outputs/apk/debug/app-debug.apk
adb -s 127.0.0.1:16416 shell am start -S -n org.pixivyou.m3reference/.MainActivity --es scene date-range --ez dark false --es locale en-US
```

The upstream beta01 release defaults `isCheckboxStylingFixEnabled=false` (legacy M2 checkbox branch) and `isUpdatedTimepickerToggleEnabled=true`. This paired reference explicitly defaults checkbox styling to the M3 migration branch; `--ez checkboxM3 false` preserves access to the upstream legacy branch. Date-range scenes make February 20 unavailable through the official `SelectableDates` API. Selected ranges remain inclusive through that disabled day. The paired layout/action manifest is `../ReferenceUi/scene-parity.json`; unsupported alternate library branches are recorded there.

