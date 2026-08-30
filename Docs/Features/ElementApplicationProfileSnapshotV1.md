# 功能：元素施加配置与快照契约 v1

- 状态：Closed
- 验证：Verified
- 维护日期：2026-08-23
- 关联 ADR：[`ADR-Element-Application-Profile-Snapshot-v1.md`](../Decisions/ADR-Element-Application-Profile-Snapshot-v1.md)

## Current
- 可观察目标：合法配置能冻结为独立元素来源快照；一次有效攻击或技能执行在不创建伤害请求的情况下也能为敌方权威目标建立确定的元素施加请求；非法配置或身份返回明确原因。
- 非目标：实际附着状态、应用间隔计时、刷新/消耗、元素反应、武器元素选择、伤害公式修改、事件与表现反馈。
- `ElementApplicationProfileConfig` 只定义元素、来源—目标应用间隔和附着持续时间；`ConfigId` 是 Definition 查询键，不是运行时来源身份。
- 来源快照是不可变 class，冻结 Profile Id 与数值、责任 Combatant 引用/Id/阵营及具体 SourceObject；后续请求共享该引用，不重新读取配置或当前控制角色，缺失来源直接以 `null` 表达。
- `ElementApplicationRequest` 直接由来源快照、`AttackExecutionId` 和当前目标创建，与伤害请求并列；零伤害、免疫伤害或纯元素技能不因缺少 `DamageResult` 而失去附着入口。

## Evidence
| 层级 | 内容 | 结果 | 证据 |
|---|---|---|---|
| EditMode | 配置、查询、冻结、失败原因、间隔键、真实资产 | 完整套件 31/31；本功能 5/5 | `Logs/Verification/20260812-011452/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 无伤害依赖、目标禁用/复用生命周期 | 完整套件 6/6；本功能 1/1 | `Logs/Verification/20260812-011609-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
- 验证结论：当前切片所需的 EditMode、PlayMode 与真实资产加载证据均针对当前源码通过；Windows64、主线人工验收和性能检查未运行。

## Remaining Boundaries
- 当前演进：`WPN-010` 已由真实步枪来源保存并按责任角色生命周期重建 Fire/Electric `SourceId`，两个 Profile 已进入玩家开火链；真实技能来源仍由后续技能任务接入。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
