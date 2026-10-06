# Spec #1 集成交付与验收状态

## 结论

**M3 库内实现、Windows x64 包消费/AOT/裁剪和本轮代码审查修复完成；母规格 #1 与发布验收 #20 的整体状态仍为部分验收。** 不将独立宿主当作真实 App，不将 UIA 当作人工读屏或物理设备证据，不声称已上传公共包源。

集成分支：`integration/m3-spec-1`；本轮代码集成提交：`24b99edc8cfe23cc2a79c9067ef4f74e96eae849`。`main` 未合并。#2 是既有交付；#3–#19 的组件实现、展厅场景、公开契约、验证记录及审查修复已集成。

## 版本与产物来源

候选版本：**`Avalonia.Material3 0.1.0-preview.2`**；Avalonia **12.1.3**，.NET SDK **10.0.112**，目标 **net10.0**。

| 产物 | Producer | 包 SHA256 | 证据 |
| --- | --- | --- | --- |
| 完整执行原生发布协议的候选包 | `3032585ff1903caf10ed41c0d5e6a5e68ade192e` | `4613A5A44E94116F6D2E1CC7C68AD538FF68C6327802465F2222E9C8AD565B4B` | 716/740、兼容门禁、四种 published host 完成 |
| 集成后重新打包的包 | `24b99edc8cfe23cc2a79c9067ef4f74e96eae849` | `F96E33DCA1021BE5684E26A24AD3CB9A7CDBEA6C7898620CF3AFE9825B939694` | 独立复跑 716/740、SDK/API/资源/旧客户端、15 个入口与 8 个门禁探针 |

以上两个提交的源码树相同：`d4e31a3fe11b25869dc97b713d7051077190b0f8`。**不同包的 Git/版本元数据和哈希不得混用；303 的原生结果不改称为 24b99 包的原生运行。** 本文之后的文档记录提交也不改变这些 producer 标识。

冻结 `0.1.0-preview.1` 基线包 SHA256：`C52D2E60A9E508ACF7EA1915EFBD5A84E7508AD39D8662453793759AC44174B6`，未覆盖原包或用户全局缓存。

## 验证

- **716 源码场景、740 全新隔离包消费场景通过，0 失败、0 跳过。** Ordinal 身份并集保留 667 个原始共享场景，新增 49 个共享回归；24 个独立消费场景全部保留。
- 公开 API/资源快照、真实 SDK package/API 基线、参数名兼容、原六颜色 constructor/Deconstruct/`with` 和初始行为通过。
- 旧客户端与编译 XAML 先使用旧包，再只换 M3 DLL而不重编客户端，最后同源码重编新包：均通过。
- 15 个仍被文档引用的普通组件验证入口通过同一精确包/缓存与权威场景清单；请求 SDK 基线的复用门禁不能忽略基线，正/负探针通过。
- 静态注册的完整 Gallery 与独立包宿主覆盖 **17 页**，使用窗口级 overlay、有限 viewport、页面取消 generation、全局模态与 Back/焦点契约。
- Windows x64 的 Gallery/StandaloneHost 分别完成 **NativeAOT 与 full-trim managed** 发布及真实 published EXE 协议：严格 IL/链接门禁、全部页面与延迟形式、UIA/键盘/鼠标/结果、主题/种子/平台调色输入、形状/字体/减少动效、窗口变化、200% 窄窗重访、每宿主五次新进程启动。
- 最后一次发布命令完成四个编译，其中 Standalone/managed 原始运行在 Favorite:selected 等待超时。保留失败 JSON/截图/日志；**同一已发布 EXE、同一断言与脚本的完整重试通过**。记录 originalPublisherExit=1 和 resumedNativeCompletion=true，不声称最后一轮不中断退出 0。
- Gallery/AOT 的两个非 owned-foreground 截图安全跳过；相应路由/运行断言通过，但不声称缺失图片存在。
- 启动测量使用新进程但 OS 缓存温热，不是重启/清缓存基准，也没有未约定的性能阈值。

## 两轴审查

### Standards

原始 4 个契约问题、1 个纯弹簧数学重复的判断性建议，以及复核新增的基线复用门禁问题均修复；独立静态复核 **0 个库内问题未解决**。公开回归覆盖嵌套滑块输入归属、必选组可用性、原生 worker 查询、旧入口权威委托和基线请求。

### Spec

原始 7 个交互/状态问题与 1 个文档问题均修复。物理 RTL 疑点被独立窗口 AABB/像素/指针 oracle 实际重现并修复；再次复核发现的 Always 范围值标签问题也修复。独立静态复核 **0 个库内问题未解决**。新增回归不使用重复镜像的 owner-local 预期坐标。

详见 [审查修复记录](../review-fixes.md)、[M3-19 历史与发布验证](m3-19.md)。未要求或宣称像素/内部引擎完全克隆 Compose。

## 整体验收仍开放的条件

| 条件 | 当前状态 / 后续责任 |
| --- | --- |
| M11 真实 App 包升级 | App 当前为 docs-only `4fda55f`，无可执行项目、PackageReference 和旧→新场景。由 [App #3](https://github.com/ODtian/MaterixivYou/issues/3) 提供早期真实消费者，关联 [App spec #1](https://github.com/ODtian/MaterixivYou/issues/1) 与 [App #27](https://github.com/ODtian/MaterixivYou/issues/27)；独立兼容 fixture 不替代 App |
| 人工读屏 | 尚无指定 Narrator/NVDA 版本的实际朗读、顺序、错误和 live announcement 观察；UIA 元数据/操作证据单独记录 |
| 物理触摸/多接触/笔 | 尚无真实设备矩阵验证；routed touch 与鼠标证据不扩大为硬件认证 |
| Android/其他 native 平台 | Android 消费项目、OS/ABI/SDK-NDK/device 范围和生命周期/IME/insets/TalkBack 仍需消费方交付；Windows 与 Linux headless CI 不自动认证其他后端 |
| 公共分发与项目许可 | 当前为明确版本的本地候选包；未冒称公共 feed/tag/许可决策完成。第三方归属不等于项目许可授予 |

因此只按真实完成的组件票据记录实施交付，**不虚假关闭 #1/#20 的整体验收**。源文件、包/manifest、TRX、编译日志、原生目录完整哈希及成功/失败记录均保留，清理工作树之前已经移到独立保存目录。
