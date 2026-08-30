# Development Roadmap Reference

本页保存正常推进时不需要反复加载的路线参考信息。

`继续下一阶段` 默认不要读取本页。只有需要核对已完成能力、重构归属、版本范围或某项历史路线约束时才读取相关 section。

## 已完成主线基线

以下能力已经形成当前主线事实。具体实现以当前代码和 Architecture 为准，Feature/ADR 只在需要追溯决策时读取。

### Foundation / Definition

- 基础事件设施、对象池等通用运行时能力已进入独立 Foundation 边界。
- Definition 保存编辑期配置和索引，不持有可变 Gameplay 状态。
- 新主线程序集依赖方向已经建立。

### Combat

已完成的核心能力包括：

- `Combatant` 战斗目标根、阵营和生命周期身份；
- 战斗目标解析；
- 范围目标查询；
- `AttackExecutionId`；
- `DamageRequest` / `DamageResult`；
- 伤害裁决、阵营许可和同执行去重；
- `HealthComponent` 生命事实；
- 已提交伤害和生命事件。

当前事实以 `../Architecture/Combat.md` 为准。

### Elements

已完成的核心能力包括：

- 元素应用 Profile；
- 来源身份和生命周期快照；
- `ElementApplicationRequest`；
- `ElementReactionPipeline`；
- 目标侧元素附着运行时；
- 来源—目标应用间隔；
- 同元素刷新；
- 不同元素反应解析；
- 附着版本和反应执行去重；
- 附着变化事件。

当前事实以 `../Architecture/Elements.md` 为准。

### Enemy Toughness / Control

已完成的核心能力包括：

- 敌人韧性配置与运行时快照；
- `ToughnessComponent`；
- `HardControlComponent`；
- 敌人等级控制策略；
- Normal / Elite / Boss 控制转换；
- 控制执行去重；
- AI 对失衡和硬控事实的阻断。

当前事实以 `../Architecture/EnemyControl.md` 为准。

## 路线分域

### 01 Combat / Elements / Weapons

负责：

- 战斗事实；
- 元素施加与反应；
- 武器实例；
- 开火；
- 弹药；
- 投射物；
- 爆炸。

路线文件：

```
01-CombatElementsWeapons.md
```

### 02 Characters / Party / Skills

负责：

- 输入；
- 玩家角色；
- 角色切换；
- Party；
- 倒地/复活；
- 技能；
- 能量；
- HUD 对应 Gameplay 事实。

路线文件：

```
02-CharactersPartySkills.md
```

### 03 Enemies / Run / Boss

负责：

- 普通敌人；
- AI；
- 波次；
- Run；
- 强化；
- Boss；
- Boss 特殊机制。

路线文件：

```
03-EnemiesRunBoss.md
```

### 04 Integration / Release

负责：

- Bootstrap；
- Composition Root；
- 主线集成；
- Legacy 收敛；
- 第三方许可；
- 验证；
- 性能；
- 构建和发布候选。

路线文件：

```
04-IntegrationRelease.md
```

## 重构归属

发现问题时优先归入原有领域，不自动创建新的横向框架。

| 问题                     | 默认归属             |
| ------------------------ | -------------------- |
| 战斗身份、阵营、目标解析 | Combat               |
| 伤害、生命、伤害去重     | Combat               |
| 元素来源、附着、反应     | Elements             |
| 武器开火和弹药           | Weapons              |
| 投射物和爆炸             | Projectile           |
| 玩家输入和角色行为       | Characters           |
| 队伍、切换、倒地复活     | Party / Life         |
| 技能、能量               | Skills               |
| 敌人状态和 AI            | Enemies              |
| 韧性、失衡、硬控         | Enemy Control        |
| Run、波次和强化          | Run                  |
| Boss 专属玩法            | Boss                 |
| 场景装配、Bootstrap      | Integration          |
| 旧新系统收敛             | Integration / Legacy |
| 发布级性能与构建         | Release              |

只有同一问题已经在多个领域真实重复出现，并且当前任务必须解决这种重复时，才考虑提取共享抽象。

## 首版范围控制

路线中的未来任务不构成当前代码的预实现要求。

例如某个后续任务计划支持：

- 更多武器；
- 更多元素；
- 更多角色；
- 联机；
- 对象池；
- 存档；
- 异步资源；
- Buff 系统；
- 更复杂的技能组合；

在对应任务真正开始前，都不能仅凭 Roadmap 中的存在要求当前功能提前增加完整接口、状态层或兼容逻辑。

当前任务只需要提供：

1. 当前验收所需行为；
2. 已确定会被下一项直接消费的最低成本契约；
3. 不造成已经确定的序列化/API 迁移阻碍。

除此以外留到后续任务处理。

## 版本裁剪

路线可能包含发布前能力，但不表示每项都必须进入首个可玩版本。

出现时间或范围压力时，优先保证完整垂直闭环：

```
输入
→ 角色/武器
→ 攻击
→ 战斗
→ 元素
→ 敌人
→ 反馈
→ Run/Boss
→ 可构建版本
```

比横向铺开大量未闭环系统优先级更高。

裁剪某项时应修改 Roadmap 状态，而不是在当前代码中留下半套“以后可能用”的框架。

## 历史信息规则

路线正文不保存详细开发流水账。

需要知道：

- 某个实现什么时候建立；
- 曾经尝试过什么方案；
- 某次修改具体改了哪些文件；
- 某次测试输出是什么；

优先查看 Git、对应 Feature Spec 或 ADR。

只有仍会约束未来开发的结论才保留在本页。