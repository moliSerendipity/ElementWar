# 功能：战斗目标、阵营与攻击执行身份 v1

- 状态：Verified
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-11
- 关联 Roadmap 任务：`CMB-010`
- 关联 ADR：[`ADR-Combatant-Faction-Execution-Identity-v1.md`](../Decisions/ADR-Combatant-Faction-Execution-Identity-v1.md)
- 授权记录：2026-08-11，用户在只读检查与推荐契约总结后明确回复“开始实施”。
- 修正授权：2026-08-11，用户确认按推荐方案删除语义冲突且载荷重复的 `HitConfirmedEvent`，并保持现有表现来源过滤策略不变。

## 目标与范围

- 可观察目标：同一攻击执行通过多个 Collider 命中同一战斗目标时只提交一次伤害；敌人不能伤害敌方阵营；已提交结果和事件能关联一次运行时攻击执行与一个权威目标。
- 非目标：元素附着/反应、Party 切换、网络实体 ID、完整威胁系统、敌人完整攻击时序、敌人池化重构、玩家爆炸自伤策略、Legacy/Lua 或旧场景迁移。
- 允许修改：Gameplay 战斗公共契约、当前步枪与 `EnemyAttack` 生产链、必要事件、Bootstrap 主线序列化装配、Gameplay 测试和相关权威文档。
- 禁止或只读：`Assets/Backup`、`Assets/Script_Legacy`、`Assets/LuaScripts`、`Assets/Scenes/SampleScene.unity`、第三方资源和无关配置。

## 当前事实与批准方案

- 当前程序集与伤害主链以 [`Architecture.md`](../Architecture.md) 为准；此前 `CMB-001` 已统一 `DamageRequest → DamageResolver → HealthComponent → DamageResult/Event`，证据见 [`CombatDomainContractV1.md`](CombatDomainContractV1.md)。
- `Combatant` 是 Gameplay 中的权威战斗目标根，持有 `HealthComponent` 引用、首版阵营、当前活动生命周期的 `CombatantId` 和已接受攻击执行集合。
- `CombatantId` 与 `AttackExecutionId` 是只在当前运行期有意义的强类型递增身份；`0` 为无效值，不序列化，也不承诺网络或跨运行稳定性。
- 每次真正成立的步枪开火生成一个 `AttackExecutionId`；每次 `EnemyAttack.TryBeginAttack` 成功时生成一个，并在取消或结束时清空活动引用。
- Collider 通过统一目标解析器向父级寻找最近的有效 `Combatant`。LayerMask 只做物理粗筛，最终目标合法性由战斗契约裁决。
- `DamageRequest` 在创建时冻结攻击执行、责任者和目标身份；`DamageResult` 与事件保留这些快照，目标之后禁用或复用也不会改变历史归因。
- 已提交伤害的表现统一消费 `DamageAppliedEvent`；删除原先仅在伤害提交后发布、却被描述为“只确认命中”的 `HitConfirmedEvent`。步枪对任意物理表面的原始命中仍由 `WeaponFiredEvent.HadHit` 表达。
- `Combatant` 在同一活动生命周期内对已接受的 `AttackExecutionId` 精确去重：同一执行对同一目标至多一次，不同目标可各一次，不同执行可再次命中。
- `OnDisable` 使当前目标身份失效并清空去重记录；再次启用会建立新身份。生命耗尽不改变身份或清空记录，但 `HealthComponent` 会拒绝后续伤害。
- 首版阵营只有 `Unassigned`、`PlayerParty`、`Enemy`。只有 `PlayerParty → Enemy` 与 `Enemy → PlayerParty` 允许伤害；同阵营和任何 `Unassigned` 组合均拒绝。
- 玩家爆炸仅自伤的例外由 `PRJ-020` 建立显式策略，本阶段不预设环境、混乱或可配置友伤框架。
- Bootstrap 中三个现有 `HealthComponent` 根均显式装配 `Combatant`：玩家为 `PlayerParty`，活动和停用敌人均为 `Enemy`；保留已有 GUID 和其他序列化引用。
- `EnemySensor` 的威胁、锁定和双角色选择留给 `ENM-020`；`EnemyAttack` 的低帧率、动画窗口与完整池化生命周期留给 `ENM-030` / `ENM-040`。

## 行为契约与验收

