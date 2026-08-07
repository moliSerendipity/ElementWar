# 功能：最小 EditMode 自动化验证基线

状态：Closed（Fast Verified）
负责人：Codex / 项目负责人
维护日期：2026-08-05
权威流程：[`Docs/Workflow.md`](../Workflow.md)

## 目标

提供仓库内的 EditMode 专用命令，运行一组真实、稳定、无 scene/prefab 依赖的项目测试，并产出可判定成功或失败的 XML、日志和摘要；首个切片验证 `GameEventBus` 的同步分发契约。

本基线仅达到 Fast Verified，不代表包含 PlayMode、Windows64 Player 和主线场景人工验收的 Full Verified / Accepted。

## 当前有效事实

- 项目使用 Unity `2022.3.62f2c1`，测试框架 `com.unity.test-framework` `1.1.33`。
- `GameEventBus` 位于 `Game.Foundation`，通过 `Subscribe<TEvent>`、`Unsubscribe<TEvent>`、`Publish<TEvent>` 同步分发结构体事件。
- `Game.EditModeTests` 是 Editor-only、`autoReferenced: false` 的测试程序集，首批只引用 `Game.Foundation`。
- 四个测试使用独立临时对象，只验证 public API；不依赖帧、物理、scene、prefab 或 `GameEventBus.Instance`，并在用例后销毁对象。
- `Tools/Verify-ElementWarEditMode.ps1` 是唯一 EditMode 命令入口；证据默认位于被忽略的 `Logs/Verification/<timestamp>/`。
- 程序集依赖和验证等级分别以 [`Docs/Architecture.md`](../Architecture.md) 与 [`Docs/Workflow.md`](../Workflow.md) 为准，本 Spec 不复制其通用规则。

## 行为契约与验收

| ID | 验收条件 | 证据 |
|---|---|---|
| AC-01 | Unity 能识别 `Game.EditModeTests` 为仅限 Editor、`autoReferenced: false`、启用 `TestAssemblies` 且只引用 `Game.Foundation` 的测试程序集 | asmdef、编译日志、XML |
| AC-02 | 发布已订阅的同类型事件时，处理器在 `Publish` 返回前恰好收到一次原载荷 | NUnit EditMode 测试 |
| AC-03 | 同时订阅两种事件而只发布一种时，仅匹配类型的处理器被调用 | NUnit EditMode 测试 |
| AC-04 | 处理器退订后再次发布同类型事件时不再收到事件 | NUnit EditMode 测试 |
| AC-05 | 没有匹配订阅时发布结构体事件不抛异常 | NUnit EditMode 测试 |
| AC-06 | 启动 Unity 前只清除证据目录中的 `EditMode-results.xml`、`EditMode.log`、`verification-summary.json`；路径为目录或无法精确清除即失败 | 脚本、摘要 `clearedArtifacts` |
| AC-07 | 摘要记录本次 Unity PID、起止 UTC、`unityExitCode`；XML 文件/XML 时间落在进程时间窗内（允许 2 秒误差），程序集 `_PID` 精确匹配 | XML、日志、摘要 |
| AC-08 | XML 精确包含程序集 `Game.EditModeTests.dll`、夹具 `Game.Tests.EditMode.Foundation.Events.GameEventBusTests` 和四个批准方法；每项恰好一次且为 `Passed` | 源码、asmdef、XML、摘要 |
| AC-09 | 任意 EditMode 失败、根结果非 `Passed`、测试总数为 0 或 Unity 退出码非 0 均整体失败；摘要分别统计全部测试与项目测试 | XML、摘要、脚本退出码 |
| AC-10 | 功能范围不触及生产代码、scene/prefab、Packages、PlayMode、Windows64 或既有用户改动；报告保留既有工作树历史无法追溯的边界 | `git status --short`、scoped diff |
| AC-11 | 只有新的 XML、日志、摘要同时满足 AC-06～AC-10，状态才可为 `Closed（Fast Verified）` | 新证据目录、本 Spec |

## 范围与非目标

