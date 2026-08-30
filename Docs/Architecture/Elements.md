# Elements 架构事实

只在任务涉及元素来源、附着或反应管线时读取。

## 主链

```
Gameplay 来源所有者 → ElementApplicationSourceSnapshot → ElementApplicationRequest
                     → ElementReactionPipeline（弹药 → 技能）
                     → 内部 ElementApplicationResolver → 目标 ElementAttachmentRuntime
                     → ElementReactionResult / ElementAttachmentChangedEvent
                     → 后续反应输出 / Presentation
```

## Current

- `ElementType`、`DamageDeliveryType`、`HitPartType` 分别表达伤害元素、传递形态和命中部位；伤害元素轴当前只参与抗性，不隐式产生附着。
- 元素施加与伤害请求并列。`ElementApplicationProfileConfig` 定义元素、来源间隔和持续时间；`ElementApplicationSourceId` / `ElementApplicationSourceSnapshot` 冻结来源生命周期与归属；`ElementApplicationRequest` 使用独立 `AttackExecutionId` 和目标身份。Profile 结构只在 Bootstrap 配置校验阶段验证。
- `ElementReactionPipeline` 是生产侧入口：支持单请求和固定“弹药、技能”双请求；双请求只预检共同执行/目标/时间，首次反应或当前阶段拒绝后停止。四元素六个无序组合是固定 Gameplay 规则，不维护无真实换表需求的反应表资产；低层 Resolver 仅供管线内部使用。
- `ElementAttachmentRuntime` 唯一拥有当前附着、来源间隔、附着版本和本目标生命周期反应执行去重。间隔在当前目标生命周期内以 `ElementApplicationSourceId` 为键；同元素刷新使用最近合法来源，不同元素按管线规则原子登记并消费版本匹配附着。
- `EnemyRoot` 显式推进附着到期/生命清理；Presentation 只消费已提交事件、成功反应结果和只读快照。当前没有反应事件或具体反应输出。

## ADR

- `ADR-Element-Application-Profile-Snapshot-v1.md`
- `ADR-Element-Attachment-Runtime-Lifecycle-v1.md`
- 现行精简决策：`ADR-Element-Pipeline-Simplification-v1.md`