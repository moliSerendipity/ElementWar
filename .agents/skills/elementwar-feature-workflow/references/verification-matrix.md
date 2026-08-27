# ElementWar 验证矩阵

只在设计或执行测试、构建、性能检查时读取。根据行为风险选择验证，不按文件数量选择。

## 风险到证据

| 变化 | 最低验证 | 按需增加 |
|---|---|---|
| 纯规则、数值、配置校验 | 精确 EditMode | 边界值、非法数据 |
| Gameplay 可变状态 | EditMode + 精确 PlayMode | 禁用、死亡、重置、对象池复用 |
| MonoBehaviour 生命周期/事件 | PlayMode | 重启用、场景卸载、重复订阅 |
| NavMesh、物理、帧或计时 | PlayMode | 容差、目标隔离、重复触发 |
| scene/prefab/序列化 | PlayMode + 引用扫描 + 人工场景检查 | 迁移与旧数据兼容 |
| 公共接口/asmdef/架构 | 编译 + 受影响测试 + scoped review | ADR、调用方扫描、独立审查 |
| 玩家可见里程碑 | Full Verified + Bootstrap smoke | 截图或录像 |
| 性能声明 | 代表性 Player Profiling | 同设备、同场景的前后数据 |

## 当前仓库入口

保存并关闭 Unity Editor，确认没有 `Temp/UnityLockfile`，再从项目根按需要运行：

```powershell
pwsh -File .\Tools\Verify-ElementWarEditMode.ps1
pwsh -File .\Tools\Verify-ElementWarPlayMode.ps1
pwsh -File .\Tools\Verify-ElementWarWindows64.ps1
```

不存在 `Tools/Verify-ElementWar.ps1` 统一入口，不得自行假定。Windows64 脚本负责启用场景、旧 SampleScene、构建报告、输出边界和恢复安全检查；不要在普通功能中绕过它。

## 结果判定

- 测试 XML 必须含 `/test-run`，且 `total > 0`、`failed = 0`；Unity/脚本退出码必须为 0。不得删除或弱化测试只为得到绿色结果。
- Windows64 必须生成成功报告和预期 Player。保留 XML、日志、摘要、Player 或其他原始证据路径。
- 证据必须晚于当前源码；超时后先检查进程、锁和摘要，不直接判定测试失败。每次 Unity 运行后检查并精确还原无关的 Project Settings 自动改动。
- 未执行项写“未运行”。人工证据记录版本、配置、场景、步骤、预期和观察；性能结论记录设备、构建、场景、采样和基线，不把 Editor 数据泛化为 Player。
