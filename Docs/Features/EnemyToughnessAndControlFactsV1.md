# 功能：敌人韧性、失衡与硬控制事实 v1

- 状态：Closed
- 验证：Verified
- 维护日期：2026-08-30
- 关联 ADR：[`ADR-Enemy-Toughness-And-Control-Facts-v1.md`](../Decisions/ADR-Enemy-Toughness-And-Control-Facts-v1.md)

## Current
- 不同敌人配置可提供不同韧性上限和恢复速度。
- 严格低于单次阈值的独立攻击不累计；有效高频攻击可压过恢复并造成失衡。
- Normal 接受完整硬控，Elite 接受一半时长，Boss 不进入硬控；Boss 同次攻击的基础削韧与转换削韧先相加，再只经过一次阈值。
- `ToughnessComponent` 与 `HardControlComponent` 只保存各自状态；一次攻击的跨组件规则由无状态 `EnemyControlApplicationResolver` 统一提交。
- `EnemyRoot` 推进状态，`EnemyBrain` 只读取失衡/硬控事实；当前没有无消费者控制事件或详细拒绝枚举。
- `OverloadReactionResolver` 是首个生产消费者：同一个反应执行身份分别进入伤害与控制的独立目标侧去重集合。

## Evidence
| 层级 | 内容 | 结果 | 证据 |
|---|---|---|---|
| `pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1` | 配置、9/10 阈值、恢复/失衡、合并与跨域去重、三等级、Boss `6 + 6`、延长、死亡、Bootstrap | 64/64 通过；本功能 11/11 | `Logs/Verification/20260830-211319/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| `pwsh -NoProfile -File .\Tools\Verify-ElementWarPlayMode.ps1` | 真实禁用复用、旧 TargetId、AI 取消攻击与恢复求值，以及 Overload 生产接入 | 15/15 通过；本功能 2/2 | `Logs/Verification/20260830-211458-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
| 静态完整性 | 控制状态仍只有既有两组件与一个无状态 Resolver；Overload 不新增控制状态、事件或拒绝枚举 | 通过 | 当前 scoped diff 与 `git diff --check` |
- Windows64、性能和 Bootstrap 主线人工游玩未运行；Normal 水平推动仍等待稳定移动接缝。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
