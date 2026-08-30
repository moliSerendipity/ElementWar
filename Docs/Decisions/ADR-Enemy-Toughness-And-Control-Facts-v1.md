# ADR：敌人控制申请边界与状态组件拆分 v1

- 状态：Accepted
- 日期：2026-08-30
- 关联 Feature 记录：[`EnemyToughnessAndControlFactsV1.md`](../Features/EnemyToughnessAndControlFactsV1.md)

## Context

旧配置在敌人、角色和通用 Stat 中保存静态 `toughness`，但没有削减、恢复或失衡消费者；共享抗性表中的三个泛化控制抗性也没有公式或消费者。第一版 CMB-030 实现虽然补齐玩法，却让 `ToughnessComponent` 与 `HardControlComponent` 各自承担身份、阵营、请求校验、TargetId 生命周期、执行集合、单调时间线、事件发布和等级转换。两个简单状态组件因此各超过 500 行，并出现事件重入、生命周期多入口及 Boss 基础削韧与转换削韧顺序依赖。

用户同时确认：Boss 的基础削韧与硬控转换削韧属于同一次攻击时应先相加，再只经过一次单次阈值；两个独立小攻击不能相加。

## Decision

保留两个按状态命名的 Component，但把一次攻击的跨组件规则移动到无状态 `EnemyControlApplicationResolver`：

1. `EnemyControlApplicationRequest` 冻结攻击执行、责任者 TargetId、目标 TargetId、基础削韧、硬控时长和 Boss 转换削韧。
2. Resolver 一次校验身份、阵营、有限非负输入和敌人接收资格。
3. Normal 使用基础削韧和完整硬控；Elite 使用基础削韧和一半硬控；Boss 使用 `基础削韧 + 转换削韧` 且硬控为零。
4. Resolver 在任何状态写入前用 `Combatant` 的控制执行集合登记一次合并执行；该集合与生命伤害去重分开。
5. Resolver 只调用一次韧性写入，因此 Boss 的和值只经过一次最低阈值；随后写入一个硬控结束时间。
6. 两个写入之间不发布同步事件，结果全部冻结后才返回。
7. `Combatant` 非序列化缓存同一目标上的 `EnemyRoot`，Resolver 复用其现有 `Stat`、`Toughness` 和 `HardControl` 引用，不在每次申请时查找组件。
8. 拒绝只返回默认结果；没有真实消费者前不维护详细拒绝原因枚举。

`ToughnessComponent` 只拥有当前值、线性恢复、最低阈值、失衡和本地启停重置。`HardControlComponent` 只拥有一个结束时间、到期、延长和本地启停重置。它们不再保存 Combatant、TargetId、执行集合或敌人等级，也不再提供 `ValidateRequest`、`BeginTargetLifecycle`、`EndTargetLifecycle`。

当前没有真实事件消费者，因此删除两套独立 Request/Result/Event。将来出现表现层需求时，从完整申请结果发布一次已提交事实；不得在第一个组件写回后、第二个组件写回前同步发布。

## Consequences
- 两个 Component 的代码量与职责和当前状态复杂度匹配，AI 读取接口不变。
- 同一次攻击的两种控制输出使用一个身份和一个等级裁决，Boss 加法语义不依赖调用顺序。
- 旧 TargetId 在查找或写入组件前被拒绝；拒绝结果不读取新生命周期状态。
- 生命伤害与敌人控制分别去重，同一攻击可以合法提交两个领域结果。
- 没有无消费者事件、来源对象包装、组件内集合或隐式激活分支。
- 正常申请路径只读取目标缓存引用，不执行 `TryGetComponent`。
- 生产消费者必须一次提供该攻击的三项控制输出，不能分两次绕过 Resolver。
- 每个敌人仍有两个 MonoBehaviour，且由 `EnemyRoot` 每帧推进；当前无代表性 Player 数据证明需要进一步合并或优化。

> 旧备选方案展开、迁移步骤、验证流水账和回滚过程由 Git 历史追溯；当前开发只依赖本页决定、活动 Architecture/Design 与代码。
