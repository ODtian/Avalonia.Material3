# Material 3 Expressive 覆盖矩阵

## 锁定基线

- 规范观察日期：**2026-10-04**；母规格 1.0；本矩阵基线 1。
- 规范入口：[Material 3 标准组件][components]、[Material 3 Expressive / Compose][expressive]。
- 可复现令牌参考：[AndroidX 固定提交 `11ece46a49d485c7644e53cb0684a611d7a0ec10`][tokens]。
  ButtonSmall / FilledButton 的生成版本为 `v0_11_0`；颜色、字阶和状态为 `v0_210`。
- 对照实现：[Material Components Android 固定提交 `60ff09436d5d477a4b9d02940f31eb01e1250620`][android-buttons]。
- 色彩算法参考：[Material Color Utilities][mcu]；算法实现与固定版本／向量在 M3-02 交付，不将本票的默认调色板冒充动态算法。
- 框架：Avalonia **12.1.3**（稳定版）；目标框架 **net10.0**；SDK **10.0.112**；包 **0.1.0-preview.1**。

本表是完整交付清单，不是“全部完成”的声明。`初始` 表示仅交付本票明确的子集，`待交付` 必须由归属票据补上场景、状态、输入与无障碍证据。后续规范更新须更改基线并审查此表；不得静默跟随上游主分支。

## 设计令牌与共通能力

| 能力 | 本票状态 / 交付范围 | 后续归属 | 来源 / 证据 |
| --- | --- | --- | --- |
| 语义色彩角色、明暗主题 | 初始：Primary、OnPrimary、Surface、OnSurface、OnSurfaceVariant、Outline | [M3-02 #3][i3] | ColorLightTokens、ColorDarkTokens、PaletteTokens；`Existing_button_follows_host_light_dark_and_light_changes` |
| 种子色、动态色、完整方案及对比度 | 待交付 | [M3-02 #3][i3] | [MCU][mcu] |
| 字体、标准与强调字阶、混排、字体尺度 | 初始：宿主字体、尺度、14/500 按钮标签与 16 正文字号；完整字阶、行高、字距及强调角色待交付 | [M3-02 #3][i3] | TypeScaleTokens；`Host_updates_initial_design_tokens_on_existing_content` |
| 形状尺度、形态变化 | 初始：小按钮 20 圆角、按下 8 圆角、宿主覆盖 | [M3-02 #3][i3]、[M3-03 #4][i4] | [ButtonSmallTokens][small-button]、ShapeTokens |
| 海拔与阴影 | 初始：filled button 常态 0、悬停 1 的基础阴影；完整体系待交付 | [M3-02 #3][i3] | FilledButtonTokens、ElevationTokens |
| 状态层 | 初始：悬停 0.08、按下 0.10；禁用容器 0.10、标签 0.38 | [M3-02 #3][i3]、[M3-03 #4][i4] | StateTokens、FilledButtonTokens；指针与禁用场景 |
| 动效、Expressive 弹簧、减少动效 | 初始：100 ms 状态层过渡及 ReduceMotion；完整弹簧方案与形状动画待交付 | [M3-02 #3][i3]、[M3-03 #4][i4] | MotionTokens、ExpressiveMotionTokens |
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
| Text fields：filled、outlined；标签、辅助/错误文本、图标、计数、多行 | 待交付 | [M3-07 #6][i6] | [Text fields][text-fields]、Filled/OutlinedTextFieldTokens |
| Search：search bar、search view、建议/autocomplete | 待交付 | [M3-08 #11][i11] | [Search][search]、SearchBar/SearchView、Filled/OutlinedAutocompleteTokens |
| Chips：assist、filter、input、suggestion | 待交付 | [M3-08 #11][i11] | [Chips][chips]、Assist/Filter/Input/SuggestionChipTokens |
| Cards：elevated、filled、outlined、交互 | 待交付 | [M3-09 #7][i7] | [Cards][cards]、Elevated/Filled/OutlinedCardTokens |
| Lists：一/二/三行、leading/trailing、expanded、reorder、reveal | 待交付 | [M3-09 #7][i7] | [Lists][lists]、List/ExpandedList/ReorderList/RevealListTokens |
| Badges：点、计数 | 待交付 | [M3-09 #7][i7] | [Badges][badges]、BadgeTokens |
| Dividers：水平、垂直、inset | 待交付 | [M3-09 #7][i7] | [Dividers][dividers]、DividerTokens |
| Sliders：单值、范围、连续、离散、标记 | 场景交付：另含 vertical/centered/RTL、两端点 focus/RangeValue、预览与保存恢复；headless/API 和新包消费通过，真实读屏/硬件与最终展厅集成未认领 | [M3-10 #8][i8] | [Sliders][sliders]、SliderTokens v2_3_5；[公开契约](components/m3-10.md)、[验证记录](verification/m3-10.md) |
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
