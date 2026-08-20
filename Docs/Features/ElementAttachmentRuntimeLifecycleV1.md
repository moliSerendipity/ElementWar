# 功能：元素附着运行时与生命周期 v1

- 状态：Verified
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-20
- 关联 Roadmap 任务：`ELM-020`
- 关联 ADR：[`ADR-Element-Attachment-Runtime-Lifecycle-v1.md`](../Decisions/ADR-Element-Attachment-Runtime-Lifecycle-v1.md)
- 授权记录：2026-08-20，用户在只读检查与推荐契约总结后明确回复“开始实施”，授权包含 Bootstrap 精确序列化迁移及同元素刷新采用最近一次合法来源。

## 目标与范围

- 可观察目标：敌方权威目标拥有一个可查询的主要元素附着；附着会按配置持续、同元素刷新、不同元素保留为待反应输入，并在到期、消费、死亡、禁用和复用时确定清理；开发调试视图只读展示已提交附着。
- 非目标：任一具体元素反应、反应伤害或控制、真实武器/技能来源接入、玩家元素附着、多主要槽、伤害公式或生命事实修改。
- 允许修改：Gameplay 元素状态/结果/事件、`Combatant` 与 `EnemyRoot` 生命周期接入、最小 Presentation 调试反馈、Bootstrap 两处敌方根及调试 Presenter、相关测试与权威文档。
- 禁止或只读：`DamageRequest` / `DamageResult` / `DamageResolver`、元素 Profile 数值、玩家根附着装配、Legacy/Lua、旧场景、第三方资源和无关配置。

## 当前事实与批准方案

- 架构与状态所有权以 [`Architecture.md`](../Architecture.md) 为准；元素目标规则以 [`Elements.md`](../Design/Elements.md) 为准；输入请求契约见 [`ElementApplicationProfileSnapshotV1.md`](ElementApplicationProfileSnapshotV1.md)。
- `ElementAttachmentRuntime` 是敌方 `Combatant` 根的唯一附着事实所有者；首版只启用索引 `0` 的主要槽，并通过只读数量/索引查询保留未来集合边界。
- `ElementApplicationResolver` 消费 `ElementApplicationRequest`。目标身份、目标 Health 初始化/耗尽、来源—目标间隔或时间非法时返回明确拒绝，不写入部分状态。
- 无附着时提交新状态；同元素再次施加时刷新持续时间，并以最近一次合法请求更新来源与执行快照；完全相同请求不制造新版本或事件。
- 不同元素返回保留已有附着与触发请求的待反应结果，不修改当前槽，也不提前提交来源—目标间隔；`ELM-030` 负责原子判定与消费。
- 每次附着或刷新生成递增版本；显式消费必须匹配当前版本，避免迟到消费者清除更新后的状态。
- `Combatant` 建立/结束目标生命周期，`EnemyRoot` 使用显式 `Time.time` 推进附着；不为 Gameplay 附着组件增加第二条独立 `Update` 主链。
- 到期、消费、Health 耗尽/重置和禁用仅在当前槽实际存在时清空一次；禁用同时清空来源—目标间隔，重新启用后的新 `TargetId` 不继承旧状态。
- `ElementAttachmentChangedEvent` 只在附着事实真正提交、刷新或清除后发布。Presentation 调试层只订阅/查询，不裁决附着。
- Bootstrap 精确为两处 `Enemy` Combatant 装配运行时所有者，并在现有 EventBus 根装配开发期调试叠层；玩家 Combatant 保持无附着所有者。

## 行为契约与验收

