# 功能：最小 Windows64 自动化构建基线

状态：Closed  
维护日期：2026-08-06  
可信起点：`57d50e091978836e0aa02f4740cf969c2079dc10`

## 行为契约

- 独立入口 `Tools/Verify-ElementWarWindows64.ps1` 只构建 `Assets/Scenes/Bootstrap/Bootstrap.unity`，并精确核对 GUID `d5ba7b6c1b4ae954b9bbab4fb20481a2`；不读取或修改 `EditorBuildSettings`，不包含 `SampleScene`。
- 每次运行生成唯一 `runId`，把 `runId`、Player 路径和 C# 报告路径显式传给 `Game.Editor.Build.ElementWarWindows64Build.Build`。输出与证据目录必须全新，碰撞即失败，不覆盖或清理旧内容。
- Addressables 固定使用序列化的 `BuildWithPlayer`（值 `1`）；保持并记录 Standalone scripting backend。BuildSummary warnings 只记录，errors 阻断。
- 构建前拒绝任意现有 `Unity.exe`、项目 `Temp/UnityLockfile`、非空 index、错误 HEAD、reparse point、缺失 Windows Standalone 支持或正式文件漂移。
- Unity 进程以 PID 和创建时间绑定；最长等待 60 分钟。超时时仅可终止脚本启动且身份仍匹配的精确 PID，不结束其他进程。
- Unity 退出后最多等待 15 秒，每 250 ms 轮询本次 Unity、运行期间发现的子进程和项目 Lockfile；连续 3 次全部消失才进入恢复。无关 Unity 只记诊断。超时保持 Lockfile/产物现场，CAS=`Skipped`，结果 Failed。
- 构建前冻结完整 tracked/untracked 状态、正式文件 SHA-256，以及 4 个已知副作用的存在状态和字节。恢复状态必须为 `Skipped`、`InProgress`、`Failed`、`Partial` 或 `Succeeded`，每个路径均记录结果。恢复时先在目标旁创建并以独占句柄校验 replacement，再以阻止读写、允许重命名的句柄锁定目标并通过同一句柄校验长度与 SHA-256；匹配后保持锁定将目标非覆盖移动到唯一 quarantine，最后以非覆盖原子移动安装 replacement。任一竞态、占用或路径重建均失败并保留可恢复字节。
- C# schema v2 的 entry 与 `BuildPipeline.BuildPlayer` Unix 毫秒是硬时间证据；ISO 解析为 `DateTimeOffset` 后与数值差异不得超过 1 ms。Unity `BuildSummary` 时间只作诊断。
- Player、`UnityPlayer.dll` 必须为 AMD64；`ElementWar_Data` 和当前后端必要文件必须存在且非空。逐文件记录 SHA-256；BuildReport 外只可选地存在精确普通非空文件 `ElementWar_BurstDebugInformation_DoNotShip/Data/Plugins/x86_64/lib_burst_generated.txt`。
- 顶层失败与 `finally` 次生失败必须同时进入摘要。唯一证据目录创建后，常规写入失败由最小 fail-safe writer 兜底。

## 正式边界与非目标

基线正式边界为以下 6 个文件；本次最终修复实际只修改 PowerShell 与本 Spec，C# schema v2、Addressables 设置和两个 `.meta` 均保持不变：

- `Docs/Features/Windows64AutomationBaseline.md`
- `Assets/Scripts/Editor/Build.meta`
- `Assets/Scripts/Editor/Build/ElementWarWindows64Build.cs`
- `Assets/Scripts/Editor/Build/ElementWarWindows64Build.cs.meta`
- `Tools/Verify-ElementWarWindows64.ps1`
- `Assets/AddressableAssetsData/AddressableAssetSettings.asset`

不修改生产运行时代码、场景、prefab、Packages、ProjectSettings、asmdef、现有验证器、候选包或用户改动；不运行 EditMode/PlayMode，不启动 Player，不做人工场景验收、性能测试、安装包或签名，不统一验证入口，不暂存、提交或推送。

`Game.Editor.dll` 空壳程序集和 `Game.Editor.asmdef` 平台配置属于既有架构债务，本切片仅记录。

