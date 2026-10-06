# 场景验证边界

本规格沿用 `docs/SPEC.md` 中用户已确认的组件展厅／等价通用宿主、独立算法向量、公开契约/API资源及全新版本化包入口；覆盖 M01–M11，未执行的 App/platform/manual 场景必须明确记录。测试只穿过公开消费契约，不测试内部协作者、私有方法或模板子元素名称。

- 宿主安装主题并展示完整标准家族，通过真实平台输入观察宿主结果、公开属性和可见反馈；初始按钮回归继续保留。
- 宿主改变明暗主题与设计令牌，观察已显示控件和主题资源。
- 全新目录通过版本化 NuGet 包构建展厅、独立宿主，并运行相同场景；不得以源码项目引用代替包消费。

每个行为采用单独的 red → green 周期；规范默认值来自锁定的 AndroidX 参考令牌，而不是被测代码的计算结果。

## 统一清单与证据维度

`tests/ScenarioInventory.props` 是源码/包共享场景、Gallery 页面/helpers 与参考资源的权威编译集；源码和包 bootstrap 各自独立，PackageConsumption 自有实际应用 fixture 不导入源码启动器。`consumer.ps1` 从相同树复制普通输入，排除 bin/obj/artifacts 和源码 csproj/bootstrap，拒绝库源码项目引用及与确切包 hash/version 不符的恢复。TRX 使用 Ordinal 场景身份逐个比较，而非只比较数量。API/resource 基线只可由明确 maintainer-generation 更新，正常门禁不得自动改写 expected。

NativeUIA/SendKeys 操作共享真实桌面，所有此类运行必须串行；编译与 headless 可并行。发布证明以 `published` 二进制为对象，分开 NativeAOT/full-trim managed。进程启动计时记录首次可用 UIA ActionButton，重复新进程但不假称清空 OS 文件缓存。UIA/截图/headless framed-touch 不是 spoken Narrator/NVDA、物理触控或 Android 真机认证。M11 只能来自真正 App 而非复制样例。
