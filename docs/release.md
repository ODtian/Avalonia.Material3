# Reproducible candidate / publication process

Authority: `Directory.Build.props` Material3Version **0.1.0-preview.3**. This implementation produces a local verified artifact only. Coordinator owns integration/tracker/push; maintainer owns immutable tag, rights/license, public publication and feed proof. No tag/upload/NuGet publication is represented as completed.

当前候选包含图标、布局与动效反馈修复；人工复验按 [manual-quality-feedback.md](verification/manual-quality-feedback.md) 逐项记录。preview.1 / preview.2 的历史产物和验证 provenance 保留，当前候选以本次 manifest 的源码 SHA、包 hash 和发布目录为准。

## Gates

1. Merge/review the integration tip; clean source tree. Lock SDK10.0.112, net10.0, Avalonia12.1.3. Review the full coverage matrix, projection boundaries and the actual platform/manual acceptance table. Blocked criteria remain open rather than turning all cells green.
2. Run `pwsh scripts/verify.ps1 -KeepSandbox -BaselinePackage <immutable-preview.1>`: source tests, strict library AOT/trim analysis, SDK package/API baseline, one fresh pack and public-only consumer sandbox. The output includes commit/version/package hash, full shared ordinal scenario identities and package-only extras. Copy/compile inventory is shared, not hand-maintained per ticket.
3. Run `pwsh scripts/verify-compatibility.ps1 -Manifest <manifest.json> -BaselinePackage <immutable-preview.1>`: old compiled XAML/client unchanged plus rebuilt-source route. This is not literal App M11.
4. Run `pwsh scripts/publish-release.ps1 -Manifest <manifest.json> -NativeSmoke` on an interactive Windows x64 desktop, **serialized** with other native runs. Publishes both Gallery/independent package-only hosts as NativeAOT AND full-trim managed, strict analyzers/IL warning errors, records compiler logs/binlogs and published executable/directory hashes/sizes. UIA/key/edit/overlay/all-page interactions and repeated fresh-process readiness are against published output, never `bin/Release` substituted as published proof. Native script closes only its owned process; collector cleanup is scoped to the unique sandbox. Publishing revalidates/reuses the same exact-candidate source-free consumer/feed/cache (short path for MSVC) rather than creating a second full dependency tree. Retained component entrypoints can use `-Manifest` with that candidate; default entrypoints still create a fresh exact gate.
5. Retain exact artifact, source SHA, manifest/scenario identity, TRX, API/resource baselines, baseline provenance, all compiler diagnostics and native evidence. Review changes to `tests/ReferenceVectors/public-api.txt` and `resource-keys.txt`; never regenerate expected snapshots automatically in the gate. Explicit generation mode is `M3_WRITE_BASELINES=<directory>` with the filtered contract test, then review the diff. SDK binary baseline remains an independent compatibility check.
6. Actual product/App/device/screen-reader rows need their named owner evidence. Missing App executable, Android SDK/NDK/device scope, human reader/physical input, and redistribution license decisions are exact outstanding prerequisites, not inferred passes. Avoid cross-repo cycle: ship a candidate for App's early package consumer, not wait for App #27 while it already depends on M3 #20.

## Publish (maintainer only)

After rights/license/metadata and required acceptance are settled, tag the reviewed **source SHA** and publish the **already verified exact nupkg hash** without rebuilding. The package includes README, changelog and third-party attributions; project license metadata is intentionally not invented. Obtain an explicit maintainer license decision before public redistribution. Do not print credentials or edit user/global configuration.

The local source mapping is NOT proof of public availability. Post-publication smoke uses a separate public-only NuGet configuration with `Avalonia.Material3` explicitly mapped to the actual public feed and no local feed, exact version/new cache, and compares package provenance. Passing `--source` with contradictory source mapping is not a reliable migration. Record publish receipt/feed endpoint/hash and run a real App old→new host upgrade separately. See [upgrade/rollback](upgrade.md).

## Failure policy

Stop on scenario omissions, skipped/failed tests, library ProjectReference, wrong artifact/hash/version/commit, API break, or IL warnings. Preserve failed logs; fix typed reachability/bindings/lifecycle/serialization narrowly and rerun affected gates. No blanket assembly roots or global IL2xxx/IL3xxx suppressions. A NativeAOT successful compilation alone never certifies templates/resources/automation on a running process or a different RID/device.
