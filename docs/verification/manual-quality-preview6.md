# Preview.6 — rendering and desktop input regressions

Candidate version: `0.1.0-preview.6`. The initial integrated code frontier is `7e1cdd1ba59210890bf7041754012f0da6298b82`.

The native reference remains Material3 **1.5.0-beta01**, Compose **1.12.0**, with `MaterialExpressiveTheme`, the purple palette, `en-US`, font scale 1.0 and the explicit M3 checkbox branch. The pinned [source hashes](manual-quality-preview5-sources.json), [beta01 comparison](manual-quality-preview5-androidx-beta01.json) and [component motion matrix](manual-quality-preview5-motion.md) identify the inputs. Native app instructions are in [AndroidReference](../../samples/AndroidReference/README.md).

## Changes and source basis

- Radio: paint the animated diameter on a fixed 20-DIP canvas around the ring center. Pinned `RadioButton.kt` uses a fixed canvas center and FastSpatial radius minus half the 2-DIP stroke. Rendered regressions cover entry and exit at 100%, 125% and 150% DPI.
- Date picker: measure the headline inside the calendar's stable 360-DIP default width, including explicit wide and constrained hosts. `DateEntryContainer` gives the headline a weighted slot; `DateRangePicker.kt` joins endpoint halves and clips range bands to actual month cells. Public date clicks and cross-month rendered edges are covered.
- Search: place the search shell, supporting text and token/action rows in their separate layout rows; paint the full-screen backdrop against the host viewport.
- Slider: use the pinned MDC tooltip drawable's 28-DIP minimum width, 4-DIP horizontal padding and slider label's 32-DIP minimum height. The relative corner radius resolves against the smaller dimension. See [Slider.Label styles](https://raw.githubusercontent.com/material-components/material-components-android/60ff09436d5d477a4b9d02940f31eb01e1250620/lib/java/com/google/android/material/slider/res/values/styles.xml) and [TooltipDrawable](https://raw.githubusercontent.com/material-components/material-components-android/60ff09436d5d477a4b9d02940f31eb01e1250620/lib/java/com/google/android/material/tooltip/TooltipDrawable.java).
- Overlay: use bitmap physical pixel dimensions for the source rectangle and DIP dimensions for the destination, matching [Avalonia 12.1.3 bitmap drawing](https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Skia/Avalonia.Skia/DrawingContextImpl.cs#L224). Rendered exit regressions cover 100%, 125% and 150% DPI.
- Tooltip and sheet: pinned `Tooltip.kt` centers on the anchor with 4-DIP spacing and a caret tracking the clamped anchor midpoint. The translated sheet surface stays inside its declared viewport; expansion and RTL refresh its placement.
- Checkbox and content slots: a matching fill/stroke uses the pinned checkbox's single filled rounded rectangle. Sheet body applies the caller's padding once and collapses an empty bottom header. Authored M3 paint, including string bodies and background progress, stays enabled under a modal scrim; actual input and cached automation retain the real disabled gate. The time-dialog scroll viewport reserves title and footer space at enlarged typography.
- Theme: publish typography, shape and motion families through [ResourceDictionary.SetItems](https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Base/Controls/ResourceDictionary.cs). Consumers receive the complete family in one notification; the existing dictionary and host resources are retained.
- Carousel: adapt dominant horizontal and Shift+vertical wheel input to horizontal layouts, and dominant vertical input to full-screen layout. Outward boundary input reaches the parent; fractional ticks accumulate. Navigation uses the existing pinned pager spring.

The new [shared Avalonia reference](../../samples/ReferenceUi/README.md) runs on Android and desktop from the versioned component package. Its [15-scene matrix](../../samples/ReferenceUi/scene-parity.json) matches native layouts, remembered state and actions. Material Symbols Rounded artwork is declared alongside the native Compose Filled artwork. The native paired APK was built from `deaaefffc4827d7c67623753ed7893c366288ce6`; SHA256 `55BE2AD94F337C1EBDAE415180AE3A242FA7B14FD675D620F24973253427F7A8`.

## Candidate verification — pending

The merged candidate's source/package gates, SDK compatibility, published binaries, physical desktop capture and native continuous-frame comparison are pending. The final evidence update will record the tested producer commit, exact package and executable hashes, scenario identities, frame timestamps and capture configuration.

Slice evidence is retained in `F:/PixivYou/analysis/m3-native-reference`: `toggle-test-results`, `date-test-results`, `search-slider-test-results`, `evidence/overlay-fix`, `theme-atomic-test-results` and `wheel-test-results`. These implementation regressions are distinct from the pending merged-candidate verification.
