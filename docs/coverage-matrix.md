# Material 3 Expressive 覆盖矩阵

## 锁定基线

- 规范观察日期：**2026-10-04**；母规格 1.0；本矩阵基线 1。
- 规范入口：[Material 3 标准组件][components]、[Material 3 Expressive / Compose][expressive]。
- 可复现令牌参考：[AndroidX 固定提交 `11ece46a49d485c7644e53cb0684a611d7a0ec10`][tokens]。
  ButtonSmall / FilledButton 的生成版本为 `v0_11_0`；颜色与状态为 `v0_210`；TypeScale/Elevation/Motion 为 `v0_103`，Shape 为 `14_1_0`，标准/Expressive spring 为 `v0_14_0`。
- 对照实现：[Material Components Android 固定提交 `60ff09436d5d477a4b9d02940f31eb01e1250620`][android-buttons]。
- 色彩算法参考：[Material Color Utilities][mcu]；M3-02 固定 Google MCU npm 0.3.0 / `6bda88814da380664aaecc163ecdb8ac8caebb0a` 为独立 oracle，C# HCT 端依赖 MaterialColorUtilities 0.3.0；CorePalette.of + 本基线角色 tone 映射，不冒充较新的动态对比解析器。
- 框架：Avalonia **12.1.3**（稳定版）；目标框架 **net10.0**；SDK **10.0.112**；包 **0.1.0-preview.1**。

本表是完整交付清单，不是“全部完成”的声明。`初始` 表示仅交付本票明确的子集，`待交付` 必须由归属票据补上场景、状态、输入与无障碍证据。后续规范更新须更改基线并审查此表；不得静默跟随上游主分支。

## 设计令牌与共通能力

