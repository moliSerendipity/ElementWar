# <Feature ID> — <Feature Name>

- 状态：Draft / Approved / Implemented / Verified / Closed
- 路线任务：
- 更新时间：

> 本文只记录当前功能契约和最终证据。不要把调查过程、聊天记录、逐次实现日志或所有被考虑过的方案写入正文。

## Current Contract

### Goal

一句到数句描述用户最终能观察到的行为。

### Non-Goals

只列容易被误认为属于本任务、但明确不做的内容。

### Current Facts

只记录完成本功能必须依赖、且无法直接从名称看出的当前仓库事实。

- 权威状态所有者：
- 当前入口：
- 当前消费者：
- 相关生命周期：
- 直接依赖：

不要复制 Architecture 已经完整记录的事实；使用链接即可。

## Implementation

### Minimal Slice

描述本次实际需要形成的最小垂直链：

```
<入口> → <处理> → <权威事实> → <消费者/可观察结果>
```

### Files / Contracts

| 文件或契约 | 作用 |
| ---------- | ---- |
|            |      |

只有真实需要新增的类型、状态、事件或公共契约才列入。

### Design Constraints

只写当前功能特有、以后修改时仍必须保留的规则。

例如：

- 同一次攻击只能提交一次该效果；
- 选择在开火瞬间冻结，而不是命中瞬间；
- Presentation 只能读取已提交事实。

不要加入对象池、异步、重连、重复消息、旧数据兼容等通用风险清单，除非当前仓库确实存在对应路径。

## Acceptance

使用可观察结果表达验收。

- 

  AC-01：

- 

  AC-02：

- 

  AC-03：

验收数量按实际复杂度决定，不为了模板凑数量。

## Verification

| Evidence        | Result |
| --------------- | ------ |
| 精确测试        | 未运行 |
| EditMode        | 未运行 |
| PlayMode        | 未运行 |
| Windows64 Build | 未运行 |
| 人工验收        | 未运行 |

不适用项删除。

### Commands / Evidence

只记录最终有效的验证命令、结果和证据路径。失败后已经被替代的中间日志不保留在 Closed 文档正文。

## Remaining Boundaries

只列当前明确存在但不属于本任务的真实边界。

- 无 / ...

## Related Decisions

仅在存在长期 ADR 时链接：

- 无 / `ADR-...`

## Closed Compression

功能关闭后，将本文压缩到：

1. 最终 Goal；
2. 最终可观察契约；
3. 关键文件/公共契约；
4. 验证结果和证据；
5. Remaining Boundaries；
6. ADR 链接。

删除：

- 调查过程；
- 已被替代方案；
- 中间实现步骤；
- 重复的 Architecture 内容；
- 可以由 Git 恢复的文件变更历史；
- 已解决风险；
- 多轮授权和聊天记录。
