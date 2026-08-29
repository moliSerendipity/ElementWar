# ElementWar 架构与边界

- 状态：当前新 C# 架构基线
- 核对日期：2026-08-30
- Unity：`2022.3.62f2c1`

本文件是采用后的架构、主线和旧代码边界唯一维护来源。每次高影响任务仍需根据实时仓库复核是否发生漂移。

## 程序集职责

```text
Game.Foundation → Game.Definition → Game.Gameplay → Game.Presentation
Game.Editor → 上述程序集的编辑器工具
```

| 层 | 职责 | 禁止事项 |
|---|---|---|
| Foundation | 事件、对象池、调度和通用运行时设施 | 依赖游戏业务层 |
| Definition | ScriptableObject 定义、索引和校验 | 持有可变玩法运行时状态 |
| Gameplay | 角色、武器、战斗、敌人状态与规则 | 依赖 Presentation 决定玩法事实 |
| Presentation | 动画、音频、VFX、UI 和视图适配 | 写入伤害、AI 等权威事实 |
| Editor | 编辑器和构建工具 | 进入 Player 运行时代码 |

新增依赖不得反向穿透这些边界。

## 运行时责任流

```text
Input → Decision → Execution → Fact writeback → Event → Presentation
```

- 输入层表达意图。
- Character/Enemy 决策层选择行为。
- 执行器完成武器、移动或战斗操作。
- 权威运行时组件提交生命、状态等事实。
- 事件总线传播已经发生的事实。
- 表现层响应事实，不重新裁决事实。

当前伤害主链可概括为：

```text
WeaponFireExecutor → AttackExecutionId → HitScanService ─┐
EnemyAttack        → AttackExecutionId → Collider Query ─┴→ CombatTargetResolver
                                                           → DamageRequest (身份快照)
                                                           → DamageResolver (身份/阵营/去重)
                                                           → HealthComponent
                                                           → DamageResult / DamageAppliedEvent / Health Events
                                                           → Presentation / 其他消费者
```

当前元素施加、附着与反应主链可概括为：

```text
Gameplay 来源所有者 → ElementApplicationSourceSnapshot → ElementApplicationRequest
                     → ElementReactionPipeline（弹药 → 技能）
                     → 内部 ElementApplicationResolver → 目标 ElementAttachmentRuntime
                     → ElementReactionResult / ElementAttachmentChangedEvent
                     → 后续反应输出 / Presentation
```

当前敌人韧性与硬控制主链可概括为：

```text
Gameplay 控制来源 → EnemyControlApplicationRequest（同次攻击身份与三项输出快照）
                   → EnemyControlApplicationResolver（身份 / 阵营 / 等级 / 合并去重）
                   ├→ Normal：基础削韧 + 完整硬控
                   ├→ Elite：基础削韧 + 一半硬控
                   └→ Boss：（基础削韧 + 转换削韧）一次过阈值，不进入硬控
                   → ToughnessComponent / HardControlComponent
EnemyRoot 显式 Tick → 两类状态到期与生命清理 → EnemyBrain 只读阻断事实
```

