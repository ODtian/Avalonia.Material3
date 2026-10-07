# preview.4 normative geometry

The baseline remains AndroidX `11ece46a49d485c7644e53cb0684a611d7a0ec10` and MDC Android `60ff09436d5d477a4b9d02940f31eb01e1250620`.

| Public rendered/input seam | Locked recipe and result |
|---|---|
| Slider hover/focus/repeated press | `Slider.ThumbContent`:4×44 layout,4×44 idle/hover ink,2×44 focus/press/drag ink; gap8 from center; immediate width changes. `RippleDefaults.ThemeConfiguration=Opacity` chooses the standard narrow focus handle. |
| Stock side-sheet header | MDC SideSheet anatomy/catalog supplies headline plus trailing24-DIP close icon. Native48-DIP target, localized DismissText, keyboard/Invoke and existing session dismissal are retained. Horizontal plain-content drag coexists with native editing and vertical scrolling; explicit header/template drag handles remain consumable. |
| Date range9..16 and7..24 | `drawRangeBackground` draws40-high rectangles from endpoint centers to row edges. In February2024's336-wide/48-cell grid,9..16 starts(264,52), ends(264,100), with first/last row widths72/264. Selected40-DIP circles supply outward caps; the joining half-cells and cross-week square edges follow those literal rectangles. Same-row9..10 and RTL masks pass. Existing200% circular endpoint and1.25-DPI offscreen masks pass. |
| AM/PM | The pinned default-on updated toggle branch is used in full: PrimaryContainer/OnPrimaryContainer selected, SurfaceContainerLowest idle, selected Bold;4-DIP gaps;52×38 /106×38 faces; Full→12 corners via FastSpatial. Native48-DIP targets center on the artwork. Color/weight change immediately. |

Slider, side header, side plain-content drag and updated period each have a red→green public input/raster test. Corresponding geometry/sheet/date-time scenarios:99 passed. The period raster test also verifies a public FastSpatial override, fixed target bounds and distinct intermediate/final corner frames. Historical scenario identities remain in the inventory; legacy period assertions now use the updated reference recipe.

Research source URLs, SHA256 and exact-tip handoff are in `F:/PixivYou/analysis/m3-standard-round2/geometry-research.md` and `handoffgeometry.md`. Native125% desktop ownership stays with the integrating verifier.
