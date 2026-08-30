# 功能：Combat Domain Contract v1

- 状态：Closed
- 验证：Verified
- 维护日期：2026-08-08
- 关联 ADR：[`ADR-Combat-Domain-Contract-v1.md`](../Decisions/ADR-Combat-Domain-Contract-v1.md)

## Current
- 可观察目标：步枪 Hitscan 与 `EnemyAttack` 通过同一确定性伤害主链，结果明确保留责任角色、攻击来源、元素、传递形态和生命耗尽事实。
- 非目标：元素附着与反应、投射物/榴弹枪/手雷、双角色/AI 队友/倒地/复活、技能/Buff/波次/Boss/网络、全面拆分 `WeaponRuntime`、Legacy/Lua 清理。
- 当前伤害链与程序集边界以 [`Architecture.md`](../Architecture.md) 为准；设计约束以 [`Combat.md`](../Design/Combat.md) 和 [`Elements.md`](../Design/Elements.md) 为准。
- `Instigator` 是承担伤害/击杀归属的角色或敌人 `GameObject`；`SourceObject` 是具体武器运行时或攻击配置 `UnityEngine.Object`。
- 当前步枪映射为角色根 + `WeaponRuntime` + `None/Direct`；当前敌人攻击映射为敌人根 + `EnemyAttackConfig` + 配置元素/传递形态。
- `HealthComponent.CurrentHealth` 是唯一存储的生命事实；`IsHealthDepleted` 从已初始化且生命值不大于零派生。`CharacterFacts` 只读引用该事实，不保存第二个死亡布尔值。

## Evidence
| 层级 | 内容 | 结果 | 证据 |
|---|---|---|---|
| EditMode | 伤害公式、来源透传、弱点、抗性、生命耗尽和 CharacterFacts | 12/12 通过；其中本功能 7/7 | `Logs/Verification/20260808-201748/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 当前步枪和 EnemyAttack 生产链集成 | 3/3 通过；其中本功能 2/2 | `Logs/Verification/20260808-201828-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
- 验证结论：当前切片所需的 EditMode 与 PlayMode 证据均针对当前源码通过；Windows64 构建与主线场景人工验收未运行。

## Remaining Boundaries
- 剩余风险：`SourceObject` 是本地 Unity 对象引用，不是网络稳定 ID；备份、Legacy/Lua 和旧场景按范围保持不变。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
