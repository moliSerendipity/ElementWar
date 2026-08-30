# 路线 01：战斗、元素与武器

- 上级路线：[`DevelopmentRoadmap.md`](../DevelopmentRoadmap.md)
- 目标设计：[`Combat.md`](../Design/Combat.md)、[`Elements.md`](../Design/Elements.md)
- 当前架构：[`Architecture.md`](../Architecture.md)
- 维护日期：2026-08-30

本路线先建立可复用战斗身份，再完成一个“开火 → 附着 → 超载 → 范围伤害/控制 → 反馈”的真实闭环，随后才扩展武器实例、弹药、投射物和其余反应。任务默认只记录目标与可观察结果；Feature Spec 仅在 Full 或确有长期契约需要时建立。

> Planned 任务只保留标题、依赖、可观察结果、明确非目标和解锁关系；不额外展开“当前缺口”、预定类型/Runtime/事务/生命周期或验证形状。Ready 可保留启动前必须知道的范围事实；Next 才可基于当前代码补有限实施约束。
> Planned 的边界条件只来自已确认 Design、当前真实调用路径或已复现缺陷；不得为了“完整性”枚举假设故障模式。标题描述能力/行为，不预定 Runtime、事务、状态机或其他实现形状。

## 已完成

- `CMB-001` — Combat Domain Contract v1 → [`CombatDomainContractV1.md`](../Features/CombatDomainContractV1.md)
- `CMB-010` — 战斗目标、阵营与攻击执行身份 → [`CombatantFactionExecutionIdentityV1.md`](../Features/CombatantFactionExecutionIdentityV1.md)
- `ELM-010` — 元素施加配置与快照契约 → [`ElementApplicationProfileSnapshotV1.md`](../Features/ElementApplicationProfileSnapshotV1.md)
- `ELM-020` — 元素附着运行时与生命周期 → [`ElementAttachmentRuntimeLifecycleV1.md`](../Features/ElementAttachmentRuntimeLifecycleV1.md)
- `ELM-030` — 反应判定、消耗与归因管线 → [`ElementReactionPipelineV1.md`](../Features/ElementReactionPipelineV1.md)
- `CMB-020` — 范围目标查询与友伤过滤 → [`CombatRangeTargetQueryV1.md`](../Features/CombatRangeTargetQueryV1.md)
- `CMB-030` — 韧性、失衡与受控状态事实 → [`EnemyToughnessAndControlFactsV1.md`](../Features/EnemyToughnessAndControlFactsV1.md)
- `WPN-010` — 步枪最小火/雷元素来源。

## M01 首个元素反应垂直切片

### WPN-010 步枪最小火/雷元素来源
- 状态：Done
- 依赖：`ELM-020`。
- 当前事实：当前步枪实例以 `T / Combat/IsSwitchAmmo` 在 Fire/Electric 间即时切换，`PlayerInputReader` 通过序列化 `InputActionReference` 接入该 Action；`WeaponRuntime` 保存每条元素通道的稳定来源身份，开火成立后、Hitscan 前冻结来源，伤害与元素请求共享同一 `AttackExecutionId`。Ammo HUD 与命中调试日志显示当前/实际元素。
- 可观察完成：玩家可连续用火与雷命中同一敌人，攻击日志和附着事实与开火瞬间选择一致。
- 范围：当前步枪、输入适配和最小反馈。
- 当前证据：EditMode `64/64`（`WeaponElementInputContractTests` `1/1`），PlayMode `15/15`（`RifleElementSelectionIsFrozenBeforeHitAndFeedsReactionPipeline` `1/1`）；Windows64 与 Bootstrap 人工游玩未运行。
- 解锁：`ELM-040`。

### ELM-040 超载范围伤害与控制闭环
- 状态：Next
- 依赖：`ELM-030`、`CMB-020`、`CMB-030`、`WPN-010`。
- 可观察完成：Bootstrap 中用两种元素触发一次超载，附近合法敌人各受一次确定伤害/控制，责任归属正确，事件与反馈不重复。
- 非目标：其余反应、完整武器库存和最终美术品质。
- 解锁：`WPN-020`、`ELM-050`，并形成首个可玩元素里程碑。

### WPN-020 WeaponRuntime 职责与开火时序重构
- 状态：Planned
- 依赖：`ELM-040`。
- 可观察完成：现有步枪玩家行为不变，重复动画事件不能重复开火或结算，职责可被后续多武器复用。
- 非目标：新增武器内容。
- 解锁：`WPN-025`。

