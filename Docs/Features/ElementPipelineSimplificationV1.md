# 功能：元素施加与反应管线精简 v1

- 状态：Fast Verified（并额外通过 PlayMode；现有 Editor 手动 Test Runner 证据）
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-26
- 关联 Roadmap 任务：`ELM-010`、`ELM-020`、`ELM-030` 维护切片
- 关联 ADR：[`ADR-Element-Pipeline-Simplification-v1.md`](../Decisions/ADR-Element-Pipeline-Simplification-v1.md)
- 授权记录：2026-08-23，用户指出反应配置、双层校验、结果结构和 Gameplay 脚本数量过度，并在确认精简范围后明确回复“开始实施”。

## 目标与范围

- 可观察目标：不改变 ELM-010～030 已批准玩法行为，删除没有独立状态或真实配置需求的包装类型、重复验证和查询入口，使生产链能沿一条短路径读懂。
- 保留行为：元素与伤害并列；来源身份跨攻击稳定；目标单槽附着、刷新、到期和复用清理；弹药先于技能；六种无序反应；首次反应停止；原子消费、执行去重、来源间隔和第二元素归因。
- 非目标：真实武器/技能来源、具体反应伤害或控制、反应事件、第五种元素、多槽附着、scene/prefab/asmdef 修改、Windows64 与性能优化。
- 允许修改：现有 Definition/Gameplay 元素契约、默认 Registry 的反应表字段、相关测试和权威文档。

## 当前实现契约

- `ElementApplicationSourceSnapshot` 是不可变引用对象；不存在时直接使用 `null`，不再用结构体默认值加 `IsCreated` 模拟可空状态。
- `ElementApplicationRequest` 只保存来源引用、执行、目标引用/身份和时间。目标 Runtime 自己限定目标生命周期，间隔字典只需以 `ElementApplicationSourceId` 为键。
- `ElementApplicationResult` 只保存状态、拒绝原因和一个相关附着；调用管线已经持有请求，不再把请求及前后两个相同快照复制进结果。
- `ElementAttachmentRuntime` 只公开 `TryGetPrimaryAttachment`；索引查询、槽数量和任意公开消费入口均已移除。反应消费只能走内部原子事务。
- `ElementApplicationProfileConfig.Validate` 是配置结构的唯一校验。来源工厂信任已经初始化并通过 Bootstrap 校验的正式配置，不在热路径重复同一组枚举和浮点检查。
- 目标 Runtime 在改变状态前只核对请求冻结的 `TargetCombatant + TargetId` 是否仍属于当前生命周期；Runtime/时间、Health/阵营分别由 `TryAdvanceTime`、`CanReceiveAttachment` 裁决，反应提交不再重复整组请求与时间校验。
- `ElementReactionPipeline` 直接提供单请求和“弹药、技能”双请求两个重载；双请求只预检共同执行、目标和时间，随后由目标 Runtime 裁决可变状态。
- 四元素六反应是首版固定规则，由管线中的一个无序映射函数表达；不再维护可编辑但没有真实设计需求的反应表 ScriptableObject、Registry 字段和资产。
- `ElementReactionResult` 只保存反应类型、被消费附着和第二元素请求。默认值表示没有触发反应，不再暴露处理计数、拒绝矩阵、表 Id、基准伤害或包装请求。

当前元素域保留六个有独立语义的只读结构体：`ElementApplicationSourceId`、`ElementApplicationRequest`、`ElementApplicationResult`、`ElementAttachmentSnapshot`、`ElementAttachmentChangedEvent`、`ElementReactionResult`。来源快照因需要表达“存在/不存在”并被多个请求共享，改为不可变 class。

## 删除与迁移

