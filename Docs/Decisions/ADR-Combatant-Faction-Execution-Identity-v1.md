# ADR：Combatant、阵营与攻击执行身份 v1

- 状态：Accepted
- 日期：2026-08-11
- 负责人：Codex / 项目维护者
- 关联 Feature Spec：[`CombatantFactionExecutionIdentityV1.md`](../Features/CombatantFactionExecutionIdentityV1.md)

## 背景

`CMB-001` 已统一确定性伤害主链，但请求仍以 `GameObject` 和 `HealthComponent` 表达责任者与目标，没有运行时目标身份、阵营或攻击执行身份。Hitscan 按单次 Raycast 命中，而 `EnemyAttack` 对 Overlap 返回的 Collider 逐个提交；多 Collider、范围伤害、友伤、AI 选敌和后续元素归因会因此各自建立临时判断。

当前 Bootstrap 依靠 Player/Enemy LayerMask 粗略隔离生产攻击，但玩家武器查询包含全部层，且公共伤害入口本身不验证阵营。LayerMask 不能承担稳定目标身份、结果归因或跨物理查询一致性的职责。

## 决策因素

- 玩家结果：一次攻击不能因多个 Collider 重复伤害同一实体，同阵营不能互伤。
- 架构约束：权威目标、生命和已提交伤害都留在 Gameplay；Presentation 只消费结果。
- 生命周期：禁用、复用和迟到请求不能把旧身份或去重记录带入新实体生命周期。
- 扩展性：元素、范围查询、投射物和 Party 后续应复用同一目标/执行身份，但本阶段不引入网络 ID 或完整威胁系统。
- 完成风险：Bootstrap 只有三个活动主线生命根，适合一次显式、小范围序列化迁移。

## 备选方案

### 方案 A：根级 Combatant + 强类型运行时 ID + 目标侧精确去重

- 做法：每个战斗目标根装配 `Combatant`；攻击创建强类型执行 ID；请求冻结责任者和目标 ID；目标按执行 ID 精确去重；DamageResolver 统一验证阵营与身份。
- 优点：Collider、来源链和事件共享同一契约；同一执行可安全覆盖多个目标；禁用复用边界明确。
- 成本与风险：公共构造函数和 Bootstrap 需要同切片迁移；活动期去重集合随合法命中数量增长，直到禁用时清空。

### 方案 B：各生产者本地 HashSet + LayerMask 继续承担阵营

- 做法：Hitscan、EnemyAttack、未来范围/投射物各自按 `HealthComponent` 去重并维护不同 LayerMask。
- 优点：当前公共伤害契约改动较少。
- 成本与风险：每个生产者重复规则，无法为事件提供稳定目标/执行身份，配置错误可绕过友伤边界。

### 方案 C：立即使用序列化 GUID 或网络实体身份

- 做法：为场景/Prefab 目标保存跨运行 GUID，并让攻击上下文按网络可复制格式设计。
- 优点：可以提前支持存档或联机稳定身份。
- 成本与风险：当前没有对应消费者，增加资产迁移、唯一性校验和网络生命周期复杂度，超出首版范围。

## 决策

选择方案 A。`Combatant` 是目标根与阵营事实所有者；`CombatantId` 和 `AttackExecutionId` 只表达当前运行期身份。DamageResolver 是最终阵营、目标生命周期与重复执行裁决点，LayerMask 仅保留为物理查询优化。

首版阵营矩阵只允许 `PlayerParty ↔ Enemy`。`Unassigned`、同阵营和自身伤害均默认拒绝；玩家爆炸的来源自身例外必须由后续任务以显式策略加入，不能把“同阵营可伤害”作为隐式全局开关。

## 后果

正面影响：

- 所有命中方式可把子 Collider 解析为同一权威目标。
- 结果与事件可以稳定关联责任者、目标和一次攻击执行。
- 同一执行对同一目标至多提交一次，同阵营规则不能被单个生产者遗漏。
- 禁用/复用会失效旧 ID，迟到请求不能写入新活动生命周期。

代价与限制：

- 当前公共伤害请求、结果和事件构造函数必须一次迁移，旧调用方不保留兼容重载。
- 每个活动 Combatant 保存精确执行集合；首版以正确性优先，不用有限窗口静默拒绝合法迟到攻击。后续只有在代表性 Player 数据证明有必要时才优化。
- 身份不跨运行稳定，不可作为网络、存档或配置 ID。
- 完整 AI 选敌、敌人攻击时序和对象池租借/归还仍由后续路线任务完成。

## 迁移与回滚

- 增量步骤：先增加身份与阵营单元测试，再迁移 DamageRequest/Result/Resolver；随后迁移 Hitscan、步枪和 EnemyAttack；最后修改 Bootstrap 与生产 PlayMode 测试。
- 序列化/API 兼容性：新增脚本保留各自 `.meta`；Bootstrap 三个 Health 根显式新增 Combatant，其他 YAML 不批量改写；旧场景与 Legacy 保持只读。
- 回滚单位：公共契约、生产者、Bootstrap、测试、Feature Spec/ADR 和路线记录整体回滚。
- 对旧架构的影响：无；不修改或扩展 Legacy/Lua 路径。

## 验证

- 自动化证据：EditMode 覆盖身份、阵营和去重；PlayMode 覆盖真实步枪/EnemyAttack、多 Collider 与禁用复用。
- 人工证据：本切片不声明主线人工验收；Bootstrap 序列化装配通过自动化与引用扫描证明。
- 性能证据：未计划性能声明；不把 Editor 分配观察泛化为 Player 结论。
- 重新评估条件：网络实验、跨运行持久身份、玩家自伤策略或代表性长局数据证明精确去重集合需要不同生命周期。
