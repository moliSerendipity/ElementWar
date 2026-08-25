# 功能：元素施加配置与快照契约 v1

- 状态：Verified
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-23
- 关联 Roadmap 任务：`ELM-010`
- 关联 ADR：[`ADR-Element-Application-Profile-Snapshot-v1.md`](../Decisions/ADR-Element-Application-Profile-Snapshot-v1.md)
- 后续实现修订：[`ElementPipelineSimplificationV1.md`](ElementPipelineSimplificationV1.md)
- 授权记录：2026-08-12，用户先纠正“元素请求依赖已提交伤害”的方案，随后明确同意元素与伤害并列、使用独立来源身份的修订契约并回复“同意，开始实施”。

## 目标与范围

- 可观察目标：合法配置能冻结为独立元素来源快照；一次有效攻击或技能执行在不创建伤害请求的情况下也能为敌方权威目标建立确定的元素施加请求；非法配置或身份返回明确原因。
- 非目标：实际附着状态、应用间隔计时、刷新/消耗、元素反应、武器元素选择、伤害公式修改、事件与表现反馈。
- 允许修改：Definition 元素配置与 Registry、Gameplay 元素请求契约、必要配置资产、Gameplay 测试及相关权威文档。
- 禁止或只读：`DamageRequest` / `DamageResult` / `DamageResolver`、场景、Prefab、Legacy/Lua、第三方资源和无关配置。

## 当前事实与批准方案

- 架构和伤害主链以 [`Architecture.md`](../Architecture.md) 为准；元素目标行为以 [`Elements.md`](../Design/Elements.md) 为准。
- 旧 `ElementReactionConfig` 没有元素对或反应类型，没有资产引用，默认 Registry 列表为空；删除该无消费者壳，真正的反应表由 `ELM-030` 建立。
- `ElementApplicationProfileConfig` 只定义元素、来源—目标应用间隔和附着持续时间；`ConfigId` 是 Definition 查询键，不是运行时来源身份。
- `ElementApplicationSourceId` 标识一个运行时来源生命周期。同一来源跨攻击复用身份，禁用、结束或池复用后必须创建新身份，不能每次攻击创建以绕过应用间隔。
- 来源快照是不可变 class，冻结 Profile Id 与数值、责任 Combatant 引用/Id/阵营及具体 SourceObject；后续请求共享该引用，不重新读取配置或当前控制角色，缺失来源直接以 `null` 表达。
- `ElementApplicationRequest` 直接由来源快照、`AttackExecutionId` 和当前目标创建，与伤害请求并列；零伤害、免疫伤害或纯元素技能不因缺少 `DamageResult` 而失去附着入口。
- 应用间隔由每个目标的 `ElementAttachmentRuntime` 按 `ElementApplicationSourceId` 保存；目标生命周期已经隔离 TargetId，因此不再复制组合键结构。`AttackExecutionId` 用于单次执行关联，`ConfigId` 用于定义追踪，`SourceObject` 用于具体归因。
- Profile 的元素、间隔和持续时间只由 Bootstrap 配置校验负责；来源工厂信任已初始化的正式配置，不在运行时重复同一组结构判断。
- 首版只允许 `PlayerParty → Enemy` 创建元素请求；敌人不向角色附着元素。目标的附着资格、死亡/禁用、间隔和生命周期现由 `ELM-020` 的 `ElementAttachmentRuntime` 裁决。

## 行为契约与验收

| ID | Given | When | Then | 自动/人工证据 |
|---|---|---|---|---|
| AC-01 | Profile 具有有效 Id、正式元素、非负间隔和正持续时间 | Registry 初始化与校验 | 可按 Id 查询且无配置错误 | EditMode / 真实资产加载 |
| AC-02 | Profile Id 为空/重复，或元素、间隔、持续时间非法 | 运行配置校验 | 每类非法输入产生明确 Error，不静默默认 | EditMode |
| AC-03 | 活动玩家来源、有效 SourceId 与已注册 Profile | 建立来源快照后修改原 Profile | 快照仍保留建立时的 Id、元素、间隔、持续时间与归属 | EditMode |
| AC-04 | 没有 DamageRequest/Result，只有有效执行、玩家来源和敌方 Combatant | 创建元素施加请求 | 请求成功并冻结来源引用、执行、目标身份和时间 | EditMode / PlayMode |
| AC-05 | 同一来源跨不同执行或命中不同目标 | 分别创建请求 | 请求共享同一个来源快照；执行与目标身份分别冻结，目标各自持有间隔状态 | EditMode |
| AC-06 | 配置服务/Profile/SourceId/责任者/SourceObject/执行/目标/阵营/时间任一非法 | 尝试建立快照或请求 | 返回对应失败原因且输出保持默认值 | EditMode / PlayMode |
| AC-07 | 目标禁用后重新启用 | 使用旧目标或当前目标建立请求 | 禁用期拒绝；新生命周期请求使用新的 TargetId，旧目标状态不能泄漏 | PlayMode |

## 验证与最终证据

| 层级 | 用例 | 实际结果 | 证据路径 |
|---|---|---|---|
| EditMode | 配置、查询、冻结、失败原因、间隔键、真实资产 | 完整套件 31/31；本功能 5/5 | `Logs/Verification/20260812-011452/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 无伤害依赖、目标禁用/复用生命周期 | 完整套件 6/6；本功能 1/1 | `Logs/Verification/20260812-011609-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |

- 实际命令：`pwsh -File .\Tools\Verify-ElementWarEditMode.ps1`；`pwsh -File .\Tools\Verify-ElementWarPlayMode.ps1`；新旧 GUID/Registry/旧类型引用扫描；测试日志失败标记扫描；`git diff --check`。
- 历史实际修改：替换未使用配置壳；新增元素应用 Profile、运行时来源身份、来源快照、独立请求和明确失败原因；注册两个真实资产；新增 EditMode/PlayMode 测试并同步 ADR、架构、设计与路线。组合间隔键及默认结构体标记已在 2026-08-23 精简切片中移除。
- 验收等级：达到 Fast Verified，并额外完成完整 PlayMode 与真实资产加载。Windows64、主线人工验收和性能检查未运行，因此不声明 Full Verified 或 Accepted。
- 剩余风险：`ELM-020/030` 已提供附着与反应消费者，但尚无真实武器/技能来源负责保存和重建 SourceId；该生产接入由 `WPN-010` 负责。当前两个 Profile 已注册但仍不会改变玩家行为。
- 回滚单位：配置类型与资产、Registry 序列化字段、Gameplay 请求契约、测试、Feature Spec/ADR 和路线状态整体回滚。

## 收口检查

- [x] 目标、范围、方案和可观察验收已有明确授权。
- [x] 实现与 scoped diff 未超出授权，且未吸收用户无关改动。
- [x] 实际测试数量大于 0；失败、未运行和证据缺口均如实记录。
- [x] 最终行为、运行方式、证据路径、维护约束和回滚单位仍然有效。
- [x] 项目级事实与 ADR 使用引用，本 Spec 不保留重复正文或可恢复的过程细节。
- [x] 对应 Roadmap 任务已更新状态、证据链接、后续解锁项和新的唯一 `Next`。
