# 路线 01：战斗、元素与武器

- 上级路线：[`DevelopmentRoadmap.md`](../DevelopmentRoadmap.md)
- 目标设计：[`Combat.md`](../Design/Combat.md)、[`Elements.md`](../Design/Elements.md)
- 当前架构：[`Architecture.md`](../Architecture.md)
- 维护日期：2026-08-30

本路线先建立可复用战斗身份，再完成一个“开火 → 附着 → 超载 → 范围伤害/控制 → 反馈”的真实闭环，随后才扩展武器实例、弹药、投射物和其余反应。任务默认只记录范围与可观察结果；Feature Spec 仅在 Full 或确有长期契约需要时建立。

> Planned/Ready 任务只表达目标、依赖和可观察结果；实现形状在任务成为 Next 时根据当前代码重新审计。旧实施提示由 Git 历史追溯，不保留在活动 Roadmap。

## 已完成基线

### CMB-001 Combat Domain Contract v1

- 状态：Done（Fast Verified，并额外通过 PlayMode；未做 Windows64 与主线人工验收）
- 依赖：无。
- 已完成：统一 `DamageRequest → DamageResolver → HealthComponent → DamageResult/Event`，分离元素、传递形态和命中部位，统一生命耗尽事实并移除活动主链随机暴击。
- 证据：[`CombatDomainContractV1.md`](../Features/CombatDomainContractV1.md)。
- 剩余边界：没有稳定攻击执行 ID、战斗目标 ID、阵营、附着、反应、倒地或网络稳定来源 ID。
- 解锁：`CMB-010`。

## M01 首个元素反应垂直切片

### CMB-010 战斗目标、阵营与攻击执行身份

- 状态：Done（Fast Verified，并额外通过 PlayMode 与 Bootstrap 序列化扫描；未做 Windows64 与主线人工验收）
- 依赖：`CMB-001`。
- 已完成：新增权威 `Combatant` 根、运行时 `CombatantId` / `AttackExecutionId`、首版阵营矩阵、Collider 根解析和目标侧精确去重；步枪、`EnemyAttack`、伤害结果及事件共享身份上下文；Bootstrap 三个生命根已显式装配阵营。
- 可观察结果：同一敌人攻击扫到目标多个 Collider 只扣血一次；同阵营敌人不扣血；步枪与敌人攻击结果可关联同一次执行和权威目标；禁用复用后旧请求被拒绝。
- 证据：[`CombatantFactionExecutionIdentityV1.md`](../Features/CombatantFactionExecutionIdentityV1.md)；决策见 [`ADR-Combatant-Faction-Execution-Identity-v1.md`](../Decisions/ADR-Combatant-Faction-Execution-Identity-v1.md)。
- 剩余边界：元素附着/反应由 `ELM-010`～`ELM-030` 提供，通用范围目标集合由 `CMB-020` 提供；Party/威胁、网络身份、完整敌人攻击时序和玩家爆炸自伤例外仍由后续任务负责。
- 解锁：`ELM-010`、`CMB-020`、`CMB-030`、`INP-010`、`ENM-010`。

### ELM-010 元素施加配置与快照契约

- 状态：Done（Fast Verified，并额外通过 PlayMode 与真实 Profile/Registry 加载；未做 Windows64 与主线人工验收）
- 依赖：`CMB-010`。
- 已完成：用 `ElementApplicationProfileConfig` 定义元素、来源间隔和持续时间；用独立 `ElementApplicationSourceId` 与不可变来源快照冻结配置、责任者和具体来源；小型请求只补充攻击执行、目标身份和时间。目标 Runtime 在自身生命周期内按 SourceId 保存间隔，不维护复合键类型。
- 可观察结果：没有 DamageRequest/Result 且 Health 未初始化时仍能建立合法元素请求；配置结构在 Bootstrap 统一校验，请求身份、阵营或时间非法时返回明确原因；目标禁用复用后使用新 TargetId。
- 配置迁移：删除无资产引用的旧 `ElementReactionConfig` 壳；默认 Registry 登记火弹/雷弹两个真实 Profile，均为 0 秒应用间隔与 6 秒持续时间；不把反应定义误迁移为应用定义。
- 证据：[`ElementApplicationProfileSnapshotV1.md`](../Features/ElementApplicationProfileSnapshotV1.md)；现行实现修订见 [`ElementPipelineSimplificationV1.md`](../Features/ElementPipelineSimplificationV1.md)。
- 剩余边界：`ELM-020` 已补齐附着消费者与生命周期；真实武器来源和反应输出仍未接入。
- 解锁：`ELM-020`。

