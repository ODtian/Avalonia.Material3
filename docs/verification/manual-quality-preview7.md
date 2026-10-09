# Preview.7 原生对照与交付验证

本地候选 **0.1.0-preview.7** 已完成交付验证。生产版本固定为 `caf39dce9566114a81970a6a08f7838a7bf9c063`；[发布清单](../../artifacts/v7-caf39dce/preview7.json)记录所有产物、SHA256、SDK 和完整用例身份。

[Gallery](../../artifacts/v7-caf39dce/Gallery/Gallery.exe)、[桌面对照 App](../../artifacts/v7-caf39dce/ReferenceDesktop/ReferenceDesktop.exe)、[Avalonia Android APK](../../artifacts/v7-caf39dce/ReferenceAndroid/org.pixivyou.m3avaloniareference-Signed.apk)、[原生 Android APK](../../artifacts/v7-caf39dce/NativeAndroidReference.apk)、[NuGet 包](../../artifacts/v7-caf39dce/feed/Avalonia.Material3.0.1.0-preview.7.nupkg)均保存在独立发布目录。

源码 **292/292**、全新缓存独立包 **292/292** 通过，Ordinal 用例身份完全一致。preview.1 SDK/API 严格兼容基线通过；Gallery 与 ReferenceDesktop 的 Windows x64 NativeAOT/full-trim、ReferenceAndroid 的 Android x64 AOT/full-trim 构建通过。两份最终 Windows 程序已实际启动、激活并截图；最终 APK 已安装至模拟器。

共同修复覆盖物理像素／DIP 快照、内容视口与海拔溢出、零视口恢复、反转与释放、调用方布局／画刷／输入状态，以及弹层安全区与焦点方式。纯色扩散与渐变闪光 Ripple 两套配方保持可选。[17 场景矩阵](../../samples/ReferenceUi/scene-parity.json)记录初始状态、布局和操作。

原生锁定 Material3 1.5.0-beta01／Compose 1.12.0；对照配置为 1240×2772、density 1.25／3.5、fontScale 1、classic、en-US、M3 checkbox、expressive buttons。Avalonia 宿主启用 `UseStencilBuffers=true`；原生 surface sampleCount 0、SurfaceProps flags 0，GrContext internalMultisampleCount 4 单独记录。

实际证据：

- 复选框未选及禁用选中状态，两档密度完整 ROI 一致；350% mixed、Radio 选中状态一致。禁用 Radio／Switch 的主体和 on／off 图标实心颜色按源 alpha 与 Surface 合成核对。
- 五种 FAB 上下阴影剖面，两档密度逐行 RGB 一致；Extended FAB 的完整外侧阴影区域一致。125% Elevated 的实际触摸配方、51px 表面及底部阴影已核对。
- Dialog、Menu、Bottom sheet、Side sheet 的边界一致；触摸焦点外观已核对。Windows 125% 侧面板首次、退出、重入及 bottom→side 的实际采样中，editor 的 Y 始终为 323..391，正文与页脚保持布局。
- Windows 125% 设置（`2c043e64` 同源族复验）三次切换中位数：字体 **102.0ms**、形状 **77.7ms**、reduce motion **57.5ms**；UIA 调用 8.8..14.7ms。
- 最终包时钟原图：普通 3 与完全覆盖 3 的 ROI `[920,1418,1028,1522)`，minute 07 部分覆盖 5 的 ROI `[760,1115,845,1208)`，三者均 **0 个不同像素、max 0、MAE 0**。[原图统计](../../artifacts/v7-caf39dce/clock-raw/final-comparison.json)包含选择圆混合边缘与字缘。[联系图](../../artifacts/v7-caf39dce/clock-raw/final-contact.png)已逐项查看。
- 最终包按钮 Create、Edit、Share 整个 capsule／字形 ROI 分别为 `[36,650,360,806)`、`[392,650,656,806)`、`[688,650,994,806)`，均 **0 个不同像素、max 0、MAE 0**；More 的受限表面与 48DIP 触摸目标按原生分离。[原图统计](../../artifacts/v7-caf39dce/button-comparison.json)记录容器、文字及图标分区。

运动证据按各自原始 PTS 与实际覆盖位置核对。Native 的 part-switch 录像为 129 帧，Avalonia 为 148 帧，编码尺寸 620×1386；用于确认旧数字退出、新数字进入、选择圆覆盖处切换字色。严格字缘像素比较采用上列 1240×2772 原始 PNG。Windows 视频保留实际 CopyFromScreen 时间，用于入场／退出布局检查。

17 场景两端原始截图与联系图保存在 [场景复拍](../../artifacts/v7-2c043e64/paired/final17.json)。后续修复涉及 Group／caption 与共享 shadow 的关闭生命周期 guard；live 布局、设置和侧面板代码族保持。最终 producer 已重新复拍按钮、时钟并启动两份最终 Windows 程序。相同生产代码族的 [设置延迟](../../artifacts/v7-2c043e64/windows-settings/settings-latency.json)、[侧面板实际采样](../../artifacts/v7-2c043e64/windows-side/first-visible-curve.json)、[part-switch](../../artifacts/v7-2c043e64/clock-part-switch/part-switch-manifest.json)证据保留。

字体源／派生哈希、许可证与阴影源码许可随包保存。Android Medium 字形仅在公开系统 face 的 SHA、style、glyph count、cmap guard 全部匹配时使用；其他调用方字体保留原有路径。Material Symbols Rounded 与原生 Compose Filled 的素材由矩阵分别记录：More 的 147 个图标像素差属于该已声明素材分区，外表面一致。

Standards／Spec 固定点审查记录均保存于 `analysis/m3-pattern-audit/review-*.md`，具体发现已闭环。此次资源库存同步精确增加已实现的 8 个 Card／Selection／Switch 资源名，原有名称、公开 API 与 Ordinal 强断言保持；[历史 preview.6](manual-quality-preview6.md)与母规格平台矩阵继续保存各自范围。