| 能力 | 本票状态 / 交付范围 | 后续归属 | 来源 / 证据 |
| --- | --- | --- | --- |
| 语义色彩角色、明暗主题 | 令牌已交付：49 角色及 Color/Brush 资源；保留六角色构造与默认值 | [M3-02 #3][i3] | ColorLightTokens、ColorDarkTokens、PaletteTokens；[M3-02 证据](verification/m3-02.md) |
| 种子色、动态色、完整方案及对比度 | 已交付：HCT/CorePalette.of、平台六调色板或角色方案输入、运行时优先级与恢复；默认对比度、WCAG 测量，不宣称自适应对比解析器 | [M3-02 #3][i3] | 16 完整角色方案、HCT 边界独立向量及真实输入；[契约](components/m3-02.md) |
| 字体、标准与强调字阶、混排、字体尺度 | 令牌已交付：30 角色的字体/字号/行高/字距/字重；100/150/200% 混排布局 headless 证据，真实平台字形与字体许可由宿主验收 | [M3-02 #3][i3] | TypeScaleTokens v0_103；全量独立参考资源向量；[M3-02](verification/m3-02.md) |
| 形状尺度、形态变化 | 完整圆角尺度与边缘令牌已交付；保留按钮常态/按下覆盖，组件形态动画归各组件票 | [M3-02 #3][i3]、[M3-03 #4][i4] | ShapeTokens 14_1_0；[契约与 Full/RTL 边界](components/m3-02.md) |
| 海拔与阴影 | 令牌已交付：0/1/3/6/8/12 DIP、可覆盖 Avalonia 阴影投影；不宣称与 Android 阴影像素相同 | [M3-02 #3][i3] | ElevationTokens v0_103；[M3-02](verification/m3-02.md) |
| 状态层 | 完整 hover/focus/press/drag 与禁用 alpha 输入已交付；初始按钮实时消费 hover/press/disabled，其他控件状态归各组件票 | [M3-02 #3][i3]、[M3-03 #4][i4] | StateTokens v0_210、FilledButtonTokens；真实指针场景 |
| 动效、Expressive 弹簧、减少动效 | 令牌已交付：16 时长、10 easing、标准与 Expressive 各 6 spring 对；减少动效将有效时长归零/弹簧标为瞬时。组件 spring 驱动与形变播放不在令牌完成声明内 | [M3-02 #3][i3]、[M3-03 #4][i4]及各组件票 | MotionTokens v0_103、Standard/ExpressiveMotionTokens v0_14_0；[M3-02](verification/m3-02.md) |
| 密度、尺寸、窗口适配 | 初始：40 单位视觉高度、至少 48 单位触达；字体放大可增长 | [M3-19 #20][i20]及各组件票 | ButtonSmallTokens；触摸场景 |
| 鼠标、触摸、键盘、焦点与无障碍 | 初始按钮：点击、取消、Tab、Enter/Space、可见焦点、Button peer 的名称/角色/禁用语义 | [M3-19 #20][i20]及各组件票 | `ButtonScenarioTests`、`ContractScenarioTests`；真实 Windows UI Automation |
| 模板、属性、内容插槽与命令 | 初始按钮：Avalonia 公开契约与 ContentTemplate/CommandParameter | [M3-19 #20][i20]及各组件票 | `Host_content_template_and_command_work_without_replacing_input_behavior` |
| AOT、裁剪、API 兼容与发布 | 待交付；本票仅普通桌面构建与本地预览包 | [M3-19 #20][i20] | M10、M11，不宣称已验收 |

## 全部标准组件与 Expressive 变体

| 标准组件 / 锁定范围 | 状态 | 归属票据 | 规范来源 |
| --- | --- | --- | --- |
| Buttons：filled、tonal、elevated、outlined、text；round/square、XS/S/M/L/XL、toggle | 初始：仅 small round filled 非切换按钮，其余待交付 | [M3-01 #2][i2] → [M3-03 #4][i4] | [Buttons][buttons]、ButtonXSmall/Small/Medium/Large/XLarge、Filled/Tonal/Elevated/Outlined/TextButtonTokens |
| Icon buttons：standard、filled、tonal、outlined；toggle、尺寸/宽度/形状 | 待交付 | [M3-03 #4][i4] | [Icon buttons][icon-buttons]、各尺寸 IconButtonTokens |
| FAB：标准、small/medium/large、扩展 FAB、FAB menu | 待交付 | [M3-04 #9][i9] | [FAB][fab]、[Extended FAB][extended-fab]、FabMenuBaselineTokens |
| Toolbars：docked、floating、水平/垂直、展开与收起 | 待交付 | [M3-04 #9][i9] | DockedToolbarTokens、FloatingToolbarTokens |
| Button groups：connected、非连接分组、选择与形变 | 待交付 | [M3-05 #10][i10] | ButtonGroupSmallTokens、ConnectedButtonGroupSmallTokens |
| Segmented buttons：单选、多选 | 待交付 | [M3-05 #10][i10] | [Segmented buttons][segmented]、OutlinedSegmentedButtonTokens |
| Split buttons：主操作、次级入口、全部尺寸 | 待交付 | [M3-05 #10][i10] | SplitButtonXSmall/Small/Medium/Large/XLargeTokens |
| Checkbox：二态、三态、错误/禁用 | 待交付 | [M3-06 #5][i5] | [Checkbox][checkbox]、CheckboxTokens |
| Radio buttons | 待交付 | [M3-06 #5][i5] | [Radio buttons][radio]、RadioButtonTokens |
| Switch | 待交付 | [M3-06 #5][i5] | [Switch][switch]、SwitchTokens |
| Text fields：filled、outlined；标签、辅助/错误文本、图标、计数、多行 | 已实现并通过真实编辑、包隔离与 Windows UIA 场景；实际读屏、平台中文 IME 候选会话待核验 | [M3-07 #6][i6] | [Text fields][text-fields]、Filled/OutlinedTextFieldTokens；[公开契约](components/m3-07.md)、[证据与限制](verification/m3-07.md) |
| Search：search bar、search view、建议/autocomplete | 待交付 | [M3-08 #11][i11] | [Search][search]、SearchBar/SearchView、Filled/OutlinedAutocompleteTokens |
| Chips：assist、filter、input、suggestion | 待交付 | [M3-08 #11][i11] | [Chips][chips]、Assist/Filter/Input/SuggestionChipTokens |
| Cards：elevated、filled、outlined、交互 | 待交付 | [M3-09 #7][i7] | [Cards][cards]、Elevated/Filled/OutlinedCardTokens |
| Lists：一/二/三行、leading/trailing、expanded、reorder、reveal | 待交付 | [M3-09 #7][i7] | [Lists][lists]、List/ExpandedList/ReorderList/RevealListTokens |
| Badges：点、计数 | 待交付 | [M3-09 #7][i7] | [Badges][badges]、BadgeTokens |
| Dividers：水平、垂直、inset | 待交付 | [M3-09 #7][i7] | [Dividers][dividers]、DividerTokens |
| Sliders：单值、范围、连续、离散、标记 | 待交付 | [M3-10 #8][i8] | [Sliders][sliders]、SliderTokens |
| Progress indicators：线性、圆形、确定/不确定、Expressive 波形 | 待交付 | [M3-11 #12][i12] | [Progress][progress]、Linear/CircularProgressIndicatorTokens |
| Loading indicator：Expressive 形状循环 | 待交付 | [M3-11 #12][i12] | LoadingIndicatorTokens |
| Dialogs：basic、全屏；打开/确认/取消/焦点返回 | 待交付 | [M3-12 #13][i13] | [Dialogs][dialogs]、DialogTokens |
| Menus：标准、vibrant、segmented、子菜单、锚定/上下文 | 待交付 | [M3-13 #15][i15] | [Menus][menus]、Menu/StandardMenu/VibrantMenu/SegmentedMenuTokens |
| Tooltips：plain、rich | 待交付 | [M3-13 #15][i15] | [Tooltips][tooltips]、Plain/RichTooltipTokens |
| Snackbar：消息、行动、消失 | 待交付 | [M3-13 #15][i15] | [Snackbar][snackbar]、SnackbarTokens |
| Bottom sheets：standard、modal、drag handle | 待交付 | [M3-14 #16][i16] | [Bottom sheets][bottom-sheets]、SheetBottom/DragHandle/ScrimTokens |
| Side sheets：standard、modal | 待交付 | [M3-14 #16][i16] | [Side sheets][side-sheets] |
| Navigation bar：水平/垂直 item、选中、徽标 | 待交付 | [M3-15 #14][i14] | [Navigation bar][navigation-bar]、NavigationBar*Tokens |
| Navigation rail：collapsed、expanded、水平/垂直 item | 待交付 | [M3-15 #14][i14] | [Navigation rail][navigation-rail]、NavigationRail*Tokens |
| Tabs：primary、secondary、固定/滚动 | 待交付 | [M3-15 #14][i14] | [Tabs][tabs]、Primary/SecondaryNavigationTabTokens |
| Top app bars：small、medium、large、center-aligned、flexible | 待交付 | [M3-16 #17][i17] | [Top app bar][top-app-bar]、AppBar*Tokens |
| Bottom app bar | 待交付 | [M3-16 #17][i17] | [Bottom app bar][bottom-app-bar]、BottomAppBarTokens |
| Navigation drawer：standard、modal | 待交付 | [M3-16 #17][i17] | [Navigation drawer][navigation-drawer]、NavigationDrawerTokens |
| Date pickers：modal、docked、单日/范围、date input | 待交付 | [M3-17 #18][i18] | [Date pickers][date-pickers]、DatePickerModal/DateInputModalTokens |
| Time pickers：钟面、键盘输入、12/24 小时 | 待交付 | [M3-17 #18][i18] | [Time pickers][time-pickers]、TimePicker/TimeInputTokens |
| Carousel：multi-browse、uncontained、hero、full-screen | 待交付 | [M3-18 #19][i19] | [Carousel][carousel] |
| Pull-to-refresh：手势、指示器、内容更新 | 待交付 | [M3-18 #19][i19] | [Expressive Compose][expressive] |
| 完整展厅、包文档、宿主矩阵、API/版本兼容、AOT/裁剪及发布 | 初始展厅与本地包已交付；完整验收待交付 | [M3-19 #20][i20] | [验证记录](verification/m3-01.md)、[消费契约](public-contract.md) |

[components]: https://m3.material.io/components
[expressive]: https://developer.android.com/develop/ui/compose/designsystems/material3
[tokens]: https://github.com/androidx/androidx/tree/11ece46a49d485c7644e53cb0684a611d7a0ec10/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/tokens
[small-button]: https://github.com/androidx/androidx/blob/11ece46a49d485c7644e53cb0684a611d7a0ec10/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/tokens/ButtonSmallTokens.kt
[android-buttons]: https://github.com/material-components/material-components-android/blob/60ff09436d5d477a4b9d02940f31eb01e1250620/lib/java/com/google/android/material/button/res/values/styles.xml
[mcu]: https://github.com/material-foundation/material-color-utilities
[buttons]: https://m3.material.io/components/buttons/overview
[icon-buttons]: https://m3.material.io/components/icon-buttons/overview
[fab]: https://m3.material.io/components/floating-action-button/overview
[extended-fab]: https://m3.material.io/components/extended-fab/overview
[segmented]: https://m3.material.io/components/segmented-buttons/overview
[checkbox]: https://m3.material.io/components/checkbox/overview
[radio]: https://m3.material.io/components/radio-button/overview
[switch]: https://m3.material.io/components/switch/overview
[text-fields]: https://m3.material.io/components/text-fields/overview
[search]: https://m3.material.io/components/search/overview
[chips]: https://m3.material.io/components/chips/overview
[cards]: https://m3.material.io/components/cards/overview
[lists]: https://m3.material.io/components/lists/overview
[badges]: https://m3.material.io/components/badges/overview
[dividers]: https://m3.material.io/components/divider/overview
[sliders]: https://m3.material.io/components/sliders/overview
[progress]: https://m3.material.io/components/progress-indicators/overview
[dialogs]: https://m3.material.io/components/dialogs/overview
[menus]: https://m3.material.io/components/menus/overview
[tooltips]: https://m3.material.io/components/tooltips/overview
[snackbar]: https://m3.material.io/components/snackbar/overview
[bottom-sheets]: https://m3.material.io/components/bottom-sheets/overview
[side-sheets]: https://m3.material.io/components/side-sheets/overview
[navigation-bar]: https://m3.material.io/components/navigation-bar/overview
[navigation-rail]: https://m3.material.io/components/navigation-rail/overview
[tabs]: https://m3.material.io/components/tabs/overview
[top-app-bar]: https://m3.material.io/components/top-app-bar/overview
[bottom-app-bar]: https://m3.material.io/components/bottom-app-bar/overview
[navigation-drawer]: https://m3.material.io/components/navigation-drawer/overview
[date-pickers]: https://m3.material.io/components/date-pickers/overview
[time-pickers]: https://m3.material.io/components/time-pickers/overview
[carousel]: https://m3.material.io/components/carousel/overview
[i2]: https://github.com/ODtian/Avalonia.Material3/issues/2
[i3]: https://github.com/ODtian/Avalonia.Material3/issues/3
[i4]: https://github.com/ODtian/Avalonia.Material3/issues/4
[i5]: https://github.com/ODtian/Avalonia.Material3/issues/5
[i6]: https://github.com/ODtian/Avalonia.Material3/issues/6
[i7]: https://github.com/ODtian/Avalonia.Material3/issues/7
[i8]: https://github.com/ODtian/Avalonia.Material3/issues/8
[i9]: https://github.com/ODtian/Avalonia.Material3/issues/9
[i10]: https://github.com/ODtian/Avalonia.Material3/issues/10
[i11]: https://github.com/ODtian/Avalonia.Material3/issues/11
[i12]: https://github.com/ODtian/Avalonia.Material3/issues/12
[i13]: https://github.com/ODtian/Avalonia.Material3/issues/13
[i14]: https://github.com/ODtian/Avalonia.Material3/issues/14
[i15]: https://github.com/ODtian/Avalonia.Material3/issues/15
[i16]: https://github.com/ODtian/Avalonia.Material3/issues/16
[i17]: https://github.com/ODtian/Avalonia.Material3/issues/17
[i18]: https://github.com/ODtian/Avalonia.Material3/issues/18
[i19]: https://github.com/ODtian/Avalonia.Material3/issues/19
[i20]: https://github.com/ODtian/Avalonia.Material3/issues/20
