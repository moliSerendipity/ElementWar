# ADR：Combat Domain Contract v1

- 状态：Accepted
- 日期：2026-08-08
- 关联 Feature 记录：[`CombatDomainContractV1.md`](../Features/CombatDomainContractV1.md)

## Context

当前 `CombatDamageKind` 同时表达物理、元素与爆炸，通用伤害请求依赖 Hitscan 上下文并携带随机暴击参数。步枪把武器子物体当作攻击者，敌人近战则伪造 Hitscan 上下文。`HealthComponent` 与 `CharacterFacts` 还分别保存死亡布尔值，且后者没有可靠写入者。这些事实会阻碍后续元素、投射物、角色切换和归属判断。

## Decision

选择方案 A。元素、传递形态和命中部位是正交事实；责任角色与具体来源分别保存。`HealthComponent.CurrentHealth` 是唯一生命事实，生命耗尽只表示 Health 数值归零，不在 Combat 域裁决倒地、复活或最终实体生命周期。

## Rationale
- 当前步枪与敌人攻击必须立即迁移到一条可测试的主链。
- Fire、Water、Electric、Ice 需要正式但最小的入口，本阶段不能实现反应系统。
- 战斗结果必须可重复，生命状态必须只有一个权威存储者。
- Definition、Gameplay、Presentation 的依赖方向和现有 Unity 序列化引用必须保持安全。
- 不为未来功能提前引入来源接口、反应服务或第二套状态容器。

## Consequences
- 步枪和敌人攻击共享同一确定性解析器，并能保留正确归属。
- Fire/Water/Electric/Ice 与 Explosion 可以组合，不再依赖枚举互斥。
- 角色、敌人、事件和表现读取同一生命事实。
- 旧公共类型被直接替换，不提供兼容重载；所有活动调用方必须同切片迁移。
- 元素轴当前只参与抗性，不产生附着或反应。
- `SourceObject` 使用 Unity 对象引用，仅表达本地运行时身份，不是网络稳定 ID。

> 旧备选方案展开、迁移步骤、验证流水账和回滚过程由 Git 历史追溯；当前开发只依赖本页决定、活动 Architecture/Design 与代码。