| ID | Given | When | Then | 自动/人工证据 |
|---|---|---|---|---|
| AC-01 | 一个启用且正确装配的 Combatant | 读取当前身份并禁用/重新启用 | 活动期内 ID 稳定；禁用时无效；重新启用获得不同有效 ID | EditMode / PlayMode |
| AC-02 | 同一目标的多个子 Collider | 分别解析目标根 | 全部解析为同一个 Combatant 与同一个当前 TargetId | EditMode / PlayMode |
| AC-03 | 同一有效执行与同一目标 | 重复提交相同请求 | 只有第一次写回生命并发布伤害事件，后续结果为 DuplicateExecution | EditMode / PlayMode |
| AC-04 | 同一执行命中两个不同敌方目标，或两个执行命中同一目标 | 分别提交 | 每个合法执行-目标组合各提交一次 | EditMode |
| AC-05 | PlayerParty、Enemy、Unassigned 的全部来源/目标组合 | 解析伤害许可 | 仅跨玩家队伍/敌人方向允许；同阵营和未分配均以明确原因拒绝且不发布已提交事件 | EditMode |
| AC-06 | 当前 Hitscan 步枪命中合法敌人 | 完成一次真正开火 | WeaponFired、DamageRequest、DamageResult 和 DamageAppliedEvent 共享同一有效 ExecutionId，目标为权威 Combatant | PlayMode |
| AC-07 | EnemyAttack 的 AOE 同时扫到同一角色的多个 Collider或同阵营敌人 | Strike 提交 | 多 Collider 目标只扣血一次；同阵营目标不扣血；结果保留敌人责任者和攻击配置 | PlayMode |
| AC-08 | Bootstrap 完成迁移 | Unity 导入、测试与引用扫描 | 三个生命根都有正确阵营 Combatant，无 Missing Script、无丢失引用，旧场景未修改 | EditMode / PlayMode / 引用扫描 |

## 验证与最终证据

| 层级 | 用例 | 实际结果 | 证据路径 |
|---|---|---|---|
| EditMode | 身份、阵营矩阵、拒绝原因、去重、`DamageAppliedEvent` 完整表现载荷与既有伤害公式 | 26/26 通过；`DamageContractTests` 21/21 | `Logs/Verification/20260811-220515/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 步枪、EnemyAttack、多 Collider、同阵营、禁用复用与事件生命周期 | 5/5 通过；`DamageProducerPlayModeTests` 4/4 | `Logs/Verification/20260811-220613-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
| 序列化 | Bootstrap Combatant 数量、阵营和 GUID/引用 | 3/3 生命根装配：玩家 `PlayerParty`，两处敌人 `Enemy`；脚本 GUID 对应唯一 `.meta`，场景引用 3 处 | [`Bootstrap.unity`](../../Assets/Scenes/Bootstrap/Bootstrap.unity) 与下方可复现扫描命令 |

- 实际命令：`pwsh -File .\Tools\Verify-ElementWarEditMode.ps1`；`pwsh -File .\Tools\Verify-ElementWarPlayMode.ps1`；`rg -n "HitConfirmedEvent" Assets -g "*.cs"`；删除事件 GUID 的 `Assets` 扫描；`rg -n -C 4 "880000000000000010[123]|f9ac9b9280038824cbebbb01bf1fc8b3|faction: [12]" Assets/Scenes/Bootstrap/Bootstrap.unity`；新 GUID 的 scoped `.meta` / scene / prefab / asset 扫描；`git diff --check`。
- 验收等级：达到 Fast Verified，并额外完成路线要求的 PlayMode 与序列化证据。Windows64、主线场景人工验收和性能检查未运行，因此不声明 Full Verified 或 Accepted。
- 回滚单位：身份契约与伤害公共 API、两条生产链、Bootstrap 迁移、测试、Feature Spec/ADR 和路线状态作为同一切片整体回滚。

## 收口检查

- [x] 目标、范围、方案和可观察验收已有明确授权。
- [x] 实现与 scoped diff 未超出授权，且未吸收无关改动。
- [x] 实际测试数量大于 0；失败、未运行和证据缺口均如实记录。
- [x] 最终行为、运行方式、证据路径、维护约束和回滚单位仍然有效。
- [x] 架构、设计、ADR 和路线使用引用保持一致。
- [x] `CMB-010` 已同步状态、证据、解锁项和新的唯一 `Next`。
