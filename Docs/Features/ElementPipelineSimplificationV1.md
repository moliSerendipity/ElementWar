# 功能：元素施加与反应管线精简 v1

- 状态：Fast Verified（并额外通过 PlayMode；现有 Editor 手动 Test Runner 证据）
- 维护日期：2026-08-26
- 关联 ADR：[`ADR-Element-Pipeline-Simplification-v1.md`](../Decisions/ADR-Element-Pipeline-Simplification-v1.md)

## Current
- 可观察目标：不改变 ELM-010～030 已批准玩法行为，删除没有独立状态或真实配置需求的包装类型、重复验证和查询入口，使生产链能沿一条短路径读懂。
- 非目标：真实武器/技能来源、具体反应伤害或控制、反应事件、第五种元素、多槽附着、scene/prefab/asmdef 修改、Windows64 与性能优化。
- `ElementApplicationSourceSnapshot` 是不可变引用对象；不存在时直接使用 `null`，不再用结构体默认值加 `IsCreated` 模拟可空状态。
- `ElementApplicationRequest` 只保存来源引用、执行、目标引用/身份和时间。目标 Runtime 自己限定目标生命周期，间隔字典只需以 `ElementApplicationSourceId` 为键。
- `ElementApplicationResult` 只保存状态、拒绝原因和一个相关附着；调用管线已经持有请求，不再把请求及前后两个相同快照复制进结果。
- `ElementAttachmentRuntime` 只公开 `TryGetPrimaryAttachment`；索引查询、槽数量和任意公开消费入口均已移除。反应消费只能走内部原子事务。

## Evidence
- 当前编译：2026-08-26 使用生成的 `Game.Gameplay.csproj`、`Game.EditModeTests.csproj` 与 `Game.PlayModeTests.csproj` 进行 MSBuild，Foundation、Definition、Gameplay 和两个测试程序集均编译通过；输出位于系统临时目录 `ElementWar-AttachmentValidation-20260826-0054`。
- 当前 Unity 回归：2026-08-26 用户在现有 Unity Editor Test Runner 中手动运行全部测试并确认成功；Test Runner 当前测试总量为 EditMode 48、PlayMode 9，因此记录为 EditMode 48/48、PlayMode 9/9。
- 静态检查：`ValidateRequestStructure` 与反应提交中的重复时间推进已无残留；旧 TargetId 拒绝仍由 EditMode/PlayMode 生命周期测试覆盖；`git diff --check` 通过。

## Remaining Boundaries
- 证据边界：批处理验证入口两次停在 Editor 初始化前，故本轮没有独立 XML、验证摘要或仓库内日志；最新运行结果来自用户在现有 Editor 中的手动 Test Runner，当前窗口重新打开结果页后只保留测试总量，没有保留绿色结果计数。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
