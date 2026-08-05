# 功能：最小 PlayMode 自动化验证基线

状态：Closed（本切片 PlayMode 验证通过；不等同于 Full Verified）
负责人：Codex / 项目负责人
日期：2026-08-06
关联 ADR / Issue / Commit：可信起点 `fef84f2`；不需要 ADR

## 目标与完成等级

通过仓库内的 PlayMode 专用命令运行一个真实依赖 Unity 帧循环的项目测试，并生成可判定结果、可绑定单次 Unity 运行的 XML、日志和 JSON 摘要。

本 Spec 的 Closed 仅表示该 PlayMode 最小切片验证通过；未达到要求 EditMode、PlayMode、Windows64 Player 和主线场景人工验收的 Full Verified / Accepted。

## 范围与交付物

- `Game.PlayModeTests` 测试程序集，只引用 `Game.Foundation`。
- `RuntimeTickSchedulerPlayModeTests` 生命周期测试及 Unity 生成的对应 `.meta`。
- 独立入口 `Tools/Verify-ElementWarPlayMode.ps1` 和本 Spec。
- 原始证据保存在被 Git 忽略的 `Logs/Verification/<timestamp>-playmode/`。
- 不修改生产代码、现有 EditMode 测试及验证入口、scene、prefab、Packages、ProjectSettings、`.gitignore` 或既有用户改动。
- 不包含验证入口统一化、Windows64 构建、性能测试或主线场景人工验收。

## 核心行为契约

- 测试创建对象前必须断言 `RuntimeTickScheduler.Instance == null`；若存在来源不明的实例则失败，且不得替换或销毁它。
- 测试只持有自己创建的 GameObject、调度器和回调，通过 public `Subscribe` / `Unsubscribe` 验收，不使用反射或私有状态。
- 订阅返回时回调数必须为零；随后只通过真实 PlayMode 帧推进，在 1 秒 realtime 超时内观察到回调。
- 退订后继续以 realtime 观察至少 50ms，回调数必须保持不变；测试不主动修改 `Time.timeScale`。
- `UnityTearDown` 再次安全退订，只销毁测试自己创建的对象，推进一帧并确认测试创建的单例已清空。
- 测试不依赖既有 scene、prefab、NavMesh、物理场景或外部资源。

验证器必须：

- 只运行 PlayMode；启动前精确清理目标目录中的 `PlayMode-results.xml`、`PlayMode.log` 和 `PlayMode-verification-summary.json`，并拒绝目录冲突或残留。
- 将 XML、日志的新鲜度和 XML `_PID` 绑定到本次 Unity PID/时间窗，并要求 Unity 退出码为零。
- 区分大小写地精确匹配项目程序集、夹具和唯一预期方法；拒绝零测试、任意失败、根结果非 `Passed` 或身份不一致。
- 分别记录全部 PlayMode 与 `Game.PlayModeTests.dll` 的测试统计。

## 运行方式

```powershell
pwsh -NoProfile -File .\Tools\Verify-ElementWarPlayMode.ps1 `
  -ArtifactsPath .\Logs\Verification\<timestamp>-playmode
```

省略 `-ArtifactsPath` 时，脚本使用 `Logs/Verification/<yyyyMMdd-HHmmss>-playmode/`。调用者也可以指定其他目录。

## 验收结果

- 接受运行使用 Unity PID `68720`，`unityExitCode=0`；全部 PlayMode 与项目程序集统计均为 `total=1`、`passed=1`、`failed=0`、`skipped=0`、`inconclusive=0`。
- 精确测试为 `Game.PlayModeTests.dll` / `Game.Tests.PlayMode.Foundation.Runtime.RuntimeTickSchedulerPlayModeTests` / `SubscribedHandlerRunsAfterFrameAdvancementAndStopsAfterUnsubscribe`，恰好出现一次且为 `Passed`；程序集 XML 为 `_PID=68720`、`platform=PlayMode`。
- `PlayMode-results.xml` 和 `PlayMode.log` 由 PID `68720` 的 Unity 进程生成；`PlayMode-verification-summary.json` 由 PowerShell 在该进程结束后生成，并记录 PID、进程时间窗、退出码、文件新鲜度和测试统计以绑定同一次运行。
- 摘要确认三个预期证据文件均在运行前被精确清理并由本次运行重新生成；asmdef JSON 与 PowerShell AST 静态检查通过，日志没有 C# 编译或测试失败标记。
- 原始证据：`Logs/Verification/20260806-011007-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json`。

## 已知限制

- 当前只有一个 Foundation 调度器测试，证明帧驱动订阅、退订和测试清理契约；`RuntimeTickScheduler` 尚无生产调用点，因此不证明玩法集成。
- 本切片未重跑 EditMode，也未运行 Windows64、性能或主线场景人工验收，不能提升为 Full Verified / Accepted。
- 日志保留 Licensing、MMD4Mecanim 旧平台目标和 Mono 退出相关环境警告；最终判定以退出码、XML 和精确门禁共同为准。
- `-ArtifactsPath` 可由调用者指定；脚本会在所选目录精确清理三个固定证据文件。这是保留的低风险限制，本轮不修改验证脚本。
- PlayMode 与 EditMode 验证脚本仍有重复逻辑；入口统一化应另立任务。

## Git 边界与生成文件

- 验收基于 `fef84f266ad70b6c5fe6a0147c2d66e3b631abf8`；实施与本次文档维护均不暂存、不提交、不推送。
- `ElementWar.sln` 的更新与 `Game.PlayModeTests.csproj` 均由 Unity 在导入新增 PlayMode 程序集时自动生成；前者被 `.gitignore` 忽略，后者当前未跟踪。两者均保留且不作手工修改。
- 新增测试切片之外的用户修改、删除项和候选包均不属于本功能；生产代码、ProjectSettings 与现有 EditMode 入口没有本切片差异。

## 回滚

回滚单位是本 Spec、`Tools/Verify-ElementWarPlayMode.ps1`、`Assets/Tests/PlayMode/` 及其新 `.meta`。本轮不执行回滚；只有获得单独授权后才能移除该切片并让 Unity 重新生成工程文件。既有用户改动、EditMode 基线和其他生成工程不在手工回滚范围。
