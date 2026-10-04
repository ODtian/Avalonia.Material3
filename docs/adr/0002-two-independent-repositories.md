# M3 控件库与 App 使用两个独立仓库

用户明确要求将 M3 库与本 App 拆成两个独立仓库。M3 仓库负责标准 Material 3 Expressive 令牌、组件、展厅、文档与包发布；App 仓库负责 Pixiv 业务、平台服务、正式页面与 Android 交付，并通过版本化包消费 M3 库。

两个仓库分别维护自己的规格、GitHub Issues、词汇表、ADR 与发布流程。依赖方向为 App → M3，跨仓库任务通过明确的版本契约和 Issue 链接协调。
