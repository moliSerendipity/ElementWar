# ADR：元素附着运行时所有权与生命周期 v1

- 状态：Accepted（实现形状由 [`ADR-Element-Pipeline-Simplification-v1.md`](ADR-Element-Pipeline-Simplification-v1.md) 修订）
- 日期：2026-08-20
- 关联 Feature 记录：[`ElementAttachmentRuntimeLifecycleV1.md`](../Features/ElementAttachmentRuntimeLifecycleV1.md)

## Context

`ELM-010` 已建立与伤害并列的元素来源快照和施加请求，但目标尚无消费者。首版敌人只允许一个主要附着槽；状态必须在配置持续时间后到期，并在死亡、禁用和对象复用时清理。当前 `Combatant` 掌握目标生命周期身份，`HealthComponent` 是生命事实源，`EnemyRoot` 是敌方逐帧 Gameplay 主驱动。

## Decision

选择方案 A。目标侧组件拥有状态，`Combatant` 负责明确的启用/禁用边界，`EnemyRoot` 负责时间推进。Resolver 只接受 ELM-010 的请求，先验证当前目标身份、Health 和来源—目标间隔，再原子写入状态。

同元素刷新以最近一次合法请求替换来源与执行快照。不同元素只返回 `ReactionRequired`，既不修改已有槽，也不提前提交间隔；反应管线以当前附着快照和触发请求进入目标侧事务。消费只能由该内部事务完成并必须匹配版本，避免迟到调用清除刷新后的附着；单槽只暴露 `TryGetPrimaryAttachment`，不预设集合查询接口。

## Rationale
- 玩家与开发者结果：相同输入产生确定附着、刷新、待反应与一次性清理；调试层能读取已提交状态。
- 架构约束：Definition 不持有运行时状态，Presentation 不裁决元素；不得新增第二套生命事实或独立敌人 Update 主链。
- 生命周期：旧 `TargetId`、附着版本和应用间隔不能跨禁用或对象池复用泄漏；死亡不会自动禁用 `Combatant`，必须显式处理。
- 扩展性：`ELM-030` 需要稳定读取已有附着与触发请求，并以版本防止迟到消费；首版不能提前实现反应规则。
- 迁移风险：Bootstrap 只有两处敌方 `Combatant` 根，可做精确组件装配并保留现有 GUID/引用。

## Consequences
- 每个敌方目标只有一个附着真相源，事件只描述已提交事实。
- 到期、死亡、禁用和池复用能够幂等清理；旧请求不能写入新目标生命周期。
- ELM-030 可复用明确的待反应结果与版本化消费，而无需回迁 ELM-010 请求。
- 调试 Presentation 可以仅依赖事件和只读快照。
- 未装配 `ElementAttachmentRuntime` 的敌方目标会明确拒绝请求；Bootstrap 和未来敌人 prefab 必须同步装配。
- 首版运行时只支持一个主要槽；未来真实多附着需求出现时重新设计查询契约，不提前暴露索引集合接口。
- 不同元素在 ELM-030 完成前不会改变状态，也不会产生反应结果。
- 过期由敌方 Gameplay Tick 推进；没有 `EnemyRoot` 的未来目标必须提供等价的权威驱动后才能接入。

> 旧备选方案展开、迁移步骤、验证流水账和回滚过程由 Git 历史追溯；当前开发只依赖本页决定、活动 Architecture/Design 与代码。
