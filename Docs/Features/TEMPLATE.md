# <Feature ID> — <Feature Name>

- 状态：Draft / Active / Implemented / Verified / Closed
- 路线任务：
- 更新时间：

> 只记录当前功能契约和最终证据；调查过程、聊天记录、逐次实现日志和已被替代方案由 Git 追溯。

## Current Contract

### Goal

用一到数句描述最终可观察行为。

### Non-Goals

只列容易被误认为属于本任务、但明确不做的内容。

### Current Facts

只记录完成本功能必须依赖、且无法直接从名称看出的事实：权威所有者、入口/消费者和直接依赖。Architecture 已展开的事实只链接，不复制。

## Minimal Slice

```text
<入口> → <处理> → <权威事实> → <消费者/可观察结果>
```

只列当前切片真实需要的新类型、状态、事件或公共契约，不为未来任务预留结构。

## Design Constraints

只写本功能特有、以后修改时仍必须保留的规则。当前仓库不存在的故障模式、生命周期或兼容路径不要列入。

## Acceptance

- [ ] AC-01：
- [ ] AC-02：

按实际复杂度增减，不为模板凑数量。

## Evidence

| 验证 | 结果 | 证据 |
|---|---|---|
| `<当前切片实际需要的验证>` | 未运行 | |

验证种类和等级按项目 Skill / verification matrix 选择；不因为模板存在而补跑无关测试。只保留最终有效证据，失败后已被替代的中间日志不写入 Closed 正文。

## Remaining Boundaries

只列当前明确存在但不属于本任务的真实边界；没有则写“无”。

## Related Decisions

仅在存在长期 ADR 时链接；否则删除本节。

## Closed Compression

关闭后只保留最终 Goal/契约、关键文件或公共契约、最终证据、Remaining Boundaries 和 ADR 链接；删除调查过程、中间实现、已解决风险、重复 Architecture 内容和可由 Git 恢复的历史。
