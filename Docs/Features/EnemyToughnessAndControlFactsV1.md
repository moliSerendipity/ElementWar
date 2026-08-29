# 功能：敌人韧性、失衡与硬控制事实 v1

- 状态：Implemented（本轮边界精简后按用户要求未重新运行测试；下列 EditMode / PlayMode 证据早于当前源码）
- 负责人：Codex / 项目维护者
- 维护日期：2026-08-30
- 关联 Roadmap 任务：`CMB-030`
- 关联 ADR：[`ADR-Enemy-Toughness-And-Control-Facts-v1.md`](../Decisions/ADR-Enemy-Toughness-And-Control-Facts-v1.md)
- 授权记录：用户先批准韧性差异、恢复、单次阈值与三等级方案，后批准 Boss 的“基础削韧 + 硬控转换削韧”同次相加，并明确要求重写两个过度膨胀的 Component 后回复“开始实施”。

## 目标与范围

- 不同敌人配置可提供不同韧性上限和恢复速度。
- 严格低于单次阈值的攻击无论次数都不累计削韧；有效高频攻击仍可压过恢复并造成失衡。
- Normal 接受完整硬控，Elite 接受一半时长，Boss 不进入硬控。
- 对 Boss，同一次攻击的基础削韧与硬控转换削韧先相加，再只经过一次最低阈值。
- AI 在失衡或硬控期间取消攻击并停止，结束后继续原状态求值。
- 死亡、禁用、复用和旧 TargetId 请求都有确定结果。

非目标：生产武器/元素消费者、生命伤害公式、玩家韧性或硬控、击退/减速/Modifier、动画/VFX/HUD、Boss 弱点窗口、Legacy/Lua 与 SampleScene。

## 旧配置复核结论

| 构件 | 结论 | 理由 |
|---|---|---|
| `EnemyBaseStatConfig.toughness` | 修改 | 敌人配置链已有真实消费者位置，但单值不足以表达恢复、阈值与失衡 |
| `CharacterBaseStatConfig` / `ActorStatBase` 韧性 | 删除 | 没有玩家韧性需求或运行时消费者，保留会强迫角色与敌人同质化 |
| `ResistanceSetConfig` 六个伤害抗性 | 保留 | 已被伤害公式消费 |
| `staggerResistance` / `knockBackResistance` / `debuffResistance` | 删除 | 值为零且没有公式、语义或消费者 |
| `EnemyDefinitionConfig` | 修改 | 作为敌人组合根保存 Normal/Elite/Boss 等级事实 |
| `CombatControlRuntime` | 不采用 | 名称和所有权无法说明当前两个不同状态，容易继续聚合未来控制分支 |

## 当前最小实现

| 构件 | 唯一职责 | 明确不负责 |
|---|---|---|
| `ToughnessComponent` | 当前韧性、连续恢复、最低阈值、失衡、局部启停重置 | 身份、阵营、攻击去重、敌人等级、Boss 转换、事件 |
| `HardControlComponent` | 单一硬控结束时间、到期、只延长到更晚时间、局部启停重置 | 身份、阵营、攻击去重、敌人等级、Boss 转换、事件 |
| `EnemyControlApplicationRequest` | 冻结一次攻击的来源/目标身份、基础削韧、硬控时长、Boss 转换削韧 | 状态、来源对象包装、时间线状态 |
| `EnemyControlApplicationResolver` | 从 `Combatant` 缓存的 `EnemyRoot` 读取状态组件，一次校验、按等级换算、去重并写入 | 持有运行时状态、查找目标组件、发布事件、生产具体玩法效果 |
| `EnemyControlApplicationResult` | 返回是否接受以及实际发生的削韧、失衡和硬控事实 | 保存详细拒绝原因、重复请求或目标引用 |
| `Combatant` | 当前 TargetId、两份执行去重集合，以及非序列化的可选 `EnemyRoot` 引用缓存 | 驱动两个状态组件的 Begin/End 生命周期 |

保留三个控制边界脚本的原因：请求必须冻结 TargetId 才能拒绝对象复用后的旧攻击；结果必须表达已提交的两类事实；无状态解析器必须保证同次攻击的两种输出先按等级合并、再一次去重和写入。更小的“分别调用两个 Component”会重新产生顺序依赖和 Boss 两次阈值问题。

当前没有表现层或其他生产订阅者，因此不保留 `ToughnessChangedEvent`、`HardControlChangedEvent` 及两套独立 Request/Result。未来出现真实事件消费者时，应从一次已冻结的原子结果发布事实，而不是在组件写入中途同步发布。

## 行为与生命周期

