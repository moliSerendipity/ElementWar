# Enemy Control 架构事实

只在任务涉及韧性、失衡、硬控制或敌人等级转换时读取。

## 主链

```
Gameplay 控制来源 → EnemyControlApplicationRequest（同次攻击身份与三项输出快照）
                   → EnemyControlApplicationResolver（身份 / 阵营 / 等级 / 合并去重）
                   ├→ Normal：基础削韧 + 完整硬控
                   ├→ Elite：基础削韧 + 一半硬控
                   └→ Boss：（基础削韧 + 转换削韧）一次过阈值，不进入硬控
                   → ToughnessComponent / HardControlComponent
EnemyRoot 显式 Tick → 两类状态到期与生命清理 → EnemyBrain 只读阻断事实
```

## Current

- `EnemyBaseStatConfig` 定义韧性上限、每秒恢复、单次最低伤害和失衡时长；`EnemyDefinitionConfig.EnemyTier` 定义 Normal/Elite/Boss 硬控策略，`EnemyStat` 保存本次初始化快照。玩家配置和共用 `ActorStatBase` 不包含韧性。
- `EnemyControlApplicationRequest` 与 `DamageRequest` 正交，但同一攻击可共享 `AttackExecutionId`；请求冻结责任者/目标身份、基础削韧、硬控时长和 Boss 转换削韧。
- `EnemyControlApplicationResolver` 是唯一跨组件入口，通过 `Combatant` 缓存的 `EnemyRoot` 读取状态组件，一次校验、按等级换算，并使用独立于生命伤害的目标侧控制执行集合去重。
- Boss 最终削韧 = 同次攻击基础削韧 + 硬控转换削韧，并只调用一次 `ToughnessComponent`，因此最低阈值只判断一次；两个独立攻击不共享削韧残量。Normal/Elite 只使用基础削韧，并分别采用完整/一半硬控时长。
- `ToughnessComponent` 只拥有当前韧性、连续恢复、单次阈值和失衡；严格低于阈值的最终削韧不推进状态，失衡期间保持零并暂停恢复，到期回满。
- `HardControlComponent` 只拥有一个结束时间，只接受更晚结束时间，不维护列表或叠层计时器。
- `EnemyRoot` 在 AI 前显式推进韧性和硬控；`EnemyBrain` 只读取 `IsStaggered || IsHardControlled`，首次阻断时取消攻击并停止移动，结束后继续原状态求值。当前没有控制事件消费者，状态组件不发布事件。

## ADR

- `ADR-Enemy-Toughness-And-Control-Facts-v1.md`