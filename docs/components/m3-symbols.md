# Genuine Material Symbols — preview.3 foundation

`Avalonia.Material3.Controls.MaterialSymbol` is a small, directly rendering **Module**. It does not require Fluent/Simple templates or an installed icon font. Its **Interface** is:

| Property | Default | Contract |
|---|---|---|
| Symbol | `add` | Official pinned Material Symbols name; unknown names rejected, never document-font fallback/tofu |
| Size | 24 | Finite positive DIP square; independent of document FontFamily/FontSize/LineHeight/tracking/Scale |
| Filled | false | Genuine static FILL0/FILL1 font instance, not FontWeight or another Unicode character |
| Foreground | inherited text foreground | Bindable brush; owning action controls semantic colors |

The visual is decorative, Raw accessibility, not focusable/hit-testable. Give its **owning button/action** a localized automation name. Directional back/forward/chevron/undo/redo artwork mirrors once under local or inherited RTL; non-directional artwork is not blanket-mirrored. It measures a fixed icon frame, preserves nominal em scaling, and centers ink by translation rather than stretching its tight bounds. Cached outline geometry from the actual font bypasses text baseline snapping. Local icon rounding is disabled, not global layout/stroke/text policy.

```xml
<m3:MaterialButton Content="Save" xmlns:m3="using:Avalonia.Material3.Controls">
  <m3:MaterialButton.LeadingIcon><m3:MaterialSymbol Symbol="add" Size="20" /></m3:MaterialButton.LeadingIcon>
</m3:MaterialButton>
<m3:MaterialIconButton xmlns:m3="using:Avalonia.Material3.Controls" AutomationProperties.Name="Favorite">
  <m3:MaterialSymbol Symbol="favorite" Filled="True" />
</m3:MaterialIconButton>
```

Create a **fresh Control per use**, not a shared Control resource. Existing object/content/template slots remain the public **Seam**: callers may supply arbitrary controls, data templates, geometry or branded assets. Explicit caller ContentTemplate/IconTemplate still wins. The library's immutable owned default recipes create fresh symbols through ordinary data templates; default glyph objects are not a new public data model. Do not interpret all string content as icon names. Currency, numerals, punctuation and document labels stay text.

**Source/license:** official Google Material Symbols Rounded2.973 at google/material-design-icons `737e3324305806514d7909874fa1818ae1808232`, **Apache-2.0, not OFL**. Full license, source/derived font hashes, codepoints and instance axes are in `Assets/Icons/`. Exact raw binary font hashes are distinct from codepoints' **canonical UTF8/LF hash**; Windows checkout/package text may contain CRLF and has a different raw hash. Normal builds/runtime perform no network request, font installation, reflection discovery or font generation. Offline `scripts/generate-symbol-assets.py` requires pinned fontTools4.53.0 and exact official inputs; the complete pinned map is generated, not copied from classic Material Icons codepoints.

**Document typography:** consumer MaterialTypography defaults remain host-owned. Only Gallery.App.Initialize selects bundled static400/500/700 **Gallery Roboto + Gallery Noto Sans SC** for deterministic Latin/CJK demonstration. Their separate SIL OFL licenses/source/derived hashes and offline generator live with Gallery assets; no document fonts are added to the control-library nupkg, no consumer Typography overwrite and no OS installation. Role families/local values retain precedence; changing document scale does not change symbol artwork except an explicitly bound component IconSize.

**Stable state layout:** default button/split/group/chip recipes reserve outline content insets independently of painted state border thickness. Segmented/filter selection swaps one reserved icon lane rather than shifting its label. The native Border renderer retains arbitrary/nonuniform caller strokes and brush semantics. Full9999 shape tokens resolve before interpolation with independent side normalization, preserving fixed4/8 inner corners; motion clock/Bounds policy is a separate Module.

**Evidence/limits:** public paint/frame/asset/API tests run against source and a fresh package. Independent source-font add bounds200..760/UPEM960 yield14×14 ink in a24 frame. No count of passing tests establishes final visual conformance. Native monitor DPI/frame capture and manual acceptance are recorded separately by the quality handoff; typography.Scale is not physical DPI. Custom templates keep responsibility for their layout/state/target visual recipes.