### ELM-020 元素附着运行时与生命周期

- 状态：Done（2026-08-26 请求校验精简后 Fast Verified，并额外通过 PlayMode；手动 Test Runner：EditMode 48/48、PlayMode 9/9，无独立 XML）
- 依赖：`ELM-010`。
- 已完成：每个 Bootstrap 敌方 `Combatant` 根装配唯一 `ElementAttachmentRuntime`；首版主要槽支持合法施加、同元素以最近来源刷新、不同元素返回待反应输入、来源间隔、显式到期、内部反应事务消费及生命/禁用/复用清理；只暴露主槽查询，实际变化才发布事件。
- 可观察结果：开发调试 Presenter 通过只读事件稳定维护当前附着；完全重复、迟到/旧生命周期请求和重复清理保持幂等；禁用再启用使用新 `TargetId` 且不继承旧状态、间隔或反应账本。
- 证据：[`ElementAttachmentRuntimeLifecycleV1.md`](../Features/ElementAttachmentRuntimeLifecycleV1.md)；决策见 [`ADR-Element-Attachment-Runtime-Lifecycle-v1.md`](../Decisions/ADR-Element-Attachment-Runtime-Lifecycle-v1.md)。
- 剩余边界：`ELM-030` 已提供反应事务；没有生产武器/技能来源，Windows64、性能与 Bootstrap 人工玩法观察未运行。
- 解锁：`ELM-030`、`WPN-010`。

### ELM-030 反应判定、消耗与归因管线

- 状态：Done（2026-08-26 最新差异 Fast Verified，并额外通过 PlayMode；手动 Test Runner：EditMode 48/48、PlayMode 9/9，无独立 XML）
- 依赖：`ELM-020`。
- 已完成：Gameplay 固定表达四元素六个无序组合；公共管线提供单请求和“弹药、技能”双请求入口，首次反应或当前阶段拒绝后停止；目标 `ElementAttachmentRuntime` 原子登记触发来源间隔与执行去重、消费版本匹配的已有附着；最小结果保留反应类型、被消费附着和第二元素来源归因。
- 可观察结果：交换两个元素顺序仍命中同一反应；同一执行重复到达不重复反应或留下附着；触发来源间隔和旧目标请求不能错误消费当前附着；禁用复用后新 `TargetId` 使用新去重生命周期。
- 证据：[`ElementReactionPipelineV1.md`](../Features/ElementReactionPipelineV1.md)；精简范围见 [`ElementPipelineSimplificationV1.md`](../Features/ElementPipelineSimplificationV1.md)；现行决策见 [`ADR-Element-Pipeline-Simplification-v1.md`](../Decisions/ADR-Element-Pipeline-Simplification-v1.md)。
- 剩余边界：真实武器/技能生产来源、具体反应伤害/控制/范围输出、反应反馈事件与主线玩法仍未接入；Windows64、性能和人工验收未运行。
- 解锁：`ELM-040`、`ELM-060`～`ELM-090`。

### CMB-020 范围目标查询与友伤过滤

