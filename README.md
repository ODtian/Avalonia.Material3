# Avalonia Material 3 Expressive

独立、通用、版本化的 Material 3 Expressive 控件库。当前本地候选为 **0.1.0-preview.5**，覆盖完整锁定组件清单：设计令牌、按钮/FAB/工具栏/分组、选择/输入/搜索/Chips、内容/导航、进度/加载、共享 overlay/对话框/菜单/提示/面板、应用栏/抽屉、日期时间与 Carousel/刷新。17 个页面由静态工厂显式注册，Gallery 与独立包宿主访问相同完整展厅。

preview.5 提供纯色扩散与渐变闪光两套 Ripple，修复日期范围的动态排列，并按官方构造校正日期时间控件。逐项证据见[反馈验收记录](docs/verification/manual-quality-feedback.md)。

这是**本地预览候选**，不是已发布到 nuget.org 的声明；也不等于母规格全部验收。真实 App 升级、人工读屏、物理移动设备及 Android 验收仍有明确外部条件，见[最终证据与支持矩阵](docs/verification/m3-19.md)。各组件的 Avalonia 投影边界仍适用，不宣称 Compose 内部物理/帧像素等价。

- .NET SDK **10.0.112** / net10.0；Avalonia **12.1.3**
- 本地包 **Avalonia.Material3 0.1.0-preview.5**；包消费者不引用库源码
- `Directory.Build.props` 是唯一版本来源；历史 preview.1 / preview.2 记录保留

## 构建、验证、运行

```powershell
pwsh ./scripts/verify.ps1 -KeepSandbox
# 输出 artifacts/release-<unique>/manifest.json：确切包/hash、全新缓存、场景身份清单
# 可提供不可变旧包路径，启用 SDK API/package 基线检查
pwsh ./scripts/verify.ps1 -KeepSandbox -BaselinePackage <archived-preview.1.nupkg>

# package-only Windows x64 NativeAOT + full-trim managed，各两个完整宿主
pwsh ./scripts/publish-release.ps1 -Manifest <manifest.json>
# 在交互式桌面上串行运行已发布二进制 UIA/键鼠/多次进程冷启动
pwsh ./scripts/publish-release.ps1 -Manifest <manifest.json> -NativeSmoke
pwsh ./scripts/verify-compatibility.ps1 -Manifest <manifest.json> -BaselinePackage <archived-preview.1.nupkg>

# 开发期首次先打包。改包内容须更新版本或使用隔离脚本，勿重用常用缓存同版本。
dotnet pack src/Avalonia.Material3/Avalonia.Material3.csproj -c Release -o artifacts/packages
dotnet run --project samples/Gallery -c Release
dotnet run --project samples/StandaloneHost -c Release
```

NativeSmoke 操作真实桌面焦点/键盘，不能并行运行。发布门禁不添加整程序集保留根或全局 IL 告警抑制。独立 SDK 兼容 fixture 验证旧编译客户端，不冒充实际 App。

## 文档

- [公开契约、安装与组件索引](docs/public-contract.md)
- [锁定规范、逐项组件证据](docs/coverage-matrix.md)
- [最终本地验收 / 平台矩阵 / 未满足项](docs/verification/m3-19.md)
- [升级与回滚](docs/upgrade.md)、[发布过程](docs/release.md)、[变更记录](CHANGELOG.md)
- [场景测试边界](docs/testing.md)、[规格](docs/SPEC.md)、[票据图](docs/TICKETS.md)
- [历史初始契约](docs/compatibility/preview1-contract.md)、[M3-01 历史验证](docs/verification/m3-01.md)
- [第三方归属](THIRD-PARTY-NOTICES.md)、[领域词汇](GLOSSARY.md)、[ADR](docs/adr/)

通用设计系统不包含 Pixiv 业务。实际消费方 [MaterixivYou](https://github.com/ODtian/MaterixivYou) 通过版本化包采用公开契约；其 docs-only 基线不是可执行消费方，M11 不能由本仓库样例替代。

本轮标准动效、阴影和几何修复与新版展厅：[preview.5 复验记录](docs/verification/manual-quality-preview5.md)。此前产物与指标见 [preview.3 记录](docs/verification/manual-quality-preview3.md)。
