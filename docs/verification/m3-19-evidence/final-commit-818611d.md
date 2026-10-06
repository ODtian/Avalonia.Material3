# Renewed exact-final-commit verification — 818611d

This is an ADDITIONAL immutable record, not a relabeling of the earlier3d888d5 artifact. Requested continuation checked the real filesystem: HEAD818611d, clean, no running owned pipeline/host/compiler. Nothing was reset or duplicated. Parent/local integration remainedc99bef5 and ancestry/merge passed.

Producer **818611dcd803662c59d8f865e5ee6c110aaddb62**, SDK10.0.112, Avalonia12.1.3, net10.0, Windows10.0.26200/win-x64.
Version0.1.0-preview.2.
Exact new package SHA256 **9D1A18D376DF7728F0BF0DD0461748B4F6B9E82550AD0829FD5B367B13034D57**.
Source/package run `artifacts/release-9dc3d982fa104ed6acd1a8ba2ae248d8`; pack/API binlog, logs, manifest, Ordinal scenarios and TRX retained there. **667 source +691 fresh-package passed,0 failed/skipped**;667 shared+24 reviewed package-only; frozen preview.1 SDK/API and API/resource baselines passed. Old compiled binary/XAML client against old→new, unchanged client hash, and rebuilt source all passed via verify-compatibility.ps1 with this exact manifest.

One UNINTERRUPTED serialized `publish-release.ps1 -NativeSmoke` run **completed and returned PASS for all four actual published hosts** at `artifacts/p-32756cbd/published.json`. No resumed/manual completion was needed in this renewed run. Same package-only isolated feed/cache; full trimming; strict analyzers/IL warnings; no blanket roots/global suppression. Four build/compiler logs/binlogs and complete relative-file inventories retained. All4 Native UIA/card Invoke/input/command/form/search/content/result/root modality/focus/back/delayed forms/runtime theme/seed/platform/shape/motion/resize/17-page +200%-narrow revisit completed, with5 fresh processes/host.

| Mode/host | EXE SHA256 | Complete inventory SHA256 |
| --- | --- | --- |
| Gallery/AOT |F28D14D4E0A3A409903F0B69D53BA0D4B0DE47C032DBAADEE271C8B8B11D72C5|5170197D27DAD17BA08A848153A13FD395F2EE15088EB39DFE443B87267D086C|
| Standalone/AOT |2C591A13779A00E2280A6DA7CF812BDB42E064433CEC52C06F75578775B64CDF|AA8C36846CF0B587DC8EC5B274C1DBFBC5AA065C1DC7C720C2BD925C1D579E98|
| Gallery/managed trim |5D6D6158AB0A3AE80EF52A1153014E4AFC0D836E53715498FEC2004039858C30|7C5CAD8834AC314C983460BB97E0EC3A8FA3AD698B1996F9EB5054193C3EC5EA|
| Standalone/managed trim |48D0150E5D23288E766344DD5470DBE629C2D291C5B49542C1BF3041D913F5F8|4811811F58445F6968D769ACFE3B0D63E23B223B7DE1411879FEF350A7A92D46|

Fresh-process launch→native UIA enabled ActionButton milliseconds, warm OS cache/no reboot/cache flush/no promised threshold:
- Gallery/AOT1143.21,902.57,886.41,855.80,865.18
- Standalone/AOT1094.52,913.10,887.25,892.63,858.02
- Gallery/trim3285.06,3242.98,3172.77,3290.54,3221.94
- Standalone/trim4645.66,3218.25,3235.64,3238.24,3245.21

Exact commands:
```powershell
pwsh -NoProfile -File scripts/verify.ps1 -KeepSandbox -BaselinePackage F:/PixivYou/analysis/m3-spec-1-orchestration/baseline/Avalonia.Material3.0.1.0-preview.1.nupkg
pwsh -NoProfile -File scripts/verify-compatibility.ps1 -Manifest artifacts/release-9dc3d982fa104ed6acd1a8ba2ae248d8/manifest.json -BaselinePackage <same immutable archive>
# Wrapped by orchestration with-desktop-lock.ps1:
pwsh -NoProfile -File scripts/publish-release.ps1 -Manifest artifacts/release-9dc3d982fa104ed6acd1a8ba2ae248d8/manifest.json -NativeSmoke
```

Earlier artifacts remain historical and were not overwritten. External App M11, spoken reader, physical device/Android/other RID, public feed/license prerequisites remain precisely partial per m3-19.md; this Windows evidence does not substitute. Subsequent evidence-only commits must not rewrite this producer. The latest coordinator handoff names any newer exact final-tip recheck and its actual hashes.