- 状态：Done（Fast Verified，并额外通过 PlayMode；EditMode 52/52、PlayMode 12/12）
- 依赖：`CMB-010`。
- 已完成：新增无状态 `CombatRangeQuery.QueryDamageableTargets` 与只读 `CombatRangeTarget`；统一活动目标根解析、存活/阵营过滤、多 Collider 最近表面事实去重、可选环境 LOS、距离/CombatantId 稳定排序和 LOS 后数量上限。
- 可观察结果：同一物理场景重复查询得到相同目标集合和顺序，每个目标只出现一次；Trigger、错误层、同阵营、死亡、禁用和被环境遮挡的目标按契约排除。
- 证据：[`CombatRangeTargetQueryV1.md`](../Features/CombatRangeTargetQueryV1.md)；决策见 [`ADR-Combat-Range-Target-Query-v1.md`](../Decisions/ADR-Combat-Range-Target-Query-v1.md)。
- 剩余边界：没有 Request、接口、配置、缓存、事件或生产消费者；具体爆炸/反应输出、自伤例外和性能优化由真实消费者任务负责；Windows64、性能与主线人工验收未运行。
- 解锁：`ELM-040`、`PRJ-020`、`ELM-070`。

### CMB-030 韧性、失衡与受控状态事实

- 状态：Done（Implemented；此前 EditMode 63/63、PlayMode 14/14，最新边界精简按用户要求未重跑）
- 依赖：`CMB-010`。
- 已完成：配置提供韧性上限、连续恢复、单次阈值、失衡时长和 Normal/Elite/Boss 等级；`ToughnessComponent` / `HardControlComponent` 只保存各自状态，无状态 `EnemyControlApplicationResolver` 以一次攻击为单位完成身份、等级、合并与去重。删除角色/通用 Stat 韧性、三个无消费者控制抗性、两套独立 Request/Result/Event，不建立 `CombatControlRuntime` 或新配置资产。
- 可观察结果：严格低于 10 的独立攻击永不累计，10 起生效；低频有效攻击可被恢复抵消，高频压力可造成失衡并回满；Normal 完整硬控、Elite 一半，Boss 把同次基础削韧与转换削韧相加后只过一次阈值；死亡、禁用和复用清理状态并拒绝旧 TargetId 请求。
- 证据边界：[`EnemyToughnessAndControlFactsV1.md`](../Features/EnemyToughnessAndControlFactsV1.md)；决策见 [`ADR-Enemy-Toughness-And-Control-Facts-v1.md`](../Decisions/ADR-Enemy-Toughness-And-Control-Facts-v1.md)。历史 EditMode 63/63、PlayMode 14/14 早于最新精简。
- 剩余边界：尚无武器/元素生产消费者和玩家可见动画/VFX/HUD；玩家韧性、击退、减速、Modifier、Boss 弱点窗口、Windows64、性能和主线人工验收仍由后续任务处理。
- 解锁：`ELM-040`、`ELM-080`、`ELM-090`、`BOS-040`。

### WPN-010 步枪最小火/雷元素来源
- 状态：Next
- 依赖：`ELM-020`。
- 当前缺口：当前 Hitscan 步枪只产生 `None/Direct`；尚无可玩方式验证附着与反应管线。
- 当前约束：① 为当前武器实例增加最小、显式的 Fire/Electric 选择；② 把选择快照进攻击执行而不是命中后读取可变状态；③ 通过已有输入中的批准按键或最小临时调试入口切换；④ HUD/调试反馈显示当前元素；⑤ 不提前实现完整武器/弹药库存。
- 可观察完成：玩家可连续用火与雷命中同一敌人，攻击日志和附着事实与开火瞬间选择一致。
- 范围：当前步枪、输入适配和最小反馈。临时入口必须在 `WPN-060` 前迁移或删除。
- 解锁：`ELM-040`。

### ELM-040 超载范围伤害与控制闭环
- 状态：Planned
- 依赖：`ELM-030`、`CMB-020`、`CMB-030`、`WPN-010`。
- 当前缺口：设计中的首个反应尚无生产消费者、范围输出、控制结果和反馈。
- 可观察完成：Bootstrap 中用两种元素触发一次超载，附近合法敌人各受一次确定伤害/控制，责任归属正确，事件与反馈不重复。
- 范围：首个完整垂直切片。非目标是其余反应、完整武器库存和最终美术品质。
- 解锁：`WPN-020`、`ELM-050`，并形成首个可玩元素里程碑。

