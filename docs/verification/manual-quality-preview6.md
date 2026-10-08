# Preview.6 对照版本与验证

Avalonia 产物源码：`6eb8e147bb285dcb247467318cec280a7050239e`。原生 APK 源码：`1e5b9ea501d244dc2f63daf97569ac8bf82e169e`。版本 `0.1.0-preview.6`。

同一套 15 个场景、初始值、标签和操作由 [场景矩阵](../../samples/ReferenceUi/scene-parity.json)记录。两端 APK 已安装到 `127.0.0.1:16416`。比较配置：1240×2772、density 3.5、fontScale 1、classic、en-US、M3 checkbox、expressive buttons；Roboto 实际字体权重为 400／500／700。

- 源码与独立新包各 **132/132** 通过，场景名称逐项一致；preview.1 严格 SDK 兼容验证通过。
- Gallery、ReferenceDesktop：Windows x64 NativeAOT／完整裁剪；ReferenceAndroid：Android x64 MonoAOT／裁剪。
- 15 个实际设备场景已复拍并并排检查。FAB 按钮、filled 输入框、tonal ripple 按钮的两端像素边界完全相同；日期 weekday 居中、空辅助行、旧按钮组 padding、24-DIP 图标插槽及 FAB menu 内边距按实际原生重载校准。
- Windows 实际输入验证通过：搜索 100／200%、tooltip、side／bottom sheet、日期连续选择保持宽度、横向滚轮。
- 圆点连续录像：两端各 166 个有效观测（质量阈值为峰值 10%），最大圆心偏移原生 0.0238 DIP、Avalonia 0.0182 DIP。原始 PTS、输入时间和近零墨迹观测均保留。该录像来自 `0f796d2`；后续源码保留同一 selection 绘制实现，最终 APK 的 selection 实拍已复核。
- 月份切换两端连续录像、dialog 语言与圆角实拍、Windows basic／fullscreen 退场录像已保存。

[完整产物与 SHA256 清单](F:/PixivYou/m3/artifacts/v6-6eb8e14/preview6.json) · [并排图](F:/PixivYou/m3/artifacts/v6-6eb8e14/paired/scenes-04-06.png) · [实际边界](F:/PixivYou/m3/artifacts/v6-6eb8e14/paired/layout-profile.json)

规范依据保持锁定的官方 Material3 1.5.0-beta01／Compose 1.12.0；[源码哈希](manual-quality-preview5-sources.json)与[动效矩阵](manual-quality-preview5-motion.md)记录具体输入。Avalonia 使用 Material Symbols Rounded，原生使用 Compose Filled 图标；场景矩阵记录两套素材。

[Standards 审查](F:/PixivYou/analysis/m3-native-reference/review-7458-profile-standards.md)与[Spec 审查](F:/PixivYou/analysis/m3-native-reference/review-7458-profile-spec.md)记录修复与源码核对。
