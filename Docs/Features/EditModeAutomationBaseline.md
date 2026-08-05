# 功能：最小 EditMode 自动化验证基线

状态：Closed（Fast Verified；独立审查修复后重新验证）  
负责人：Codex / 项目负责人  
日期：2026-08-05  
关联 ADR / Issue / Commit：不需要 ADR；独立审查会话 `019fd24c-af07-7d82-8cce-2de26483b259`；2026-08-05 已授权实施及修复

## 目标

让开发者能够通过一个仓库内的 EditMode 专用命令，运行至少一组真实、稳定、无 scene/prefab 依赖的自动化测试，并得到可判定成功或失败的测试 XML、日志和摘要；首个行为切片优先验证 `GameEventBus` 的同步分发契约。

本里程碑以 **Fast Verified** 为完成上限，不声称达到包含 PlayMode、Windows64 Player 和主线场景人工验收的 Full Verified / Accepted。

## 独立审查结论与重新打开

2026-08-05，独立审查会话 `019fd24c-af07-7d82-8cce-2de26483b259` 判定原 EditMode 验证基线存在阻塞问题：

- 验证脚本没有在启动 Unity 前清除本次预期的精确证据文件，因而可能复用 `ArtifactsPath` 中的陈旧 XML。
- 原项目测试识别只依赖 `fullname` 包含 `GameEventBusTests` 且数量至少为 4，不能证明准确的测试程序集、完整类名和四个预期方法均由本次运行执行并通过。
- 原摘要没有显式持久化 `unityExitCode`，也没有分别记录全部 EditMode 测试与项目自身测试的数量。
- 原证据没有记录足以回溯既有脏工作树来源的前后状态快照，不能证明所有非本功能改动的历史归因。

因此，原 `Closed（Fast Verified）` 状态已重新打开。`Logs/Verification/20260805-213243/` 仍可作为历史运行记录，但不能单独证明修复后的验证器满足可信新鲜度和精确项目测试识别要求。只有修复后在新的专用证据目录中完成一次全新验证，且新 XML、日志和摘要全部满足下述验收标准，才能重新标记为 `Closed（Fast Verified）`。

## 实施前已验证行为

- Unity 版本：`2022.3.62f2c1`；本机存在 `E:\Unity\2022.3.62f2c1\Editor\Unity.exe`。
- `Packages/manifest.json` 与 `Packages/packages-lock.json` 均确认直接依赖 `com.unity.test-framework` `1.1.33`，无需修改包配置。
- 正式工程当前有 `Game.Foundation`、`Game.Definition`、`Game.Gameplay`、`Game.Presentation`、`Game.Editor` 五个 asmdef；依赖方向与 `Docs/Architecture.md` 一致。`Game.Foundation` 没有项目程序集依赖。
- 正式工程当前没有 `Assets/Tests` 下的测试 asmdef，也没有使用 NUnit / `[Test]` / `[UnityTest]` 的项目测试代码；当前运行测试会存在“零真实项目测试”的风险。
- `Tools/Verify-ElementWar.ps1` 尚未进入正式工程。`_CodexWorkflowCandidate/ProposedProjectRoot` 中的 EditMode/PlayMode asmdef 与 Fast/Full 脚本只是候选提案，当前不会被 Unity 导入，且不在本功能中原样采用。
- `GameEventBus` 位于 `Game.Foundation`，公开 `Subscribe<TEvent>`、`Unsubscribe<TEvent>`、`Publish<TEvent>`，事件约束为 `struct`；实现通过按事件类型保存的委托进行同步分发，并在销毁时清空订阅。
- `GameEventBus` 继承 `SingletonBehaviour<GameEventBus>`，因此不是完全脱离 UnityEngine 的普通 C# 类；但待测的订阅、类型隔离、同步发布和退订行为是确定性的，不依赖帧、物理、计时、scene 或 prefab，可在 EditMode 中使用临时测试对象验证。
- `ConfigIdUtility` 是完全不继承 Unity 类型的纯静态规则，可作为后续 Definition 层测试候选；为保持首个切片最小，本功能不同时扩展到它。
- 2026-08-05 检查时工作树已有用户的未提交修改、删除项和未跟踪工作流文件。`GameEventBus`、`Game.Foundation.asmdef`、包清单当前没有未提交差异；实施必须继续把所有既有改动视为只读。
- 检查时正式工程没有 `Temp/UnityLockfile`，但命令行验证前必须重新检查，不能据此假定实施时 Editor 仍关闭。

