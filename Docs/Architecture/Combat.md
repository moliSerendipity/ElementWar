# Combat 架构事实

只在任务涉及战斗目标、范围查询、伤害或生命事实时读取。

## 主链

```
WeaponFireExecutor → AttackExecutionId → HitScanService ─┐
EnemyAttack        → AttackExecutionId → Collider Query ─┴→ CombatTargetResolver
                                                           → DamageRequest (身份快照)
                                                           → DamageResolver (身份/阵营/去重)
                                                           → HealthComponent
                                                           → DamageResult / DamageAppliedEvent / Health Events
                                                           → Presentation / 其他消费者
```

## Current

- `Combatant` 是权威战斗目标根和阵营事实所有者；子 Collider 先解析到最近的活动 `Combatant`。
- `CombatRangeQuery` 是球形范围目标集合的共享 Gameplay 裁决入口：忽略 Trigger，复用活动目标解析、生命与阵营规则，按当前 `CombatantId` 去重，可选环境 LOS；结果 `CombatRangeTarget` 保存目标、最近表面点和距离，并按距离、CombatantId 稳定排序。查询不持有状态或缓存。
- `DamageRequest` 创建时冻结 `AttackExecutionId`、责任 `CombatantId` 和目标 `CombatantId`；`DamageResult` 与 Combat 事件保留这些身份，`SourceObject` 保存具体来源。
- `DamageAppliedEvent` 只表达“伤害已提交”；步枪对任意物理表面的原始命中由 `WeaponFiredEvent.HadHit` 表达。
- `DamageResolver` 裁决运行时身份、阵营许可和目标侧重复执行；首版只允许 `PlayerParty ↔ Enemy`，同阵营与 `Unassigned` 拒绝。同一执行对同一目标至多提交一次，但可分别命中不同目标。
- `Combatant` 禁用时身份失效并清空生命伤害/敌人控制执行去重、结束元素附着生命周期；重新启用建立新身份。生命耗尽保留身份，由 `HealthComponent` 拒绝后续伤害。
- 解析公式确定性，不包含随机暴击；头部与弱点只应用明确倍率。
- `HealthComponent.CurrentHealth` 是生命唯一数值事实，`IsHealthDepleted` 由初始化状态和当前生命值派生。

## ADR

- `ADR-Combat-Domain-Contract-v1.md`
- `ADR-Combatant-Faction-Execution-Identity-v1.md`
- `ADR-Combat-Range-Target-Query-v1.md`