### WPN-020 WeaponRuntime 职责与开火时序重构
- 状态：Planned
- 依赖：`ELM-040`。
- 当前缺口：`WeaponRuntime` 同时承载多类职责；动画事件、输入、射击执行和装填时序在扩展多武器前需要单一权威边界。
- 可观察完成：现有步枪玩家行为不变，重复动画事件不能重复开火或结算，职责可被后续多武器复用。
- 范围：现有生产武器链和特征测试；如出现长期架构决策再评估 ADR。非目标是新增武器内容。
- 解锁：`WPN-025`。

### WPN-025 步枪射击、瞄准、散布与后坐力
- 状态：Planned
- 依赖：`WPN-020`。
- 当前缺口：当前步枪行为尚未按首版目标系统化证明腰射散布、瞄准精度、可学习竖向后坐力、少量横向偏移和冲刺/射击互斥。
- 可观察完成：瞄准明显更准确，连续射击产生可学习上抬；相同种子可复现散布，Presentation 丢失不会改变命中裁决。
- 范围：步枪执行、角色动作和相机反馈；非目标是武器获取、改装和随机暴击。
- 解锁：`WPN-030`、`HUD-010`。

### WPN-030 武器定义、运行时实例与角色 Loadout
- 状态：Planned
- 依赖：`WPN-025`。
- 当前缺口：现有 Loadout/配置 ID 不足以稳定表达每个角色的两个独立武器实例及其运行时状态。
- 可观察完成：两个同定义武器实例可拥有不同状态，角色切换或禁用后不会错误共享。
- 范围：Definition/Gameplay/必要配置调整；若实际改变序列化契约则升级 Full，并按需做 ADR 与引用检查。
- 解锁：`WPN-040`。

### WPN-040 角色 × 武器 × 元素弹药所有权
- 状态：Planned
- 依赖：`WPN-030`、`ELM-040`。
- 当前缺口：弹药是单个武器的本地弹匣/备弹，无法表达已批准的角色、武器实例和元素隔离。
- 可观察完成：两个角色、两个武器、多个元素账户互不串弹；失败重开后回到配置初值。
- 范围：弹药运行时与 HUD 只读快照；若实际改变长期状态所有权则升级 Full 并评估 ADR。非目标是装填动画与补给 UI。
- 解锁：`WPN-050`、`ARC-010`、`PTY-010`。

### WPN-050 普通/特殊装填事务与取消
- 状态：Planned
- 依赖：`WPN-040`、`WPN-020`。
- 当前缺口：未来换武器、闪避、切角色、倒地和元素弹会让当前装填时序产生重复结算或错误账户扣除。
- 可观察完成：任意取消边界下弹药守恒，迟到动画事件不会给新武器或新角色装弹。
- 范围：装填运行时、表现适配和测试。
- 解锁：`WPN-060`。

### WPN-060 双槽武器切换与 HUD
- 状态：Planned
- 依赖：`WPN-050`、`INP-010`。
- 当前缺口：没有基于实例的双槽切换事务、切换锁、取消规则和 HUD 快照。
- 可观察完成：玩家可在两武器间切换，弹药/元素/装填状态随实例正确恢复，UI 不短暂显示另一角色数据。
- 范围：武器切换、输入适配和 HUD；真实第二武器内容由 `PRJ-030` 提供。
- 解锁：`PRJ-010`、`AI-020`、`PTY-030`。

### PRJ-010 投射物攻击快照与命中提交
- 状态：Planned
- 依赖：`WPN-060`、`CMB-010`。
- 当前缺口：伤害生产链只有 Hitscan 与敌人近战，没有投射物在发射后保留责任、武器、元素、执行身份和弹药结果的契约。
- 可观察完成：发射后立刻切换角色或武器，命中仍归属原攻击且只提交一次。
- 解锁：`PRJ-020`。

