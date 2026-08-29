# 路线 01：战斗、元素与武器

- 上级路线：[`DevelopmentRoadmap.md`](../DevelopmentRoadmap.md)
- 目标设计：[`Combat.md`](../Design/Combat.md)、[`Elements.md`](../Design/Elements.md)
- 当前架构：[`Architecture.md`](../Architecture.md)
- 维护日期：2026-08-30

本路线先建立可复用战斗身份，再完成一个“开火 → 附着 → 超载 → 范围伤害/控制 → 反馈”的真实闭环，随后才扩展武器实例、弹药、投射物和其余反应。每项启动时用 [`TEMPLATE.md`](../Features/TEMPLATE.md) 建立具体 Feature Spec。

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
- 实施要点：① 为当前武器实例增加最小、显式的 Fire/Electric 选择；② 把选择快照进攻击执行而不是命中后读取可变状态；③ 通过已有输入中的批准按键或最小临时调试入口切换；④ HUD/调试反馈显示当前元素；⑤ 不提前实现完整武器/弹药库存。
- 可观察完成：玩家可连续用火与雷命中同一敌人，攻击日志和附着事实与开火瞬间选择一致。
- 范围：当前步枪、输入适配和最小反馈。临时入口必须在 `WPN-060` 前迁移或删除。
- 验证：快照 EditMode、快速切换后命中 PlayMode、Bootstrap 人工检查。
- 解锁：`ELM-040`。

### ELM-040 超载范围伤害与控制闭环

- 状态：Planned
- 依赖：`ELM-030`、`CMB-020`、`CMB-030`、`WPN-010`。
- 当前缺口：设计中的首个反应尚无生产消费者、范围输出、控制结果和反馈。
- 实施要点：① 消费管线固定映射产生的 Overload 结果；② 在真实输出阶段建立触发基准伤害并生成独立范围伤害和普通/精英/Boss 分级控制；③ 使用统一范围查询且不伤害玩家或 AI 队友；④ 派生伤害不生产元素请求，也不接受“爆破改良”；⑤ 补齐命中、附着、反应、范围结果、VFX/SFX/HUD 的最小反馈链。
- 可观察完成：Bootstrap 中用两种元素触发一次超载，附近合法敌人各受一次确定伤害/控制，责任归属正确，事件与反馈不重复。
- 范围：首个完整垂直切片。非目标是其余反应、完整武器库存和最终美术品质。
- 验证：公式/归因/去重 EditMode，真实步枪到多敌人 PlayMode，主线人工验收；达到 Full Verified 时再运行 Windows64。
- 解锁：`WPN-020`、`ELM-050`，并形成首个可玩元素里程碑。

## M02 武器实例、弹药与切换

### WPN-020 WeaponRuntime 职责与开火时序重构

- 状态：Planned
- 依赖：`ELM-040`。
- 当前缺口：`WeaponRuntime` 同时承载多类职责；动画事件、输入、射击执行和装填时序在扩展多武器前需要单一权威边界。
- 实施要点：① 用特征测试锁定当前开火/射速/装填/禁用行为；② 把武器实例状态、触发决策、射击执行和表现适配分责；③ 保持 `WeaponFireExecutor`/伤害主链唯一；④ 动画事件只确认表现时点或向权威事务发信号，不成为第二开火/装填事实；⑤ 分步迁移调用者，禁止一次性改写无关系统。
- 可观察完成：现有步枪玩家行为不变，重复动画事件不能重复开火或结算，职责可被后续多武器复用。
- 范围：现有生产武器链和特征测试；需要在 Feature Spec 中决定是否 ADR。非目标是新增武器内容。
- 验证：时序/重复/禁用测试、当前步枪 PlayMode 和 Bootstrap 回归。
- 解锁：`WPN-025`。

### WPN-025 步枪射击、瞄准、散布与后坐力