- `Combatant` 是权威战斗目标根和阵营事实所有者；子 Collider 必须先解析到最近的活动 `Combatant`，不能各自成为目标。
- `CombatRangeQuery` 是球形范围目标集合的共享 Gameplay 裁决入口：固定忽略 Trigger，复用活动目标解析、生命与阵营规则，按当前 `CombatantId` 去重，并可选执行环境 LOS；只读结果 `CombatRangeTarget` 保留目标、最近表面点和距离，最终按距离、CombatantId 稳定排序。查询不持有状态或缓存，具体反应、投射物和伤害消费者仍由后续任务接入。
- `DamageRequest` 在创建时冻结 `AttackExecutionId`、责任 `CombatantId` 和目标 `CombatantId`；`DamageResult` 与 Combat 事件保留这些身份，以 `SourceObject` 保存武器运行时、攻击配置等具体来源。
- `DamageAppliedEvent` 是表现层消费“伤害已提交”事实的唯一伤害事件；步枪对任意物理表面的原始命中仍由 `WeaponFiredEvent.HadHit` 表达，不能把两者混为同一语义。
- `DamageResolver` 是运行时身份、阵营许可和目标侧重复执行的最终裁决点；首版只允许 `PlayerParty ↔ Enemy`，同阵营与 `Unassigned` 均拒绝。同一执行对同一目标至多提交一次，但可分别命中不同目标。
- `Combatant` 禁用时使身份失效，分别清空生命伤害与敌人控制执行去重，并结束元素附着生命周期；重新启用建立新身份。韧性和硬控组件只在自身启停时重置本地状态，不接收 TargetId Begin/End 回调。生命耗尽保留身份，由 `HealthComponent` 拒绝后续伤害，并由敌方状态组件在显式 Tick 中清理状态。
- `ElementType`、`DamageDeliveryType` 和 `HitPartType` 分别表达伤害元素、传递形态和命中部位；伤害元素轴当前只参与抗性，不隐式产生附着。
- 元素施加与伤害请求并列：Definition 的 `ElementApplicationProfileConfig` 定义元素、来源间隔和持续时间；Gameplay 的 `ElementApplicationSourceId` 与不可变 `ElementApplicationSourceSnapshot` 冻结来源生命周期与归属，小型 `ElementApplicationRequest` 使用独立 `AttackExecutionId` 和目标身份表达一次尝试。该请求不依赖 `DamageRequest` 或 `DamageResult`；Profile 结构只在 Bootstrap 配置校验阶段验证。
- `ElementReactionPipeline` 是生产侧元素入口：提供单请求和固定“弹药、技能”双请求重载，双请求只预检共同执行/目标/时间，首次反应或当前阶段拒绝后停止。四元素六个无序组合是首版固定 Gameplay 规则，不维护无真实换表需求的反应表资产；低层 `ElementApplicationResolver` 仅供管线内部使用。
- `ElementAttachmentRuntime` 是敌方目标当前附着、来源间隔、附着版本和本目标生命周期反应执行去重的唯一所有者；间隔在目标生命周期内只以 `ElementApplicationSourceId` 为键。同元素刷新使用最近合法来源，不同元素由管线映射后在目标侧原子登记间隔/去重并消费版本匹配附着。`EnemyRoot` 用显式时间推进到期/生命清理，Presentation 只能消费已提交事件、成功反应结果与只读快照。本阶段没有反应事件或具体反应输出。
- `EnemyBaseStatConfig` 定义敌人韧性上限、每秒恢复、单次最低伤害和失衡时长；`EnemyDefinitionConfig.EnemyTier` 定义 Normal/Elite/Boss 硬控策略，`EnemyStat` 只保存本次初始化快照。玩家配置和共用 `ActorStatBase` 不包含韧性。
- `EnemyControlApplicationRequest` 与生命 `DamageRequest` 正交，但同一攻击可共享 `AttackExecutionId`；请求冻结责任者/目标身份、基础削韧、硬控时长和 Boss 转换削韧。`EnemyControlApplicationResolver` 是唯一跨组件入口，通过 `Combatant` 非序列化缓存的 `EnemyRoot` 读取状态组件，一次校验、按等级换算，并使用独立于生命伤害的目标侧控制执行集合去重。
- Boss 的最终削韧等于同次攻击的基础削韧加硬控转换削韧，和值只调用一次 `ToughnessComponent`，因此最低阈值只判断一次；两个独立攻击永不共享削韧残量。Normal/Elite 只使用基础削韧，并分别采用完整/一半硬控时长。
- `ToughnessComponent` 只拥有当前韧性、连续恢复、单次阈值和失衡；严格低于阈值的最终削韧不推进状态，失衡期间保持零并暂停恢复，到期回满。`HardControlComponent` 只拥有一个结束时间，只接受更晚结束时间，不维护列表或叠层计时器。
- `EnemyRoot` 在 AI 之前显式推进韧性和硬控；`EnemyBrain` 只读取 `IsStaggered || IsHardControlled`，首次阻断时取消攻击并停止移动，控制结束后继续原状态求值。当前没有控制事件消费者，两个状态组件不发布事件；未来表现事件必须从完整、已提交的申请结果产生。
- 解析公式是确定性的，不包含随机暴击；头部与弱点只应用明确倍率。
- `HealthComponent.CurrentHealth` 是生命数值的唯一存储事实，`IsHealthDepleted` 由初始化状态和当前生命值派生；角色事实、敌人状态机和表现快照只读取或映射该事实。
- 公共契约、迁移约束和取舍见 [`ADR-Combat-Domain-Contract-v1.md`](Decisions/ADR-Combat-Domain-Contract-v1.md)、[`ADR-Combatant-Faction-Execution-Identity-v1.md`](Decisions/ADR-Combatant-Faction-Execution-Identity-v1.md)、[`ADR-Combat-Range-Target-Query-v1.md`](Decisions/ADR-Combat-Range-Target-Query-v1.md)、[`ADR-Enemy-Toughness-And-Control-Facts-v1.md`](Decisions/ADR-Enemy-Toughness-And-Control-Facts-v1.md)、[`ADR-Element-Application-Profile-Snapshot-v1.md`](Decisions/ADR-Element-Application-Profile-Snapshot-v1.md)、[`ADR-Element-Attachment-Runtime-Lifecycle-v1.md`](Decisions/ADR-Element-Attachment-Runtime-Lifecycle-v1.md) 与现行 [`ADR-Element-Pipeline-Simplification-v1.md`](Decisions/ADR-Element-Pipeline-Simplification-v1.md)。

