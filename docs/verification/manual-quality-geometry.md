# preview.3 concrete geometry remediation — manual acceptance remains open

Authority: the user's latest manual screenshots reopened visual/motion acceptance; old716/740/AOT/UIA passes do not establish visual conformity. This record covers geometry work only. Foundation owns genuine symbols/document fonts/Full normalization; motion owns frame lifetime/drivers/performance; input owns TextField/Switch. Old3032585 binaries/images are not new evidence.

## Fixed reference and public seams

AndroidX **11ece46a49d485c7644e53cb0684a611d7a0ec10**, not moving main: `Slider.kt:2428–2669`, `DateRangePicker.kt:1039–1116`, `DatePicker.kt:1958–2066`, `TimePicker.kt:2412–2538,3037–3055,3416–3480,4038–4065`, `Badge.kt:64–129,198–239`, `DragHandle.kt:93–134`, plus generated Slider/DatePickerModal/TimePicker/TimeInput/SheetBottom/DragHandle tokens. Links live in the corresponding [component contracts](../public-contract.md) and [coverage matrix](../coverage-matrix.md). AM/PM deliberately uses the previously declared **legacy generated tertiary-token** recipe, not the default-on newer flag's primary/bold/rounded12 recipe.

Tests use agreed public native controls, host input, visible layout, semantic result/automation and actual rendered output. No private geometry method/part-name assertion, public clock hook, global DPI override, collaborator mock or baseline auto-regeneration was added. Existing scenario identities/API/old-client contracts are retained.

## Actual red → green slices

`tests/Avalonia.Material3.Tests/GeometryQualityScenarioTests.cs` is in the existing shared source/package inventory.

| Slice | Actual red | Remedy / observed green |
|---|---|---|
| Slider common axis | In320×64, expectedPrimary at cap center32, actualSecondaryContainer | one geometry Module for paint/endpoint targets/pointer inverse;320 fixture24..296 outer edges,32/288 cap centers, interior25/75 thumbs96/224; conditional stops, range leading stop, active marksSecondaryContainer; focused slider regression32 cases |
| Range label collision | supplemental public painted output for20/40% same-half and denseStep1 pointer57.6 | only intersecting labels separate; dense discrete inverse selects10 at its actual painted thumb; these supplemental cases are not claimed as separate red cycles |
| Date half backing | joining-half probe(34,5) expectedSecondaryContainer, actualbackground, bothLTR/RTL | rectangular40 band under circles; joining and forbidden-half probes retained, same-day no band;37 date/picker cases |
| Sheet feedback | outside marker hover pixel changed#FEF7FF→#ECE6ED | marker-local state ink and finite focus envelope;256-wide transparent target retained;30 sheet cases |
| Time numeric stroke | public selector bounds80→82 on hover | painted/inset-independent overlay; hover/focus content envelope unchanged |
| Colon/gap | independently expected24 separator, actual22; initial24 wrapper still yielded25 due child layout rounding | own24×80 frame/−4 pinned optical offset, locally unrounded numeric row (not global policy),36 display/dial gap;33 picker cases |
| AM/PM seam | expectedTertiaryContainer in joining corner, actualbackground | native disjoint48-minimum actions with coherent single outer1/radius8 outline/divider and square inner seam |
| Minute07 contrast | **zero OnPrimary numeral pixels** inside the true07 selector | default text-only Adapter renders normal text plus selected-role ink clipped spatially; native five-minute action and07 identity remain;35 focused picker cases |
| Badge anchor | dot expectedicon-left+18, actual+21 | anchor-only measure; dot18/top0, count12/top14−height, owner top/end ruler;45 navigation cases |
| Navigation selection | immediate selected underline already fullPrimary, no intermediate paint | semantic/page selection still immediate; capsule/underline alpha+width use sharedMaterialFrameLease, liveStateLayerDuration/EasingStandard; fixed header, actual intermediate rendered samples, reduced-motion snap;46 nav cases |
| Date font growth | expected56-square readable target atBodyLarge line48, actual53×56 | circular Decorator grows both axes;48 circle+two4 gutters,40 centered band unchanged; joining/forbidden-side probes still hold;38 picker cases |
| Enlarged month/year | owned native200% capture showed a cut-off year; public text-layout red width185.9125 exceeds visible162 | month action wraps the complete year; retained action/name/culture semantics;61 picker/geometry cases |
| Calendar artwork | retained visible-touch/real-ink/input test | library-owned keyboard/calendar/previous/next actions consume A's genuineMaterialSymbol; no Gallery replacement duplication; prior type-coupled brush probe now reads the actual public visual brush |

The first full source run exposed **8 retained RTL scenarios using old outer-edge interpolation**. Their identities, semantic keyboard/pointer/RangeValue checks and half-DIP tolerance were retained; expectations were corrected to pinned worked360-DIP discrete coordinates91.2/268.8,61.6/298.4,120.8/239.2 and150.4/268.8. No test was removed/skipped or tolerance increased to hide the new geometry. The minute-contrast outside-glyph check was strengthened to preserve all normal-role pixels from the ordinary00 state rather than assuming an unjustified platform-font ink area.