- 状态：Planned
- 依赖：`WPN-020`。
- 当前缺口：当前步枪行为尚未按首版目标系统化证明腰射散布、瞄准精度、可学习竖向后坐力、少量横向偏移和冲刺/射击互斥。
- 实施要点：① 将射速、散布和后坐力参数放入真实武器定义；② 开火时冻结射线方向；③ 腰射/瞄准使用明确散布模型且测试可注入随机源；④ 后坐力驱动表现/相机而不改伤害事实；⑤ 冲刺、换弹、闪避和禁用遵守统一动作矩阵。
- 可观察完成：瞄准明显更准确，连续射击产生可学习上抬；相同种子可复现散布，Presentation 丢失不会改变命中裁决。
- 范围：步枪执行、角色动作和相机反馈；非目标是武器获取、改装和随机暴击。
- 验证：散布/射速 EditMode、输入与相机 PlayMode、主线手感检查。
- 解锁：`WPN-030`、`HUD-010`。

### WPN-030 武器定义、运行时实例与角色 Loadout

- 状态：Planned
- 依赖：`WPN-025`。
- 当前缺口：现有 Loadout/配置 ID 不足以稳定表达每个角色的两个独立武器实例及其运行时状态。
- 实施要点：① 区分不可变武器定义与可变实例；② 为定义和实例建立稳定 ID/校验；③ 每角色持有两个槽位且切换不丢实例状态；④ 明确出生、重启、换角色和对象池生命周期；⑤ 配置壳只在被真实消费者采用时迁移。
- 可观察完成：两个同定义武器实例可拥有不同状态，角色切换或禁用后不会错误共享。
- 范围：Definition/Gameplay/必要配置迁移；序列化变化需要 ADR 和引用检查。
- 验证：ID/重复配置 EditMode、实例隔离 PlayMode、真实资产加载。
- 解锁：`WPN-040`。

### WPN-040 角色 × 武器 × 元素弹药所有权

- 状态：Planned
- 依赖：`WPN-030`、`ELM-040`。
- 当前缺口：弹药是单个武器的本地弹匣/备弹，无法表达已批准的角色、武器实例和元素隔离。
- 实施要点：① 定义弹匣与储备的唯一所有者和键；② 普通弹与特殊元素弹分账；③ 开火只从攻击快照对应账户扣除；④ 换武器/换角色/倒地/重启保留或清零规则一致；⑤ AI 无消耗规则由策略表达，不伪造无限数字。
- 可观察完成：两个角色、两个武器、多个元素账户互不串弹；失败重开后回到配置初值。
- 范围：弹药运行时与 HUD 只读快照；需要状态所有权 ADR。非目标是装填动画与补给 UI。
- 验证：账户矩阵 EditMode、快速切换/失败恢复 PlayMode。
- 解锁：`WPN-050`、`ARC-010`、`PTY-010`。

### WPN-050 普通/特殊装填事务与取消

- 状态：Planned
- 依赖：`WPN-040`、`WPN-020`。
- 当前缺口：未来换武器、闪避、切角色、倒地和元素弹会让当前装填时序产生重复结算或错误账户扣除。
- 实施要点：① 在开始时冻结武器实例和弹药账户；② 明确定时/动画确认与提交点；③ 切换、闪避、禁用、倒地和重启执行幂等取消；④ 重复回调只能提交一次；⑤ HUD 展示事务状态而不写事实。
- 可观察完成：任意取消边界下弹药守恒，迟到动画事件不会给新武器或新角色装弹。
- 范围：装填运行时、表现适配和测试。
- 验证：状态转换/重复回调 EditMode，动画时序/切换 PlayMode。
- 解锁：`WPN-060`。

### WPN-060 双槽武器切换与 HUD

- 状态：Planned
- 依赖：`WPN-050`、`INP-010`。
- 当前缺口：没有基于实例的双槽切换事务、切换锁、取消规则和 HUD 快照。
- 实施要点：① 输入只发意图；② Gameplay 原子切换活动实例并处理开火/装填取消；③ Presentation 绑定模型/动画/HUD；④ 快速重复输入、禁用和角色切换保持幂等；⑤ 清理 `WPN-010` 的临时元素切换入口。
- 可观察完成：玩家可在两武器间切换，弹药/元素/装填状态随实例正确恢复，UI 不短暂显示另一角色数据。
- 范围：武器切换、输入适配和 HUD；真实第二武器内容由 `PRJ-030` 提供。
- 验证：状态机 EditMode、快速输入 PlayMode、主线人工验收。
- 解锁：`PRJ-010`、`AI-020`、`PTY-030`。

