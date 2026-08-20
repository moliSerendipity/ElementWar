# ElementWar 架构与边界

- 状态：当前新 C# 架构基线
- 核对日期：2026-08-20
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

当前元素施加与附着主链可概括为：

```text
Gameplay 来源所有者 → ElementApplicationSourceSnapshot → ElementApplicationRequest
                     → ElementApplicationResolver → 目标 ElementAttachmentRuntime
                     → ElementApplicationResult / ElementAttachmentChangedEvent
                     → Presentation / 后续反应消费者
```

- `Combatant` 是权威战斗目标根和阵营事实所有者；子 Collider 必须先解析到最近的活动 `Combatant`，不能各自成为目标。
- `DamageRequest` 在创建时冻结 `AttackExecutionId`、责任 `CombatantId` 和目标 `CombatantId`；`DamageResult` 与 Combat 事件保留这些身份，以 `SourceObject` 保存武器运行时、攻击配置等具体来源。
- `DamageAppliedEvent` 是表现层消费“伤害已提交”事实的唯一伤害事件；步枪对任意物理表面的原始命中仍由 `WeaponFiredEvent.HadHit` 表达，不能把两者混为同一语义。
- `DamageResolver` 是运行时身份、阵营许可和目标侧重复执行的最终裁决点；首版只允许 `PlayerParty ↔ Enemy`，同阵营与 `Unassigned` 均拒绝。同一执行对同一目标至多提交一次，但可分别命中不同目标。
- `Combatant` 禁用时使身份失效、清空伤害去重并结束附着生命周期，重新启用建立新身份；生命耗尽保留身份，由 `HealthComponent` 拒绝后续伤害，并由敌方附着运行时在显式 Tick 中清理元素状态。
- `ElementType`、`DamageDeliveryType` 和 `HitPartType` 分别表达伤害元素、传递形态和命中部位；伤害元素轴当前只参与抗性，不隐式产生附着。
- 元素施加与伤害请求并列：Definition 的 `ElementApplicationProfileConfig` 定义元素、来源—目标间隔和持续时间；Gameplay 的 `ElementApplicationSourceId` / `ElementApplicationSourceSnapshot` 冻结来源生命周期与归属，`ElementApplicationRequest` 使用独立 `AttackExecutionId` 和目标身份表达一次尝试。该请求不依赖 `DamageRequest` 或 `DamageResult`；目标 `ElementAttachmentRuntime` 依据当前身份与 Health 资格提交一个主要附着槽。
- `ElementAttachmentRuntime` 是敌方目标当前附着、来源—目标间隔和附着版本的唯一所有者；同元素刷新使用最近合法来源，不同元素只返回 `ReactionRequired`，由后续反应事务决定是否消费。`EnemyRoot` 用显式时间推进到期/生命清理，Presentation 只能消费已提交事件与只读快照。
- 解析公式是确定性的，不包含随机暴击；头部与弱点只应用明确倍率。
- `HealthComponent.CurrentHealth` 是生命数值的唯一存储事实，`IsHealthDepleted` 由初始化状态和当前生命值派生；角色事实、敌人状态机和表现快照只读取或映射该事实。
- 公共契约、迁移约束和取舍见 [`ADR-Combat-Domain-Contract-v1.md`](Decisions/ADR-Combat-Domain-Contract-v1.md)、[`ADR-Combatant-Faction-Execution-Identity-v1.md`](Decisions/ADR-Combatant-Faction-Execution-Identity-v1.md)、[`ADR-Element-Application-Profile-Snapshot-v1.md`](Decisions/ADR-Element-Application-Profile-Snapshot-v1.md) 与 [`ADR-Element-Attachment-Runtime-Lifecycle-v1.md`](Decisions/ADR-Element-Attachment-Runtime-Lifecycle-v1.md)。

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
| 攻击执行身份 | 成立攻击的 Gameplay 生产者创建，随 `DamageRequest` 只读透传 |
| 元素施加编辑期规则 | `ElementApplicationProfileConfig` |
| 元素来源生命周期身份与冻结归属 | 真实 Gameplay 来源所有者创建 `ElementApplicationSourceId` 并保存 `ElementApplicationSourceSnapshot` |
| 元素来源—目标间隔键 | `ElementApplicationSourceId + TargetId` |
| 当前元素附着、间隔状态与附着版本 | 目标 `ElementAttachmentRuntime`；敌方由 `EnemyRoot` 显式推进时间 |
| 同一执行对目标的精确去重 | 目标 `Combatant` |
| 生命数值与生命耗尽事实 | `HealthComponent` |
| 伤害裁决与已提交结果 | `DamageResolver` / `DamageResult` |
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