## Independent oracles and device-scale evidence

- Slider320×64: axis32, tracks16 high, handle4×44, pressed/focus2×44; visible gap6, cap radius8/inner2; endpoint targets48. Scalar2-DIP stroke/device spans become2/2.5/3/4 pixels at1/1.25/1.5/2. Value-label bottom is12 above thumb top. Exact bounds values are from the fixed reference/worked fixtures, not calls to the control's own position function.
- Feb2024 Sunday-first336-wide month,7..24: startcenter168,72 / end312,168;40 bands `(168,52,168,40)`, `(0,100,336,40)`, `(0,148,312,40)`; endpoint circlesradius20. Joining probe(178,53) isSecondaryContainer, forbidden probe(158,53) background. Mirror once forRTL. Week-edge rectangles are normative; do not turn them into pills or hide the joining half.
- Minute07: center195.582191,52.942373, radius24 on256 dial,42° clockwise from12; near-edge hand joint179.523057,70.777848.05 center178.5,40.531434 is21.11475 away, so glyph overlap is normal. Bubble is not snapped to05; overlap is paint contrast, not semantic selection.
- Sheet marker: bottom32×4 / side4×48 / pressed-side12×52; transparent target may be wider. Focus is finite40 wide; state ink never fills the header.
- Badge anchor24×24: dot `(18,0,6,6)`; count `(12,14−h,w,h)` before allowed-top/end clamp. Badge growth does not remeasure the icon/label.

An isolated **published NativeAOT PickersHost** from exactsourcea4eac5f/packageSHA256CB5EF7D61878E58D7D709111C088AC0840524D8E37A48D9CF516A83E90F3F678 was actually launched under the outside desktop mutex, PID98388, and closed after capture. Actual `GetDpiForWindow=120` (**1.25×**) and unresampled982×753 window images were recorded. UIA numeric faces measured120×100 physical pixels (=96×80 DIP), separator30 pixels (=24 DIP), AM/PM65×60 each (=52×48 DIP); date targets60×60 (=48 DIP), enlarged targets70×70 (=56 DIP). Both themes, true07:07PM with05 overlap,7..24 range and200% calendar were captured. Native output confirmed real partial numeral recoloring rather than snapping the marker. The200% image also exposed the clipped month/year and led to the subsequent red/green wrapping fix; **that oldcapture does not prove the subsequentSHA**. First foreground activation was refused, safely closed, then rerun activated only the inert title of a verified owned topmost window. User's live303 process was never manipulated.

The supplemental **DPI-aware offscreen renderer** uses `RenderTargetBitmap(PixelSize,96×DPI)` and public framebuffer format conversion;1/1.25/1.5/2 literal masks for slider, dateLTR/RTL and clock07 actually rasterize at those scales. This is not resizing a1x screenshot. Nevertheless its layout window honestly remains **RenderScaling1**: it does **not** establish native125/150/200% monitor layout, OS glyph hinting or presentation cadence. Native owned-window captures and actual monitor DPI are recorded outside the repo in the orchestration handoff; absent scales remain unverified.

Nav motion's intermediate captures verify changed decoration and invariant item/header bounds, not monitor vblank FPS, input-to-present latency or zero allocation. Fixed circle/text masks allow antialias edges; fill probes are deliberately away from numeral ink. Font growth is separate from device DPI.

## Exact artifact gate and remaining limits

Early integrated geometry tip **d526416183797d16fee16937792622acffcff860** (C core + B frame frontier + A symbol/finite-corner frontier): authoritative source **734**, freshpreview.3 package **758**, sharedOrdinal identity parity, reviewedAPI/resources and SDK strict baseline compatibility passed. Old compiled/XAML/client unchanged-library-replacement and rebuilt client passed. Four strictwin-x64 published outputs (Gallery/StandaloneHost × NativeAOT/full-trim managed) compiled with IL warnings-as-errors. Those early numbers are **historical exact-artifact regression evidence**, not a visual acceptance claim and not proof for later font/capture/docs commits.

Canonical latest per-commit manifest/TRX/packageSHA/native capture/PID/DPI/compiler inventory pointers are in `F:/PixivYou/analysis/m3-manual-quality/handoffs/geometry.md`. The coordinator must re-gate the final integration/newly published candidate; a laterSHA never inherits an old binary's test or runtime declaration. Native1/1.5/2 monitor modes were not forced or claimed;1.25 is the actual available native scale, while all four offscreen scales are supplemental.

Still open: user's new manual visual round; native fractional monitor DPI where unavailable; high-refresh/presented-frame cadence; human spoken reader/physical touch; cross-platform/Android IME/back/insets; AppM11 actual integration. Existing root-modal/focus/IME/keyboard/automation tests remain mandatory but orthogonal to those visual/motion gaps. Custom templates/arbitrary host content retain their own presentation responsibility. No blanket upstream pixel/frame equality or complete-library visual conformance is claimed.
