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
- 当前步枪 `WeaponRuntime` 保存 Fire/Electric 选择及两条稳定来源身份；开火成立后、Hitscan 前冻结选中来源，伤害与元素请求共享同一攻击执行。当前 `T` 只做最小即时切换，独立元素弹匣与特殊换弹仍未实现。
- `OverloadReactionResolver` 由当前步枪在成功反应后同步调用：以反应目标为中心查询 `3.5m` 内合法目标，用一个新的反应执行身份分别提交 `80%` 触发元素/Explosion 伤害和敌人控制；基础削韧与 Boss 额外转换削韧均固定为 `200`，伤害与控制使用既有独立去重集合。
- `EnemyRoot` 显式推进附着到期/生命清理；Presentation 只消费已提交事件和只读快照。当前仍不发布无消费者的反应事件，Overload 反馈复用每个已提交范围伤害产生的一次 `DamageAppliedEvent`。

## ADR

- `ADR-Element-Application-Profile-Snapshot-v1.md`
- `ADR-Element-Attachment-Runtime-Lifecycle-v1.md`
- 现行精简决策：`ADR-Element-Pipeline-Simplification-v1.md`
