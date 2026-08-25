# 功能：元素反应判定、消费与归因管线 v1

- 状态：Fast Verified（并额外通过 PlayMode；现有 Editor 手动 Test Runner 证据）
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-26
- 关联 Roadmap 任务：`ELM-030`
- 现行 ADR：[`ADR-Element-Pipeline-Simplification-v1.md`](../Decisions/ADR-Element-Pipeline-Simplification-v1.md)
- 历史 ADR：[`ADR-Element-Reaction-Pipeline-v1.md`](../Decisions/ADR-Element-Reaction-Pipeline-v1.md)（已被取代）
- 授权记录：2026-08-22，用户批准 ELM-030 精简实现；2026-08-23，用户在审查复杂度后批准继续压缩配置、包装请求、结果结构和测试耦合。

## 目标与范围

- 可观察目标：同一次命中的弹药元素先于技能元素处理；四元素六个无序组合确定映射到唯一反应；首次成功反应原子消费已有附着、停止剩余应用，并保留第二元素来源归因。
- 非目标：具体反应伤害、控制、范围查询、触发基准伤害、武器/技能生产来源、VFX/SFX/HUD、玩家元素附着和主线人工玩法。
- 允许修改：`ElementReactionType`、Gameplay 管线/目标事务、相关测试和权威文档。
- 已删除边界：反应表 ScriptableObject/资产与 Registry 字段、批次包装请求、Origin、状态/拒绝矩阵和无消费者结果字段。

## 当前事实与方案

- 元素规则以 [`Elements.md`](../Design/Elements.md) 为准；输入和附着契约分别见 [`ElementApplicationProfileSnapshotV1.md`](ElementApplicationProfileSnapshotV1.md) 与 [`ElementAttachmentRuntimeLifecycleV1.md`](ElementAttachmentRuntimeLifecycleV1.md)。
- `ElementReactionPipeline.ResolveAndApply` 提供单请求和双请求重载。双请求参数语义固定为弹药、技能，不接受可空集合或额外批次容器。
- 双请求在任何写回前确认来源/目标存在，并比较共同执行、目标引用/身份和应用时间；单个请求的目标状态、时间轴、间隔与附着资格仍由目标 Runtime 统一裁决。
- `TryResolveReactionType` 直接表达火、水、雷、冰六个固定无序组合。当前不存在运行时换表、多个规则集或设计师编辑映射需求，因此不维护 Definition 表资产。
- `ElementAttachmentRuntime` 是当前附着、来源间隔、版本与当前目标生命周期反应执行去重的唯一所有者。反应提交在一个内部事务中重验目标/时间、当前附着版本、元素关系、触发来源间隔和执行去重，然后登记间隔/去重并消费附着。
- `ElementReactionResult` 只在反应成功时保存反应类型、被消费附着和第二元素请求；默认值表示没有反应。调用者可从第二元素请求读取责任 Combatant、SourceObject、执行与目标身份。
- 当前不发布反应事件，也不在管线携带基准伤害。`ELM-040/060` 出现真实输出消费者时，基于具体伤害/控制阶段建立输入，不把未使用字段重新塞回反应判定结果。
- 反应派生伤害/控制不附着元素或递归反应，是未来输出生产者的强制契约；当前没有派生元素调用者，不保留 Origin 标记进行假设性防御。

## 行为契约与验收

| ID | Given | When | Then | 证据 |
|---|---|---|---|---|
| AC-01 | 火、水、雷、冰任一合法异元素对 | 正反顺序查询 | 六个组合各返回唯一相同反应类型；同元素或 None 不触发 | EditMode |
| AC-02 | 同一命中具有弹药和技能元素 | 双请求管线处理 | 固定先弹药后技能；首次反应成功或当前阶段拒绝后停止 | EditMode |
| AC-03 | 目标已有异元素附着且版本未变化 | 第二元素触发反应 | 原子登记触发来源间隔和执行去重，消费当前槽，结果归属于第二元素来源 | EditMode / PlayMode |
| AC-04 | 同一执行重复到达同一目标 | 再次提交 | 不重复反应、不留下新附着、不重复发布事件 | EditMode / PlayMode |
| AC-05 | 双请求执行、目标或时间不一致 | 提交管线 | 在弹药阶段写回前停止，不产生部分附着 | EditMode |
| AC-06 | 触发来源仍处于来源间隔 | 尝试消费当前异元素附着 | 反应不提交，已有附着保持不变 | EditMode |
| AC-07 | 目标禁用后重新启用 | 旧请求与新请求分别到达 | 旧请求不能消费新生命周期附着；新 TargetId 使用新的反应去重账本 | EditMode / PlayMode |

## 验证与最终证据

- 历史初版证据：精简前 EditMode 45/45、PlayMode 9/9，路径为 `Logs/Verification/20260823-190101` 与 `Logs/Verification/20260823-190124-playmode`；仅证明当时实现，不替代本次修改后的新证据。
- 修改前精简证据：2026-08-23 在已打开的 Unity 2022.3.62f2c1 Editor 中全量运行 EditMode 48/48、PlayMode 9/9；覆盖六个无序映射、顺序、归因、来源间隔、执行去重和目标复用，但早于 2026-08-26 请求校验再次精简。
- 当前编译：2026-08-26 使用生成的 Gameplay、EditModeTests 与 PlayModeTests 工程进行 MSBuild，生产程序集及两个测试程序集全部编译通过。
- 当前 Unity 回归：2026-08-26 用户在现有 Unity Editor Test Runner 中手动运行并确认 EditMode 48/48、PlayMode 9/9 全部成功；测试总量已从当前 Test Runner 列表核对。
- 当前静态检查：删除类型与两个删除资产 GUID 在 `Assets`、`Packages`、`ProjectSettings` 中无残留，默认 Registry 不再引用反应表，`git diff --check` 通过。
- 证据边界：批处理入口未能完成 Editor 初始化，因此没有生成独立 XML；最新结果依赖用户手动运行确认，重新打开结果页后只保留 48/9 的测试总量，没有保留绿色结果计数。
- 未运行：Windows64、性能、真实武器来源和主线人工验收；不以代码或旧日志替代。
- 回滚单位：Gameplay 管线/目标事务、测试、Feature Spec/ADR 和路线记录整体回滚。

## 收口检查

- [x] 当前目标、保留行为、删除范围和非目标已有明确授权。
- [x] 管线没有新增 MonoBehaviour、单例、Update 或平行状态所有者。
- [x] 2026-08-26 最新差异的 EditMode 48/48 与 PlayMode 9/9 已由用户手动运行并确认成功。
- [x] 删除类型、资产 GUID、Registry 和文档引用扫描无残留。
- [x] 最终证据等级、未运行项与剩余风险已如实记录。
