# 功能：战斗目标、阵营与攻击执行身份 v1

- 状态：Verified
- 维护日期：2026-08-11
- 关联 ADR：[`ADR-Combatant-Faction-Execution-Identity-v1.md`](../Decisions/ADR-Combatant-Faction-Execution-Identity-v1.md)

## Current
- 可观察目标：同一攻击执行通过多个 Collider 命中同一战斗目标时只提交一次伤害；敌人不能伤害敌方阵营；已提交结果和事件能关联一次运行时攻击执行与一个权威目标。
- 非目标：元素附着/反应、Party 切换、网络实体 ID、完整威胁系统、敌人完整攻击时序、敌人池化重构、玩家爆炸自伤策略、Legacy/Lua 或旧场景迁移。
- 当前程序集与伤害主链以 [`Architecture.md`](../Architecture.md) 为准；此前 `CMB-001` 已统一 `DamageRequest → DamageResolver → HealthComponent → DamageResult/Event`，证据见 [`CombatDomainContractV1.md`](CombatDomainContractV1.md)。
- `Combatant` 是 Gameplay 中的权威战斗目标根，持有 `HealthComponent` 引用、首版阵营、当前活动生命周期的 `CombatantId` 和已接受攻击执行集合。
- `CombatantId` 与 `AttackExecutionId` 是只在当前运行期有意义的强类型递增身份；`0` 为无效值，不序列化，也不承诺网络或跨运行稳定性。
- Collider 通过统一目标解析器向父级寻找最近的有效 `Combatant`。LayerMask 只做物理粗筛，最终目标合法性由战斗契约裁决。

## Evidence
| 层级 | 内容 | 结果 | 证据 |
|---|---|---|---|
| EditMode | 身份、阵营矩阵、拒绝原因、去重、`DamageAppliedEvent` 完整表现载荷与既有伤害公式 | 26/26 通过；`DamageContractTests` 21/21 | `Logs/Verification/20260811-220515/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 步枪、EnemyAttack、多 Collider、同阵营、禁用复用与事件生命周期 | 5/5 通过；`DamageProducerPlayModeTests` 4/4 | `Logs/Verification/20260811-220613-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
| 序列化 | Bootstrap Combatant 数量、阵营和 GUID/引用 | 3/3 生命根装配：玩家 `PlayerParty`，两处敌人 `Enemy`；脚本 GUID 对应唯一 `.meta`，场景引用 3 处 | [`Bootstrap.unity`](../../Assets/Scenes/Bootstrap/Bootstrap.unity) 与下方可复现扫描命令 |
- 验收等级：达到 Fast Verified，并额外完成路线要求的 PlayMode 与序列化证据。Windows64、主线场景人工验收和性能检查未运行，因此不声明 Full Verified 或 Accepted。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
