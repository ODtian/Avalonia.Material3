# Final review corrections — spec #1 / local preview.2

Scope: independent Standards/Spec reviews at `e4ddf3d78bf0012c8b70935c8e82b0f2df49e694`, fixed point `207a094c943dffd7b8eb5acea8de3a0741eca5ce`. Corrections are internal/public-behavior repairs, not a new animation framework or application service. No public API/resource baseline regeneration or version change. The immutable initial preview.1 binary/hash, original native evidence and historical component artifacts retain their own provenance.

## Standards dispositions

| Finding | Correction and actual tracer evidence |
| --- | --- |
| ST-P01 (same bug as S6) | Shared internal setting-input guard recognizes MaterialSlider (including range/presenter descendants), alongside native Button/TextBox/Slider. Both real downward setting drags originally emitted one unwanted refresh; repaired public regressions emit zero and keep slider adjustment. Ordinary pull/carousel/background/cancellation scenarios retained. Carousel ItemTemplate setting drags also covered. |
| ST-P02 | Effective child availability reconciles only a missing required Single choice. Direct IsEnabled, command CanExecute and parent effective-enable each originally left null; all now select exactly once without Click/Command. Existing selected disabled choice remains selected. |
| ST-P03 (Fowler recommendation, not normative failure) | Six drivers call one internal pure unit-mass response. Instant returns1, including t0; component clocks/detach/settle/clamp/geometry policies remain local. Existing public motion/spring/sheet regressions61 passed before and after extraction; no private math test hook or public framework. |
| ST-F04 | Actual retained `verify-issue13 -KeepSandbox` reproduced MSB4019 for missing ScenarioInventory.props after source689 passed. All retained component entrypoints now delegate the same verify/consumer inventory/Ordinal gate, preserving their native scripts, screenshot environment/optional DesktopSmoke and adding explicit Evidence/Manifest options. No copied-file whitelist repair. Default invocation makes one fresh candidate; `-Manifest` revalidates the same clean exact-HEAD/package/asset/TRX gate rather than cloning/repacking for every ticket. Native actions are never claimed from an unexecuted switch. |
| ST-F05 | Actual worker query threw UI-affinity InvalidOperationException. List peer now keeps UI-updated volatile expandable/expanded descriptors, including false→true→false availability and read-only worker state. UI-thread Expand/Collapse, disabled rejection and notifications retained. |

## Spec dispositions

| Finding | Correction and actual tracer evidence |
| --- | --- |
| S1 | Scrollable measure caps long labels but sums natural desired widths, matching arrangement. Primary/secondary A+B panel originally reported800 at400 viewport instead of180; now intrinsic content284/204 including gutters has no blank scroll tail through400→320→1000→400. |
| S2 | Submenu Tab/ShiftTab walks to root and performs normal LIFO dismissal, returning original opener with no command/payload. Original two/three-level cases left OpenCount1/2; corrected four LTR/RTL cases close the chain. Escape deepest-only and normal veto/commands remain in existing regressions. |
| S3 | Retained invalid/unavailable text reparses under new Culture/InputFormat, updating BOTH range candidates before availability/Confirm. March4→April3 grammar tracer originally retained March4; repaired modal payload is April3. Native text/caret remain untouched on reparse. Additional real Undo diagnostic found default-priority Text coercion during error-style reevaluation cleared history; date editors now establish local Text priority before native editing and avoid equal-string synchronization. Native Backspace/insert Undo history survives grammar recovery. No custom undo buffer/IME implementation. |
| S4 | Part selector activation reasserts the active selector's required checked state even when ActivePart is unchanged. Both12/24h original same-part Invoke turned it off; Hour/Minute/owner/Toggled/style now agree with PrimaryContainer/OnPrimaryContainer and unchanged time. |
| S5 |40-DIP vertically centered visual range band is separate from48-minimum day targets. Endpoints cover their appropriate half-cells; equal range has only its selected circle. Literal physical pixels cover same-row, cross-row, equal-day and font200: outer strips SurfaceContainerHigh, interior band SecondaryContainer. Original four cases painted outer strips SecondaryContainer. |
| S6 | Same correction as ST-P01, not counted twice. |
| S7 | GalleryShell releases/detaches/disposes its current page on real Window.Closed AND visual detach; reattached retained shell creates a fresh selected page visit. Both original multiwindow in-flight close/detach cases committed Completed after leaving; now canceled and no late result/collection commit. Navigation ordering/root Back remain intact. Library still does not own application jobs. |
| S8 | Only the two relocated relative links are corrected to ../coverage-matrix.md and ../verification/m3-01.md. Historical version/commands/hash/provenance remain unchanged. |

## R1 — independent physical-space hypothesis reproduced and repaired

All oracles use transformed physical AABBs then raw Window points/pixels/visible item identities, **not** owner-local TranslatePoint expected positions. Both explicit childRTL underLTR Window and inherited WindowRTL were tested.

