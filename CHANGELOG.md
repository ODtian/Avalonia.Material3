# Changelog

## 0.1.0-preview.7 — 共同渲染边界修复

- 将时钟、月份与浮层退场位图收敛到共同的像素／DIP 快照契约。
- 统一前景裁切、海拔溢出及装饰与测量的边界，处理重入、反转与尺寸变化。
- 统一临时输入阻断的绘制资格与 C# disabled 配方，保持作者输入状态。
- 默认海拔按锁定 AOSP／Skia 原生光照与阴影网格绘制，保存源许可与自定义阴影分支。
- 按原生像素规则绘制选择控件边框与禁用颜色，宿主启用对应的 Stencil 渲染配置。
- 时钟字色跟随选择圆覆盖区域；按已确认字体源、整数段落测量与相同坐标核对原图。
- 对话框与侧面板使用共同安全区，初始、陷阱和恢复焦点保留实际输入方式。
- 对照 App 增加禁用 on／off 场景，Android 与桌面共享 17 场景矩阵。
- 按原生适配前缀和居中规则布局按钮组，恢复重测后可见成员及完整视觉树。
- 按受限表面／触摸目标分别测量 More，使用同源 Medium 字体实例和整数行基线。

## 0.1.0-preview.6 — 渲染与桌面交互修复候选

- 在固定绘制区域内缩放单选圆点，保持中间帧的圆心。
- 固定日期选择器的默认宽度，保留显式宽度与大字体标题布局。
- 分离搜索头部、辅助文字与 token 行；按原生尺寸绘制 Slider 数值标签。
- 使用物理像素源矩形保留浮层退场画面；按原生锚点与间距定位 tooltip。
- 将完整 Sheet 表面置于固定视口内，更新展开与 RTL 定位。
- 按原生一次填充合成复选框边缘；Sheet 正文应用调用方 padding，空标题收起。
- 模态覆盖保留已启用控件的绘制与背景进度；输入及缓存自动化继续使用真实禁用状态。
- 批量发布字体、形状与动效资源，保持一次完整通知。
- 接入 Carousel 横向滚轮、Shift+滚轮与触控板轴向路由，边界滚动交给父容器。
- 新增共享 15 场景的 Avalonia Android／桌面对照 App，与原生对照 App 共用布局、初始状态及交互矩阵。

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
