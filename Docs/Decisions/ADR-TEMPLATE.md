# ADR — <Decision Title>

- 状态：Proposed / Accepted / Superseded
- 日期：
- 关联领域：
- 关联 Feature：

> ADR 只保存长期设计决定。普通实现细节、Feature 验收、测试日志和开发过程不写入 ADR。

## Context

只描述为什么这个问题需要长期决策。

重点回答：

- 当前存在什么长期架构冲突或选择？
- 为什么不能仅作为局部实现细节处理？
- 哪些未来修改会依赖这个决定？

不要复制完整 Feature 背景。

## Decision

使用明确、可执行的陈述记录最终决定。

例如：

- `HealthComponent` 是生命数值唯一权威所有者。
- Presentation 不允许直接提交伤害。
- 元素反应组合保持代码内固定规则，当前不建立反应表资产。

如果有多个子规则，使用短列表。

## Rationale

只解释代码或规则本身无法表达的关键原因。

避免把所有分析过程写进来；保留以后重新评估时真正需要知道的信息。

## Alternatives

只记录主要、真实可行且曾经会改变长期架构的替代方案。

### <Alternative A>

未采用原因：

### <Alternative B>

未采用原因：

如果没有值得长期记录的替代方案，整个 section 可以删除。

## Consequences

### Positive

- ...

### Trade-offs

- ...

只记录长期后果，不记录一次性实施成本。

## Revisit When

只有存在明确重新评估条件时保留，例如：

- 出现第二个真实消费者；
- 需要支持运行时动态配置；
- 进入联机架构；
- 当前静态规则产生已测量性能瓶颈；
- 需要兼容真实历史存档。

不要使用“以后可能需要扩展”作为重新评估条件。

## Supersession

如果本 ADR 替代旧 ADR：

- Supersedes：
- Superseded by：

未发生替代时删除本 section。
