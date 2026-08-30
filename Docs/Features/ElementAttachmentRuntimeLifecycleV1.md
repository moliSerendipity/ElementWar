# 功能：元素附着运行时与生命周期 v1

- 状态：Closed
- 验证：Verified
- 维护日期：2026-08-26
- 关联 ADR：[`ADR-Element-Attachment-Runtime-Lifecycle-v1.md`](../Decisions/ADR-Element-Attachment-Runtime-Lifecycle-v1.md)

## Current
- 可观察目标：敌方权威目标拥有一个可查询的主要元素附着；附着会按配置持续、同元素刷新、不同元素保留为待反应输入，并在到期、消费、死亡、禁用和复用时确定清理；开发调试视图只读展示已提交附着。
- 非目标：任一具体元素反应、反应伤害或控制、真实武器/技能来源接入、玩家元素附着、多主要槽、伤害公式或生命事实修改。
- `ElementAttachmentRuntime` 是敌方 `Combatant` 根的唯一附着事实所有者；首版只有一个主要槽，并只暴露 `TryGetPrimaryAttachment`，不为尚不存在的多槽需求预设数量或索引 API。
- 目标提交前只额外核对请求冻结的目标引用与 `TargetId`；时间合法性和 Runtime 绑定由 `TryAdvanceTime` 统一负责，Health/阵营由 `CanReceiveAttachment` 负责，反应提交不重复执行已经同步完成的请求与时间校验。
- 不同元素返回一个关联已有附着的待反应结果，不修改当前槽，也不提前提交来源间隔；调用管线继续持有触发请求并负责进入反应事务。

## Evidence
| 层级 | 内容 | 结果 | 证据 |
|---|---|---|---|
| EditMode | 施加、重复、刷新、待反应、间隔边界、版本消费、到期、重置、非法目标、事件次数、Bootstrap 装配与 Missing Script | 完整套件 36/36；本功能 5/5 | `Logs/Verification/20260820-201348/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 真实组件禁用/复用、旧请求拒绝、生命耗尽/重置、事件到调试 Presenter | 完整套件 8/8；本功能 2/2 | `Logs/Verification/20260820-201420-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
| 当前编译 | 2026-08-26 请求校验精简后的 Gameplay、EditModeTests 与 PlayModeTests | MSBuild 全部通过 | 系统临时目录 `ElementWar-AttachmentValidation-20260826-0054` |
| 当前 Unity 回归 | 2026-08-26 最新差异的 EditMode / PlayMode | 用户在现有 Editor 中手动运行并确认 EditMode 48/48、PlayMode 9/9 全部成功 | 无独立 XML；Test Runner 当前列表核对总量 48/9 |
| 人工验收 | Bootstrap 中以真实武器来源观察附着倒计时 | 未运行；`WPN-010` 尚未接入生产来源 | 无，不以自动化或代码检查替代 |

## Remaining Boundaries
- 剩余风险：`ELM-030` 已消费异元素待反应结果；真实武器/技能仍未生产元素请求，调试叠层因此没有生产来源可供人工观察。
- [x] 2026-08-26 最新差异的 EditMode 48/48、PlayMode 9/9 已由用户手动运行并确认成功，证据边界已如实记录。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