- Slider physical left10% originally selected10 instead of90; Reverse=true selected90 instead of10. Logical pointer/render/range coordinates now apply only ReverseDirection; Avalonia supplies the single RTL mirror. Keyboard retains its numeric RTL/reverse mapping: Right decreases ordinaryRTL70→60 while handle moves physically right. Range endpoint numeric identity/48-DIP target centers and physical clicks tested (standard layout rounding ≤.5 DIP).
- Linear240/value.25 original physical220 pixel was track rather than Primary. Active220 Primary #6750A4, gap176 Surface #FEF7FF, track100 SecondaryContainer #E8DEF8, stop2 Primary now pass. Logical rendering has no second manual RTL reflection.
- Carousel original MultiBrowse current left0 rather than157; Hero0 rather than56. Uncontained positions already correct but physical rightward100 drag stayed at index0. All horizontal forms now arrange logical rectangles and use logical negative drag as forward. Verified exact positions/widths: Multi157/243 and56/93; Hero56/344 and0/48; Uncontained214/186 and20/186, six original image identities, physical rightward drag→1 and RTL Left key→2.
- Dialog actions analog was also reproduced: RTL confirm remained physically right of cancel. Panel now arranges logical cancel→confirm/end alignment; both explicit/inherited RTL confirm is physically left/end. Existing dialog behavior remains covered.

Two retained RTL scenario identities previously used mirrored-owner expected coordinates. Their input/pixel oracles are corrected to physical-window coordinates without deleting/renaming tests or weakening expected semantics. A range-center test was adjusted from impossible exact subpixel equality to the framework's ≤.5-DIP layout rounding, not a behavioral waiver.

Actual focused gates:43 new shared review scenarios passed; affected slider/progress/carousel88 and physical/dialog50 passed. Source/package inventory includes these automatically. Interim diagnostic failures are retained in the fix worktree `artifacts/review-*.log`, including Undo snapshot setup/coercion/one-way-template experiment (template experiment reverted), physical RTL reds and the legacy verifier failure. First logging attempt had no artifacts directory and produced no test result. An attempted external framework-source download was denied; no downloaded source/config changes or workaround was used. Native Undo diagnosis used the public CanUndo notification and observed stack, not a private test oracle.

## Exact final release evidence and disk policy

Run on a **clean committed producer**, after merging latest local integration:

```powershell
pwsh -NoProfile -File scripts/verify.ps1 -KeepSandbox -BaselinePackage <immutable-preview.1>
pwsh -NoProfile -File scripts/verify-compatibility.ps1 -Manifest <new manifest> -BaselinePackage <immutable-preview.1>
# Serialize actual UIA/input on the shared desktop:
& F:/PixivYou/analysis/m3-spec-1-orchestration/with-desktop-lock.ps1 -CommandText 'pwsh -NoProfile -File scripts/publish-release.ps1 -Manifest <new manifest> -NativeSmoke'
# Retained commands reuse/revalidate this EXACT candidate without another feed/cache:
pwsh -NoProfile -File scripts/verify-issue13.ps1 -Manifest <new manifest>
pwsh -NoProfile -File scripts/verify-issue15.ps1 -Manifest <new manifest>
pwsh -NoProfile -File scripts/verify-content.ps1 -Manifest <new manifest>
```

The definitive new producer SHA, package/hash, actual results/TRX/Ordinal identity comparison and FOUR published-host compiler/native/full-payload hashes are recorded in `F:/PixivYou/analysis/m3-spec-1-orchestration/handoffs/review-fixes.md` and original generated manifests/logs it points to. Those records, not this document's source commit or older014/e4dd artifacts, determine verification provenance. Do not relabel an older binary as a new producer. Optional legacy native checks retain their own scope; final published protocol proves actual17-page/global modal/Back/focus/input/theme/font/resize/delayed forms and repeated fresh-process readiness for both NativeAOT and full-trim managed Gallery/Standalone.

Incremental tracer tests reuse ONE source output with --no-restore. Final gates use ONE fresh, short-path candidate consumer/feed/cache; publishing and legacy entrypoints revalidate/reuse it instead of multiplying full dependency copies. The reproduced failed legacy sandbox was removed after retaining its error log. Keep selected nupkg/TRX/binlogs/four published payloads until coordinator preserves them; remove only owned disposable intermediates/collectors, never historical or foreign evidence/global packages.

## Whole-spec acceptance remains partial

These library-local fixes do not substitute for actual App M11 executable/pinned old→new consumer, human spoken Narrator/NVDA, physical touch/stylus/device matrix, Android/TalkBack/IME/insets/lifecycle/AOT host, other RID certification, public feed/tag publication or maintainer license/redistribution decisions. Existing explicit projections remain projections, not exact Compose-engine/pixel claims. #1/#20 remain **PARTIAL** until those named external/manual prerequisites are actually satisfied or explicitly resolved by their owners. No App counterfeit, tracker closure, public publication or license grant is made here.
