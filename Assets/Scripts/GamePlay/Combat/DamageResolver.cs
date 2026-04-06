using Game.Foundation.Events;
using Game.Definition.Combat;
using Game.Gameplay.Character;
using Game.Gameplay.Combat.Events;
using Game.Gameplay.Enemy;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Combat 域唯一伤害裁决点。
    ///
    /// 职责：
    /// 1. 统一处理部位倍率、暴击、防御、抗性与承伤乘区
    /// 2. 把最终结果提交给 HealthComponent
    /// 3. 在事实写回成功后，通过 GameEventBus 发布结果事件
    ///
    /// 伤害公式：
    /// FinalDamage = BaseDamage × HitPartMultiplier × CritMultiplier × DefenseMultiplier × ResistanceMultiplier × DamageTakenMultiplier
    ///
    /// 约束：
    /// 1. 运行时只读 HealthComponent 和 ActorStatBase，不在热路径回查配置表
    /// 2. 只裁决并提交，不缓存运行时状态
    ///
    /// 扩展预留：
    /// 元素处理（Step 5）和韧性/受击处理（Step 6）当前未实现，
    /// 后续接入时在 ResolveAndApply 中对应位置插入调用即可。
    /// </summary>
    public static class DamageResolver
    {
        /// <summary>
        /// 执行完整伤害裁决并提交到生命事实组件。
        /// </summary>
        public static CombatDamageResult ResolveAndApply(in CombatDamageRequestContext _request)
        {
            HealthComponent healthComponent = _request.HitContext.HealthComponent;
            if (healthComponent == null || healthComponent.CanReceiveDamage == false)
            {
                return CombatDamageResult.None;
            }

            ActorStatBase targetStat = healthComponent.OwnerStat;

            // --- Step 2: 命中部位倍率 ---
            float hitPartMultiplier = ResolveHitPartMultiplier(_request, healthComponent);

            // --- Step 3: 攻击侧修正（暴击） ---
            float criticalMultiplier = ResolveCriticalMultiplier(
                _request.CritChance, _request.CritDamageMultiplier, out bool isCritical);

            // --- Step 4: 防守侧修正 ---
            float defenseMultiplier = ResolveDefenseMultiplier(targetStat);
            float resistanceMultiplier = ResolveResistanceMultiplier(_request.DamageKind, targetStat);
            float damageTakenMultiplier = ResolveDamageTakenMultiplier(targetStat);

            // --- Step 5: 元素处理（预留，当前版本跳过） ---
            // TODO: 接入 ElementReactionService 后在此处插入反应判定与附着写入

            // --- Step 6: 韧性/受击处理（预留，当前版本跳过） ---
            // TODO: 接入 PoiseComponent 后在此处插入削韧与受击等级结算

            // --- 收敛所有乘区，计算最终伤害 ---
            float finalDamage = Mathf.Max(
                0f,
                _request.BaseDamage
                * hitPartMultiplier
                * criticalMultiplier
                * defenseMultiplier
                * resistanceMultiplier
                * damageTakenMultiplier);

            // --- Step 7: 提交到生命事实组件 ---
            CombatDamageResult result = healthComponent.ApplyResolvedDamage(
                _request.Attacker,
                _request.DamageKind,
                _request.HitContext.HitPartType,
                finalDamage,
                isCritical,
                _request.HitContext.HitPoint,
                _request.HitContext.HitNormal,
                _request.RequestTime);

            // 只有事实写回成功后，才对外发布结果事件。
            if (result.IsApplied)
            {
                PublishDamageEvents(result);
            }

            return result;
        }

        #region Hit Part

        /// <summary>
        /// 根据命中部位类型解析伤害倍率。
        /// </summary>
        private static float ResolveHitPartMultiplier(in CombatDamageRequestContext _request, HealthComponent _healthComponent)
        {
            return _request.HitContext.HitPartType switch
            {
                CombatHitPartType.Head => Mathf.Max(1f, _request.HeadShotDamageMultiplier),
                CombatHitPartType.WeakPoint => ResolveWeakPointMultiplier(_request.WeakPointDamageMultiplier, _healthComponent),
                _ => 1f,
            };
        }

        /// <summary>
        /// 弱点伤害倍率 = 武器弱点倍率 × 敌人弱点受击倍率。
        /// </summary>
        private static float ResolveWeakPointMultiplier(float _requestWeakPointMultiplier, HealthComponent _healthComponent)
        {
            if (_healthComponent.OwnerStat is EnemyStat enemyStat)
            {
                return Mathf.Max(1f, _requestWeakPointMultiplier * enemyStat.WeakPointDamageMultiplier);
            }

            return Mathf.Max(1f, _requestWeakPointMultiplier);
        }

        #endregion

        #region Critical

        /// <summary>
        /// 暴击判定。当前请求范围内随机一次，后续链路直接消费裁决结果。
        /// </summary>
        private static float ResolveCriticalMultiplier(float _critChance, float _critDamageMultiplier, out bool _isCritical)
        {
            _isCritical = false;

            if (_critChance <= 0f)
            {
                return 1f;
            }

            _isCritical = Random.value * 100f <= _critChance;
            return _isCritical ? Mathf.Max(1f, _critDamageMultiplier) : 1f;
        }

        #endregion

        #region Defense & Resistance

        /// <summary>
        /// 防御减伤，使用递减公式：100 / (100 + defense)。
        /// defense = 0 时不减伤，defense = 100 时减 50%。
        /// </summary>
        private static float ResolveDefenseMultiplier(ActorStatBase _targetStat)
        {
            if (_targetStat == null)
            {
                return 1f;
            }

            float defense = Mathf.Max(0f, _targetStat.Defense);
            return 100f / (100f + defense);
        }

        /// <summary>
        /// 元素/物理抗性减伤。
        ///
        /// 抗性值域约定（与 ResistanceSetConfig 一致）：
        ///   [-1, 1] 范围，其中 0 = 无抗性，0.5 = 减伤 50%，1.0 = 完全免疫，-0.5 = 增伤 50%。
        /// 最终乘区 = Clamp(1 - resistance, 0, 2)，即最多免疫（×0）或最多受 200% 伤害（×2）。
        /// </summary>
        private static float ResolveResistanceMultiplier(CombatDamageKind _damageKind, ActorStatBase _targetStat)
        {
            if (_targetStat == null)
            {
                return 1f;
            }

            float resistance = _damageKind switch
            {
                CombatDamageKind.Fire => _targetStat.FireResistance,
                CombatDamageKind.Electric => _targetStat.ElectricResistance,
                CombatDamageKind.Ice => _targetStat.IceResistance,
                CombatDamageKind.Explosion => _targetStat.ExplosionResistance,
                _ => _targetStat.PhysicalResistance,
            };

            // 安全 Clamp：即使运行时被 Buff 改到超出配置范围，也不会产生负伤害或无限伤害。
            return Mathf.Clamp(1f - resistance, 0f, 2f);
        }

        /// <summary>
        /// 全局承伤倍率。由目标自身 Stat 提供，Buff 可以修改。
        /// </summary>
        private static float ResolveDamageTakenMultiplier(ActorStatBase _targetStat)
        {
            return _targetStat != null ? Mathf.Max(0f, _targetStat.DamageTakenMultiplier) : 1f;
        }

        #endregion

        #region Event Publishing

        /// <summary>
        /// 在伤害事实提交成功后，按"命中 → 伤害 → 生命变化 → 击杀"顺序发布结果事件。
        /// </summary>
        private static void PublishDamageEvents(in CombatDamageResult _result)
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (eventBus == null)
            {
                return;
            }

            GameObject target = _result.Target != null ? _result.Target.gameObject : null;

            eventBus.Publish(new HitConfirmedEvent(
                _result.Attacker, target, _result.HitPartType, _result.HitPoint, _result.HitNormal));

            eventBus.Publish(new DamageAppliedEvent(_result));

            eventBus.Publish(new HealthChangedEvent(
                _result.Target, _result.RemainingHealth, _result.Target.MaxHealth));

            if (_result.WasKilled)
            {
                eventBus.Publish(new EntityDiedEvent(_result.Attacker, _result.Target));
            }
        }

        #endregion
    }
}
