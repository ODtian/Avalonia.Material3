# Preview.7 原生对照与共同渲染边界

最终发布验证进行中。最终 producer、场景计数及产物 SHA256 由独立包发布清单记录，完成实际复拍后填写。

共同修复覆盖位图物理像素／DIP 映射、内容视口与海拔溢出、零视口恢复、反转与释放、调用方布局／画刷／输入状态，以及弹层安全区与输入方式。

[17 场景矩阵](../../samples/ReferenceUi/scene-parity.json)记录两端初始状态和操作。对照配置：1240×2772、density 1.25／3.5、fontScale 1、classic、en-US、M3 checkbox、expressive buttons。Avalonia 对照宿主启用 `SkiaOptions.UseStencilBuffers=true`；原生锁定 Material3 1.5.0-beta01／Compose 1.12.0。

原生 OpenGL 目标采用 surface sampleCount 0、SurfaceProps flags 0；GrContext 的 internalMultisampleCount 4 单独记录。选择圆混合目标按前两项配置。

已保存的实际证据：

- Checkbox 未选及禁用选中状态，两档密度完整 ROI 与原生一致；350% mixed、Radio 选中状态也一致。
- 禁用 Radio／Switch 主体及 on／off 图标实心颜色按源 alpha 和 Surface 合成校准。
- 五种 FAB 上下阴影剖面，在两档密度逐行 RGB 与原生一致；125% Elevated 的实际触摸配方、51px 表面和底部阴影已核对。
- Extended FAB 两档密度的表面尺寸一致，完整外侧阴影区域逐像素一致。
- Dialog、Menu、Bottom sheet、Side sheet 的实际边界一致；触摸焦点外观和侧面板首次入场的固定正文布局已核对。
- 时钟普通数字 3、完全覆盖后的数字 3、minute 07 下部分覆盖的数字 5，三个完整原图 ROI 逐像素一致；选择圆边缘混合与字缘同时一致。接近、部分覆盖、完全覆盖、离开四阶段按选择圆实际区域切换字色。
- Windows 设置切换实测：字体中位数 95.3ms，形状 69.6ms，reduce motion 64.1ms。最终包复验随发布清单记录。

时钟按原生 Clear／Xor／DstOver 混合顺序、物理坐标及 canonical Float 角度绘制。字体 DEFAULT／同源 variable／派生 static400 的数字 3、5，原生相同位置原图逐像素一致。官方阴影来源、字体派生的源／输出哈希与许可随包保存。Material Symbols Rounded 与原生 Compose Filled 素材由场景矩阵分别记录。

最终证据表：

| 项目 | 最终记录 |
|---|---|
| 源码与独立包相同场景清单 | 发布验证进行中 |
| preview.1 SDK 兼容基线 | 发布验证进行中 |
| Gallery／ReferenceDesktop Windows NativeAOT | 发布验证进行中 |
| ReferenceAndroid Android AOT | 发布验证进行中 |
| 原图、录像 PTS、边界与像素统计 | 最终复拍进行中 |
| Standards／Spec 固定点审查 | 最终增量确认进行中 |

[前版记录](manual-quality-preview6.md)保留历史产物与验收范围。
