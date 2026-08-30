# Development Roadmap Reference

本页只保留正常推进时不需要反复加载、但仍会约束未来开发的路线原则。`继续下一阶段` 默认不要读取本页；当前能力和任务状态分别以 Architecture 与 `DevelopmentRoadmap.md` 为准。

## 当前基线索引

- 全局架构与状态所有权：[`../Architecture.md`](../Architecture.md)
- Combat：[`../Architecture/Combat.md`](../Architecture/Combat.md)
- Elements：[`../Architecture/Elements.md`](../Architecture/Elements.md)
- Enemy Control：[`../Architecture/EnemyControl.md`](../Architecture/EnemyControl.md)
- 当前唯一 Next、依赖骨架和分域路线：[`../DevelopmentRoadmap.md`](../DevelopmentRoadmap.md)

本页不重复上述事实；需要追溯证据或长期决策原因时再读取对应 Feature / ADR。

## 重构归属

发现问题时优先归入原有领域，不自动创建新的横向框架。

| 问题 | 默认归属 |
|---|---|
| 战斗身份、阵营、目标解析、伤害与生命 | Combat |
| 元素来源、附着与反应 | Elements |
| 武器开火、弹药、投射物与爆炸 | Weapons / Projectile |
| 玩家输入和角色行为 | Characters |
| 队伍、切换、倒地复活 | Party / Life |
| 技能、能量和属性修改 | Skills |
| 敌人状态、AI、韧性与控制 | Enemies / Enemy Control |
| Run、波次和强化 | Run |
| Boss 专属玩法 | Boss |
| 场景装配、Bootstrap | Integration |
| 旧新系统收敛 | Integration / Legacy |
| 发布级性能与构建 | Release |

只有同一问题已经在多个领域真实重复出现，并且当前任务必须解决这种重复时，才考虑提取共享抽象。

## 首版范围控制

Roadmap 中的未来任务不构成当前代码的预实现要求。当前任务只需要提供：

1. 当前验收所需行为；
2. 已确定会被下一项直接消费的最低成本契约；
3. 不造成已经确定的 API / 序列化迁移阻碍。

更多武器、元素、角色、联机、对象池、存档、异步资源、Buff 或复杂技能等未来能力，在对应任务开始前都不能单凭 Roadmap 的存在要求当前功能预建接口、状态层或兼容逻辑。

## 版本裁剪

出现时间或范围压力时，优先保证完整垂直闭环：

```text
输入 → 角色/武器 → 攻击 → 战斗 → 元素 → 敌人 → 反馈 → Run/Boss → 可构建版本
```

裁剪任务时修改 Roadmap 状态，不在当前代码中留下半套“以后可能用”的框架。

## 历史信息

路线正文不保存开发流水账。实现时间、曾尝试方案、逐次文件变更和旧测试输出优先由 Git 追溯；当前事实看 Architecture/Design，当前范围看 Roadmap，只有需要追溯证据或长期决策原因时才读取 Feature / ADR。
