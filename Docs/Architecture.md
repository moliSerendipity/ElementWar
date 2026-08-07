# ElementWar 架构与边界

状态：当前新 C# 架构基线  
核对日期：2026-08-05  
Unity：`2022.3.62f2c1`

本文件是采用后的架构、主线和旧代码边界唯一维护来源。每次高影响任务仍需根据实时仓库复核是否发生漂移。

## 程序集职责

```text
Game.Foundation → Game.Definition → Game.Gameplay → Game.Presentation
Game.Editor → 上述程序集的编辑器工具
```

| 层 | 职责 | 禁止事项 |
|---|---|---|
| Foundation | 事件、对象池、调度和通用运行时设施 | 依赖游戏业务层 |
| Definition | ScriptableObject 定义、索引和校验 | 持有可变玩法运行时状态 |
| Gameplay | 角色、武器、战斗、敌人状态与规则 | 依赖 Presentation 决定玩法事实 |
| Presentation | 动画、音频、VFX、UI 和视图适配 | 写入伤害、AI 等权威事实 |
| Editor | 编辑器和构建工具 | 进入 Player 运行时代码 |

新增依赖不得反向穿透这些边界。

## 运行时责任流

```text
Input → Decision → Execution → Fact writeback → Event → Presentation
```

- 输入层表达意图。
- Character/Enemy 决策层选择行为。
- 执行器完成武器、移动或战斗操作。
- 权威运行时组件提交生命、状态等事实。
- 事件总线传播已经发生的事实。
- 表现层响应事实，不重新裁决事实。

当前伤害主链可概括为：

```text
WeaponFireExecutor → HitScanService → DamageResolver
                   → HealthComponent → DamageAppliedEvent
                   → Presentation / 其他消费者
```

## 当前主线

默认允许在已批准任务范围内修改：

- `Assets/Scripts/Foundation`
- `Assets/Scripts/Definition`
- `Assets/Scripts/GamePlay`
- `Assets/Scripts/Presentation`
- `Assets/Scripts/Editor`
- `Assets/Configs`
- `Assets/Scenes/Bootstrap/Bootstrap.unity`

“主线”不等于无限授权。公共接口、asmdef、序列化数据、scene/prefab 和跨模块改动仍需先澄清。

## 默认只读旧架构

- `Assets/Script_Legacy`
- `Assets/LuaScripts`
- `Assets/Scenes/SampleScene.unity`
- 仅供旧场景使用的 prefab 与组件

可以读取它们以理解历史行为和迁移要求，但普通功能不得同时扩展两套实现。

## 共享资源

美术、动画、音频、VFX、第三方插件、Input System、URP、Addressables 等可能被新旧链路共同引用，不能仅凭路径判断为旧资源。移动或删除前必须检查 `.meta` GUID 和序列化引用。

## 当前已知越界点

1. `Assets/Script_Legacy` 没有 asmdef，仍进入 `Assembly-CSharp` 编译。
2. `Assets/Scenes/SampleScene.unity` 仍在 Build Settings 中启用。
3. `Bootstrap.unity` 的 `FN IWS Primary` 仍有旧 `WeaponView`。
4. 旧 Lua 元素反应与新 C# 伤害主链尚未收敛。

这些问题必须作为独立迁移任务处理，并保留引用检查、验证与可恢复提交。

## 状态所有权

| 内容 | 权威所有者 |
|---|---|
| 编辑期默认值 | Definition 配置 |
| 可变运行时数值 | Gameplay Stat/Runtime 组件 |
| 生命和伤害事实 | Combat 运行时组件 |
| AI 决策 | Enemy Runtime/Brain |
| 移动执行与修饰 | Enemy Locomotion |
| 已发生事实通知 | Foundation Event Bus |
| 视觉/音频响应 | Presentation |

不要为单项功能增加第二套生命、伤害、移动或配置真相源。

## 需要 ADR 的变化

以下变化先使用 `Docs/Decisions/ADR-TEMPLATE.md`：

- 程序集方向或层级职责变化。
- 权威状态所有者变化。
- 公共战斗、事件或配置契约变化。
- 场景组合根变化。
- 需要迁移的序列化结构变化。
- 新旧架构收敛策略。