### PRJ-020 统一爆炸输出
- 状态：Planned
- 依赖：`PRJ-010`、`CMB-020`。
- 当前缺口：`Explosion` 只有抗性维度，没有生产范围、中心距离、遮挡、自伤/友伤和去重规则。
- 可观察完成：爆炸对每个合法目标一次结算，中心/边缘与遮挡结果确定，Instigator/Source/Execution 保留。
- 解锁：`PRJ-030`、`PRJ-040`、`ELM-070`。

### PRJ-030 榴弹发射器垂直切片
- 状态：Planned
- 依赖：`PRJ-020`、`WPN-030`。
- 当前缺口：第二武器槽没有不同交付机制的真实消费者。
- 可观察完成：第二槽可稳定发射榴弹，换枪后在途榴弹仍正确归因并按策略自伤/友伤。
- 解锁：`PRJ-050`、Run 中的重武器升级内容。

### PRJ-040 手雷投掷、轨迹、反弹与引信
- 状态：Planned
- 依赖：`PRJ-020`、`INP-010`。
- 当前缺口：没有独立于武器槽的投掷意图、库存/冷却、轨迹预览、反弹和引信生命周期。
- 可观察完成：预览与实际落点在容差内，迟到回调/多次碰撞不重复爆炸。
- 解锁：`PRJ-050`。

### PRJ-050 投射物与范围效果池化生命周期
- 状态：Planned
- 依赖：`PRJ-030`、`PRJ-040`。
- 当前缺口：Foundation 有对象池能力，但新投射物、爆炸和反馈尚未证明完整 reset/return 契约。
- 可观察完成：连续复用不出现旧来源、旧元素、重复爆炸或事件泄漏。
- 解锁：`PERF-010`、大量敌人/波次场景。

### ELM-050 榴弹枪 Water/Ice 可玩来源
- 状态：Planned
- 依赖：`ELM-040`、`PRJ-030`、`WPN-050`。
- 当前缺口：Water/Ice 虽可在伤害契约表达，但没有首版生产施加来源。
- 可观察完成：步枪提供 Fire/Electric、榴弹枪提供 Water/Ice，四元素都能从玩家生产链稳定施加且来源可追溯。
- 解锁：`ELM-060`～`ELM-090`。

### ELM-060 Vaporize 与 Melt 伤害放大阶段
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`。
- 当前缺口：当前确定性伤害公式没有反应前置/后置修饰阶段，直接插倍率会污染 DamageResolver 或重复结算。
- 可观察完成：顺序和数值确定，重复解析一致，反应放大不生成第二次伤害事件。
- 解锁：`ELM-100`。

### ELM-070 Electro-Charged 连锁目标选择
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-020`。
- 当前缺口：没有最近合法目标、LOS、最大跳数、重复目标和连锁归因的统一规则。
- 可观察完成：相同场景总选中相同目标，遮挡和上限生效，无循环跳转。
- 解锁：`ELM-100`。

### ELM-080 Freeze 冻结与解冻
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-030`。
- 当前缺口：没有可与敌人状态机协作的冻结请求、免疫/转换、持续、打破和复用清理。
- 可观察完成：冻结不会让 AI 永久卡死，死亡、禁用、复用后无残留。
- 解锁：`ELM-100`、`BOS-050`。

### ELM-090 Superconduct 减速与非反应易伤
- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-030`、`MOD-010`。
- 当前缺口：没有有期限、可叠加规则明确且可恢复的减速与“仅非反应伤害易伤” Modifier 运行时。
- 可观察完成：持续 5 秒期间移动与非反应伤害按规则变化，反应伤害不被放大，到期精确恢复且无浮点累计漂移。
- 解锁：`ELM-100`。

### ELM-100 六反应集成、反馈与回归门
- 状态：Planned
- 依赖：`ELM-060`、`ELM-070`、`ELM-080`、`ELM-090`。
- 当前缺口：各反应即使单独完成，仍需统一配置校验、反馈优先级、同帧事件顺序和性能上限。
- 可观察完成：六种反应都能在主线场景复现，元素对无空洞/歧义，反馈与已提交事实一致。
- 解锁：`BOS-050`、最终战斗内容收口。
