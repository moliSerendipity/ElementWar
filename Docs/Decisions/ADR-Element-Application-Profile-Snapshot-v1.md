# ADR：元素施加配置、来源身份与请求快照 v1

- 状态：Accepted（实现形状由 [`ADR-Element-Pipeline-Simplification-v1.md`](ADR-Element-Pipeline-Simplification-v1.md) 修订）
- 日期：2026-08-12
- 关联 Feature 记录：[`ElementApplicationProfileSnapshotV1.md`](../Features/ElementApplicationProfileSnapshotV1.md)

## Context

当前新 C# 主线只把 `ElementType` 用于伤害抗性，没有独立元素施加输入。旧 `ElementReactionConfig` 没有元素对或反应类型，也没有资产或运行时消费者。元素附着既可能伴随伤害，也可能来自零伤害、免疫伤害或纯施加技能，因此不能以 `DamageResult.IsApplied` 作为元素请求的成立条件。

## Decision

选择方案 B。元素来源快照在来源生命周期建立时冻结配置与责任归属；每次攻击或技能执行可以独立产生零个或多个元素请求以及可选伤害请求。来源—目标应用间隔由每个目标 Runtime 按 `ElementApplicationSourceId` 保存；目标生命周期已经由 Runtime 边界隔离，不再额外建立 `SourceId + TargetId` 组合键类型。`ConfigId`、`SourceObject` 与 `AttackExecutionId` 均不冒充来源生命周期。

## Rationale
- 玩家与设计结果：伤害和元素施加是正交输出，任一方不能成为另一方的隐式前置条件。
- 架构约束：Definition 保存不可变规则，Gameplay 保存来源、执行和目标运行时身份；Event Bus 不传播尚未提交的请求。
- 生命周期：同一技能、武器元素通道或持续区域需要跨多次攻击保持来源身份，同时在结束或复用后获得新身份。
- 配置与兼容性：逻辑配置键、Unity 资产 GUID、运行时来源 ID 和攻击执行 ID 必须分责。
- 完成风险：本阶段只固定输入契约，不提前实现附着容器、间隔状态或反应表。

## Consequences
- 无伤害元素攻击拥有正式入口，且不会放宽或绕过伤害域规则。
- 同一命中可以按后续规则处理弹药元素和技能附加元素，二者可保留不同具体来源。
- 配置变更、角色切换和后续持续效果不会改写已经建立的来源快照。
- `ELM-010` 只创建请求，不代表目标已经获得附着，也不发布元素事实事件。
- SourceId 生命周期由未来真实来源所有者负责；每次攻击重建 SourceId 属于契约违规。
- 首版应用方向固定为玩家队伍到敌人；更广泛阵营或环境元素需要重新批准。

> 旧备选方案展开、迁移步骤、验证流水账和回滚过程由 Git 历史追溯；当前开发只依赖本页决定、活动 Architecture/Design 与代码。
