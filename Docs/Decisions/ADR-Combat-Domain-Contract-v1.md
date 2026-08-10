# ADR：Combat Domain Contract v1

- 状态：Accepted
- 日期：2026-08-08
- 负责人：Codex / 项目维护者
- 关联 Feature Spec：[`CombatDomainContractV1.md`](../Features/CombatDomainContractV1.md)

## 背景

当前 `CombatDamageKind` 同时表达物理、元素与爆炸，通用伤害请求依赖 Hitscan 上下文并携带随机暴击参数。步枪把武器子物体当作攻击者，敌人近战则伪造 Hitscan 上下文。`HealthComponent` 与 `CharacterFacts` 还分别保存死亡布尔值，且后者没有可靠写入者。这些事实会阻碍后续元素、投射物、角色切换和归属判断。

## 决策因素

- 当前步枪与敌人攻击必须立即迁移到一条可测试的主链。
- Fire、Water、Electric、Ice 需要正式但最小的入口，本阶段不能实现反应系统。
- 战斗结果必须可重复，生命状态必须只有一个权威存储者。
- Definition、Gameplay、Presentation 的依赖方向和现有 Unity 序列化引用必须保持安全。
- 不为未来功能提前引入来源接口、反应服务或第二套状态容器。

## 备选方案

### 方案 A：正交语义轴与唯一生命事实

- 用 `ElementType`、`DamageDeliveryType`、`HitPartType` 分开表达语义。
- 请求/结果同时保存 `Instigator` 与 `SourceObject`。
- 通用请求直接保存目标和命中事实，不引用 Hitscan 专用结构。
- Health 只保存数值，生命耗尽由当前生命派生。
- 优点：当前迁移完整，后续元素与投射物无需替换伤害主链。
- 成本：需要同步公共契约、活动配置、场景、表现消费者和测试。

### 方案 B：保留混合枚举并追加来源字段

- 在现有请求上增加 Instigator/SourceObject，继续使用 `CombatDamageKind`。
- 优点：短期修改较少。
- 成本：元素与爆炸组合仍不明确，Water 和未来元素攻击会再次触发公共迁移。

### 方案 C：立即建立完整元素/来源接口体系

- 引入伤害来源接口、元素应用 Profile 和反应服务。
- 优点：未来扩展入口更多。
- 成本：超出本阶段目标，增加未被当前玩法证明的抽象和完成风险。

## 决策

选择方案 A。元素、传递形态和命中部位是正交事实；责任角色与具体来源分别保存。`HealthComponent.CurrentHealth` 是唯一生命事实，生命耗尽只表示 Health 数值归零，不在 Combat 域裁决倒地、复活或最终实体生命周期。

## 后果

正面影响：

- 步枪和敌人攻击共享同一确定性解析器，并能保留正确归属。
- Fire/Water/Electric/Ice 与 Explosion 可以组合，不再依赖枚举互斥。
- 角色、敌人、事件和表现读取同一生命事实。

代价与限制：

- 旧公共类型被直接替换，不提供兼容重载；所有活动调用方必须同切片迁移。
- 元素轴当前只参与抗性，不产生附着或反应。
- `SourceObject` 使用 Unity 对象引用，仅表达本地运行时身份，不是网络稳定 ID。

## 迁移与回滚

- `CombatDamageKind` 显式迁移为元素与传递形态两个字段，不按旧整数自动解释。
- 保留已有 Unity 资源 GUID；需要重命名的脚本连同 `.meta` 一起迁移。
- `Assets/Backup`、Legacy/Lua 和 SampleScene 不参与迁移。
- 契约、运行时、活动 YAML、测试和文档作为一个回滚单位。

## 验证

- EditMode 已证明确定性公式、抗性组合、归属透传和单次生命耗尽；证据见关联 Feature Spec。
- PlayMode 已证明当前 Hitscan 步枪与 EnemyAttack 生产入口均进入新主链；证据见关联 Feature Spec。
- Windows64 和人工主场景验收不属于本阶段证据，重新评估时另行授权。