## M03 投射物、爆炸与榴弹

### PRJ-010 投射物攻击快照与命中提交

- 状态：Planned
- 依赖：`WPN-060`、`CMB-010`。
- 当前缺口：伤害生产链只有 Hitscan 与敌人近战，没有投射物在发射后保留责任、武器、元素、执行身份和弹药结果的契约。
- 实施要点：发射时冻结上下文；飞行期不读取已切换武器/角色状态；碰撞解析到权威目标；命中/超时/禁用只完成一次；复用前清空全部上下文。
- 可观察完成：发射后立刻切换角色或武器，命中仍归属原攻击且只提交一次。
- 验证：快照/重复碰撞 EditMode，飞行切换 PlayMode。
- 解锁：`PRJ-020`。

### PRJ-020 统一爆炸输出

- 状态：Planned
- 依赖：`PRJ-010`、`CMB-020`。
- 当前缺口：`Explosion` 只有抗性维度，没有生产范围、中心距离、遮挡、自伤/友伤和去重规则。
- 实施要点：使用统一范围查询；明确距离衰减与 LOS；所有目标走同一 DamageResolver；玩家输入的爆炸只可能按原始基础伤害/半径的批准比例伤来源角色，不伤队友，AI 自主爆炸不自伤；自身伤害快照不接受攻击增益、反应或爆破强化但仍经过防御；表现消费已提交结果。
- 可观察完成：爆炸对每个合法目标一次结算，中心/边缘与遮挡结果确定，Instigator/Source/Execution 保留。
- 验证：几何/衰减/过滤 EditMode，真实物理 PlayMode。
- 解锁：`PRJ-030`、`PRJ-040`、`ELM-070`。

### PRJ-030 榴弹发射器垂直切片

- 状态：Planned
- 依赖：`PRJ-020`、`WPN-030`。
- 当前缺口：第二武器槽没有不同交付机制的真实消费者。
- 实施要点：定义榴弹武器资产；接入实例/弹药/装填/切换；直接命中与范围伤害分开提交且都不计算弱点；发射投射物并统一爆炸；处理近距离命中与枪口遮挡；补齐最小反馈。
- 可观察完成：第二槽可稳定发射榴弹，换枪后在途榴弹仍正确归因并按策略自伤/友伤。
- 验证：武器集成 PlayMode、Bootstrap 人工验收。
- 解锁：`PRJ-050`、Run 中的重武器升级内容。

### PRJ-040 手雷投掷、轨迹、反弹与引信

- 状态：Planned
- 依赖：`PRJ-020`、`INP-010`。
- 当前缺口：没有独立于武器槽的投掷意图、库存/冷却、轨迹预览、反弹和引信生命周期。
- 实施要点：定义投掷事务；预览只读同一初始条件；释放时冻结上下文；反弹与引信只触发一次爆炸；切角色/倒地/禁用遵守批准取消规则。
- 可观察完成：预览与实际落点在容差内，迟到回调/多次碰撞不重复爆炸。
- 验证：弹道数学 EditMode、物理/切换 PlayMode、人工手感检查。
- 解锁：`PRJ-050`。

### PRJ-050 投射物与范围效果池化生命周期

- 状态：Planned
- 依赖：`PRJ-030`、`PRJ-040`。
- 当前缺口：Foundation 有对象池能力，但新投射物、爆炸和反馈尚未证明完整 reset/return 契约。
- 实施要点：枚举所有可变字段/订阅/定时器；OnRent/OnReturn 对称重置；迟到回调使用代次或执行身份拒绝；池满策略显式；Profiler 前先证明正确性。
- 可观察完成：连续复用不出现旧来源、旧元素、重复爆炸或事件泄漏。
- 验证：复用压力 PlayMode、分配与峰值数量 Profiler 记录。
- 解锁：`PERF-010`、大量敌人/波次场景。

## M04 其余元素与反应

### ELM-050 榴弹枪 Water/Ice 可玩来源

