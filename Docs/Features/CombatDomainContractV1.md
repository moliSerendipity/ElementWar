# 功能：Combat Domain Contract v1

- 状态：Verified
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-08
- 关联 ADR：[`ADR-Combat-Domain-Contract-v1.md`](../Decisions/ADR-Combat-Domain-Contract-v1.md)
- 授权记录：2026-08-08，用户确认推荐方案并明确回复“开始实施”。

## 目标与范围

- 可观察目标：步枪 Hitscan 与 `EnemyAttack` 通过同一确定性伤害主链，结果明确保留责任角色、攻击来源、元素、传递形态和生命耗尽事实。
- 非目标：元素附着与反应、投射物/榴弹枪/手雷、双角色/AI 队友/倒地/复活、技能/Buff/波次/Boss/网络、全面拆分 `WeaponRuntime`、Legacy/Lua 清理。
- 允许修改：当前 Definition、Gameplay、必要 Presentation/Editor 消费者、活动配置、Bootstrap、Gameplay 测试程序集与相关文档。
- 禁止或只读：`Assets/Backup`、`Assets/Script_Legacy`、`Assets/LuaScripts`、`Assets/Scenes/SampleScene.unity`、第三方资源以及无关用户改动。

## 当前事实与批准方案

- 当前伤害链与程序集边界以 [`Architecture.md`](../Architecture.md) 为准；设计约束以 [`Combat.md`](../Design/Combat.md) 和 [`Elements.md`](../Design/Elements.md) 为准。
- `ElementType` 表达 `None/Fire/Water/Electric/Ice`；`DamageDeliveryType` 表达 `Direct/Explosion`；命中部位继续由 `HitPartType` 表达。
- `Instigator` 是承担伤害/击杀归属的角色或敌人 `GameObject`；`SourceObject` 是具体武器运行时或攻击配置 `UnityEngine.Object`。
- 当前步枪映射为角色根 + `WeaponRuntime` + `None/Direct`；当前敌人攻击映射为敌人根 + `EnemyAttackConfig` + 配置元素/传递形态。
- 伤害公式不包含随机暴击：`BaseDamage × HitPart × Defense × ElementResistance × DeliveryResistance × DamageTaken`。`None` 使用物理抗性，`Explosion` 在元素抗性之外独立应用爆炸抗性。
- `HealthComponent.CurrentHealth` 是唯一存储的生命事实；`IsHealthDepleted` 从已初始化且生命值不大于零派生。`CharacterFacts` 只读引用该事实，不保存第二个死亡布尔值。
- 生命首次从正数降到零时发布一次 `HealthDepletedEvent`；敌人状态机把生命耗尽映射为 `Dead`。本阶段不定义倒地或复活行为。
- 活动主链端到端移除暴击输入、结果和表现分支；弱点与头部倍率保持确定性。
- 本次本来需要修改的契约类型移除冗余 `Combat` 前缀；不执行仓库级命名清理。

## 行为契约与验收

| ID | Given | When | Then | 自动/人工证据 |
|---|---|---|---|---|
| AC-01 | 角色持有当前 Hitscan 步枪 | 命中合法生命目标 | 只经过 `DamageResolver` 一次，结果为角色 Instigator、`WeaponRuntime` SourceObject、`None/Direct` | PlayMode |
| AC-02 | EnemyAttack 已选中攻击配置 | Strike 命中目标 | 使用同一主链和配置来源，不构造 Hitscan 上下文 | PlayMode |
| AC-03 | 请求、目标数值与生命初值相同 | 重复解析 | 最终伤害完全相同，不存在暴击随机输入或结果 | EditMode |
| AC-04 | Default、Head、WeakPoint 使用相同基础数据 | 分别解析 | Head/WeakPoint 只应用各自确定性倍率 | EditMode |
| AC-05 | 元素与传递形态任意组合 | 解析抗性 | 元素抗性与爆炸抗性按批准规则独立组合；Water 可被正式表达 | EditMode |
| AC-06 | 目标生命值大于零 | 一次伤害跨越到零 | 生命钳制为零，只发布一次耗尽事件，后续伤害不重复提交 | EditMode |
| AC-07 | CharacterFacts 与 EnemyBrain 读取生命状态 | Health 归零 | 两者观察同一派生事实，不存在第二个可写死亡状态 | EditMode / PlayMode |
| AC-08 | 活动配置与 Bootstrap 完成迁移 | Unity 导入并运行 | 无本次迁移造成的脚本、字段或引用丢失；活动主路径没有旧枚举或第二伤害链 | 引用扫描 / PlayMode |

## 验证与最终证据

| 层级 | 用例 | 数量与结果 | 证据路径 |
|---|---|---|---|
| EditMode | 伤害公式、来源透传、弱点、抗性、生命耗尽和 CharacterFacts | 12/12 通过；其中本功能 7/7 | `Logs/Verification/20260808-201748/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 当前步枪和 EnemyAttack 生产链集成 | 3/3 通过；其中本功能 2/2 | `Logs/Verification/20260808-201828-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |

- 运行命令：`pwsh -File .\Tools\Verify-ElementWarEditMode.ps1`；`pwsh -File .\Tools\Verify-ElementWarPlayMode.ps1`。
- 首次 EditMode 运行因新增测试未显式建立 `GameEventBus` 生命周期而为 10/12；修正测试夹具后最终运行 12/12 通过。失败证据保留在 `Logs/Verification/20260808-201604/`。
- 实际修改：统一伤害请求/结果与事件契约；迁移步枪和敌人攻击；移除活动主链暴击；统一生命耗尽事实；迁移活动配置、Bootstrap 和表现消费者；新增 Gameplay 测试。
- 验收等级：达到 Fast Verified，并额外完成完整 PlayMode；Windows64 构建与主线场景人工验收未运行，因此不声明 Full Verified 或 Accepted。
- 剩余风险：`SourceObject` 是本地 Unity 对象引用，不是网络稳定 ID；备份、Legacy/Lua 和旧场景按范围保持不变。
- 回滚单位：契约与运行时代码、活动序列化迁移、测试、Feature Spec/ADR 作为同一功能切片整体回滚。

## 收口检查

- [x] 目标、范围、方案和可观察验收已有明确授权。
- [x] scoped diff 未超出授权，且未吸收无关改动。
- [x] 实际测试数量大于 0，失败和未运行项已如实记录。
- [x] 最终行为、证据路径、维护约束和回滚单位已更新。
- [x] 架构、设计与 ADR 已同步为唯一事实源。
