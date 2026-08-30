# 路线 04：集成、验证与发布

- 上级路线：[`DevelopmentRoadmap.md`](../DevelopmentRoadmap.md)
- 架构与旧路径：[`Architecture.md`](../Architecture.md)
- 流程：按项目 Skill 选择 Fast / Standard / Full；仅 Full 读取 [`Workflow.md`](../Workflow.md)
- 维护日期：2026-08-30

本路线包含贯穿主线的门禁任务。许可、组合根和验证不能全部拖到发布前；Legacy/Lua、旧场景与空配置壳也不能在没有消费者和迁移证据时顺手清理。

> Planned 任务只保留标题、依赖、可观察结果、明确非目标和解锁关系；不额外展开“当前缺口”、预定类型/Runtime/事务/生命周期或验证形状。Ready 可保留启动前必须知道的范围事实；Next 才可基于当前代码补有限实施约束。
> Planned 的边界条件只来自已确认 Design、当前真实调用路径或已复现缺陷；不得为了“完整性”枚举假设故障模式。标题描述能力/行为，不预定 Runtime、事务、状态机或其他实现形状。

## 已完成验证基线

- `VER-001` — EditMode 自动化基线 → [`EditModeAutomationBaseline.md`](../Features/EditModeAutomationBaseline.md)
- `VER-002` — PlayMode 自动化基线 → [`PlayModeAutomationBaseline.md`](../Features/PlayModeAutomationBaseline.md)
- `VER-003` — Bootstrap-only Windows64 自动化基线 → [`Windows64AutomationBaseline.md`](../Features/Windows64AutomationBaseline.md)

## P0 早期门禁

### LIC-010 第三方资产来源、许可与分发清单
- 状态：Ready
- 依赖：无。
- 当前缺口：仓库没有统一 `ThirdPartyAssets.md`；第二角色、动画、音频、VFX、场景资产和插件在公开作品集前无法逐项证明来源与许可。
- 可观察完成：每项主线第三方资产都有可审计条目，未知许可资产不会进入第二角色或发布候选。
- 范围：文档与只读资产引用审计；下载、替换或删除第三方资产不属于本审计任务，只有用户明确要求相应资产变更时才执行。
- 解锁：`CHR-020`、`LVL-010`、`REL-010`。

### VER-010 当前 HEAD 验证策略与统一入口
- 状态：Ready
- 依赖：`VER-001`、`VER-002`、`VER-003`。
- 当前缺口：EditMode、PlayMode、Windows64 三个脚本入口重复；阶段 2 统一脚本尚未接入；Windows 基线的可信起点与当前开发 HEAD 不再等同。
- 可观察完成：一次命令可运行批准的验证组合并准确报告每阶段；任一失败不被后续成功覆盖。
- 范围：Tools/Editor build/测试文档；修改验证脚本时检查退出码传播、失败不被后续成功覆盖和副作用恢复。非目标是 CI 或跨平台。
- 解锁：所有后续任务更低成本的按需验证、`BUILD-010`。

### ARC-010 Bootstrap 组合根与服务装配
- 状态：Planned
- 依赖：`WPN-040`。
- 可观察完成：Party/Run 等新所有者有唯一创建和销毁路径，测试可显式装配，场景重载不留下旧实例。
- 非目标：一次移除全部旧静态 API。
- 解锁：`PTY-010`、`RUN-010`、`ARC-020`。

### ARC-020 主线全局查找与 Active/Instance 收敛
- 状态：Planned
- 依赖：`ARC-010`；随各消费者任务分批完成。
- 可观察完成：主线 Gameplay 不通过全局搜索猜测活动角色/武器/Run，兼容入口的剩余调用有清单和删除条件。
- 解锁：`LEG-010`、发布前架构收口。

### VER-020 真实 scene/prefab/config 资产验证
- 状态：Planned
- 依赖：`ARC-010`、`ELM-040`、`PTY-030`。
- 可观察完成：关键主线资产能在干净导入后加载并执行最小行为，引用缺失在自动化阶段失败。
- 解锁：`RUN-090`、`BUILD-010`。

### CFG-010 Definition 配置壳与 Registry 收敛
- 状态：Planned
- 依赖：`ELM-100`、`SKL-060`、`RUN-030`、`UPG-010`。
- 可观察完成：主线加载的每类配置都有生产消费者与校验，无“看似可用但运行时忽略”的资产。
- 解锁：发布配置冻结。

### LEG-010 Legacy C# 与 Lua 主线引用审计及冻结
- 状态：Planned
- 依赖：`ARC-020`、`ELM-100`、`RUN-050`。
- 可观察完成：可以准确说明哪些旧代码仍编译/运行、哪些仅历史；新功能扫描不再增加旧路径引用。
- 解锁：`LEG-020`、发布体积/编译边界收口。

### LEG-020 旧场景、旧 WeaponView 与 Build Settings 迁移
- 状态：Planned
- 依赖：`LEG-010`、`WPN-060`、`RUN-090`。
- 可观察完成：发布 Player 只进入新主线场景且不实例化旧 WeaponView；旧场景处理状态有明确记录。
- 非目标：顺手删除全部旧资源。
- 解锁：`BUILD-010`、`REL-010`。

### UX-010 暂停、设置与基础可访问性
- 状态：Planned
- 依赖：`INP-010`、`HUD-030`、`RUN-050`。
- 可观察完成：暂停时批准系统停止、UI 仍可操作；恢复/重启/再次启动设置一致且不破坏冷却/无敌/引信。
- 解锁：发布候选体验门。

### PERF-010 代表性 Player 性能预算与回归
- 状态：Planned
- 依赖：`PRJ-050`、`RUN-030`、`BOS-030`。
- 可观察完成：发布候选在已声明条件下达到批准预算，或明确记录未达标项与裁剪决定。
- 解锁：`BUILD-010`、`REL-020`。

### BUILD-010 当前主线 Windows64 构建与启动 smoke
- 状态：Planned
- 依赖：`VER-010`、`VER-020`、`LEG-020`、`PERF-010`、`RUN-090`。
- 可观察完成：从全新 Windows64 输出可启动并完成 Must 流程，无 Missing Script/配置或旧场景入口。
- 非目标：安装器、签名、其他平台。
- 解锁：`REL-010`、`REL-020`。

### REL-010 许可、Credits、README 与作品集交付包
- 状态：Planned
- 依赖：`LIC-010`、`BUILD-010`、`CFG-010`。
- 可观察完成：陌生用户可按说明启动游戏，所有第三方内容可追溯，作品集描述不夸大未测结果。
- 解锁：`REL-020`。

### REL-020 发布候选审查与最终验收
- 状态：Planned
- 依赖：`RUN-090`、`BUILD-010`、`REL-010`。
- 可观察完成：无未处理 P0/P1 或违反 Must 验收，所有完成声明有证据；用户完成主线人工验收决定。
- 解锁：可发布候选。

### NET-010 双人网络实验

- 状态：Deferred
- 依赖：单机版本 `REL-020` 完成后重新评估。
- 延期理由：当前 `SourceObject`、Unity 对象引用、场景组合和大量可变运行时状态不是网络稳定身份；在单机状态所有权未稳定前接入联网会扩大所有契约和测试矩阵。
- 未来入口：先按 [`Networking.md`](../Design/Networking.md) 选一个隔离实验，定义网络实体/攻击 ID、服务器权威、输入同步、预测/回滚和断线恢复；不得把本地 Unity 引用当网络标识。
- 非目标：首版作品集不承诺联网、跨机同步或在线服务。
