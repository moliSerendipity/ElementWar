# 路线 01：战斗、元素与武器

- 上级路线：[`DevelopmentRoadmap.md`](../DevelopmentRoadmap.md)
- 目标设计：[`Combat.md`](../Design/Combat.md)、[`Elements.md`](../Design/Elements.md)
- 当前架构：[`Architecture.md`](../Architecture.md)
- 维护日期：2026-08-30

本路线先建立可复用战斗身份，再完成一个“开火 → 附着 → 超载 → 范围伤害/控制 → 反馈”的真实闭环，随后才扩展武器实例、弹药、投射物和其余反应。任务默认只记录目标与可观察结果；Feature Spec 仅在 Full 或确有长期契约需要时建立。

> Planned 任务只保留标题、依赖、可观察结果、明确非目标和解锁关系；不额外展开“当前缺口”、预定类型/Runtime/事务/生命周期或验证形状。Ready 可保留启动前必须知道的范围事实；Next 才可基于当前代码补有限实施约束。

## 已完成

- `CMB-001` — Combat Domain Contract v1 → [`CombatDomainContractV1.md`](../Features/CombatDomainContractV1.md)
- `CMB-010` — 战斗目标、阵营与攻击执行身份 → [`CombatantFactionExecutionIdentityV1.md`](../Features/CombatantFactionExecutionIdentityV1.md)
- `ELM-010` — 元素施加配置与快照契约 → [`ElementApplicationProfileSnapshotV1.md`](../Features/ElementApplicationProfileSnapshotV1.md)
- `ELM-020` — 元素附着运行时与生命周期 → [`ElementAttachmentRuntimeLifecycleV1.md`](../Features/ElementAttachmentRuntimeLifecycleV1.md)
- `ELM-030` — 反应判定、消耗与归因管线 → [`ElementReactionPipelineV1.md`](../Features/ElementReactionPipelineV1.md)
- `CMB-020` — 范围目标查询与友伤过滤 → [`CombatRangeTargetQueryV1.md`](../Features/CombatRangeTargetQueryV1.md)
- `CMB-030` — 韧性、失衡与受控状态事实 → [`EnemyToughnessAndControlFactsV1.md`](../Features/EnemyToughnessAndControlFactsV1.md)

## M01 首个元素反应垂直切片

### WPN-010 步枪最小火/雷元素来源
- 状态：Next
- 依赖：`ELM-020`。
- 当前缺口：当前 Hitscan 步枪只产生 `None/Direct`；尚无可玩方式验证附着与反应管线。
- 当前约束：① 为当前武器实例增加最小、显式的 Fire/Electric 选择；② 把选择快照进攻击执行而不是命中后读取可变状态；③ 通过已有输入中的批准按键或最小临时调试入口切换；④ HUD/调试反馈显示当前元素；⑤ 不提前实现完整武器/弹药库存。
- 可观察完成：玩家可连续用火与雷命中同一敌人，攻击日志和附着事实与开火瞬间选择一致。
- 范围：当前步枪、输入适配和最小反馈。临时入口必须在 `WPN-060` 前迁移或删除。
- 解锁：`ELM-040`。

### ELM-040 超载范围伤害与控制闭环
- 状态：Planned
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

### WPN-030 武器定义、运行时实例与角色 Loadout
- 状态：Planned
- 依赖：`WPN-025`。
- 可观察完成：两个同定义武器实例可拥有不同状态，角色切换或禁用后不会错误共享。
- 解锁：`WPN-040`。

### WPN-040 角色 × 武器 × 元素弹药所有权
- 状态：Planned
- 依赖：`WPN-030`、`ELM-040`。
- 可观察完成：两个角色、两个武器、多个元素账户互不串弹；失败重开后回到配置初值。
- 非目标：装填动画与补给 UI。
- 解锁：`WPN-050`、`ARC-010`、`PTY-010`。

### WPN-050 普通/特殊装填事务与取消
- 状态：Planned
- 依赖：`WPN-040`、`WPN-020`。
- 可观察完成：任意取消边界下弹药守恒，迟到动画事件不会给新武器或新角色装弹。
- 解锁：`WPN-060`。

### WPN-060 双槽武器切换与 HUD
- 状态：Planned
- 依赖：`WPN-050`、`INP-010`。
- 可观察完成：玩家可在两武器间切换，弹药/元素/装填状态随实例正确恢复，UI 不短暂显示另一角色数据。
- 解锁：`PRJ-010`、`AI-020`、`PTY-030`。

### PRJ-010 投射物攻击快照与命中提交
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
- 可观察完成：预览与实际落点在容差内，迟到回调/多次碰撞不重复爆炸。
- 解锁：`PRJ-050`。

### PRJ-050 投射物与范围效果池化生命周期
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
- 可观察完成：冻结不会让 AI 永久卡死，死亡、禁用、复用后无残留。
- 解锁：`ELM-100`、`BOS-050`。

### ELM-090 Superconduct 减速与非反应易伤
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-030`、`MOD-010`。
- 可观察完成：持续 5 秒期间移动与非反应伤害按规则变化，反应伤害不被放大，到期精确恢复且无浮点累计漂移。
- 解锁：`ELM-100`。

### ELM-100 六反应集成、反馈与回归门
- 状态：Planned
- 依赖：`ELM-060`、`ELM-070`、`ELM-080`、`ELM-090`。
- 可观察完成：六种反应都能在主线场景复现，元素对无空洞/歧义，反馈与已提交事实一致。
- 解锁：`BOS-050`、最终战斗内容收口。
