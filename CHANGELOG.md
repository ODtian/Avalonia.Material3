# Changelog

## 0.1.0-preview.3 — 本地视觉与动效复验候选

- 接入固定版本 Material Symbols Rounded、来源／许可与真实字体资源，统一图标对齐和展厅字体。
- 修复 focus／selection 布局跳动、输入框浮动标签与清除图标、Switch 中间帧、Slider 轨道端点、范围带、时间选择器与导航徽标。
- 统一帧驱动与生命周期，保持浮动操作锚点和按钮组中心的连续子像素布局，缓存多图布局计划并在配方改变时更新模板测量。
- 修复时间盘切换后的控件释放；原有公开契约、包消费与兼容验证持续执行。人工视觉与交互复验记录见 docs/verification/manual-quality-feedback.md。

## 0.1.0-preview.2 — local release candidate (not public-feed publication)

- Add every locked standard component family and explicit17-page Gallery plus independent package-only host.
- Full theme inputs/resources, component-state/input/automation/result scenarios, shared window-root modality and collapse-first/veto-safe Back.
- Reusable/detach-safe async sample lifetime; static page factories preserve AOT reachability.
- Enable library NativeAOT/trimming analysis and eliminate library reflection bindings using typed Avalonia-property observables.
- Consolidate shared source/package scenario/fixture/page inventory, fresh exact-artifact feed/cache and ordinal identity gate.
- Add reviewed API/resource inventories, immutable preview.1 SDK API gate, old compiled XAML/client fixture, strict Windows x64 AOT/full-trim host publishing and real published-process verification.
- Preserve initial #2 six-color constructor/deconstruct/with, XAML namespace, resource keys and default button behavior. Additive preview API does not claim stable cross-major guarantees.
- See docs/verification/m3-19.md for actual evidence and remaining App/platform/manual acceptance. No publication, spoken reader or physical Android proof is invented.

## 0.1.0-preview.1 — historical M3-01 / #2

Initial small round filled button, basic theme inputs, six semantic colors, initial Gallery and independent package consumer. Frozen artifact/provenance is recorded in docs/upgrade.md; later development same-version packs in ticket records are historical, not the immutable baseline.
