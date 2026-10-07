# Date and range standards review — resolved

Final reviewed endpoint: `2f4294867c29d8555dd6a7dbff36276df7c1ca33`; review base: `f4dbe7c7aaa3891a88f535d1f96f03f70fb08f21`. Current merged root: `1069630773172764853471092515580ce6d6905f`. Standards: `docs/testing.md`, `docs/public-contract.md`, `docs/components/m3-12.md`, `docs/components/m3-17.md`, and the code-review smell baseline.

Both DateInput P2 findings are resolved:

- Asynchronous paste captures attachment generation, presentation identity, original text/selection, root and pattern. Attach/detach, disable/read-only changes and replacement requests cancel the old wait. The public clipboard deferred-transfer scenario closes/reopens the same picker, resets/edits its draft and selection, returns old data, and observes the preserved new Text/SelectedDate/selection and native Undo result.
- The authoritative editing paragraph now describes stock delimiter formatting through native edit operations, caller InputFormat grammar, script-retaining digits, native caret/selection/IME/Undo ownership, and generation-bound paste behavior consistently.

The stock input field keeps its public MaterialTextField type and style key; its internal supporting-region recipe gives valid/error states the same measured height. Public caller field overrides, custom dialog Theme/Template and ContentTemplate/ActionsTemplate bindings remain the extension seams. Bounded typed panels preserve the actual footer/body/viewport constraint. Calendar virtualization, day activation, input editing and shared overlay result/unparenting remain distinct implementation owners. Delayed input focus cancels on detach/mode replacement.

Review evidence: exact committed source, contracts and focused public regression scenarios. The root performs the unified source/package/NativeAOT candidate gate.

Residual findings: 0 documented-standard findings; 0 actionable smell findings.

The final YearStateLayer correction uses the full72-DIP surface with0/6 insets. Existing public hover/press edge-pixel assertions exercise both states at the left/right artwork edges.