### 已批准约定

- 推荐把“稳定纯 C# 行为”解释为“可在 EditMode 中确定性调用、无 scene/prefab/帧依赖的行为”，因此首批测试仍选择 `GameEventBus`；不为追求完全普通 C# 类型而重构生产代码。
- 推荐把可复现的命令行入口纳入“自动化验证基线”，但只新增 EditMode 专用脚本，不接入候选脚本中的 PlayMode 或 Windows64 分支。
- 用户已通过“按推荐方案开始实施”批准上述两项约定；不再把首批对象改为 `ConfigIdUtility`。

## 目标行为与验收

| ID | Given | When | Then | 自动/人工证据 |
|---|---|---|---|---|
| AC-01 | Unity Test Framework 1.1.33 已安装，正式工程新增 EditMode 测试程序集 | Unity 导入项目并编译测试程序集 | `Game.EditModeTests` 被识别为仅限 Editor 的测试程序集，且只引用首批测试需要的 `Game.Foundation` | Unity 编译日志；asmdef；测试 XML |
| AC-02 | 一个 `GameEventBus` 测试实例订阅某个测试事件 | 发布带有已知载荷的同类型事件 | 处理器在 `Publish` 返回前恰好收到一次，并得到原载荷 | NUnit EditMode 测试 |
| AC-03 | 总线同时存在两种测试事件的处理器 | 只发布其中一种事件 | 只调用匹配事件类型的处理器 | NUnit EditMode 测试 |
| AC-04 | 处理器已订阅后又退订 | 再发布同类型事件 | 已退订处理器不再被调用 | NUnit EditMode 测试 |
| AC-05 | 没有任何匹配订阅 | 发布一个结构体事件 | 调用不抛异常 | NUnit EditMode 测试 |
| AC-06 | Unity Editor 已关闭、项目不存在 `Temp/UnityLockfile`，且选定 `ArtifactsPath` 可能已有同名旧文件 | 运行 EditMode 专用验证脚本 | 启动 Unity 前只清除本次预期的 `EditMode-results.xml`、`EditMode.log`、`verification-summary.json`；不删除证据目录或其他文件；若任一路径是目录或无法精确清除则失败 | 脚本实现审查；摘要中的 `clearedArtifacts`；证据目录检查 |
| AC-07 | 脚本启动一个新的 Unity 测试进程 | Unity 完成并生成 XML | 摘要显式记录本次 Unity PID、开始/结束 UTC 与 `unityExitCode`；XML 修改时间及 XML 自带开始/结束时间落在本次进程时间窗内（允许 2 秒文件系统时间误差）；项目测试程序集的 `_PID` 等于本次 Unity PID | 新 XML；文件时间；Unity 日志；摘要 JSON |
| AC-08 | 当前测试代码定义了已批准的 `GameEventBus` 四个行为用例 | 脚本解析当前运行 XML | 精确找到程序集 `Game.EditModeTests.dll`、夹具 `Game.Tests.EditMode.Foundation.Events.GameEventBusTests`，且夹具方法集合恰好为四个预期方法；每个完整测试名恰好出现一次且结果为 `Passed` | 测试源码；asmdef；新 XML；摘要中的预期方法和项目测试明细 |
| AC-09 | Unity 运行可能同时发现包测试或未来新增的其他 EditMode 测试 | 脚本判定全局结果 | 任意 EditMode 测试失败、根结果非 `Passed`、测试总数为 0 或 Unity 退出码非 0 均使整体失败；摘要分别记录 `allEditModeTests` 与 `projectTests` 的 total/passed/failed/skipped/inconclusive | 新 XML；摘要 JSON；脚本退出码 |
| AC-10 | 实施前工作树已有无法归因的用户改动 | 完成修复后的 scoped diff 与状态检查 | 本次直接源码/文档编辑只涉及验证脚本和本 Spec，并生成被忽略的新证据；不对生产代码、scene/prefab、Packages、PlayMode、Windows64、敌人受击减速或既有用户改动执行清理、覆盖、暂存或格式化。报告明确保留“无法用当前证据追溯既有工作树历史归因或排除并发变化”的边界 | `git status --short`；获批文件内容检查；最终报告 |
| AC-11 | AC-06 至 AC-10 的脚本修复已经完成 | 在新的专用证据目录执行一次全新验证 | 只有新 XML、日志和摘要均满足全部验收标准时，Spec 才重新标记为 `Closed（Fast Verified）`；不得用原证据目录或旧结果收口 | 新证据目录；本 Spec 状态 |

