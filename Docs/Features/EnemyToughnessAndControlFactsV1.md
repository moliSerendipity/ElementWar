# 功能：敌人韧性、失衡与硬控制事实 v1

- 状态：Implemented（本轮边界精简后按用户要求未重新运行测试；下列 EditMode / PlayMode 证据早于当前源码）
- 维护日期：2026-08-30
- 关联 ADR：[`ADR-Enemy-Toughness-And-Control-Facts-v1.md`](../Decisions/ADR-Enemy-Toughness-And-Control-Facts-v1.md)

## Current
- 不同敌人配置可提供不同韧性上限和恢复速度。
- 严格低于单次阈值的独立攻击不累计；有效高频攻击可压过恢复并造成失衡。
- Normal 接受完整硬控，Elite 接受一半时长，Boss 不进入硬控；Boss 同次攻击的基础削韧与转换削韧先相加，再只经过一次阈值。
- `ToughnessComponent` 与 `HardControlComponent` 只保存各自状态；一次攻击的跨组件规则由无状态 `EnemyControlApplicationResolver` 统一提交。
- `EnemyRoot` 推进状态，`EnemyBrain` 只读取失衡/硬控事实；当前没有无消费者控制事件或详细拒绝枚举。
- 本轮边界精简后未重跑自动化，因此当前状态仍为 Implemented，历史测试不能升级为当前 Verified。

## Evidence
| 层级 | 内容 | 结果 | 证据 |
|---|---|---|---|
| `pwsh -NoProfile -File .\Tools\Verify-ElementWarEditMode.ps1` | 配置、9/10 阈值、恢复/失衡、合并与跨域去重、三等级、Boss `6 + 6`、延长、死亡、Bootstrap | 63/63 通过；本功能 11/11 | `Logs/Verification/20260830-012848/EditMode-results.xml`、`EditMode.log`、`verification-summary.json` |
| `pwsh -NoProfile -File .\Tools\Verify-ElementWarPlayMode.ps1` | 真实禁用复用、旧 TargetId、AI 取消攻击与恢复求值 | 14/14 通过；本功能 2/2 | `Logs/Verification/20260830-012914-playmode/PlayMode-results.xml`、`PlayMode.log`、`PlayMode-verification-summary.json` |
| 静态完整性 | 控制目录 5 个脚本/5 个 `.meta`、GUID 唯一、旧六类契约无引用、Bootstrap 字段、Markdown 链接、`git diff --check`、暂存区 | 通过 | 最终可复现扫描 |
- 本轮精简后 EditMode、PlayMode、Windows64、性能和主线人工游玩均未运行；生产武器/元素消费者与玩家可见反馈仍由后续任务完成。

> 本页只保留关闭后的当前摘要；详细 AC、调查过程、中间失败、授权往返和回滚历史由 Git 追溯。
