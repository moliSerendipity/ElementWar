# ADR：元素施加配置、来源身份与请求快照 v1

- 状态：Accepted（实现形状由 [`ADR-Element-Pipeline-Simplification-v1.md`](ADR-Element-Pipeline-Simplification-v1.md) 修订）
- 日期：2026-08-12
- 负责人：Codex / 项目维护者
- 关联 Feature Spec：[`ElementApplicationProfileSnapshotV1.md`](../Features/ElementApplicationProfileSnapshotV1.md)

## 背景

当前新 C# 主线只把 `ElementType` 用于伤害抗性，没有独立元素施加输入。旧 `ElementReactionConfig` 没有元素对或反应类型，也没有资产或运行时消费者。元素附着既可能伴随伤害，也可能来自零伤害、免疫伤害或纯施加技能，因此不能以 `DamageResult.IsApplied` 作为元素请求的成立条件。

## 决策因素

- 玩家与设计结果：伤害和元素施加是正交输出，任一方不能成为另一方的隐式前置条件。
- 架构约束：Definition 保存不可变规则，Gameplay 保存来源、执行和目标运行时身份；Event Bus 不传播尚未提交的请求。
- 生命周期：同一技能、武器元素通道或持续区域需要跨多次攻击保持来源身份，同时在结束或复用后获得新身份。
- 配置与兼容性：逻辑配置键、Unity 资产 GUID、运行时来源 ID 和攻击执行 ID 必须分责。
- 完成风险：本阶段只固定输入契约，不提前实现附着容器、间隔状态或反应表。

## 备选方案

### 方案 A：伤害结果驱动元素请求

- 做法：只在 `DamageResult.IsApplied` 后建立元素请求。
- 优点：可以复用已经通过伤害裁决的身份与目标。
- 成本与风险：零伤害、伤害免疫和纯元素技能无法附着；元素系统错误依赖 Health 与伤害提交。

### 方案 B：伤害与元素并列，使用独立来源生命周期身份

- 做法：Profile 定义规则，SourceId 标识运行时来源；元素请求直接使用执行与目标身份，与 DamageRequest 并列。
- 优点：支持无伤害施加、多来源同一命中和持续来源间隔；配置键不承担运行时身份。
- 成本与风险：来源运行时所有者必须正确保存、结束并重建 SourceId，后续附着运行时需要独立裁决目标状态。

### 方案 C：立即抽取通用 CombatHitSnapshot 并迁移伤害主链

- 做法：把 DamageRequest 的共同身份和命中字段抽成所有效果共享的公共命中类型。
- 优点：伤害与元素可以复用一个通用输入对象。
- 成本与风险：必须再次迁移已验证的伤害公共契约和全部生产者，超出建立元素最小输入边界的需要。

## 决策

选择方案 B。元素来源快照在来源生命周期建立时冻结配置与责任归属；每次攻击或技能执行可以独立产生零个或多个元素请求以及可选伤害请求。来源—目标应用间隔由每个目标 Runtime 按 `ElementApplicationSourceId` 保存；目标生命周期已经由 Runtime 边界隔离，不再额外建立 `SourceId + TargetId` 组合键类型。`ConfigId`、`SourceObject` 与 `AttackExecutionId` 均不冒充来源生命周期。

## 后果

正面影响：

- 无伤害元素攻击拥有正式入口，且不会放宽或绕过伤害域规则。
- 同一命中可以按后续规则处理弹药元素和技能附加元素，二者可保留不同具体来源。
- 配置变更、角色切换和后续持续效果不会改写已经建立的来源快照。

代价与限制：

- `ELM-010` 只创建请求，不代表目标已经获得附着，也不发布元素事实事件。
- SourceId 生命周期由未来真实来源所有者负责；每次攻击重建 SourceId 属于契约违规。
- 首版应用方向固定为玩家队伍到敌人；更广泛阵营或环境元素需要重新批准。

## 迁移与回滚

- 增量步骤：删除无引用的旧反应配置壳；新增 Application Profile 与两个真实资产；更新 Registry；新增 Gameplay 快照/请求和测试。
- 序列化/API 兼容性：旧类型 GUID 在仓库没有资产引用，默认 Registry 旧列表为空；迁移显式替换字段，不使用 `FormerlySerializedAs` 把反应配置误解释为应用配置。
- 回滚单位：配置、Registry、Gameplay 契约、测试、Feature Spec/ADR 和路线状态整体回滚。
- 对旧架构的影响：无；Legacy/Lua 和旧场景保持只读。

## 验证

- 自动化证据：EditMode 31/31（本功能 5/5）、PlayMode 6/6（本功能 1/1）；原始 XML、日志与摘要路径见 Feature Spec。
- 人工证据：本阶段没有玩家可见附着，未计划主线场景人工验收。
- 性能证据：没有性能声明；不做 Player 性能泛化。
- 重新评估条件：环境元素、敌方向玩家附着、跨运行/联网身份、来源共享规则或通用 Combat 命中契约出现真实消费者。