| ID | Given | When | Then | 自动/人工证据 |
|---|---|---|---|---|
| AC-01 | 活动、已初始化且存活的敌方目标没有附着 | 提交合法元素请求 | 主要槽保存元素、最近来源、执行、目标、起止时间和版本，并发布一次 Attached | EditMode / PlayMode |
| AC-02 | 目标已有同元素附着 | 在间隔允许时再次施加 | 到期时间按新请求刷新，来源更新为最近合法来源，版本递增且只发布一次 Refreshed | EditMode |
| AC-03 | 目标已有不同元素附着 | 提交另一元素请求 | 返回 ReactionRequired，保留已有附着与触发请求，槽、版本、间隔和事件均不变 | EditMode |
| AC-04 | 同一来源—目标配置了非零间隔 | 在边界前及边界时再次施加 | 边界前明确拒绝；边界时允许，不建立全局冷却 | EditMode |
| AC-05 | 当前附着版本有效 | 到期、匹配版本消费或重复处理 | 首次清除并发布对应事件；后续处理保持空状态且不重复发布 | EditMode / PlayMode |
| AC-06 | 目标带有附着 | Health 耗尽/重置，或目标禁用再启用 | 状态和间隔清空；重新启用使用新 TargetId，旧请求/版本不能修改新生命周期 | PlayMode |
| AC-07 | Bootstrap 已完成精确迁移 | 加载场景与检查组件 | 两处敌方 Combatant 各有一个运行时所有者，玩家没有；调试 Presenter 存在且没有 Missing Script | EditMode / 序列化扫描 |
| AC-08 | 已提交附着事件到达 Presentation | 附着、刷新或清除 | 开发期叠层稳定维护当前目标列表；Presentation 不反向写入 Gameplay | PlayMode / 人工观察 |

## 验证与最终证据

| 层级 | 用例 | 实际结果 | 证据路径 |
|---|---|---|---|
| EditMode | 施加、重复、刷新、待反应、间隔边界、版本消费、到期、重置、非法目标、事件次数、Bootstrap 装配与 Missing Script | 完整套件 36/36；本功能 5/5 | `Logs/Verification/20260820-201348/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 真实组件禁用/复用、旧请求拒绝、生命耗尽/重置、事件到调试 Presenter | 完整套件 8/8；本功能 2/2 | `Logs/Verification/20260820-201420-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
| 人工验收 | Bootstrap 中以真实武器来源观察附着倒计时 | 未运行；`WPN-010` 尚未接入生产来源 | 无，不以自动化或代码检查替代 |

- 实际命令：`pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1`；`pwsh -NoProfile -File .\Tools\Verify-ElementWarPlayMode.ps1`；Bootstrap GUID/引用扫描；测试日志失败标记扫描；`git diff --check`。
- 实际修改：新增目标侧附着快照、结果、Resolver、版本化消费、来源—目标间隔和已提交事件；由 `Combatant` 管理身份生命周期、`EnemyRoot` 推进时间；Bootstrap 两处敌方根装配唯一所有者，EventBus 根装配只读开发调试叠层，玩家根保持无所有者。
- 验收等级：达到 Fast Verified，并额外完成完整 PlayMode 与 Bootstrap 序列化/Missing Script 检查。Windows64、性能与主线人工玩法验收未运行，因此不声明 Full Verified 或 Accepted。
- 剩余风险：真实武器/技能尚未生产元素请求；异元素当前只返回 `ReactionRequired`，尚无 `ELM-030` 反应事务消费它；调试叠层虽通过事件同步测试，但没有生产来源可供本阶段人工观察。
- 回滚单位：Gameplay 状态/事件与接入、Presentation 调试层、Bootstrap 迁移、测试、Feature Spec/ADR 和路线状态整体回滚。

## 收口检查

- [x] 目标、范围、方案和可观察验收已有明确授权。
- [x] 实现与 scoped diff 未超出授权，且未吸收用户无关改动。
- [x] 实际测试数量大于 0；失败、未运行和证据缺口均如实记录。
- [x] 最终行为、运行方式、证据路径、维护约束和回滚单位仍然有效。
- [x] 项目级事实与 ADR 使用引用，本 Spec 不保留重复正文或可恢复的过程细节。
- [x] 对应 Roadmap 任务已更新状态、证据链接、后续解锁项和新的唯一 `Next`。