### WPN-025 步枪射击、瞄准、散布与后坐力
- 状态：Planned
- 依赖：`WPN-020`。
- 可观察完成：瞄准明显更准确，连续射击产生可学习上抬；相同种子可复现散布，Presentation 丢失不会改变命中裁决。
- 非目标：武器获取、改装和随机暴击。
- 解锁：`WPN-030`、`HUD-010`。

### WPN-030 多武器实例与角色装备
- 状态：Planned
- 依赖：`WPN-025`。
- 可观察完成：两个同定义武器实例可保持各自独立状态，角色装备关系明确且互不覆盖。
- 解锁：`WPN-040`。

### WPN-040 角色、武器与元素弹药隔离
- 状态：Planned
- 依赖：`WPN-030`、`ELM-040`。
- 可观察完成：两个角色、两个武器、多个元素账户互不串弹；初始化结果与配置一致。
- 非目标：装填动画与补给 UI。
- 解锁：`WPN-050`、`ARC-010`、`PTY-010`。

### WPN-050 普通/特殊装填与取消
- 状态：Planned
- 依赖：`WPN-040`、`WPN-020`。
- 可观察完成：装填成功时弹药守恒，取消装填不提交未完成的弹药变更。
- 解锁：`WPN-060`。

### WPN-060 双槽武器切换与 HUD
- 状态：Planned
- 依赖：`WPN-050`、`INP-010`。
- 可观察完成：玩家可在两武器间切换，弹药/元素/装填状态随实例正确恢复，UI 不短暂显示另一角色数据。
- 解锁：`PRJ-010`、`AI-020`、`PTY-030`。

### PRJ-010 投射物发射后归因与命中
- 状态：Planned
- 依赖：`WPN-060`、`CMB-010`。
- 可观察完成：发射后立刻切换角色或武器，命中仍归属原攻击且只提交一次。
- 解锁：`PRJ-020`。

### PRJ-020 统一爆炸输出
- 状态：Planned
- 依赖：`PRJ-010`、`CMB-020`。
- 可观察完成：爆炸对每个合法目标一次结算，中心/边缘与遮挡结果确定，Instigator/Source/Execution 保留。
- 解锁：`PRJ-030`、`PRJ-040`、`ELM-070`。

### PRJ-030 榴弹发射器垂直切片
- 状态：Planned
- 依赖：`PRJ-020`、`WPN-030`。
- 可观察完成：第二槽可稳定发射榴弹，换枪后在途榴弹仍正确归因并按策略自伤/友伤。
- 解锁：`PRJ-050`、Run 中的重武器升级内容。

### PRJ-040 手雷投掷、轨迹、反弹与引信
- 状态：Planned
- 依赖：`PRJ-020`、`INP-010`。
- 可观察完成：预览与实际落点在容差内，每次手雷只产生一次爆炸。
- 解锁：`PRJ-050`。

### PRJ-050 投射物与范围效果复用
- 状态：Planned
- 依赖：`PRJ-030`、`PRJ-040`。
- 可观察完成：连续复用不出现旧来源、旧元素、重复爆炸或事件泄漏。
- 解锁：`PERF-010`、大量敌人/波次场景。

### ELM-050 榴弹枪 Water/Ice 可玩来源
- 状态：Planned
- 依赖：`ELM-040`、`PRJ-030`、`WPN-050`。
- 可观察完成：步枪提供 Fire/Electric、榴弹枪提供 Water/Ice，四元素都能从玩家生产链稳定施加且来源可追溯。
- 解锁：`ELM-060`～`ELM-090`。

### ELM-060 Vaporize 与 Melt 伤害放大阶段
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`。
- 可观察完成：顺序和数值确定，重复解析一致，反应放大不生成第二次伤害事件。
- 解锁：`ELM-100`。

### ELM-070 Electro-Charged 连锁目标选择
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-020`。
- 可观察完成：相同场景总选中相同目标，遮挡和上限生效，无循环跳转。
- 解锁：`ELM-100`。

### ELM-080 Freeze 冻结与解冻
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-030`。
- 可观察完成：冻结可按规则开始和结束，结束或目标失效后 AI 恢复正常行为。
- 解锁：`ELM-100`、`BOS-050`。

### ELM-090 Superconduct 减速与非反应易伤
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-030`、`MOD-010`。
- 可观察完成：持续 5 秒期间移动与非反应伤害按规则变化，反应伤害不被放大，到期后属性恢复到施加前值。
- 解锁：`ELM-100`。

### ELM-100 六反应集成、反馈与回归门
- 状态：Planned
- 依赖：`ELM-060`、`ELM-070`、`ELM-080`、`ELM-090`。
- 可观察完成：六种反应都能在主线场景复现，元素对无空洞/歧义，反馈与已提交事实一致。
- 解锁：`BOS-050`、最终战斗内容收口。
