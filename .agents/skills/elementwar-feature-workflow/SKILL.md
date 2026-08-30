---
name: elementwar-feature-workflow
description: 在 ElementWar 中继续路线、实现新增行为、处理非简单缺陷或重构时，按 Fast / Standard / Full 选择最小流程并控制上下文、工具输出和实现复杂度。
---

# ElementWar 开发路由

先遵守根 `AGENTS.md`；只有 Full 读取 `Docs/Workflow.md`。

## 等级

| 等级 | 条件 | 默认动作 |
|---|---|---|
| Fast | 需求明确，沿用现有契约，无高影响边界 | 直接实施；读直接代码/调用者/测试；不建 Spec/ADR |
| Standard | 跨现有系统，但所有权、公共契约和序列化边界已确定 | 直接实施；按职责补读最少 Architecture/Design；默认不建 Spec/ADR |
| Full | 状态所有者、跨模块公共契约、asmdef/依赖方向、序列化契约或共享 scene/prefab 结构迁移、Legacy 收敛、发布级性能/兼容性，或多个方案明显改变行为/接口/验收 | 读 Workflow；只确认未决高影响选择；按需建 Spec/ADR |

跨文件、新功能和文件数量不决定等级；只有实际触发 Full 条件才升级。

## 上下文

1. 按职责取最少事实：实现读代码，所有权/边界读相关 Architecture，玩法读相关 Design，范围只读当前 Roadmap task；先搜符号，再读定义、直接调用者和最相关测试。
2. 长文档/源码按 heading 或符号区间读取；约 300 行以上源码先读目标类型/方法附近，确有跨区控制流再扩展。已读且未变化的内容不重复全文读；`继续下一阶段` 只读唯一 `Next` 和对应任务块。
3. Done 依赖以当前代码/Architecture 为契约来源；历史 Feature/Superseded ADR 只在追溯证据或决策原因时读取。diff、设计、验证和性能资料均按当前任务缩小范围。

## 工具输出

- 完整 Unity/构建/Test Runner 日志写文件；上下文只留退出码、数量摘要、失败项和相关错误区间。大型 XML/JSON/YAML/scene/prefab 先定位目标字段、对象或 GUID。
- diff 先看 `--stat` / `--name-only` 再看 scoped diff；搜索先限定代码/测试或对应文档域，并排除 `Library`、`Temp`、`Logs` 和生成目录。预计超过约 150～200 行时改用过滤、区间或摘要；修改后不默认全文重读。

## 复杂度

默认实现当前验收所需的最直接方案。Request/Result/RejectionReason、新 Event、缓存/字典、平行状态、Target/Lifecycle ID、去重集合、生命周期包装、策略/工厂/通用接口都不是默认模板。

新增复杂度必须有当前证据：消费者需要；合法路径存在具体失败；任务明确要求；或不现在处理会造成已确定的 API/序列化迁移或重复实现。Roadmap、未来需求、其他组件已有结构或“更健壮”不能单独作为依据。

已由单一边界保证的不变量不重复校验；当前调用链不存在的故障模式不设计防线。详细代码注释按 `Docs/CodingStandards.md` 保留，不视为设计复杂度。

## 执行

1. 定位入口、权威所有者、直接消费者和相关测试。
2. 完成最小可观察行为链，不顺手扩范围、清理无关旧架构或预埋未来框架。
3. 从最低成本的相关验证开始，只按目标证据等级递进。
4. 用 scoped diff 收口，只同步当前事实、路线状态和证据；最终报告只写实际修改、验证、未运行项和真实剩余风险。
5. 仓库是跨任务记忆，聊天上下文不是；下一任务应能从当前代码、Architecture/Design、Roadmap 和 Git 恢复。
