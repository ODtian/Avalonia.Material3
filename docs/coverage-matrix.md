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
| Buttons：filled、tonal、elevated、outlined、text；round/square、XS/S/M/L/XL、toggle | 已交付：五种配色、五档尺寸、形状/切换/插槽/命令；真实输入、主题角色、弹簧/减少动效、包展厅及 Windows UIA；人工读屏/跨平台/正式发布仍由 #20 验证 | [M3-01 #2][i2] → [M3-03 #4][i4] | [Buttons][buttons]、锁定 Button*Tokens / ButtonDefaults；[契约](components/m3-03.md)、[证据与限制](verification/m3-03.md) |
| Icon buttons：standard、filled、tonal、outlined；toggle、尺寸/宽度/形状 | 已交付：四种配色、五档尺寸 × 三档宽度、round/square 选中翻转；真实输入、自动化、明暗/字体与包展厅；平台限制见证据 | [M3-03 #4][i4] | [Icon buttons][icon-buttons]、各尺寸 IconButtonTokens；[契约](components/m3-03.md)、[证据与限制](verification/m3-03.md) |
| FAB：标准、small/medium/large、扩展 FAB、FAB menu | 已交付：四档 FAB/扩展尺寸、文本/图标槽、四档菜单触发器与 close recipe、多行动/锚点/RTL/滚动/焦点返回、输入/自动化及弹簧/减少动效；包展厅与 Windows UIA 验证，人工读屏/跨平台与上游帧像素等价未认领 | [M3-04 #9][i9] | [FAB][fab]、[Extended FAB][extended-fab]、FabMenuBaselineTokens / 锁定实现修正；[契约](components/m3-04.md)、[证据与投影边界](verification/m3-04.md) |
| Toolbars：docked、floating、水平/垂直、展开与收起 | 已交付：四布局 × standard/vibrant、真实 core/leading/trailing/FAB 槽、局部/整面收起、56→80 披露 FAB、锚点/尺寸变化/滚动/焦点与 ExpandCollapse；docked vertical 和动效是明确的 Avalonia 投影，非新增上游函数或帧速度复刻 | [M3-04 #9][i9] | DockedToolbarTokens、FloatingToolbarTokens / FloatingToolbarDefaults；[契约](components/m3-04.md)、[证据](verification/m3-04.md) |
| Button groups：connected、非连接分组、选择与形变 | 已交付：action/single/multiple、连接形变/RTL/vertical、自适应 reflow、非连接宽度弹簧与 overflow；真实输入、UIA、包展厅；明确长文案/动效投影边界 | [M3-05 #10][i10] | ButtonGroupSmallTokens、ConnectedButtonGroupSmallTokens；[契约](components/m3-05.md)、[证据与限制](verification/m3-05.md) |
| Segmented buttons：单选、多选 | 已交付：outlined、等宽/共享边框、选中图标、required/empty、多选、焦点/键盘与 Selection 关系；生成令牌与 Compose disabled-outline 差异明确记录 | [M3-05 #10][i10] | [Segmented buttons][segmented]、OutlinedSegmentedButtonTokens；[契约](components/m3-05.md)、[证据](verification/m3-05.md) |
| Split buttons：主操作、次级入口、全部尺寸 | 已交付：Filled/Tonal/Elevated/Outlined × XS/S/M/L/XL、独立主/次 Command、checkable/plain、icon/text、ExpandCollapse/焦点返回与长文案共同高度；人工读屏/平台/AOT 等未认领 | [M3-05 #10][i10] | SplitButtonXSmall/Small/Medium/Large/XLargeTokens；[契约](components/m3-05.md)、[证据与限制](verification/m3-05.md) |
| Checkbox：二态、三态、错误/禁用 | 已实现：18 DIP、48 触达、mixed/error/disabled、TwoWay 表单；headless + Windows UIA；人工读屏/移动平台未验收 | [M3-06 #5][i5] | [Checkbox][checkbox]、CheckboxTokens 14_1_0；[契约](components/m3-06.md)、[证据](verification/m3-06.md) |
| Radio buttons | 已实现：20 DIP、48 触达、互斥/方向键/禁用/宿主错误扩展；headless + Windows UIA；人工读屏未验收 | [M3-06 #5][i5] | [Radio buttons][radio]、RadioButtonTokens v0_117；[M3-06](verification/m3-06.md) |
| Switch | 已实现：52×32 track、16/24/28 handle、48 触达、图标/拖动/禁用/宿主错误扩展；headless + Windows UIA；物理触摸/人工读屏未验收 | [M3-06 #5][i5] | [Switch][switch]、SwitchTokens v0_210；[M3-06](verification/m3-06.md) |
| Text fields：filled、outlined；标签、辅助/错误文本、图标、计数、多行 | 已实现并通过真实编辑、包隔离与 Windows UIA 场景；实际读屏、平台中文 IME 候选会话待核验 | [M3-07 #6][i6] | [Text fields][text-fields]、Filled/OutlinedTextFieldTokens；[公开契约](components/m3-07.md)、[证据与限制](verification/m3-07.md) |
| Search：search bar、search view、建议/autocomplete | 已实现：bar、docked/full-screen view、filled/outlined autocomplete、原生编辑/公开候选与校验/词条编辑/显式意图；source + 新包展厅 + Windows UIA/键鼠；inline dropdown/宿主全屏布局是明确投影，人工读屏/平台中文候选/移动端未验 | [M3-08 #11][i11] | [Search][search]、SearchBar/SearchView、Filled/OutlinedAutocompleteTokens；[契约](components/m3-08.md)、[证据与边界](verification/m3-08.md) |
| Chips：assist、filter、input、suggestion | 已实现：四用途、flat/elevated、选择、独立删除、avatar/图标、32 visual/48 target、全量 live LabelLarge 与状态；长混排/200%/键盘/Toggle/Invoke 与新包验证；人工读屏/物理触摸未验 | [M3-08 #11][i11] | [Chips][chips]、Assist/Filter/Input/SuggestionChipTokens 7_0_1；[契约](components/m3-08.md)、[证据](verification/m3-08.md) |
| Cards：elevated、filled、outlined、交互 | 已交付：三变体、独立插槽/操作、选择与拖动态；真实平台及穷尽视觉一致性未验 | [M3-09 #7][i7] | [Cards][cards]、Elevated/Filled/OutlinedCardTokens；[契约](components/m3-09.md)、[证据](verification/m3-09.md) |
| Lists：一/二/三行、leading/trailing、expanded、reorder、reveal | 已交付：最小行高/变量高度、完整插槽、真实展开/排序/行动底层 reveal；手势适配契约与证据分别记录 | [M3-09 #7][i7] | [Lists][lists]、List/ExpandedList/ReorderList/RevealListTokens；[契约](components/m3-09.md)、[证据](verification/m3-09.md) |
| Badges：点、计数 | 已交付：点/计数/上限/零值、完整计数自动化语义及字体尺度 | [M3-09 #7][i7] | [Badges][badges]、BadgeTokens；[契约](components/m3-09.md)、[证据](verification/m3-09.md) |
| Dividers：水平、垂直、inset | 已交付：水平/垂直/全宽/边距、1 DIP 令牌及装饰性语义 | [M3-09 #7][i7] | [Dividers][dividers]、DividerTokens；[契约](components/m3-09.md)、[证据](verification/m3-09.md) |
| Sliders：单值、范围、连续、离散、标记 | 场景交付：另含 vertical/centered/RTL、两端点 focus/RangeValue、预览与保存恢复；headless/API 和新包消费通过，真实读屏/硬件与最终展厅集成未认领 | [M3-10 #8][i8] | [Sliders][sliders]、SliderTokens v2_3_5；[公开契约](components/m3-10.md)、[验证记录](verification/m3-10.md) |
| Progress indicators：线性、圆形、确定/不确定、Expressive 波形 | 已交付真实轨道/间隙/stop/波形、宿主值与结果、暂停/减少动效/可控时间、只读 ProgressBar/RangeValue 与 Windows UIA；不声称 Compose PathMeasure 像素/帧等价 | [M3-11 #12][i12] | [Progress][progress]、Linear/CircularProgressIndicatorTokens v0_7_0；[契约](components/m3-11.md)、[证据与投影边界](verification/m3-11.md) |
| Loading indicator：Expressive 形状循环 | 已交付锁定七种圆角形状的连续循环及进度驱动 circle→soft-burst、contained/uncontained、宿主状态和结果、新包展厅；形状对应/弹簧为明确的 Avalonia/#3 令牌投影，非上游 Morph 引擎复刻 | [M3-11 #12][i12] | LoadingIndicatorTokens v0_7_0、锁定 LoadingIndicator/MaterialShapes；[契约](components/m3-11.md)、[证据](verification/m3-11.md) |
| Dialogs：basic、全屏；打开/确认/取消/焦点返回 | 已交付：Material 表面/操作、原生编辑与验证结果、LIFO 通用 modal/modeless/anchor/edge 消费契约、焦点/取消/长内容自适应、包展厅及 Windows UIA/输入；人工读屏/跨平台/AOT 限制见证据 | [M3-12 #13][i13] | [Dialogs][dialogs]、锁定 DialogTokens / AlertDialog / ScrimTokens；[契约](components/m3-12.md)、[证据](verification/m3-12.md) |
| Menus：标准、vibrant、segmented、子菜单、锚定/上下文 | 待交付 | [M3-13 #15][i15] | [Menus][menus]、Menu/StandardMenu/VibrantMenu/SegmentedMenuTokens |
| Tooltips：plain、rich | 待交付 | [M3-13 #15][i15] | [Tooltips][tooltips]、Plain/RichTooltipTokens |
| Snackbar：消息、行动、消失 | 待交付 | [M3-13 #15][i15] | [Snackbar][snackbar]、SnackbarTokens |
| Bottom sheets：standard、modal、drag handle | 待交付 | [M3-14 #16][i16] | [Bottom sheets][bottom-sheets]、SheetBottom/DragHandle/ScrimTokens |
| Side sheets：standard、modal | 待交付 | [M3-14 #16][i16] | [Side sheets][side-sheets] |
| Navigation bar：水平/垂直 item、选中、徽标 | 已实现：两种 item 布局、实际内容、徽标/事件/绑定、输入与选择自动化；验收证据和平台限制见组件记录 | [M3-15 #14][i14] | [Navigation bar][navigation-bar]、NavigationBar*Tokens；[契约](components/m3-15.md)、[证据](verification/m3-15.md) |
| Navigation rail：collapsed、expanded、水平/垂直 item | 已实现：非 modal rail 四组合、96/80 和 220..360 DIP、实际内容及 resize 保留焦点/选择；不认领人工读屏/其他平台 | [M3-15 #14][i14] | [Navigation rail][navigation-rail]、NavigationRail*Tokens；[契约](components/m3-15.md)、[证据](verification/m3-15.md) |
| Tabs：primary、secondary、固定/滚动 | 已实现：四组合、真实页面/插槽、单 Tab stop/方向键/滚动、主题与字体混排；投影边界与 package/Windows 记录见证据 | [M3-15 #14][i14] | [Tabs][tabs]、Primary/SecondaryNavigationTabTokens；[契约](components/m3-15.md)、[证据](verification/m3-15.md) |
| Top app bars：small、medium、large、center-aligned、flexible | 待交付 | [M3-16 #17][i17] | [Top app bar][top-app-bar]、AppBar*Tokens |
| Bottom app bar | 待交付 | [M3-16 #17][i17] | [Bottom app bar][bottom-app-bar]、BottomAppBarTokens |
| Navigation drawer：standard、modal | 待交付 | [M3-16 #17][i17] | [Navigation drawer][navigation-drawer]、NavigationDrawerTokens |
| Date pickers：modal、docked、单日/范围、date input | 待交付 | [M3-17 #18][i18] | [Date pickers][date-pickers]、DatePickerModal/DateInputModalTokens |
| Time pickers：钟面、键盘输入、12/24 小时 | 待交付 | [M3-17 #18][i18] | [Time pickers][time-pickers]、TimePicker/TimeInputTokens |
| Carousel：multi-browse、uncontained、hero、full-screen | 已实现四种实际图片布局/掩膜、触摸/鼠标/键盘、宿主项目/位置/加载错误恢复、窗口连续性、Scroll 自动化、新包展厅；布局/惯性/动效明确为 Avalonia 投影，非上游算法等价 | [M3-18 #19][i19] | [Carousel][carousel]、锁定 Carousel.kt；[公开契约](components/m3-18.md)、[证据与限制](verification/m3-18.md) |
| Pull-to-refresh：手势、指示器、内容更新 | 已实现真实阈值/抵抗/释放/取消/忙态手势，standard/Expressive 反馈、宿主结果与错误恢复、F5/Invoke；读屏人工/移动硬件与 Compose nested-scroll/物理等价未认领 | [M3-18 #19][i19] | 锁定 PullToRefresh.kt；[公开契约](components/m3-18.md)、[验证](verification/m3-18.md) |
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
