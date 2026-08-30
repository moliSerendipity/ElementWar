# ElementWar 架构与边界

- 状态：当前新 C# 架构基线
- 核对日期：2026-08-30
- Unity：`2022.3.62f2c1`

本页只保存跨域稳定边界和索引。处理具体功能时只读取下方对应架构页，不要把全部领域细节一次载入上下文。

## 程序集职责

```
Game.Foundation → Game.Definition → Game.Gameplay → Game.Presentation
Game.Editor → 上述程序集的编辑器工具
```

| 层           | 职责                               | 禁止事项                       |
| ------------ | ---------------------------------- | ------------------------------ |
| Foundation   | 事件、对象池、调度和通用运行时设施 | 依赖游戏业务层                 |
| Definition   | ScriptableObject 定义、索引和校验  | 持有可变玩法运行时状态         |
| Gameplay     | 角色、武器、战斗、敌人状态与规则   | 依赖 Presentation 决定玩法事实 |
| Presentation | 动画、音频、VFX、UI 和视图适配     | 写入伤害、AI 等权威事实        |
| Editor       | 编辑器和构建工具                   | 进入 Player 运行时代码         |

新增依赖不得反向穿透这些边界。

## 运行时责任流

```
Input → Decision → Execution → Fact writeback → Event → Presentation
```

输入表达意图；Character/Enemy 选择行为；执行器完成操作；权威 Runtime 提交事实；事件传播已发生事实；Presentation 只响应，不重新裁决玩法事实。

## 领域架构索引

| 任务涉及                               | 只读文档                               |
| -------------------------------------- | -------------------------------------- |
| 战斗目标、伤害、范围查询、生命事实     | `Architecture/Combat.md`               |
| 元素来源、附着、反应管线               | `Architecture/Elements.md`             |
| 敌人韧性、失衡、硬控制                 | `Architecture/EnemyControl.md`         |
| 主线目录、Legacy、共享资源、已知越界点 | `Architecture/RepositoryBoundaries.md` |

若任务不涉及对应领域，不读取该页。长期决策需要原因时，再从领域页链接到单个 ADR。

## 状态所有权

| 内容                                            | 权威所有者                                                   |
| ----------------------------------------------- | ------------------------------------------------------------ |
| 编辑期默认值                                    | Definition 配置                                              |
| 可变运行时数值                                  | Gameplay Stat/Runtime 组件                                   |
| 战斗目标根、当前运行时身份与阵营                | `Combatant`                                                  |
| 范围查询结果                                    | 无持久所有者；`CombatRangeQuery` 每次生成只读结果            |
| 攻击执行身份                                    | 成立攻击的 Gameplay 生产者；随请求只读透传                   |
| 元素施加编辑期规则                              | `ElementApplicationProfileConfig`                            |
| 元素对到反应类型                                | `ElementReactionPipeline.TryResolveReactionType`             |
| 元素来源生命周期身份与冻结归属                  | 真实 Gameplay 来源所有者 / `ElementApplicationSourceSnapshot` |
| 元素来源—目标间隔、当前附着、版本与反应执行去重 | 目标 `ElementAttachmentRuntime`                              |
| 同一执行对目标的伤害去重                        | 目标 `Combatant`                                             |
| 生命数值与生命耗尽事实                          | `HealthComponent`                                            |
| 伤害裁决与已提交结果                            | `DamageResolver` / `DamageResult`                            |
| 敌人韧性配置快照                                | `EnemyBaseStatConfig` / `EnemyDefinitionConfig` → `EnemyStat` |
| 敌人控制执行去重                                | 目标 `Combatant`                                             |
| 当前韧性、恢复与失衡                            | `ToughnessComponent`；`EnemyRoot` 显式推进                   |
| 当前硬控结束时间                                | `HardControlComponent`；Boss 转换显式提交到 `ToughnessComponent` |
| AI 决策                                         | Enemy Runtime/Brain                                          |
| 移动执行与修饰                                  | Enemy Locomotion                                             |
| 已发生事实通知                                  | Foundation Event Bus                                         |
| 视觉/音频响应                                   | Presentation                                                 |

不要为单项功能增加第二套生命、伤害、移动或配置真相源。

## 需要 ADR 的变化

只有长期影响架构的变化才创建 ADR：程序集方向/层级职责、权威状态所有者、跨模块公共契约、场景组合根、需要迁移的序列化结构、新旧架构收敛策略。普通局部实现不创建 ADR。
