# 实施票据索引

母规格：[Spec #1](https://github.com/ODtian/Avalonia.Material3/issues/1)。
共 19 张已批准的实施票据，分诊标签为 ready-for-agent。每票覆盖完整交付路径与场景验收。

| 代号 | Issue | 交付行为 | 直接阻塞 |
| --- | --- | --- | --- |
| M3-01 | [#2 — 主题按钮从控件包到独立宿主](https://github.com/ODtian/Avalonia.Material3/issues/2) | 独立宿主安装本地版本化包后，显示一个标准按钮；用户点击得到反馈并切换基础明暗主题，展厅与包消费者使用相同公开契约。 | 可立即开始 |
| M3-02 | [#3 — 种子色与动态主题驱动完整色彩方案](https://github.com/ODtian/Avalonia.Material3/issues/3) | 宿主输入种子色、平台调色数据和明暗偏好，展厅中的真实组件实时使用对应的色彩方案、字体和形状令牌。 | [M3-01](https://github.com/ODtian/Avalonia.Material3/issues/2) |
| M3-03 | [#4 — 标准按钮与图标按钮完成操作反馈](https://github.com/ODtian/Avalonia.Material3/issues/4) | 用户在操作示例中使用各类按钮和图标按钮，得到对应的按下、选中、焦点和禁用反馈。 | [M3-01](https://github.com/ODtian/Avalonia.Material3/issues/2) |
| M3-04 | [#9 — 悬浮操作与工具栏在场景中可触达](https://github.com/ODtian/Avalonia.Material3/issues/9) | 宿主场景展示标准悬浮操作与工具栏，用户可展开操作、选择行动并得到一致的动效及焦点反馈。 | [M3-03](https://github.com/ODtian/Avalonia.Material3/issues/4) |
| M3-05 | [#10 — 按钮组与分段操作表达选择关系](https://github.com/ODtian/Avalonia.Material3/issues/10) | 用户通过按钮组、分段与拆分操作选择选项，宿主读到明确的选择结果并更新内容。 | [M3-03](https://github.com/ODtian/Avalonia.Material3/issues/4) |
| M3-06 | [#5 — 复选单选与开关驱动表单状态](https://github.com/ODtian/Avalonia.Material3/issues/5) | 用户在通用表单中使用复选、单选和开关，表单数据与控件状态同步，并能保存和恢复示例输入。 | [M3-01](https://github.com/ODtian/Avalonia.Material3/issues/2) |
| M3-07 | [#6 — 文本输入从编辑到校验结果](https://github.com/ODtian/Avalonia.Material3/issues/6) | 用户在通用编辑场景输入文本、触发校验并修正错误，输入值、标签和反馈保持一致。 | [M3-01](https://github.com/ODtian/Avalonia.Material3/issues/2) |
| M3-08 | [#11 — 搜索与词条 Chips 组成可编辑查询](https://github.com/ODtian/Avalonia.Material3/issues/11) | 用户在通用搜索示例中添加、选择、删除词条并显式提交查询，宿主收到完整的词条与搜索意图。 | [M3-07](https://github.com/ODtian/Avalonia.Material3/issues/6), [M3-03](https://github.com/ODtian/Avalonia.Material3/issues/4) |
| M3-09 | [#7 — 卡片列表与徽标呈现完整内容层级](https://github.com/ODtian/Avalonia.Material3/issues/7) | 通用内容列表展示卡片、列表项和徽标；用户选择内容或执行条目操作，宿主更新对应结果。 | [M3-01](https://github.com/ODtian/Avalonia.Material3/issues/2) |
| M3-10 | [#8 — 滑块控制单值范围与设置预览](https://github.com/ODtian/Avalonia.Material3/issues/8) | 用户在设置示例中调整单值或范围，预览随输入更新，示例再次打开时恢复选定值。 | [M3-01](https://github.com/ODtian/Avalonia.Material3/issues/2) |
| M3-11 | [#12 — 进度与 Expressive 加载反馈贯通异步操作](https://github.com/ODtian/Avalonia.Material3/issues/12) | 用户启动通用异步示例，看到标准进度或 Expressive 加载反馈，任务完成或失败后看到对应结果。 | [M3-03](https://github.com/ODtian/Avalonia.Material3/issues/4) |
| M3-12 | [#13 — 对话框从打开到确认并返回焦点](https://github.com/ODtian/Avalonia.Material3/issues/13) | 用户打开标准对话框，编辑或确认内容，宿主获得结果并将焦点还给正确的入口。 | [M3-03](https://github.com/ODtian/Avalonia.Material3/issues/4) |
| M3-13 | [#15 — 菜单提示与 Snackbar 完成次级操作反馈](https://github.com/ODtian/Avalonia.Material3/issues/15) | 用户从菜单选择操作，查看提示和 Snackbar，并执行反馈中的行动，得到正确的宿主结果。 | [M3-12](https://github.com/ODtian/Avalonia.Material3/issues/13) |
| M3-14 | [#16 — 底部与侧边信息面板完成拖动和展开](https://github.com/ODtian/Avalonia.Material3/issues/16) | 用户在通用宿主中打开、拖动和展开标准信息面板，常驻与模态布局按宿主的公开配置工作。 | [M3-12](https://github.com/ODtian/Avalonia.Material3/issues/13) |
| M3-15 | [#14 — 导航栏轨道与 Tabs 切换实际内容](https://github.com/ODtian/Avalonia.Material3/issues/14) | 用户在通用示例中切换导航或页签，当前内容与选中状态同步，并适配不同窗口宽度。 | [M3-03](https://github.com/ODtian/Avalonia.Material3/issues/4) |
| M3-16 | [#17 — 应用栏与导航抽屉贯通页面操作](https://github.com/ODtian/Avalonia.Material3/issues/17) | 用户通过应用栏和导航抽屉进入示例页面、执行页内操作并返回，导航与页面状态保持一致。 | [M3-15](https://github.com/ODtian/Avalonia.Material3/issues/14), [M3-12](https://github.com/ODtian/Avalonia.Material3/issues/13) |
| M3-17 | [#18 — 日期时间选择从输入到表单结果](https://github.com/ODtian/Avalonia.Material3/issues/18) | 用户在表单中选择或输入日期和时间，修正边界错误并确认，宿主得到对应值。 | [M3-12](https://github.com/ODtian/Avalonia.Material3/issues/13), [M3-07](https://github.com/ODtian/Avalonia.Material3/issues/6) |
| M3-18 | [#19 — Carousel 与下拉刷新推动内容更新](https://github.com/ODtian/Avalonia.Material3/issues/19) | 用户浏览通用图片集合并下拉刷新，宿主更新内容，位置与加载反馈保持连续。 | [M3-09](https://github.com/ODtian/Avalonia.Material3/issues/7), [M3-11](https://github.com/ODtian/Avalonia.Material3/issues/12) |
| M3-19 | [#20 — 独立宿主消费完整控件包并完成发布验收](https://github.com/ODtian/Avalonia.Material3/issues/20) | 全新宿主从明确版本的包安装到访问完整组件展厅，完成主题、输入、AOT 和兼容验证，并获得可复用的发布包及升级说明。 | [M3-02](https://github.com/ODtian/Avalonia.Material3/issues/3), [M3-04](https://github.com/ODtian/Avalonia.Material3/issues/9), [M3-05](https://github.com/ODtian/Avalonia.Material3/issues/10), [M3-06](https://github.com/ODtian/Avalonia.Material3/issues/5), [M3-08](https://github.com/ODtian/Avalonia.Material3/issues/11), [M3-10](https://github.com/ODtian/Avalonia.Material3/issues/8), [M3-13](https://github.com/ODtian/Avalonia.Material3/issues/15), [M3-14](https://github.com/ODtian/Avalonia.Material3/issues/16), [M3-16](https://github.com/ODtian/Avalonia.Material3/issues/17), [M3-17](https://github.com/ODtian/Avalonia.Material3/issues/18), [M3-18](https://github.com/ODtian/Avalonia.Material3/issues/19) |

## 已交付

- [M3-01 / #2 — 主题按钮从控件包到独立宿主](https://github.com/ODtian/Avalonia.Material3/issues/2)：初始包 `0.1.0-preview.1`，见[验证记录](verification/m3-01.md)和[公开消费契约](public-contract.md)。

## M3-01 完成后的就绪前沿

本批只执行 #2；以下票据等待后续授权开始：

- [M3-02 / #3 — 种子色与动态主题驱动完整色彩方案](https://github.com/ODtian/Avalonia.Material3/issues/3)
- [M3-03 / #4 — 标准按钮与图标按钮完成操作反馈](https://github.com/ODtian/Avalonia.Material3/issues/4)
- [M3-06 / #5 — 复选单选与开关驱动表单状态](https://github.com/ODtian/Avalonia.Material3/issues/5)
- [M3-07 / #6 — 文本输入从编辑到校验结果](https://github.com/ODtian/Avalonia.Material3/issues/6)
- [M3-09 / #7 — 卡片列表与徽标呈现完整内容层级](https://github.com/ODtian/Avalonia.Material3/issues/7)
- [M3-10 / #8 — 滑块控制单值范围与设置预览](https://github.com/ODtian/Avalonia.Material3/issues/8)

## 推进规则

后续根据 GitHub 原生 blocked-by 关系和实际完成状态选择可开始任务。App 消费对应能力的 M3 版本化包，跨仓库依赖使用完整 Issue 链接。
当前阶段由主代理直接推进，各任务结果与验收记录写入对应仓库和 Issue。
