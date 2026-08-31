# 功能：范围目标查询与友伤过滤 v1

- 状态：Closed
- 验证：Verified
- 维护日期：2026-08-30
- 关联 ADR：[`ADR-Combat-Range-Target-Query-v1.md`](../Decisions/ADR-Combat-Range-Target-Query-v1.md)

## Current
- 可观察目标：同一球形物理场景重复查询得到相同的合法战斗目标集合与顺序；同一目标的多个 Collider 只产生一个结果。
- 非目标：具体超载、爆炸、感电、伤害衰减、控制、VFX/SFX/HUD、玩家自伤例外、EnemyAttack 形状迁移和性能优化。
- `Combatant`、`CombatantId`、`CombatTargetResolver`、`CombatFactionRules` 与 `HealthComponent` 分别提供权威目标根、活动身份、Collider 根解析、首版阵营矩阵和生命事实。
- 新增 `CombatRangeTarget`，只携带目标、最近 Collider 表面点和距离。后两项是 `PRJ-020` 距离衰减与 `ELM-070` 最近目标选择已批准需要、且 Collider 去重后无法由调用方可靠重建的几何事实。
- 查询固定使用球形 Physics 查询并忽略 Trigger；LayerMask 只粗筛，随后解析活动 Combatant、剔除死亡/禁用目标并使用 `CombatFactionRules.CanDamage` 过滤阵营。
- 多 Collider 以当前 `CombatantId` 去重并保留最近表面事实；同距 Collider 使用最近点坐标稳定选择。最终按表面距离、CombatantId 排序，LOS 后应用数量上限。

## Evidence
| 层级 | 内容 | 结果 | 证据 |
|---|---|---|---|
| EditMode | 目标解析、过滤、边界、Trigger、排序与上限 | 64/64 通过；本功能 4/4 | `Logs/Verification/20260830-211319/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 真实 Physics 多 Collider、友伤、LOS、禁用复用与 Overload 生产接入 | 15/15 通过；本功能 3/3 | `Logs/Verification/20260830-211458-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
- 验证结论：当前切片所需的 EditMode 与 PlayMode 证据均针对当前源码通过；Windows64 构建和主线人工验收未运行。
## Remaining Boundaries
- 未运行与剩余风险：Windows64、主线人工验收和性能检查未运行；`OverloadReactionResolver` 已成为首个生产消费者，Player 分配与高密度场景性能仍未测量。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
