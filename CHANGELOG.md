# Changelog

## 0.1.0-preview.5 — 交互范围修复与两种 Ripple

- 修复日期范围连续点击后的断带、端点衔接和旧范围清除。
- 提供纯色扩散与原生渐变闪光两套 Ripple 配方，以及主题／控件级选择。
- 新增官方 Android Material 3 Expressive 对照 App。

## 0.1.0-preview.4 — 标准动效与几何复验候选

- 按锁定源码的组件重载与默认开关统一选择、输入、搜索、导航、浮动操作、面板、日期时间和进度动效。
- 按实际渲染帧计时，保留弹簧反转速度；按钮组文字保持完整，Carousel采用参考布局与连续keyline插值。
- 使用标准两层阴影及海拔时间线，修复阴影裁切，统一官方展开图标与复选标记。
- 校正Slider、侧面板和日期范围结构，保留公开接口及宿主内容插槽。

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
