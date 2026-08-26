# 功能：范围目标查询与友伤过滤 v1

- 状态：Fast Verified（并额外通过 PlayMode；未完成 Windows64 与主线人工验收）
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-26
- 关联 Roadmap 任务：`CMB-020`
- 关联 ADR：[`ADR-Combat-Range-Target-Query-v1.md`](../Decisions/ADR-Combat-Range-Target-Query-v1.md)
- 授权记录：2026-08-26，用户在重新只读检查并确认精简契约后明确回复“开始实施”。

## 目标与范围

- 可观察目标：同一球形物理场景重复查询得到相同的合法战斗目标集合与顺序；同一目标的多个 Collider 只产生一个结果。
- 非目标：具体超载、爆炸、感电、伤害衰减、控制、VFX/SFX/HUD、玩家自伤例外、EnemyAttack 形状迁移和性能优化。
- 允许修改：Gameplay Combat 查询、专用 EditMode/PlayMode 测试、Feature Spec/ADR、Architecture 与 Roadmap。
- 禁止或只读：scene、prefab、asmdef、配置资产、现有生产消费者、Legacy/Lua 和第三方资源。

## 当前事实与批准方案

- `Combatant`、`CombatantId`、`CombatTargetResolver`、`CombatFactionRules` 与 `HealthComponent` 分别提供权威目标根、活动身份、Collider 根解析、首版阵营矩阵和生命事实。
- 新增 `CombatRangeQuery.QueryDamageableTargets`，直接接收来源、中心、半径、目标层、可选 LOS、阻挡层和数量上限；输入没有独立生命周期或状态所有权，因此不建立 Request 包装。
- 新增 `CombatRangeTarget`，只携带目标、最近 Collider 表面点和距离。后两项是 `PRJ-020` 距离衰减与 `ELM-070` 最近目标选择已批准需要、且 Collider 去重后无法由调用方可靠重建的几何事实。
- 查询固定使用球形 Physics 查询并忽略 Trigger；LayerMask 只粗筛，随后解析活动 Combatant、剔除死亡/禁用目标并使用 `CombatFactionRules.CanDamage` 过滤阵营。
- 多 Collider 以当前 `CombatantId` 去重并保留最近表面事实；同距 Collider 使用最近点坐标稳定选择。最终按表面距离、CombatantId 排序，LOS 后应用数量上限。
- 查询只表达“查询时合法”的目标集合；`DamageResolver` 在消费者提交伤害时继续重验身份、阵营、生命和执行去重，因为这些状态可能在查询后变化。
- 当前事件型消费者数量和性能数据不足以证明缓存或 NonAlloc 固定缓冲区必要；v1 优先保证完整集合，不声明零分配或 Player 性能结论。

## 行为契约与验收

| ID | Given | When | Then | 自动/人工证据 |
|---|---|---|---|---|
| AC-01 | 同一活动 Combatant 下存在多个命中 Collider | 查询覆盖这些 Collider | 只返回一个权威目标，并保留最近表面点与距离 | EditMode / PlayMode |
| AC-02 | 敌方、同阵营、Unassigned、死亡、禁用和错误层目标并存 | 来源执行范围查询 | 只返回当前活动且存活的敌方目标 | EditMode / PlayMode |
| AC-03 | 目标位于半径边界内、边界外或只有 Trigger Collider | 执行查询 | 边界内实体保留；边界外和 Trigger 实体排除 | EditMode |
| AC-04 | 多个目标具有不同或相同表面距离 | 重复查询并设置上限 | 按距离、CombatantId 得到相同顺序，最后应用上限 | EditMode |
| AC-05 | 部分目标被环境阻挡 | 启用 LOS 查询 | 遮挡目标在数量上限前排除，未遮挡目标保持确定顺序 | PlayMode |
| AC-06 | 目标禁用后重新启用 | 前后分别查询 | 禁用时退出集合；重新启用后以新身份重新进入，无旧结果残留 | PlayMode |

## 验证与最终证据

| 层级 | 用例 | 数量与结果 | 证据路径 |
|---|---|---|---|
| EditMode | 目标解析、过滤、边界、Trigger、排序与上限 | 52/52 通过；本功能 4/4 | `Logs/Verification/20260826-234825/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| PlayMode | 真实 Physics 多 Collider、友伤、LOS 与禁用复用 | 12/12 通过；本功能 3/3 | `Logs/Verification/20260826-234737-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |

- 运行命令：`pwsh -File .\Tools\Verify-ElementWarEditMode.ps1`；`pwsh -File .\Tools\Verify-ElementWarPlayMode.ps1`。
- 实际修改：新增 `CombatRangeQuery` 与 `CombatRangeTarget`，并新增专用 EditMode/PlayMode 契约测试；没有新增 Request、接口、配置、缓存或事件，也没有接入生产消费者或修改 Unity 序列化资源。
- 验收等级：达到 Fast Verified，并额外完成 PlayMode；没有 Windows64 构建和主线人工验收，因此不是 Full Verified 或 Accepted。
- 未运行与剩余风险：Windows64、主线人工验收和性能检查未运行；当前没有生产消费者，Player 分配与高密度场景性能仍未测量。
- 回滚单位：两个生产类型、专用测试、Feature Spec/ADR、Architecture 与 Roadmap 同步记录。

## 收口检查

- [x] 目标、范围、方案和可观察验收已有明确授权。
- [x] 实现与 scoped diff 未超出授权，且未吸收用户无关改动。
- [x] 已完成必要性与扩展性审查。
- [x] 每项不变量只有一个权威校验层，热路径没有无依据的重复配置校验。
- [x] 实际测试数量大于 0，失败、未运行和证据缺口均如实记录。
- [x] Feature Spec/ADR、Architecture 与 Roadmap 已与最终实现同步。