## 非目标

- 不处理敌人受击减速，也不修改任何 Enemy、Combat 或 Damage 生产行为。
- 不修改任何生产 C#、生产 asmdef 或公共接口；如测试揭示生产缺陷，先记录证据并重新申请授权。
- 不修改 scene、prefab、ScriptableObject、材质、动画、Input Action 或 Project Settings。
- 不新增或运行 PlayMode 测试，不接入 Windows64 构建，不运行 Full 验证。
- 不把 `_CodexWorkflowCandidate` 整包迁入正式工程，不修改候选包。
- 不扩大到 `ConfigIdUtility`、对象池、伤害结算、武器状态等第二批测试对象。
- 不建立覆盖率门槛、CI 服务集成、性能基线或跨平台矩阵。
- 不清理、暂存、提交、推送或格式化任何既有用户改动。

## 范围

允许修改：

- `Docs/Features/EditModeAutomationBaseline.md`：本行为契约及最终证据同步。
- `Assets/Tests/EditMode/Game.EditModeTests.asmdef`：Editor-only 测试程序集。
- `Assets/Tests/EditMode/Foundation/Events/GameEventBusTests.cs`：首批四个真实测试。
- 上述新增 Unity 目录和资源由 Unity 正常导入生成的对应 `.meta`。
- `Tools/Verify-ElementWarEditMode.ps1`：推荐的 EditMode-only 命令行入口。
- `.gitignore`：只新增 `/Game.EditModeTests.csproj`，忽略 Unity 为测试程序集生成的本地 IDE 工程文件。

禁止或只读：

- `Assets/Scripts/**`、现有 asmdef、`Packages/**`。
- `Assets/Scenes/**`、所有 prefab 和其他序列化资源。
- `_CodexWorkflowCandidate/**`、`Assets/Script_Legacy/**`、`Assets/LuaScripts/**`。
- 用户当前所有既有修改、删除项和未跟踪文件；本功能不清理 `.vs`、`Library`、`Temp`、`Logs` 或 `obj`。

### 独立审查修复范围

本次重新打开后的获批修改范围收窄为：

- `Tools/Verify-ElementWarEditMode.ps1`：修复证据新鲜度、进程绑定、精确项目测试识别、保守全局门禁和摘要字段。
- `Docs/Features/EditModeAutomationBaseline.md`：记录审查结论、修复后的验收标准、环境警告、证据与归因边界。
- `Logs/Verification/<new-run>/`：仅由全新验证生成的、被 Git 忽略的 XML、日志和摘要；运行前可在该新目录中预置三个同名陈旧占位文件，用来验证脚本会精确替换它们。

既有测试源码、测试 asmdef、`.meta` 与 `.gitignore` 本次只读。生产代码、scene/prefab、Packages、PlayMode、Windows64、敌人受击减速和所有既有用户改动继续排除。

## 批准后的设计

