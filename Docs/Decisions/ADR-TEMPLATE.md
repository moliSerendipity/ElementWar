# ADR — <Decision Title>

- 状态：Proposed / Accepted / Superseded
- 日期：
- 关联领域：
- 关联 Feature：

> ADR 只保存会长期约束后续实现的设计决定；普通实现细节、Feature 验收、测试日志和开发过程不写入。

## Context

只说明为什么该选择不能作为局部实现细节处理，以及哪些后续修改会依赖它。

## Decision

使用短、明确、可执行的陈述记录最终决定；同一事实只在这里或对应 Architecture 权威位置展开一次。

## Rationale

只保留代码或规则本身无法表达、且未来重新评估仍需要知道的原因。

## Consequences

记录长期收益与真实取舍，不记录一次性实施成本、测试流水或授权过程。

## Revisit When

仅在已有明确、可观察的重新评估条件时保留本节；没有则删除。不要用“未来可能扩展”作为条件。

## Supersession

发生替代时记录 `Supersedes` / `Superseded by`；否则删除本节。被替代 ADR 正文压成状态、替代链接和必要历史边界，详细过程由 Git 追溯。
