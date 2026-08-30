# 功能：元素反应判定、消费与归因管线 v1

- 状态：Fast Verified（并额外通过 PlayMode；现有 Editor 手动 Test Runner 证据）
- 维护日期：2026-08-26

## Current
- 可观察目标：同一次命中的弹药元素先于技能元素处理；四元素六个无序组合确定映射到唯一反应；首次成功反应原子消费已有附着、停止剩余应用，并保留第二元素来源归因。
- 非目标：具体反应伤害、控制、范围查询、触发基准伤害、武器/技能生产来源、VFX/SFX/HUD、玩家元素附着和主线人工玩法。
- `ElementReactionPipeline.ResolveAndApply` 提供单请求和双请求重载。双请求参数语义固定为弹药、技能，不接受可空集合或额外批次容器。
- `TryResolveReactionType` 直接表达火、水、雷、冰六个固定无序组合。当前不存在运行时换表、多个规则集或设计师编辑映射需求，因此不维护 Definition 表资产。
- `ElementAttachmentRuntime` 是当前附着、来源间隔、版本与当前目标生命周期反应执行去重的唯一所有者。反应提交在一个内部事务中重验目标/时间、当前附着版本、元素关系、触发来源间隔和执行去重，然后登记间隔/去重并消费附着。
- `ElementReactionResult` 只在反应成功时保存反应类型、被消费附着和第二元素请求；默认值表示没有反应。调用者可从第二元素请求读取责任 Combatant、SourceObject、执行与目标身份。

## Evidence
- 当前编译：2026-08-26 使用生成的 Gameplay、EditModeTests 与 PlayModeTests 工程进行 MSBuild，生产程序集及两个测试程序集全部编译通过。
- 当前 Unity 回归：2026-08-26 用户在现有 Unity Editor Test Runner 中手动运行并确认 EditMode 48/48、PlayMode 9/9 全部成功；测试总量已从当前 Test Runner 列表核对。
- 当前静态检查：删除类型与两个删除资产 GUID 在 `Assets`、`Packages`、`ProjectSettings` 中无残留，默认 Registry 不再引用反应表，`git diff --check` 通过。

## Remaining Boundaries
- 证据边界：批处理入口未能完成 Editor 初始化，因此没有生成独立 XML；最新结果依赖用户手动运行确认，重新打开结果页后只保留 48/9 的测试总量，没有保留绿色结果计数。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