- 配置所有者：无新增配置；测试包版本继续由 `Packages/manifest.json` / `packages-lock.json` 管理。
- 运行时状态所有者：生产状态不变；每个测试只拥有自己的临时 `GameObject` 和 `GameEventBus` 组件。
- 事件/接口流：测试仅通过现有 public API 执行 `Subscribe -> Publish -> Unsubscribe`，不反射私有字典，不断言内部委托结构，不新增测试专用生产接口。
- 测试隔离：测试事件定义为测试程序集内的私有只读结构；每个用例独立创建测试总线，并在 `TearDown` 使用 `DestroyImmediate` 清理。测试不依赖 `GameEventBus.Instance`，不保存 scene/prefab。
- 程序集：`Game.EditModeTests` 设置 `includePlatforms: ["Editor"]`、`autoReferenced: false`、`optionalUnityReferences: ["TestAssemblies"]`，首批只引用 `Game.Foundation`；后续需要 Definition / Gameplay 测试时另行扩展引用。
- 自动化入口：`Tools/Verify-ElementWarEditMode.ps1` 接受项目路径、Unity 路径和可选证据路径，校验 Unity 项目与版本、Unity 可执行文件和 `Temp/UnityLockfile`，仅调用 `-runTests -testPlatform EditMode`。启动前只精确清除预期的 XML、日志和摘要文件，不删除目录或无关文件。
- 运行绑定：脚本记录它启动的 Unity PID、开始/结束 UTC 和退出码；运行后同时检查 XML 文件修改时间、XML 自带开始/结束时间以及 `Game.EditModeTests.dll` 的 `_PID`，证明结果来自本次 Unity 进程。时间比较允许 2 秒文件系统误差，但 PID 必须精确相等。
- 项目测试身份：程序集必须精确为 `Game.EditModeTests.dll`，夹具必须精确为 `Game.Tests.EditMode.Foundation.Events.GameEventBusTests`，且夹具方法集合必须恰好为 `PublishInvokesMatchingHandlerSynchronouslyWithPayload`、`PublishInvokesOnlyHandlersForMatchingEventType`、`UnsubscribePreventsLaterDelivery`、`PublishWithoutSubscribersDoesNotThrow`；每项必须恰好出现一次并通过。
- 保守门禁与摘要：精确识别项目测试不替代全局门禁；任意 EditMode 测试失败仍使整体失败。摘要显式持久化 `unityExitCode`，并分别记录 `allEditModeTests` 与 `projectTests` 的数量和结果，同时保留预期方法、实际项目测试明细、新鲜度检查与精确清除文件清单。
- 证据目录：默认写入被 `.gitignore` 排除的 `Logs/Verification/<timestamp>/`，至少包含 `EditMode-results.xml`、`EditMode.log`、`verification-summary.json`。
- 初始化、禁用、死亡、重置、对象池复用：这些玩法生命周期不适用于本切片；测试自身必须无跨用例订阅或对象残留。
- 序列化、兼容性和迁移：不改现有序列化数据；只新增测试资源及 `.meta`。默认 Unity 路径与当前项目版本一致，同时保留 `-UnityExe` 覆盖参数。
- 已拒绝方案及原因：
  - 不复制候选 `Game.EditModeTests.asmdef` 的 Foundation/Definition/Gameplay 三层引用，因为首批只需要 Foundation，额外引用会扩大编译和架构表面。
  - 不复制候选 Fast/Full 脚本，因为其中 PlayMode 与 Windows64 明确超出本功能范围。
  - 不把 `GameEventBus` 重构成普通 C# 服务，因为这会修改生产代码和公共契约。
  - 不以空测试、只编译 asmdef 或旧日志作为自动化基线成功证据。

本功能不改变程序集方向、状态所有者、公共事件契约、场景组合根或序列化结构，因此不需要 ADR。

## 测试与验收计划

| 层级 | 用例 | 计划测试/证据 |
|---|---|---|
| EditMode | 同类型同步分发与载荷；事件类型隔离；退订后不再分发；无订阅时安全发布 | 4 个精确命名的 NUnit `[Test]`；当前进程生成的 XML、日志、摘要；项目四项必须全部通过，且全部 EditMode 测试任一失败均整体失败 |
| PlayMode | 不在本功能范围 | 未运行 |
| Windows64 人工验收 | 不在本功能范围 | 未运行 |
| 性能 | 无性能声明 | 未运行 |

实施后的验证顺序：

1. 重新检查 `git status --short`、Unity 进程和 `Temp/UnityLockfile`；若 Editor 打开则暂停，请用户保存并关闭，不结束现有进程，也不删除锁文件。
2. 创建一个此前不存在的专用证据目录，并只在其中预置 `EditMode-results.xml`、`EditMode.log`、`verification-summary.json` 三个陈旧占位文件；不在其中放置其他待删除内容。
3. 从项目根运行 `pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1 -ArtifactsPath <new-run>`；脚本必须报告精确清除三个预期文件，并由本次 Unity 进程重新生成它们。
4. 解析新 XML 和摘要，逐项核对 Unity PID/时间窗/退出码、全部 EditMode 测试数量、项目测试数量、精确程序集/夹具/四个方法及每项 `Passed` 结果；零测试或任意失败均视为失败。
5. 检查新日志中的错误与环境警告，再检查 `git status --short` 和两份获批文件的 scoped diff。只有全部证据满足要求后才把本 Spec 更新回 `Closed（Fast Verified）`。