- 功能资产限于 `Assets/Tests/EditMode/**`、其 `.meta`、`Tools/Verify-ElementWarEditMode.ps1`、本 Spec，以及 `.gitignore` 中 `/Game.EditModeTests.csproj` 条目。
- `Assets/Scripts/**`、现有生产 asmdef、`Packages/**`、scene/prefab 和其他序列化资源只读。
- 不包含 PlayMode、Windows64、性能/覆盖率门槛、CI、跨平台矩阵或第二批测试对象。
- 不清理、格式化、暂存、提交或推送既有用户改动；发现生产缺陷时先记录并重新授权。
- 本功能不改变架构、公共接口或序列化结构，不需要 ADR。

## 运行方式

先关闭 Unity Editor，并确认没有 Unity 进程或 `Temp/UnityLockfile`；随后从项目根运行：

```powershell
pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1 -ArtifactsPath .\Logs\Verification\<new-run>
```

## 最终证据

- 实际命令：`pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1 -ArtifactsPath .\Logs\Verification\20260805-019fd24c-review-fix`；脚本退出码 `0`。
- Unity PID `23952`；进程时间 `2026-08-05T15:30:23.0388237Z`～`2026-08-05T15:30:41.3433828Z`；`unityExitCode=0`。
- XML 修改时间 `2026-08-05T15:30:39.8756086Z`，XML 起止时间均为 `2026-08-05T15:30:39Z`，程序集 `_PID=23952`；新鲜度与进程绑定通过。
- 全部 EditMode：`total=5`、`passed=5`、`failed=0`、`skipped=0`、`inconclusive=0`；项目程序集：`total=4`、`passed=4`、`failed=0`、`skipped=0`、`inconclusive=0`；额外 1 项为 Addressables 包测试。
- 四个项目方法为 `PublishInvokesMatchingHandlerSynchronouslyWithPayload`、`PublishInvokesOnlyHandlersForMatchingEventType`、`UnsubscribePreventsLaterDelivery`、`PublishWithoutSubscribersDoesNotThrow`，各出现一次且通过。
- 证据：`Logs/Verification/20260805-019fd24c-review-fix/EditMode-results.xml`、`EditMode.log`、`verification-summary.json`；摘要确认三个陈旧占位文件被精确替换。
- 日志仍含 Licensing、MMD4Mecanim 和 Mono `abort_threads` 环境警告，但没有 C# 编译失败或测试失败；这些警告不构成无警告构建证明。
- 历史目录 `Logs/Verification/20260805-213243/` 因缺少新鲜度和精确身份门禁，只保留为历史记录，不用于收口。
- 未运行：PlayMode、Windows64、性能检查；独立审查修复已同步到本 Spec，无 ADR。

## 维护约束

- 不在 Editor 打开或存在 `Temp/UnityLockfile` 时启动第二个 Unity；脚本遇到锁必须失败，不结束进程或删除锁文件。
- 清理仅限 AC-06 的三个精确文件；不得删除证据目录或无关文件，目录型冲突必须失败。
- 项目测试身份固定为 AC-08 的程序集、夹具和四个方法；有意增删测试时必须同步契约、脚本门禁和摘要结构。
- 全部 EditMode 数量可随包测试变化，但任意失败仍整体失败；不得用编译成功、旧日志或项目四项通过覆盖全局失败。
- 成功判定必须同时使用退出码、非零测试数、XML、PID/时间新鲜度和摘要，不只依赖日志尾部。
- 环境警告应保留原始日志并与真实失败区分；不得隐藏或改写为测试通过证据。
- 既有脏工作树缺少历史快照，当前 scoped diff 只能证明本次直接触及范围，不能追溯或排除并发变化。
- 回滚单位为测试目录及 `.meta`、EditMode 验证脚本、本 Spec 和对应 `.gitignore` 条目；删除或回滚仍需单独授权。
- 最终证据路径失效、运行入口变化或验收契约变化时必须更新本 Spec；可由 Git/日志恢复的实施过程不回填。
