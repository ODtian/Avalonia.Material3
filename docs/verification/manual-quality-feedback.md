# 人工视觉与动效反馈：重新开放质量验收

## 状态

用户运行候选包后的人工截图与交互反馈揭示系统性视觉/动效问题。**此前功能、API、包消费、AOT/裁剪和 UIA 结果保持其真实历史 provenance，但不能据此声称视觉规范和动画流畅性通过。库内视觉质量验收重新开放。** 当前反馈基线为已保存的 `3032585` 候选二进制；修复源码基线为集成分支 `9edb14d`。

## 待处理清单

| 范围 | 人工观察 | 处理方向（尚非修复结论） |
| --- | --- | --- |
| 通用布局 | 多页文字/图标错位；点击、hover、focus 和状态变化产生细微布局跳动 | 核查实际 DPI、子像素/布局舍入、字体/图标度量、状态边框是否改变 measure；独立物理像素与 bounds oracle |
| 图标 | Unicode/手绘占位符，图标样式与位置严重不一致 | 接入真实、可追溯的 Material 图标资源；独立 glyph box/字体与统一对齐，不使用正文字符冒充标准图标 |
| Floating actions | FAB menu 上下闪跳，FAB/toolbar 整体动画不流畅 | 核查 frame 驱动、anchor 稳定、每帧 measure/arrange 和分配 |
| Button groups / split | icon 不正确，展开/连接形状可疑，位置轻微振荡 | 对照锁定形状/间距，区分预期 expressive 形变与非预期 layout jitter |
| Switch | 无可见切换动画；thumb/icon 错位 | 固定轨道/触达区域，重新验证移动中心和 icon 度量/尺寸/颜色过渡 |
| Text fields | focus 微调尺寸、缺少过渡、前后 icon 不齐、outlined label 黑色块 | 保留 native 编辑/Undo/IME，固定外几何并实现 outline notch/正确表面和可控浮动状态过渡 |
| Sliders | marks 与 track/endcap 不齐 | 统一几何中心线/端点/可见轨道长度与目标命中区，检查整数/分数 DPI |
| Progress/loading | 动画卡顿 | 实测 frame cadence、UI 工作量、绘制路径分配/缓存，避免仅以时间参数断言代替性能 |
| Sheets | 侧边面板顶部异常宽的横条/竖线组合 | 对照 handle 形态、方向、尺寸及布局约束 |
| Navigation | 徽标/点被裁切，无选择过渡 | 检查 overflow/clip/indicator 与状态过渡，保留 selection/focus/automation |
| Date range | 范围带方角和 endpoint 露角 | 对照锁定范围形状与半格覆盖，保持 48 DIP hit target 与独立视觉几何 |
| Time picker | 选择区、冒号、AM/PM、钟面与 marker 布局疑似不符合规范 | 对照真实规范/固定 token/source，不以简易圆盘和普通按钮尺寸代替完整布局 |
| Carousel | 多图布局切换明显卡顿 | 区分重新构建/measure/文字格式化/clip/path 分配与所需布局过渡 |

## 验证原则

- 共性根因先修，不对每页分别追加补丁或将所有问题归因于像素舍入。
- 不未经验证全局关闭 stroke snapping；需要明确布局子像素、静态线条和文字栅格化的各自策略。
- 公共输入/布局/视觉 seam 已由 SPEC 和既有验证约定确认。新增独立 DPI/像素/bounds、真实中间帧/帧间隔/布局次数/分配指标与截图对照。
- 手工绘制轨道/状态几何不等于图标库；图标需真实资产与许可/来源/版本。
- 保留全部旧功能/API/兼容/worker/模态/生命周期回归。新版本不能指向旧的候选 EXE，人工复验必须有明确的新目录与版本。
- 修复之后交付新可运行预览，重新做人工视觉/交互复验；不要用测试总数给视觉问题结案。