## 最终验证证据

最终复审发现旧恢复逻辑在哈希校验后覆盖复制，存在 TOCTOU P1；现已改为锁定句柄校验、锁定期间 quarantine 和非覆盖安装。离线门禁在系统临时目录执行并清理，20/20 PASS；新增三项确定性竞态探针分别证明：初次检查后的并发修改不会被覆盖、目标被写句柄占用时内容不变且安全失败、隔离后原路径被重建时 replacement 不会覆盖新文件。原有错误兜底、静默期、CAS 成功/哈希拒绝/Partial、Burst、数组和物理成功证据重放探针继续全部 PASS。

最终唯一真实运行：`20260806-102207607Z-2a16a183`

- Unity PID `20040`，创建时间 `2026-08-06T10:22:12.0629607Z`，退出码 `0`，未超时、未终止；动态记录 16 个本次子进程，进程查询无错误。
- shutdown quiet period 为 `Succeeded`：4 次轮询；last-seen `10:23:02.8221455Z`，first-absent `10:23:08.0655496Z`，achieved `10:23:13.0309038Z`；无无关 Unity、无遗留 Lockfile。
- C# 报告 schema v2、BuildReport 均为 `Succeeded`；Unity `2022.3.62f2c1`，`StandaloneWindows64`，`Mono2x`，显式 `BuildWithPlayer`，场景恰好为批准的 Bootstrap 路径/GUID，BuildSummary 0 warnings/0 errors。
- 5 组 ISO/Unix 交叉检查差值均为 0 ms；PID 创建、entry、BuildPipeline 与退出观察的 Unix 毫秒顺序通过。BuildSummary 原始时间为 `Unspecified`，仅作诊断。
- Player 共 180 个文件、127,208,045 bytes，与 BuildReport 清单完全一致；本次没有额外 Burst 文件。`ElementWar.exe` 与 `UnityPlayer.dll` 为 AMD64；Mono runtime、`ElementWar_Data`、`Assembly-CSharp.dll` 均通过。
- Unity 日志：0 compiler warning、0 compiler error、3 条非阻断 licensing error、0 其他 fatal marker。
- 4 项已知副作用全部 CAS=`Succeeded`：两个原文件从本 run 的 prebuild backup 恢复，`link.xml` 与 `.meta` 移入本 run 证据目录。完整 Git 状态为 52→56→52，前后指纹同为 `3ba231c4ada47c0913574ae0eacb1c81322b5cb2d024f6f20163e9e86f023315`，范围外差异、正式文件差异和最终差异均为 0，index 为空。
- PowerShell 摘要 `result=Passed`。

证据：

- `Builds/Windows64/20260806-102207607Z-2a16a183/ElementWar.exe`
- `Logs/Verification/20260806-102207607Z-2a16a183-windows64/Windows64.log`
- `Logs/Verification/20260806-102207607Z-2a16a183-windows64/Windows64-build-report.json`
- `Logs/Verification/20260806-102207607Z-2a16a183-windows64/Windows64-verification-summary.json`
- `Logs/Verification/20260806-102207607Z-2a16a183-windows64/side-effects/recovery-result.json`

最新失败 run `20260806-085558518Z-aa758c37` 仍保持 Failed，未被补签；其 4 项遗留副作用已通过该 run 自身 manifest/backup 恢复，证据位于 `side-effects/manual-recovery-20260806-095656299Z-dd90cb6c/`。旧 Player、日志、报告、Failed 摘要和恢复副本均保留。

## 验证边界与回滚

关闭结论：Windows64 唯一真实构建已通过，离线门禁最终 20/20 PASS，最终独立复审无 P0/P1。本切片 Closed，但不扩大为整个项目 Full Verified 或 Accepted。

本状态只证明 Bootstrap-only Windows64 自动化切片。EditMode、PlayMode、生成 Player 启动、人工场景验收和性能检查均未运行，因此不声明 Full Verified 或 Accepted。

本次修复回滚单位为 `Tools/Verify-ElementWarWindows64.ps1` 与本 Spec；完整基线回滚单位为上述 6 个正式文件。构建输出与证据仅在另行授权后按精确 `runId` 处理。