- 删除 `ElementApplicationIntervalKey`：目标生命周期已经由 `ElementAttachmentRuntime` 隔离。
- 删除 `ElementReactionPipelineRequest` 及其 Origin：当前只有直接调用者，派生反应输出“不再提交元素”由输出生产者契约负责。
- 删除 `ElementReactionTableConfig`、默认反应表资产及 Registry 引用：首版没有运行时换表或设计师改映射需求。
- 删除反应状态/拒绝枚举矩阵与结果中的重复追踪字段；普通附着和拒绝继续由目标状态/事件观察，成功反应返回最小事实。
- 测试改为验证映射、顺序、停止、归因、间隔、去重、生命周期和序列化结果，不再反射调用反应提交或锁定内部字段数量。

## 行为契约与验收

| ID | Given | When | Then | 证据 |
|---|---|---|---|---|
| AC-01 | 合法 Profile 与来源生命周期 | 创建多个请求 | 来源快照被共享，执行和目标身份分别冻结，且不依赖伤害结果 | EditMode / PlayMode |
| AC-02 | 目标为空槽、同元素或异元素 | 依次提交 | 附着、刷新或反应行为与精简前一致，只有一次权威状态写回 | EditMode |
| AC-03 | 六个首版无序元素对 | 正反顺序查询 | 每对映射到唯一相同反应类型，无反应表资产或 Registry 引用 | EditMode / GUID 扫描 |
| AC-04 | 同一命中有弹药和技能元素 | 双请求管线处理 | 固定弹药先、技能后，首次反应后停止，归因来自第二元素 | EditMode |
| AC-05 | 重复执行、触发来源间隔或旧目标生命周期请求 | 再次提交 | 不重复反应、不消费当前新附着、不产生额外事件 | EditMode / PlayMode |
| AC-06 | 默认 Registry 与 Bootstrap 场景 | 配置校验和序列化检查 | 配置通过；删除的脚本/资产 GUID 无残留；场景无 Missing Script | EditMode / 静态扫描 |

## 验证与最终证据

- 修改前证据：2026-08-23 在已打开的 Unity 2022.3.62f2c1 Editor 中编译通过，Test Runner 全量 EditMode 48/48、PlayMode 9/9；它证明请求校验再次精简前的版本。
- 当前编译：2026-08-26 使用生成的 `Game.Gameplay.csproj`、`Game.EditModeTests.csproj` 与 `Game.PlayModeTests.csproj` 进行 MSBuild，Foundation、Definition、Gameplay 和两个测试程序集均编译通过；输出位于系统临时目录 `ElementWar-AttachmentValidation-20260826-0054`。
- 当前 Unity 回归：2026-08-26 用户在现有 Unity Editor Test Runner 中手动运行全部测试并确认成功；Test Runner 当前测试总量为 EditMode 48、PlayMode 9，因此记录为 EditMode 48/48、PlayMode 9/9。
- 静态检查：`ValidateRequestStructure` 与反应提交中的重复时间推进已无残留；旧 TargetId 拒绝仍由 EditMode/PlayMode 生命周期测试覆盖；`git diff --check` 通过。
- 证据边界：批处理验证入口两次停在 Editor 初始化前，故本轮没有独立 XML、验证摘要或仓库内日志；最新运行结果来自用户在现有 Editor 中的手动 Test Runner，当前窗口重新打开结果页后只保留测试总量，没有保留绿色结果计数。
- 未运行：Windows64、性能和主线人工玩法；本切片不以代码检查替代这些证据。
- 回滚单位：精简后的 Definition/Gameplay 契约、Registry 迁移、测试、Feature Spec/ADR、架构/设计/路线同步整体回滚。

## 收口检查

- [x] 精简目标、保留行为、删除范围和非目标已有明确授权。
- [x] ELM-030 只保留 `ElementReactionPipeline` 与最小 `ElementReactionResult` 两个必要 Gameplay 脚本；没有包装请求、MonoBehaviour、单例、Update 或平行状态源。
- [x] 2026-08-26 最新差异的 EditMode 48/48、PlayMode 9/9 已由用户在现有 Editor 中运行并确认成功。
- [x] 旧类型、旧资产 GUID、文档与路线引用扫描无残留。
- [x] 最终证据、未运行项和剩余风险已如实记录。
