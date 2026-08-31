# ElementWar 验证矩阵

只在设计或执行测试、构建、性能检查时读取。根据行为风险选择验证，不按文件数量选择。

## 风险到证据

额外验证只覆盖仓库当前真实存在或本任务明确引入的路径。本表出现某类风险不构成新增实现、防御代码或测试的理由。

| 变化 | 最低验证 | 仅在真实路径存在时增加 |
|---|---|---|
| 纯规则、数值、配置校验 | 精确 EditMode | 当前已定义的边界/非法输入 |
| Gameplay 可变状态 | 精确 EditMode；需要帧/组件时加 PlayMode | 当前实际启停、死亡、重置或复用路径 |
| MonoBehaviour 生命周期/事件 | 精确 PlayMode | 当前真实重启用、场景卸载或订阅路径 |
| NavMesh、物理、帧或计时 | 精确 PlayMode | 当前玩法要求的容差、低帧率或目标隔离 |
| scene/prefab/序列化普通配置 | 目标资产加载/必要 PlayMode | 仅对象引用、GUID、结构或契约变化时检查受影响引用；迁移时再检查旧数据兼容 |
| 公共接口/asmdef/架构 | 编译 + 受影响测试 + scoped review | 只有长期决策变化时 ADR/独立审查 |
| 玩家可见里程碑 | 当前切片自动化 + Bootstrap smoke | 需要人工体验证明时截图/录像 |
| 性能声明 | 代表性 Player Profiling | 需要比较时同设备、同场景前后数据 |

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
- 证据必须对应当前**行为版本**：行为代码、序列化元数据/契约或测试逻辑变化后，受影响证据需要重跑；纯注释、Markdown、格式或其他不影响执行结果的文本修改不使运行时证据失效。超时后先检查进程、锁和摘要，不直接判定测试失败。每次 Unity 运行后检查并精确还原无关的 Project Settings 自动改动。
- 未执行项写“未运行”。人工证据记录版本、配置、场景、步骤、预期和观察；性能结论记录设备、构建、场景、采样和基线，不把 Editor 数据泛化为 Player。
- 完整 XML/日志保存在证据目录；对话/终端只输出数量摘要、失败项和相关错误区间，遵守 Skill 的 Tool Output Budget。外部许可/系统弹窗阻塞 Agent 自己启动的验证时，不高频轮询，也不为关闭弹窗额外加载大型 Computer Use 上下文；确认无有效输出后终止该进程并换已知验证入口。
- 若当前任务不修改 scene/prefab/序列化引用，且代码与自动化测试能够建立所需行为，不为了验证“当前是否已装配”额外读取 `.meta` / GUID / Unity YAML；只有验收确实依赖现有场景配置时才定位目标对象/字段的必要区间。
