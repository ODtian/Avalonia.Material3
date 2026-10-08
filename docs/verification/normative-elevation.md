# Normative elevation and icon rendering

Baseline: AndroidX11ece46a49d485c7644e53cb0684a611d7a0ec10. Stock cast-shadow geometry follows AOSP/Skia android-15.0.0_r1 HWUI ambient9/255 and spot48/255, actual display/window lighting and physical elevation projection. The source ShadowRRectOp/convex tessellation and Gaussian coverage are retained. Compatible custom BoxShadow tokens follow official Material Web703aed25b91634c2473699846ac166cf2f14a195 [elevation renderer](https://github.com/material-components/material-web/blob/703aed25b91634c2473699846ac166cf2f14a195/elevation/internal/_elevation.scss).

| Level / height | Key x,y,blur,spread (30%) | Ambient x,y,blur,spread (15%) |
|---|---|---|
|0 /0|empty|empty|
|1 /1|0,1,2,0|0,1,3,1|
|2 /3|0,1,2,0|0,2,6,2|
|3 /6|0,1,3,0|0,4,8,3|
|4 /8|0,2,3,0|0,6,10,4|
|5 /12|0,4,4,0|0,8,12,6|

The table records compatible Web tokens, used by caller-modified shadow recipes. Stock native shadows use the actual rounded outline, display-centred lightX, lightY0dp, height500dp weighted by display size, radius800dp and source128-A8/convex Gaussian coverage. Shader projection and paint-overflow geometry share the same physical-domain contract. Source [Elevation.kt](https://github.com/androidx/androidx/blob/11ece46a49d485c7644e53cb0684a611d7a0ec10/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/internal/Elevation.kt) defines incoming120ms(.4,0,.2,1), hover outgoing120ms(.4,0,.6,1), press/focus/drag outgoing150ms(.4,0,.6,1). Initial/disabled/reduced-motion states snap. Height interpolation drives both shadow layers; animated paint preserves authored target resources and native Border geometry.

Source defaults: elevated button/card1→hover2, focus/press1; filled button/card0→hover1, focus/press0; outlined/text0. Elevated chips1→hover2, focus/press1; selected flat filter hover1; flat assist/input/suggestion hover0. FAB3→hover4, focus/press3. Card drag3/4 and chip drag4. App bars, search bar and navigation bar use their semantic tonal surfaces with shadow0. Rich tooltip/menu2, snackbar/dialog3, bottom/modal side sheet1. Toolbar body with FAB uses expanded1/collapsed0; other toolbar bodies0.

Shadow owners allow elevation overflow. Scroll/reveal/calendar/carousel viewports and explicit host clips keep their content boundaries. Reordering list elevation paints beside its reveal viewport. Toolbar elevation paints separately from its foreground reveal clip.

M3 checkbox chooses the documented M3 styling branch in [Checkbox.kt](https://github.com/androidx/androidx/blob/11ece46a49d485c7644e53cb0684a611d7a0ec10/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Checkbox.kt):18-DIP canvas, normalized(.25,.5)→(.4,.65)→(.75,.3),2-DIP square stroke; mixed cross gravitates to(.5,.5). List disclosure/more, toolbar/group overflow and Gallery leading artwork use pinned Material Symbols assets.

Focused rendered seams cover literal level1 pixels, outside-shape shadow gradients with explicit host clipping at96/120/144/192 DPI, checkbox mixed masks, official disclosure artwork, reordering outsets, source shadow0 defaults, and hover elevation intermediate frames with unchanged public bounds.
