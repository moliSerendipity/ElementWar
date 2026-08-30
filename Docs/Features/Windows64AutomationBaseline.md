# 功能：最小 Windows64 自动化构建基线

- 状态：Closed
- 验证：Verified（仅限 Bootstrap-only Windows64 自动化切片）
- 维护日期：2026-08-06
- 可信起点：`57d50e091978836e0aa02f4740cf969c2079dc10`

## Current

- 独立入口 `Tools/Verify-ElementWarWindows64.ps1` 只构建 `Assets/Scenes/Bootstrap/Bootstrap.unity`，并核对 GUID `d5ba7b6c1b4ae954b9bbab4fb20481a2`；不读取或修改 `EditorBuildSettings`，不包含 `SampleScene`。
- 每次运行生成唯一 `runId` 和全新的 Player/证据目录，不覆盖或清理旧结果；脚本把 `runId`、Player 路径和 C# 报告路径显式传给 `Game.Editor.Build.ElementWarWindows64Build.Build`。
- 构建前检查可信 HEAD、Git index、Unity/Lockfile、Windows Standalone 支持和正式文件状态；Unity 进程按 PID/创建时间绑定，退出后等待本次进程、子进程和 Lockfile 连续消失后再恢复副作用。
- 4 个已知副作用使用基于内容校验的 CAS 恢复；恢复失败、并发修改或路径竞争都会安全失败并保留现场，不覆盖未知新内容。
- Player 校验包含 BuildReport、目标场景、AMD64 架构、必要运行时文件、逐文件哈希和范围外差异检查。该基线只证明 Bootstrap-only Windows64 自动化，不代表 Player 启动、人工场景、性能或发布候选整体已验证。

## Evidence

- 唯一最终真实通过 run：`20260806-102207607Z-2a16a183`；Unity PID `20040`，退出码 `0`，未超时，shutdown quiet period 成功且无遗留 Lockfile。
- BuildReport `Succeeded`：Unity `2022.3.62f2c1`、`StandaloneWindows64`、`Mono2x`、`BuildWithPlayer`、仅 Bootstrap 场景，0 warnings / 0 errors。
- Player 共 180 个文件、127,208,045 bytes；`ElementWar.exe` 与 `UnityPlayer.dll` 为 AMD64，Mono runtime、`ElementWar_Data` 与 `Assembly-CSharp.dll` 均通过检查。
- 4 项副作用 CAS 均 `Succeeded`；Git 状态前后指纹一致，范围外差异、正式文件差异和最终差异均为 0，index 为空；PowerShell 摘要 `result=Passed`。
- 最终离线门禁 20/20 PASS；完整日志、BuildReport、摘要和恢复证据位于 `Logs/Verification/20260806-102207607Z-2a16a183-windows64/`，Player 位于 `Builds/Windows64/20260806-102207607Z-2a16a183/`。

## Remaining Boundaries

- 本切片未运行 EditMode、PlayMode、生成 Player 启动、人工主线场景验收或性能检查，因此不能据此声明发布候选整体已 Verified 或 Accepted。
- 旧失败 run 和恢复过程属于历史证据，需要追溯时从 Git 与对应证据目录读取，不保留在本页正文。

> 本页只保留关闭后的当前摘要；详细调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
