# Avalonia Material 3 Expressive

独立、通用、版本化的 Material 3 Expressive 控件库。当前交付 **M3-01 / Issue #2**：一个完整基础 filled button 场景、初始主题令牌、包消费者与展厅；**不是完整组件库**。

- .NET SDK **10.0.112** / net10.0
- Avalonia **12.1.3** 稳定版
- 本地 NuGet 包 **Avalonia.Material3 0.1.0-preview.1**

## 构建、验证、运行

需要 .NET SDK 和 PowerShell 7.2+：

```powershell
pwsh ./scripts/verify.ps1
# 交互式 Windows 桌面：额外执行真实宿主 UI Automation 验证
pwsh ./scripts/verify.ps1 -DesktopSmoke

# 运行展厅或独立包宿主
# 首次启动前先打包；两个应用都只引用版本化包，不引用库项目。
dotnet pack src/Avalonia.Material3/Avalonia.Material3.csproj -c Release -o artifacts/packages
dotnet run --project samples/Gallery -c Release
dotnet run --project samples/StandaloneHost -c Release
```

验证脚本在全新目录和 NuGet 缓存中恢复本地包、构建两个宿主并执行输入/主题场景；默认清理临时目录。结果与包在 `artifacts/`，不进入 Git。源码解决方案 `Avalonia.Material3.slnx` 仅包含库和源码场景测试，避免打包前循环恢复消费者。

## 文档

- [公开消费契约与主题输入](docs/public-contract.md)
- [锁定规范与完整覆盖矩阵](docs/coverage-matrix.md)
- [M3-01 验证证据](docs/verification/m3-01.md)
- [场景测试边界](docs/testing.md)
- [产品与实施规格](docs/SPEC.md)
- [实施票据索引](docs/TICKETS.md)
- [领域词汇](GLOSSARY.md)、[架构决策](docs/adr/)、[GitHub Issues 工作约定](docs/agents/issue-tracker.md)

通用设计系统不包含 Pixiv 业务。消费方 [MaterixivYou](https://github.com/ODtian/MaterixivYou) 通过明确包版本采用公开契约。母规格 [#1](https://github.com/ODtian/Avalonia.Material3/issues/1) 的后续组件、动态配色、AOT/裁剪及发布任务仍未完成。
