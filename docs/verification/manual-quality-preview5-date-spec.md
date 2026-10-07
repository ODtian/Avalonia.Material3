# Date / Range Spec 复核

最终准确端点：`2f4294867c29d8555dd6a7dbff36276df7c1ca33`（Year交互层修复 `033c88a`）；几何/clipboard片 `3b514ee8788d3494538313b9bdf37020bca130ac`，Single/input片 `329ca1a0bd5841ab0410ebdbd3c9f50ad01d7083`，有限viewport/dialog片 `7d2c050e3c48d8aacf6cd6c71381bc6d1191c574`，完整结构初片 `3f68ced1767fcc923fe86c33478c508e12f2c8bc`。审查使用精确git文件、公开几何fixture及官方alpha29/beta01相同字节源码。

已接通：

| 原Date五项 / Show链 | 结果 |
|---|---|
| Range多月结构 | 固定weekday、连续纵向虚拟月份；可见窗口+有限overscan，月份TitleSmall/OnSurfaceVariant，滚动同步DisplayMonth。 |
| Range header | 68 minimum/start64、独立Start/End及4 gap、TitleLarge、year-inclusive en-US格式、模式标题与明确Title覆盖；divider现计入header。 |
| Range并排输入 | 两个既有公开输入对象，同top/等宽/gap8、start/end24/top10；default2+2+4、uppercase placeholder、合法supporting空，partial保持正常null draft。 |
| 六行及range几何 | 42cells/6×48=288；weekday/day的SpaceEvenly与独立range背景原式保留，W336/W576+RTL literal fixtures覆盖。 |
| 宽度/有限约束/Show | bare Range消费父宽，局部Width/MaxWidth优先；Range viewport消费真实body余量。Show采用stock typed date-dialog，360/max568、end6/bottom8，body与actions分别获得有限约束。 |

Calendar初始null与Range首击start-only继续更新ValidationMessage/ItemStatus/OK状态；7d的root `_error`条件抑制可见Incomplete并排除字段错误。裸600高viewport终点600，以及modal360×568、底部8的公开fixture已覆盖。

此前4组主链已接通：

- 输入采用Date/Start/End label、数字/delimiter编辑、完整8digit验证、partial无错误；SelectedText由TextBox管理Undo。显式InputFormat和local child label/placeholder/supporting覆盖保留。默认focus300ms，Mode改变/detach取消，显式overlay focus优先。
- Single导航左year-menu/caret、右pager；展开隐藏pager、caret即时180；年表gap16并定位显示年份，weekday脱离month capture固定。
- Single/Range默认含年、本地年月/日期标点保持；无值Selected date/Entered date、Single标题Select date。明确同metadata DisplayFormat立即Refresh。
- Bare角0、Show内rounded clip与外shadow分层；Filled.Edit/Filled.DateRange；consumer替换Theme的公开fixture覆盖。

末两项的实际几何修复已核对：

- stock Date input supporting为valid16/error20，配bottom16/12；公开measure从100/96变为96/96。完整非法日期与Undo回合法态保留同一total height。自定义Theme及local插槽优先保留。
- YearFace横margin0、竖6，实际Primary两侧像素验证72×36 artwork；year touch72×48与row stride64保留。
- 延迟clipboard记录generation/session/text/selection/pattern，detach或取消后重开时保护新draft、selection及Undo；公开延期clipboardfixture覆盖该生命周期。

Year最后交互层已核对：DateTimePickers.axaml:55同步Margin0,6，与YearFace共享72×36 surface。现公开Year fixture覆盖两侧距边2pixel的Primary、hover着色及press进一步变色，触达与交互paint保持一致。

最终结论：本次完整Date/Range构造范围通过，包括Single/Range的header、month/year导航、有限多月viewport、input默认编辑/错误预留/focus、bare/Show壳及明确调用者覆盖；编辑对象、结果语义、Theme seams与clipboard生命周期保留。统一producer/package/AOT及实际窗口验收由root完成。
