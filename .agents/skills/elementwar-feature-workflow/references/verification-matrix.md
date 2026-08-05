# ElementWar 验证矩阵

只在设计或执行测试、构建、性能检查时读取本文件。根据行为风险选择验证，不按文件数量选择。

## 测试层级

| 变化 | 最低验证 | 按需增加 |
|---|---|---|
| 纯规则、数值、配置校验 | 精确 EditMode | 边界值与非法数据 |
| Gameplay 运行时状态 | EditMode + 精确 PlayMode | 重置、死亡、对象池复用 |
| MonoBehaviour 生命周期/事件 | PlayMode | 禁用、重启用、场景卸载 |
| NavMesh、物理、帧或计时 | PlayMode | 容差、目标隔离、重复触发 |
| scene/prefab/序列化结构 | PlayMode + 人工场景检查 | 迁移和引用扫描 |
| 公共接口/asmdef/架构 | 编译 + 受影响测试 + 只读审查 | ADR 和调用方扫描 |
| 玩家可见里程碑 | Full + Bootstrap smoke test | 截图或录像 |
| 性能声明 | 代表性 Player Profiling | 基线与修改后数据 |

## 阶段 2 统一入口

仅当仓库中已存在 `Tools/Verify-ElementWar.ps1`、测试程序集和构建入口时，保存并关闭 Unity Editor，再从项目根使用 PowerShell 7：

```powershell
pwsh -File .\Tools\Verify-ElementWar.ps1 -Mode Fast
pwsh -File .\Tools\Verify-ElementWar.ps1 -Mode Full
```

- Fast：EditMode 测试。
- Full：EditMode、PlayMode 和 Windows64 Player。
- 不要让命令行 Editor 与已经打开的同一工程竞争；脚本应检查 `Temp/UnityLockfile`。
- 阶段 2 未接入时，先检查仓库当前可用的测试和构建入口；不存在的检查明确标记“未运行”。

## 结果判定

- 必须生成含 `/test-run` 的测试 XML。
- 必须满足 `total > 0`、`failed = 0`。
- 缺少 XML、缺少 Player、非零退出码或零测试均为失败。
- 保留 Unity 退出码、测试数量、XML、日志和构建输出路径。
- 跳过的检查标记“未运行”，不得用旧 Editor 日志代替当前证据。

## Windows64 构建保护

候选 `Game.Editor.Build.ElementWarBuild.BuildWindows64` 应：

- 使用 EditorBuildSettings 中启用的场景。
- 没有启用场景时失败。
- 旧 `Assets/Scenes/SampleScene.unity` 仍启用时失败。
- 输出 `Builds/Windows64/ElementWar.exe`。
- BuildReport 不是 Succeeded 时抛出失败。

不要在无关功能里通过静默过滤场景绕过旧架构边界。

## 人工与性能证据

玩家可见行为记录构建版本、配置、场景、步骤、预期、观察结果和必要的日志/录像。性能声明记录设备、构建类型、场景、采样时长、指标、基线与修改后数据；不要把 Editor 数据泛化为 Player 结论。