## 已知环境警告与证据边界

### 历史运行中的环境警告

历史日志 `Logs/Verification/20260805-213243/EditMode.log` 与修复后的新日志 `Logs/Verification/20260805-019fd24c-review-fix/EditMode.log` 在测试通过并以 Unity 退出码 0 结束的同时，均包含以下环境警告；本功能不隐藏、修复或把它们误报为测试失败：

- Unity Licensing 在启动阶段记录许可证验证、握手和访问令牌错误，随后成功连接新的 LicensingClient、解析 entitlement 并继续执行测试。新运行重现该警告，但 Unity 退出码为 0，XML 的全部 5 项测试均通过。
- `MMD4Mecanim` 记录已弃用 OSX 构建目标相关警告。该第三方/既有代码不在本次修复范围。
- Mono 在退出阶段记录 `abort_threads` 相关警告。新运行已保留完整日志，并以本次进程退出码、XML 和摘要共同判定，不仅凭日志尾部推断成功。

这些警告说明运行环境并非完全无噪声；它们不改变“任意 EditMode 测试失败则整体失败”的门禁，也不构成 PlayMode、Windows64 或无警告构建的证明。

### 既有工作树归因边界

实施前仓库已有大量用户的未提交修改、删除项和未跟踪文件。历史证据目录没有持久化完整的实施前/实施后 Git 状态、文件内容哈希或其他可追溯快照，因此无法事后证明既有工作树中每一项变化由谁、在何时产生，也无法把所有非目标路径的历史状态归因给本功能或排除本功能之外的其他会话。

本次修复能证明的边界仅是：依据修复开始时的只读状态快照、两份获批文件的 scoped diff、全新验证证据和修复结束时的状态检查，说明本次操作触及了哪些已批准文件；这不能补写缺失的历史归因证据。最终报告必须继续明确这一限制。

## 风险与回滚

- 风险：`GameEventBus` 是 `MonoBehaviour`，EditMode 测试仍需 Unity 对象容器。  
  发现方式：测试编译/运行日志与对象清理检查。  
  控制：只调用确定性的 public 事件 API，不依赖帧、scene、prefab 或单例查找。
- 风险：命令行 Unity 与已打开的 Editor 竞争工程。  
  发现方式：`Temp/UnityLockfile`。  
  控制：脚本检测到锁文件即失败，不启动第二个 Unity 进程。
- 风险：Unity 首次导入会生成 `.meta` 和忽略目录中的缓存/日志。  
  发现方式：导入后 `git status --short`。  
  控制：只跟踪批准测试资源的 `.meta`；精确忽略根目录 `Game.EditModeTests.csproj`；不编辑或提交 `Library`、`Temp`、`Logs`。
- 风险：复用证据目录时，陈旧 XML 可能被误认为本次运行结果。  
  发现方式：启动前检查预期文件、运行后比较进程 PID/时间窗与 XML `_PID`/时间。  
  控制：只精确删除三个预期证据文件，拒绝目录型冲突；不删除宽泛目录；摘要保存清除清单和新鲜度判定。
- 风险：文件系统与 XML 时间戳精度不同，边界比较可能出现微小误差。  
  发现方式：比较 Unity 进程、文件和 XML 的 UTC 时间。  
  控制：时间窗只允许 2 秒误差，同时要求项目程序集 `_PID` 与本次 Unity PID 精确相等，避免只靠时间推断。
- 风险：包测试或未来新增的 EditMode 测试会改变全部测试总数。  
  发现方式：分别解析根测试统计与 `Game.EditModeTests.dll`。  
  控制：项目夹具仍要求精确四个方法全部通过；全局统计不固定为 4，但任意 EditMode 失败仍整体失败，摘要分别记录两组数量。
- 风险：当前工作树已有大量用户改动。  
  发现方式：实施前后状态快照与 scoped diff。  
  控制：本次只修改验证脚本和本 Spec；明确披露历史归因无法追溯，不把当前 scoped diff 夸大为对既有工作树来源的完整证明。
- 回滚单位：新增测试目录及其 `.meta`、EditMode 验证脚本、Feature Spec 构成一个独立切片；如需回滚，只处理该切片，且仍需用户授权删除。

