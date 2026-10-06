# 公开包消费契约 — 0.1.0-preview.2

完整锁定范围与证据见[覆盖矩阵](coverage-matrix.md)及[最终验证](verification/m3-19.md)。这是本地预览候选，不声称公共 feed 已发布、全部平台认证或稳定 API。历史 #2 文档冻结于 [preview.1 初始契约](compatibility/preview1-contract.md)。

## 安装 / 初始化

```xml
<PackageReference Include="Avalonia.Material3" Version="0.1.0-preview.2" />
<PackageReference Include="Avalonia.Desktop" Version="12.1.3" />
```

目标 net10.0，SDK10.0.112。仓库开发 feed 将 M3 映射到本地 `artifacts/packages`；默认缓存不污染用户全局缓存。发布/独立消费必须明确版本/包源，禁止库 ProjectReference。隔离脚本生成自己的 public-only 配置，只复制本次**确切包**，验证恢复的 nupkg 哈希、资产版本/类型与项目依赖闭包。不要把旧 preview.1 同名开发包当作冻结 #2 包。

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:m3="using:Avalonia.Material3.Themes" RequestedThemeVariant="Light">
  <Application.Styles><m3:MaterialTheme /></Application.Styles>
</Application>
```

**在创建内容之前**安装主题；UI 线程设置控件/主题属性。无需 FluentTheme。字体默认平台字体，无捆绑 Roboto/中文字体；字体资源、许可、fallback 与平台输入/Back/IME/insets 由宿主提供。

## 完整组件契约

组件命名空间 `Avalonia.Material3.Controls`，令牌 `Avalonia.Material3.Tokens`，主题 `Avalonia.Material3.Themes`。公开属性/插槽、状态、输入/自动化语义、模板部件与明确投影详见：

| 家族 | 权威契约 |
| --- | --- |
| 49 颜色角色、种子/平台色、30 字阶、形状/海拔/状态/动效 | [M3-02](components/m3-02.md) |
| Buttons / Icon buttons | [M3-03](components/m3-03.md) |
| FAB / 扩展 / menu / docked & floating toolbar | [M3-04](components/m3-04.md) |
| Groups / segmented / split | [M3-05](components/m3-05.md) |
| Checkbox / radio / switch 与表单 | [M3-06](components/m3-06.md) |
| Text fields / 原生编辑 / 校验 | [M3-07](components/m3-07.md) |
| Search / autocomplete / Chips | [M3-08](components/m3-08.md) |
| Cards / lists / badges / dividers | [M3-09](components/m3-09.md) |
| Slider / range / settings | [M3-10](components/m3-10.md) |
| Progress / Expressive loading | [M3-11](components/m3-11.md) |
| Dialog / shared root Overlay | [M3-12](components/m3-12.md) |
| Menu / tooltip / Snackbar | [M3-13](components/m3-13.md) |
| Standard / modal bottom & side sheet | [M3-14](components/m3-14.md) |
| Navigation bar / rail / tabs | [M3-15](components/m3-15.md) |
| Top/bottom app bar / standard/modal drawer | [M3-16](components/m3-16.md) |
| Date / range / time / input | [M3-17](components/m3-17.md) |
| Carousel / pull-to-refresh | [M3-18](components/m3-18.md) |

Avalonia 基类的 Content/ContentTemplate、命令/参数、局部样式优先级、Theme/Template 保持公开扩展边界。替换模板承担渲染/触达/状态视觉责任；不是依赖私有子元素的承诺。初始按钮 small/round/filled 默认、六颜色位置构造/Deconstruct/with、初始资源键保留；见[升级兼容](upgrade.md)。

## 聚合宿主契约

两个包应用都有显式静态17页、真实结果与运行时 theme/font/seed/platform/shape/motion/window 控制。一个**有界窗口根** MaterialOverlayHost 覆盖 header/nav/page；dialog/feedback/sheet/drawer/picker 页面借用此根，不覆盖根 Content 或嵌套 page-only modality。每次页面访问创建新 lifetime，移除旧页面先 detach；后台任务不能在离开后重新提交/抢焦点。

宿主 Back：有 OpenCount 时先转发 **且绝不路由底层页面**；RequestBack 返回 false 可能是 sheet collapse-first 或 close veto，而非未处理。运行时模态输入与 UIA 后台 gating 不改写原控件绑定。页面自己有 viewport 时不再套无界 ScrollViewer。320-DIP、字体100/150/200% 是此示例验证尺寸，不是所有产品设备/字体的认证。

## 构建 / API / 发布兼容

`verify.ps1` 为唯一共享场景/fixture/copy/身份门禁；`tests/ScenarioInventory.props` 声明编译/资源集，两个 bootstrap 不共享。source/package 的同名场景使用 Ordinal 身份比较，另有明确包应用 fixture。所有保留的组件 verifier（含 `verify-theme.ps1`）委托同一入口，保留各票 DesktopSmoke/evidence。可用 `-Manifest` 重验证当前 clean HEAD 的确切候选，复用其隔离缓存而不逐票再次复制／打包；stale commit/version/hash/资产或失败 TRX 仍被拒绝。复用时若指定 `-BaselinePackage`，必须验证冻结 archive 与该确切 producer 已成功执行的 SDK baseline/API 维度（实际 log/binlog hashes）；缺失维度／错误 archive 明确拒绝，绝不忽略请求后 PASS。API/resource 文本冻结在 `tests/ReferenceVectors/public-api.txt` / `resource-keys.txt`；SDK binary baseline 检查与旧编译消费者补充构造、XAML、资源与行为证据。

库实际启用 IsAotCompatible/trim/AOT 分析器；发布脚本以严格 IL 警告策略构建包-only Windows x64 NativeAOT 与 full-trim managed Gallery/独立宿主，必须运行 **published** executables。分析器/编译成功不是其他平台或读屏验收。实际命令、诊断、哈希/commit 和未满足项目见最终记录。版本/兼容政策：[升级](upgrade.md)，分发/授权：[发布](release.md)。
