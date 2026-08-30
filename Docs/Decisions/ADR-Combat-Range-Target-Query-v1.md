# ADR：范围战斗目标查询 v1

- 状态：Accepted
- 日期：2026-08-26
- 关联 Feature 记录：[`CombatRangeTargetQueryV1.md`](../Features/CombatRangeTargetQueryV1.md)

## Context

`CMB-010` 已建立 Collider 到权威 `Combatant` 的解析、阵营矩阵和目标侧执行去重，但范围效果仍需各自处理多 Collider、死亡/禁用、友伤、距离、遮挡和顺序。`ELM-040`、`PRJ-020` 与 `ELM-070` 已分别批准复用统一目标集合，其中爆炸衰减和感电最近目标选择需要一致的几何距离事实。

## Decision

选择方案 A。输入继续使用一个明确方法签名，不增加 Request；输出使用 `CombatRangeTarget` 保存 Collider 去重后无法可靠重建的目标、最近点与距离。查询按最近表面距离、CombatantId 确定排序，可选 LOS 后应用数量上限。

## Rationale
- 玩家结果：同一物理场景必须产生确定的目标集合和顺序，每个权威目标只出现一次。
- 架构约束：Gameplay 决定目标集合；Presentation 不重新裁决命中，DamageResolver 继续负责最终伤害提交。
- 扩展成本：消费者各自从 Combatant 根重算距离会与 Collider 范围事实分叉；当前输入本身没有独立状态或生命周期。
- 性能与完成风险：固定 NonAlloc 缓冲区可能静默截断拥挤场景，而当前没有 Player 数据证明需要缓存或池化。

## Consequences
- 超载、统一爆炸和感电可以共享同一目标集合与几何事实。
- 多 Collider、当前阵营、生命和活动身份只在查询入口统一处理。
- 没有新增 MonoBehaviour、ScriptableObject、事件、缓存、并行状态或表现层裁决。
- v1 仅支持球形查询并固定忽略 Trigger；EnemyAttack 的扇形/盒形不迁移。
- LOS 阻挡层必须只包含环境遮挡物；玩家自身伤害仍由 `PRJ-020` 建立显式例外。
- 当前不声明零分配或性能提升；性能优化必须由代表性 Player 数据触发。

> 旧备选方案展开、迁移步骤、验证流水账和回滚过程由 Git 历史追溯；当前开发只依赖本页决定、活动 Architecture/Design 与代码。
