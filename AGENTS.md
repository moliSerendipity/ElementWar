# ElementWar 仓库规则

本文件适用于整个仓库；深层 `AGENTS.md` 可为子树增加或覆盖规则。

## 入口与授权

- 新功能、跨文件/模块修改、公共接口、状态所有权、序列化、scene/prefab、性能、兼容性、迁移或复杂缺陷使用 `$elementwar-feature-workflow`，流程与完成标准以 [`Docs/Workflow.md`](Docs/Workflow.md) 为准。
- 获得明确实施授权前只做只读检查，并在对话中总结目标、非目标、范围、方案、风险和可观察验收；不得编辑文件或创建正式交付物。只询问仓库无法回答且会改变设计、范围、行为、接口或验收的问题。
- 知识问答、只读审查、状态报告和无设计取舍的低风险小改动可直接处理。实施中出现新的高影响选择时再次暂停。
- `继续下一阶段`：读取 [`Docs/DevelopmentRoadmap.md`](Docs/DevelopmentRoadmap.md) 中唯一 `Next`、对应路线细则、直接相关材料、实现和 Git 状态；不要从旧对话重新推导。
- `开始功能：<名称或 ID>`：核对指定任务及依赖。没有 `Next`、存在多个 `Next`、依赖未完成或路线与仓库冲突时暂停并报告。

## 权威来源与修改边界

- 架构现状与旧代码边界：[`Docs/Architecture.md`](Docs/Architecture.md)；开发顺序与状态：[`Docs/DevelopmentRoadmap.md`](Docs/DevelopmentRoadmap.md) 及路线细则；文档职责、精简和证据等级：[`Docs/Workflow.md`](Docs/Workflow.md)。旧对话、记忆和历史计划只作线索。
- 既有未提交改动属于用户；不得覆盖、清理、格式化或吸收无关内容。文档变更同步受影响的权威来源、导航和摘要，但不得借同步扩大范围。
- 旧架构默认只读，除非用户明确批准迁移或清理。除非任务明确针对该依赖，不得修改第三方源码/二进制；不得手工编辑或清理 `Library`、`Temp`、`Logs`、`obj`、`.vs` 和生成的 `.sln/.csproj`。仓库验证入口产生的临时状态与证据除外，运行后必须检查副作用。
- 除非用户明确要求，不删除、重置、暂存、提交、推送或创建 PR。

## Unity 资源安全

- `.cs` 与 `.meta` / GUID 一并维护；新增资源确认对应 `.meta` 正确生成和跟踪。
- scene、prefab、ScriptableObject、材质、动画控制器、Input Action 和 Project Settings 都是序列化迁移；修改前后检查引用，不批量改写 Unity YAML，未经明确授权不移动或删除共享资源。
- 命令行验证前保存并关闭 Editor；不得用第二个 Unity 进程打开同一工程。

## 实施、验证与交付

- 只修改获批范围，按最小可验证切片复用现有事实源；当前最小实现与最低成本稳定接缝的判断统一遵守 [`Docs/Workflow.md`](Docs/Workflow.md)，不预埋完整框架，也不因消费者尚未实施就自动否定已批准扩展。
- 新增或修改 C# 时读取 [`Docs/CodingStandards.md`](Docs/CodingStandards.md)，只规范触及代码并遵守 [`Docs/Architecture.md`](Docs/Architecture.md) 的依赖方向。
- 只运行仓库中实际存在的验证入口。已执行测试为 0、目标证据缺失、非零退出码或旧日志都不是通过；不适用项省略，未运行的测试、构建、手测和性能检查明确标记“未运行”。
- 收口时同步 Feature Spec/ADR（如适用）、路线状态、证据、解锁项和唯一 `Next`。最终报告只陈述有证据的行为、文件、命令、数量、结果、证据路径、未运行项、风险与回滚。
