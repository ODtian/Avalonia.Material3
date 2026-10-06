# Upgrade and rollback: preview.1 → preview.2

## Immutable identities / policy

New candidate: **0.1.0-preview.2**, net10.0 / Avalonia12.1.3 / SDK10.0.112. No stable API or arbitrary future Avalonia version guarantee: package metadata has a minimum dependency version; the tested host pins12.1.3. All additions remain prerelease; intentional future breaks require a new version, explicit migration and reviewed API/resource diff. Never replace contents of an already distributed identity.

The original #2 binary is **not** later development preview.1 packs recorded by individual tickets:

- archive: `F:/PixivYou/analysis/m3-spec-1-orchestration/baseline/Avalonia.Material3.0.1.0-preview.1.nupkg`
- SHA256 `C52D2E60A9E508ACF7EA1915EFBD5A84E7508AD39D8662453793759AC44174B6`
- original nuspec repository commit `ce1a947caf97c7bbc6257b7ad41cf4cfa835db3a`; adjacent `provenance.txt`
- preserved [initial public contract](compatibility/preview1-contract.md), unchanged [historical verification](verification/m3-01.md)

Hash-check that archive before API validation. Do not silently fetch an unproven public preview.1 or overwrite the archive with new components.

## Preserved contracts

Six-color `MaterialColorScheme` positional constructor, six-output `Deconstruct`, `with`, static Light/Dark and initial values remain. MaterialTheme initial inputs and M3 resource names, MaterialButton's original small/round/filled behavior, XAML namespaces, commands/content extension and Avalonia automation foundations are retained. Additions include full token roles and every component family; details/defaults/projections are per-component contracts.

`verify.ps1 -BaselinePackage ...` runs SDK API/package binary baseline comparison with parameter-name rule. `ReleaseContractScenarioTests` checks ordinal reviewed API/resource inventories plus ctor/deconstruct/with. `verify-compatibility.ps1` compiles a real XAML host against **the archived old package**, runs it, changes **only the M3 assembly**, reruns the unchanged client, then also rebuilds the same source against the candidate. The fixture explicitly carries MCU as a host dependency so binary loader resolution is deterministic. It is a generic headless compatibility fixture, not App, native touch or screen-reader proof. Exact hashes/results are in the final evidence.

## Consumer upgrade

1. Preserve old lock/package/hash and host commit. Use a new isolated cache/feed; verify the chosen preview.2 artifact hash from the release manifest.
2. Change PackageReference from preview.1 to **preview.2**; keep Avalonia12.1.3 and net10.0 pinned. No source reference is allowed.
3. Install MaterialTheme before creating content. Existing #2 usages need no migration. New variants have explicit component API/defaults; do not bind to private template parts.
4. NativeAOT/full trim consumers use compiled XAML/bindings or typed observable property bindings, static factories and source-generated serializers. Enable analyzers and fail on IL diagnostics; do not root the entire assembly.
5. Place shared MaterialOverlayHost at bounded window root, including chrome. Back while OpenCount>0 always remains in the overlay flow; bottom-sheet first Back may collapse while RequestBack returns false.
6. Re-test product-owned theme persistence, input/IME/lifecycle, font resources, accessibility, routes and state with the actual App. The real [App spec](https://github.com/ODtian/MaterixivYou/issues/1), [early package consumer #3](https://github.com/ODtian/MaterixivYou/issues/3) and [final host #27](https://github.com/ODtian/MaterixivYou/issues/27) require an executable consumer; docs-only4fda55f cannot satisfy M11.

## Rollback

A consumer using only initial #2 API can restore its old package reference/feed/cache and re-run the same scenario. Code using newly added component types/resources must remove those usages before returning to original preview.1. M3 owns no product schema/migrations; sample setting JSON is host-owned, not a product persistence guarantee. Preserve consumer state before upgrades. Rollback to a historical development preview.1 pack is not rollback to the frozen #2 artifact. Public distribution has not occurred here; verify feed provenance separately if/when publication happens.