- 状态：Planned
- 依赖：`ELM-040`、`PRJ-030`、`WPN-050`。
- 当前缺口：Water/Ice 虽可在伤害契约表达，但没有首版生产施加来源。
- 实施要点：为榴弹枪配置 Water/Ice 两种元素账户；复用特殊装填事务与施加契约；发射时冻结元素；直接命中与范围内每个目标按同一攻击事件去重；资产 ID 与配置校验；提供可辨识反馈。
- 可观察完成：步枪提供 Fire/Electric、榴弹枪提供 Water/Ice，四元素都能从玩家生产链稳定施加且来源可追溯。
- 验证：来源快照 EditMode、生产链 PlayMode。
- 解锁：`ELM-060`～`ELM-090`。

### ELM-060 Vaporize 与 Melt 伤害放大阶段

- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`。
- 当前缺口：当前确定性伤害公式没有反应前置/后置修饰阶段，直接插倍率会污染 DamageResolver 或重复结算。
- 实施要点：为触发命中建立显式修饰阶段；固定与攻击者增益、弱点和目标减伤顺序；Vaporize 使用 1.50 伤害倍率，Melt 使用 1.35 伤害与 1.50 韧性倍率；保留原请求和最终结果；每次执行一次。
- 可观察完成：顺序和数值确定，重复解析一致，反应放大不生成第二次伤害事件。
- 验证：公式矩阵 EditMode、真实来源 PlayMode；公共契约变化评估 ADR。
- 解锁：`ELM-100`。

### ELM-070 Electro-Charged 连锁目标选择

- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-020`。
- 当前缺口：没有最近合法目标、LOS、最大跳数、重复目标和连锁归因的统一规则。
- 实施要点：主要目标承受 50% 独立反应伤害；在 6 米内按距离稳定选择最多 2 个有 LOS 的其他敌人，各承受 35%；每目标至多一次；次要目标不继续传递；派生伤害不递归反应。
- 可观察完成：相同场景总选中相同目标，遮挡和上限生效，无循环跳转。
- 验证：图状目标 EditMode、物理场景 PlayMode。
- 解锁：`ELM-100`。

### ELM-080 Freeze 冻结与解冻

- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-030`。
- 当前缺口：没有可与敌人状态机协作的冻结请求、免疫/转换、持续、打破和复用清理。
- 实施要点：反应输出受控状态而非直接写 AI；普通敌人冻结 2 秒、精英 1 秒且受伤不提前解除；Boss 改为 25% 减速 2 秒并放大本次韧性伤害；到期精确恢复；Presentation 只消费事实。
- 可观察完成：冻结不会让 AI 永久卡死，死亡、禁用、复用后无残留。
- 验证：状态组合 EditMode、敌人 PlayMode。
- 解锁：`ELM-100`、`BOS-050`。

### ELM-090 Superconduct 减速与非反应易伤

- 状态：Planned
- 依赖：`ELM-030`、`ELM-050`、`CMB-030`、`MOD-010`。
- 当前缺口：没有有期限、可叠加规则明确且可恢复的减速与“仅非反应伤害易伤” Modifier 运行时。
- 实施要点：通过 typed modifier 而非直接改配置；普通/精英减速 25%，Boss 10%，非反应易伤均为 15%；不叠层、最强者刷新；来源与到期可追踪；死亡/禁用/重开撤销；反应伤害明确排除易伤。
- 可观察完成：持续 5 秒期间移动与非反应伤害按规则变化，反应伤害不被放大，到期精确恢复且无浮点累计漂移。
- 验证：叠加/恢复 EditMode、敌人与 Boss 替身 PlayMode。
- 解锁：`ELM-100`。

### ELM-100 六反应集成、反馈与回归门

- 状态：Planned
- 依赖：`ELM-060`、`ELM-070`、`ELM-080`、`ELM-090`。
- 当前缺口：各反应即使单独完成，仍需统一配置校验、反馈优先级、同帧事件顺序和性能上限。
- 实施要点：建立全元素对覆盖表；真实资产校验；统一 VFX/SFX/HUD 消费；组合/同帧压力回归；记录代表性 Player 分配和耗时；删掉过渡调试入口。
- 可观察完成：六种反应都能在主线场景复现，元素对无空洞/歧义，反馈与已提交事实一致。
- 验证：完整 EditMode/PlayMode、Windows64、主线人工验收和代表性 Profiler。
- 解锁：`BOS-050`、最终战斗内容收口。