- 配置：`EnemyBaseStatConfig` 保存 `maxToughness`、每秒恢复、单次阈值和失衡时长；`EnemyDefinitionConfig` 保存等级；`EnemyStat` 保存初始化快照。
- 单次攻击：Normal/Elite 采用基础削韧并分别采用完整/一半硬控时长；Boss 采用 `基础削韧 + Boss 转换削韧`，硬控时长为零。
- 阈值：合并后的最终削韧严格小于目标阈值时不扣韧性，也不留下可累积的残量。
- 恢复：未失衡且低于上限时按秒连续恢复；失衡期间保持零，到期一次回满。
- 硬控：只保存一个结束时间；新结束时间不晚于当前值时状态不变，晚于当前值时延长。
- 去重：`Combatant` 在当前 TargetId 生命周期内对整次控制申请去重；生命伤害使用独立集合，所以同一攻击可以同时造成生命伤害和控制效果。
- 复用：`Combatant` 重新启用建立新 TargetId 并清空去重；两个状态组件各自只重置本地状态，不接收 TargetId 生命周期回调。
- 驱动：`EnemyRoot` 显式 Tick 两个状态组件；`EnemyBrain` 只读取 `IsStaggered || IsHardControlled`。
- 序列化：Bootstrap 两个敌人根各装配一组组件，玩家根不装配；不创建新配置资产。

## 可观察验收

| ID | Given / When | Then | 证据 |
|---|---|---|---|
| AC-01 | 两个敌人定义使用不同面板 | `EnemyStat` 得到不同韧性上限、恢复与等级 | EditMode |
| AC-02 | 阈值为 10，提交任意多个独立 9 | 每次都不扣韧性，不保存残量 | EditMode |
| AC-03 | 提交一次 10 | 精确扣除 10，生命值不变 | EditMode |
| AC-04 | 低频/高频有效攻击推进时间 | 低频被恢复抵消；高频净削韧为正并可失衡 | EditMode |
| AC-05 | 目标已经失衡 | 期间不再扣韧性；到期回满一次 | EditMode |
| AC-06 | 同一执行同时带削韧和硬控并重复提交 | 第一次整体处理，第二次不重复任一效果 | EditMode |
| AC-07 | 同次攻击对 Boss 提供基础 6、转换 6，阈值 10 | 先得 12 再过一次阈值并扣 12；两个独立 6 都不扣 | EditMode |
| AC-08 | Normal/Elite 接收 4 秒硬控 | 分别得到 4 秒和 2 秒；更短请求不覆盖，更长请求延长 | EditMode |
| AC-09 | AI 正在攻击并进入硬控 | 取消当前攻击但保留状态；结束后恢复同状态求值 | PlayMode |
| AC-10 | 目标死亡、禁用或复用 | 状态清空；新生命周期满韧性无硬控；旧请求被拒绝 | EditMode / PlayMode |
| AC-11 | 加载 Bootstrap 并扫描引用 | 两个敌人装配完整、玩家未装配、Missing Script 为零 | EditMode |

## 历史验证证据

以下证据早于本轮缓存与拒绝结果精简，不能作为当前源码的重新验证结果。

| 层级或命令 | 覆盖 | 数量与结果 | 证据路径 |
|---|---|---|---|
| `pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1` | 配置、9/10 阈值、恢复/失衡、合并与跨域去重、三等级、Boss `6 + 6`、延长、死亡、Bootstrap | 63/63 通过；本功能 11/11 | `Logs/Verification/20260830-012848/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| `pwsh -NoProfile -File .\Tools\Verify-ElementWarPlayMode.ps1` | 真实禁用复用、旧 TargetId、AI 取消攻击与恢复求值 | 14/14 通过；本功能 2/2 | `Logs/Verification/20260830-012914-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
| 静态完整性 | 控制目录 5 个脚本/5 个 `.meta`、GUID 唯一、旧六类契约无引用、Bootstrap 字段、Markdown 链接、`git diff --check`、暂存区 | 通过 | 最终可复现扫描 |

- 两个主组件从第一版的 537 / 507 行收缩为 213 / 185 行，各 10 个方法；Resolver / Result 由 149 / 88 行收缩为 104 / 49 行。
- 当前控制目录只有两个状态组件和三个必要边界脚本；正常控制申请不执行组件查询，也没有详细拒绝枚举、控制事件、组件内执行集合、TargetId、Boss 跨组件调用或含糊总控 Runtime。
- 本轮精简后 EditMode、PlayMode、Windows64、性能和主线人工游玩均未运行；生产武器/元素消费者与玩家可见反馈仍由后续任务完成。
- 回滚单位：配置/API 迁移、统一申请边界、两个状态组件、敌人/Bootstrap 接入、测试和本组权威文档整体。
