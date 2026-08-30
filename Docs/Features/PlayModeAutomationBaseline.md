# 功能：最小 PlayMode 自动化验证基线

状态：Closed
验证：Verified
关联 ADR / Issue / Commit：可信起点 `fef84f2`；不需要 ADR

## Current
- 测试只持有自己创建的 GameObject、调度器和回调，通过 public `Subscribe` / `Unsubscribe` 验收，不使用反射或私有状态。
- 订阅返回时回调数必须为零；随后只通过真实 PlayMode 帧推进，在 1 秒 realtime 超时内观察到回调。
- `UnityTearDown` 再次安全退订，只销毁测试自己创建的对象，推进一帧并确认测试创建的单例已清空。

## Evidence
- 接受运行使用 Unity PID `68720`，`unityExitCode=0`；全部 PlayMode 与项目程序集统计均为 `total=1`、`passed=1`、`failed=0`、`skipped=0`、`inconclusive=0`。
- `PlayMode-results.xml` 和 `PlayMode.log` 由 PID `68720` 的 Unity 进程生成；`PlayMode-verification-summary.json` 由 PowerShell 在该进程结束后生成，并记录 PID、进程时间窗、退出码、文件新鲜度和测试统计以绑定同一次运行。
- 摘要确认三个预期证据文件均在运行前被精确清理并由本次运行重新生成；asmdef JSON 与 PowerShell AST 静态检查通过，日志没有 C# 编译或测试失败标记。
- 原始证据：`Logs/Verification/20260806-011007-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json`。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