## 当前主线

默认允许在已批准任务范围内修改：

- `Assets/Scripts/Foundation`
- `Assets/Scripts/Definition`
- `Assets/Scripts/GamePlay`
- `Assets/Scripts/Presentation`
- `Assets/Scripts/Editor`
- `Assets/Configs`
- `Assets/Scenes/Bootstrap/Bootstrap.unity`

“主线”不等于无限授权。公共接口、asmdef、序列化数据、scene/prefab 和跨模块改动仍需先澄清。

## 默认只读旧架构

- `Assets/Script_Legacy`
- `Assets/LuaScripts`
- `Assets/Scenes/SampleScene.unity`
- 仅供旧场景使用的 prefab 与组件

可以读取它们以理解历史行为和迁移要求，但普通功能不得同时扩展两套实现。

## 共享资源

美术、动画、音频、VFX、第三方插件、Input System、URP、Addressables 等可能被新旧链路共同引用，不能仅凭路径判断为旧资源。移动或删除前必须检查 `.meta` GUID 和序列化引用。

## 当前已知越界点

1. `Assets/Script_Legacy` 没有 asmdef，仍进入 `Assembly-CSharp` 编译。
2. `Assets/Scenes/SampleScene.unity` 仍在 Build Settings 中启用。
3. `Bootstrap.unity` 的 `FN IWS Primary` 仍有旧 `WeaponView`。
4. 旧 Lua 元素反应与新 C# 伤害主链尚未收敛。

这些问题必须作为独立迁移任务处理，并保留引用检查、验证与可恢复提交。

## 状态所有权

| 内容 | 权威所有者 |
|---|---|
| 编辑期默认值 | Definition 配置 |
| 可变运行时数值 | Gameplay Stat/Runtime 组件 |
| 战斗目标根、当前运行时身份与阵营 | `Combatant` |
| 范围查询时的合法目标集合与最近表面几何事实 | 无持久所有者；Gameplay `CombatRangeQuery` 每次查询生成只读 `CombatRangeTarget` 结果 |
| 攻击执行身份 | 成立攻击的 Gameplay 生产者创建，随 `DamageRequest` 只读透传 |
| 元素施加编辑期规则 | `ElementApplicationProfileConfig` |
| 首版元素对到反应类型的固定规则 | Gameplay `ElementReactionPipeline.TryResolveReactionType` |
| 元素来源生命周期身份与冻结归属 | 真实 Gameplay 来源所有者创建 `ElementApplicationSourceId` 并保存 `ElementApplicationSourceSnapshot` |
| 元素来源—目标间隔 | 目标 `ElementAttachmentRuntime` 在当前 TargetId 生命周期内按 `ElementApplicationSourceId` 保存 |
| 当前元素附着、间隔、附着版本与反应执行去重 | 目标 `ElementAttachmentRuntime`；敌方由 `EnemyRoot` 显式推进时间 |
| 同一执行对目标的伤害精确去重 | 目标 `Combatant` |
| 生命数值与生命耗尽事实 | `HealthComponent` |
| 伤害裁决与已提交结果 | `DamageResolver` / `DamageResult` |
| 敌人韧性配置快照 | `EnemyBaseStatConfig` / `EnemyDefinitionConfig` → `EnemyStat` |
| 敌人控制执行去重 | 目标 `Combatant` |
| 当前韧性、连续恢复与失衡 | 目标 `ToughnessComponent`；敌方由 `EnemyRoot` 显式推进时间 |
| 当前硬控结束时间、延长与敌人等级转换 | 目标 `HardControlComponent`；Boss 转换显式提交到 `ToughnessComponent` |
| AI 决策 | Enemy Runtime/Brain |
| 移动执行与修饰 | Enemy Locomotion |
| 已发生事实通知 | Foundation Event Bus |
| 视觉/音频响应 | Presentation |

不要为单项功能增加第二套生命、伤害、移动或配置真相源。

## 需要 ADR 的变化

以下变化先使用 `Docs/Decisions/ADR-TEMPLATE.md`：

- 程序集方向或层级职责变化。
- 权威状态所有者变化。
- 公共战斗、事件或配置契约变化。
- 场景组合根变化。
- 需要迁移的序列化结构变化。
- 新旧架构收敛策略。
