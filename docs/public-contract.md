# 初始公开消费契约（0.1.0-preview.1）

这是 M3-01 的预览契约，不是完整 M3 Expressive API。完整交付范围见[覆盖矩阵](coverage-matrix.md)。

## 安装和初始化

依赖：net10.0、Avalonia 12.1.3；本票验证 Windows x64 普通桌面宿主。Android、其他真实平台、AOT、裁剪及稳定 API 兼容在后续票据验收。

```xml
<PackageReference Include="Avalonia.Material3" Version="0.1.0-preview.1" />
<PackageReference Include="Avalonia.Desktop" Version="12.1.3" />
```

首次运行先打包：

```powershell
dotnet pack src/Avalonia.Material3/Avalonia.Material3.csproj -c Release -o artifacts/packages
dotnet run --project samples/StandaloneHost -c Release
```

根目录 `NuGet.Config` 将 `Avalonia.Material3` **只映射到本地源**，其他依赖来自 nuget.org；默认使用仓库内 `artifacts/nuget` 缓存，不改动用户全局缓存。包尚未发布到公共源。复制宿主到其他目录时同时带上 `Directory.Build.props`、`global.json`、配置与本地包，或在自己的项目中明确指定上述版本和包源。不得以库的 ProjectReference 替代包消费。

在创建窗口和内容之前安装主题（推荐 App.Initialize 中加载此 XAML）：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:m3="using:Avalonia.Material3.Themes"
             RequestedThemeVariant="Light">
  <Application.Styles>
    <m3:MaterialTheme />
  </Application.Styles>
</Application>
```

主题提供最小 `Window` 宿主模板和 `MaterialButton` 的 ControlTheme，不需要 FluentTheme，也不将普通 `Avalonia.Controls.Button` 冒充已交付的 M3 控件。更完整的窗口/基础控件主题随各票据扩展。

## MaterialButton

命名空间：`Avalonia.Material3.Controls`。继承 Avalonia `Button`，保留其 StyledProperty、输入和 ButtonAutomationPeer：

```xml
<m3:MaterialButton xmlns:m3="using:Avalonia.Material3.Controls"
                   Content="Continue" Command="{Binding ContinueCommand}"
                   CommandParameter="confirmed" />
```

- 初始变体：small、round、filled、非切换；其余样式/尺寸/选中状态由 M3-03 提供。
- 视觉容器最小高度 40 DIP；整体命中区域至少 48 DIP。本模板为容纳 3 DIP 焦点环及 2 DIP 间距，默认外框为 50 DIP 高；内容增长时可扩大。
- `Content`、`ContentTemplate`、`Command`、`CommandParameter`、`Click`、`IsEnabled`、`Padding`、`Background`、`Foreground`、字体、圆角、`Template` 和 `Theme` 沿用 Avalonia 公开契约。局部值按 Avalonia 优先级覆盖主题；例如显式 Background 也会覆盖禁用样式，宿主需负责对应状态外观。
- 鼠标/触摸释放在按钮内激活，移出后释放取消；Tab/Shift+Tab 导航；Enter 激活，Space 按下反馈、释放激活；禁用不激活、不参与焦点导航。
- 悬停/按下状态层、禁用配色、键盘可见焦点；pressed 圆角按锁定规范变化。
- 可通过 `AutomationProperties.Name` 命名复杂内容；默认沿用 Button 的内容名称、角色、启用状态和 Invoke 行为。真实读屏完整验收不在本票。
- 自定义 Template 须自行保留内容展示、命中区域、状态层与焦点可见性；输入/命令/自动化由 Button 保持。

## MaterialTheme 与初始令牌

命名空间：`Avalonia.Material3.Themes` / `Avalonia.Material3.Tokens`。

```csharp
var theme = new MaterialTheme
{
    LightColorScheme = MaterialColorScheme.Light with { Primary = Color.Parse("#006C4C") },
    DarkColorScheme = MaterialColorScheme.Dark,
    Typography = new MaterialTypography { FontFamily = new FontFamily("Arial"), Scale = 1.5 },
    Shapes = new MaterialShapes { ButtonCornerRadius = 20, PressedButtonCornerRadius = 8 },
    Motion = new MaterialMotion { ReduceMotion = true }
};
app.Styles.Add(theme); // 创建窗口之前
```

所有输入都是 StyledProperty，支持绑定。令牌为不可变 record；运行时用新值/`with` 赋给主题，不修改共享默认对象。所有赋值在 UI 线程进行。请求主题使用 **Application 或 Window.RequestedThemeVariant**；不另造与 Avalonia 相冲突的明暗状态。

| 输入 | 默认 / 约束 |
| --- | --- |
| LightColorScheme / DarkColorScheme | 非 null；六个语义颜色角色。初始静态紫色调色板来自锁定 PaletteTokens |
| Typography | FontFamily.Default（宿主平台字体），Scale=1；字体非 null，尺度必须为可用的正有限数 |
| Shapes | 常态 20、按下 8 DIP；均匀圆角，非负有限数 |
| Motion | StateLayerDuration=100 ms；非负；ReduceMotion=true 将过渡时长置零 |

默认未打包 Roboto 或中文字体。宿主负责字体资源/许可及平台回退；完整字阶、字距、行高、Expressive 弹簧与形状动画仍待交付。初始状态层使用 Avalonia DoubleTransition，不宣称完整 Expressive motion scheme。

### 可消费资源

宿主使用 DynamicResource 可与当前主题同步；不要依赖模板子元素名称。

- 语义画刷：`M3.PrimaryBrush`、`M3.OnPrimaryBrush`、`M3.SurfaceBrush`、`M3.OnSurfaceBrush`、`M3.OnSurfaceVariantBrush`、`M3.OutlineBrush`。
- 派生禁用画刷：`M3.DisabledContainerBrush`（OnSurface × 0.10）、`M3.DisabledForegroundBrush`（OnSurfaceVariant × 0.38）。
- 字体：`M3.FontFamily`、`M3.LabelLargeFontSize`（14 × Scale）、`M3.BodyLargeFontSize`（16 × Scale）。按钮标签字重 500。
- 形状：`M3.ButtonCornerRadius`、`M3.PressedButtonCornerRadius`、`M3.ButtonFocusCornerRadius`、`M3.PressedButtonFocusCornerRadius`。
- 动效：`M3.StateLayerDuration`（TimeSpan）。

Light/Dark 使用 Avalonia ThemeDictionaries；更新色彩输入替换相应字典，现有 DynamicResource 自动更新。字体、形状与动效输入同样更新现有资源。

## 开发和验证

```powershell
pwsh ./scripts/verify.ps1
pwsh ./scripts/verify.ps1 -DesktopSmoke
```

脚本先验证源码入口、打包，再把两个消费宿主和测试复制到**没有 src 库项目**的新目录，用全新 NuGet 缓存恢复并验证。可加 `-KeepSandbox` 保留用于运行和检查；默认自动清理。

不要反复向常用 NuGet 缓存覆盖同一个已消费版本：修改包内容应更新 `Material3Version`，或使用上述隔离验证脚本。本地预览包不是公共发布/稳定 API 的承诺。验证记录见 [M3-01](verification/m3-01.md)。