## 实施授权

AI 总结：

> 推荐新增一个只引用 `Game.Foundation` 的 Editor-only 测试程序集，以四个 `GameEventBus` EditMode 测试建立首个真实切片，并新增一个只运行 EditMode、拒绝零测试且保存 XML/日志/摘要的专用 PowerShell 入口。范围严格排除敌人受击减速、生产代码、scene/prefab、PlayMode、Windows64、候选包迁移和用户既有改动。`GameEventBus` 虽继承 MonoBehaviour，但只测试其无场景依赖的确定性事件分发；若要求完全普通 C# 类型，则需在授权前改选 `ConfigIdUtility`。

用户授权原文与日期：

> “按推荐方案开始实施”（2026-08-05）
>
> “允许忽略并收口”（2026-08-05）
>
> “按推荐方案开始修复”（2026-08-05）

## 完成证据

### 历史证据（已判定不足以收口）

- 原实现曾生成 `Logs/Verification/20260805-213243/EditMode-results.xml`、`EditMode.log` 和 `verification-summary.json`，报告 `total=5`、`passed=5`、`failed=0`，其中 `GameEventBusTests=4`，Unity 退出码 0。
- 独立审查确认该 XML 内容显示四个项目测试通过，但原脚本无法充分证明 XML 未被陈旧结果复用，也没有按精确程序集、完整夹具和四个方法逐项门禁；原摘要缺少显式 `unityExitCode` 和全部/项目测试分组。因此该目录只保留为历史记录，不作为修复后的 Fast Verified 证据。

### 当前修复状态

- 已加固 `Tools/Verify-ElementWarEditMode.ps1`：精确清除预期证据文件、绑定本次 Unity PID/时间窗、精确识别程序集/夹具/四个方法、保留全局失败门禁，并扩充摘要字段。
- PowerShell 脚本已通过命令语法解析，并在确认 `Temp/UnityLockfile` 消失且没有 Unity 进程后完成一次全新 EditMode 运行。
- 实际命令：`pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1 -ArtifactsPath .\Logs\Verification\20260805-019fd24c-review-fix`；脚本退出码 0。
- 陈旧证据演练：运行前只在该新目录预置三个无效占位文件；新摘要的 `clearedArtifacts` 精确记录 `EditMode-results.xml`、`EditMode.log`、`verification-summary.json`，运行后目录中不再包含占位标记。
- Unity 进程证据：PID `23952`；开始 `2026-08-05T15:30:23.0388237Z`；结束 `2026-08-05T15:30:41.3433828Z`；`unityExitCode=0`。
- 新鲜度证据：XML 修改时间 `2026-08-05T15:30:39.8756086Z`，XML 开始/结束时间均为 `2026-08-05T15:30:39Z`，均落在本次进程时间窗内；`Game.EditModeTests.dll` 的 `_PID=23952` 与本次 Unity PID 精确一致。
- 全部 EditMode 测试：`total=5`、`passed=5`、`failed=0`、`skipped=0`、`inconclusive=0`。项目程序集 `Game.EditModeTests.dll`：`total=4`、`passed=4`、`failed=0`、`skipped=0`、`inconclusive=0`；另 1 项仍为 Addressables 包测试。
- 项目测试身份：夹具精确为 `Game.Tests.EditMode.Foundation.Events.GameEventBusTests`；`PublishInvokesMatchingHandlerSynchronouslyWithPayload`、`PublishInvokesOnlyHandlersForMatchingEventType`、`UnsubscribePreventsLaterDelivery`、`PublishWithoutSubscribersDoesNotThrow` 各恰好出现一次并为 `Passed`。
- 新证据：`Logs/Verification/20260805-019fd24c-review-fix/EditMode-results.xml`、`EditMode.log`、`verification-summary.json`。该新证据满足 AC-06 至 AC-11，因此状态重新标记为 `Closed（Fast Verified）`。
- 新日志仍包含已披露的 Licensing、MMD4Mecanim 和 Mono `abort_threads` 环境警告；没有 `error CS`、编译失败或测试失败证据。这些既有环境问题未在本功能中修复。
- 未运行：PlayMode、Windows64、性能检查，继续属于明确非目标。
- Feature Spec / ADR：本 Spec 已同步独立审查与修复契约；本功能仍不需要 ADR。
