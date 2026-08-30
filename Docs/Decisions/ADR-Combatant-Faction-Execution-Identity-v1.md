# ADR：Combatant、阵营与攻击执行身份 v1

- 状态：Accepted
- 日期：2026-08-11
- 关联 Feature 记录：[`CombatantFactionExecutionIdentityV1.md`](../Features/CombatantFactionExecutionIdentityV1.md)

## Context

`CMB-001` 已统一确定性伤害主链，但请求仍以 `GameObject` 和 `HealthComponent` 表达责任者与目标，没有运行时目标身份、阵营或攻击执行身份。Hitscan 按单次 Raycast 命中，而 `EnemyAttack` 对 Overlap 返回的 Collider 逐个提交；多 Collider、范围伤害、友伤、AI 选敌和后续元素归因会因此各自建立临时判断。

当前 Bootstrap 依靠 Player/Enemy LayerMask 粗略隔离生产攻击，但玩家武器查询包含全部层，且公共伤害入口本身不验证阵营。LayerMask 不能承担稳定目标身份、结果归因或跨物理查询一致性的职责。

## Decision

选择方案 A。`Combatant` 是目标根与阵营事实所有者；`CombatantId` 和 `AttackExecutionId` 只表达当前运行期身份。DamageResolver 是最终阵营、目标生命周期与重复执行裁决点，LayerMask 仅保留为物理查询优化。

首版阵营矩阵只允许 `PlayerParty ↔ Enemy`。`Unassigned`、同阵营和自身伤害均默认拒绝；玩家爆炸的来源自身例外必须由后续任务以显式策略加入，不能把“同阵营可伤害”作为隐式全局开关。

## Rationale
- 玩家结果：一次攻击不能因多个 Collider 重复伤害同一实体，同阵营不能互伤。
- 架构约束：权威目标、生命和已提交伤害都留在 Gameplay；Presentation 只消费结果。
- 生命周期：禁用、复用和迟到请求不能把旧身份或去重记录带入新实体生命周期。
- 扩展性：元素、范围查询、投射物和 Party 后续应复用同一目标/执行身份，但本阶段不引入网络 ID 或完整威胁系统。
- 完成风险：Bootstrap 只有三个活动主线生命根，适合一次显式、小范围序列化迁移。

## Consequences
- 所有命中方式可把子 Collider 解析为同一权威目标。
- 结果与事件可以稳定关联责任者、目标和一次攻击执行。
- 同一执行对同一目标至多提交一次，同阵营规则不能被单个生产者遗漏。
- 禁用/复用会失效旧 ID，迟到请求不能写入新活动生命周期。
- 当前公共伤害请求、结果和事件构造函数必须一次迁移，旧调用方不保留兼容重载。
- 每个活动 Combatant 保存精确执行集合；首版以正确性优先，不用有限窗口静默拒绝合法迟到攻击。后续只有在代表性 Player 数据证明有必要时才优化。
- 身份不跨运行稳定，不可作为网络、存档或配置 ID。
- 完整 AI 选敌、敌人攻击时序和对象池租借/归还仍由后续路线任务完成。

> 旧备选方案展开、迁移步骤、验证流水账和回滚过程由 Git 历史追溯；当前开发只依赖本页决定、活动 Architecture/Design 与代码。
