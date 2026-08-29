using Game.Definition.Enemy;
using Game.Gameplay.Enemy;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 把一次攻击的基础削韧和硬控制作为一个整体按敌人等级换算，并原子写入两个状态组件。
    /// </summary>
    public static class EnemyControlApplicationResolver
    {
        /// <summary>校验一次攻击快照，按目标等级换算效果，并以一个执行身份完成去重与写入。</summary>
        /// <param name="_request">已经冻结来源、目标和三项来源效果的攻击请求。</param>
        /// <param name="_applicationTime">效果写入时的有限非负运行时时间。</param>
        /// <returns>接受时返回已处理事实；拒绝时返回默认结果。</returns>
        public static EnemyControlApplicationResult ResolveAndApply(
            in EnemyControlApplicationRequest _request,
            float _applicationTime)
        {
            Combatant instigator = _request.InstigatorCombatant;
            Combatant target = _request.TargetCombatant;
            // 请求可能晚于对象池复用到达，必须先确认冻结身份仍属于当前双方。
            if (_request.ExecutionId.IsValid == false ||
                instigator == null ||
                instigator.MatchesCurrentIdentity(_request.InstigatorId) == false ||
                target == null ||
                target.MatchesCurrentIdentity(_request.TargetId) == false ||
                target.Faction != CombatFaction.Enemy ||
                CombatFactionRules.CanDamage(instigator.Faction, target.Faction) == false)
            {
                return default;
            }

            // 生产来源尚未接入，当前公共入口只保留一次最小数值边界，避免 NaN 污染状态。
            if (IsFiniteNonNegative(_request.BaseToughnessDamage) == false ||
                IsFiniteNonNegative(_request.HardControlDuration) == false ||
                IsFiniteNonNegative(_request.BossToughnessDamage) == false ||
                IsFiniteNonNegative(_applicationTime) == false)
            {
                return default;
            }

            EnemyRoot enemy = target.Enemy;
            EnemyStat enemyStat = enemy != null ? enemy.Stat : null;
            ToughnessComponent toughnessComponent = enemy != null ? enemy.Toughness : null;
            HardControlComponent hardControlComponent = enemy != null ? enemy.HardControl : null;
            if (enemyStat == null ||
                enemyStat.IsInitialized == false ||
                toughnessComponent == null ||
                toughnessComponent.IsOperational == false ||
                hardControlComponent == null ||
                hardControlComponent.IsOperational == false)
            {
                return default;
            }

            // Normal 保留完整硬控，Elite 折半；Boss 不吃硬控，并把转换削韧加到同次基础削韧上。
            float resolvedToughnessDamage = _request.BaseToughnessDamage;
            float effectiveHardControlDuration = _request.HardControlDuration;
            switch (enemyStat.EnemyTier)
            {
                case EnemyTier.Elite:
                    effectiveHardControlDuration *= 0.5f;
                    break;
                case EnemyTier.Boss:
                    resolvedToughnessDamage += _request.BossToughnessDamage;
                    effectiveHardControlDuration = 0f;
                    break;
            }

            if (resolvedToughnessDamage <= 0f && effectiveHardControlDuration <= 0f)
            {
                return default;
            }

            // 一个目标生命周期只登记一次合并控制执行；伤害域使用独立集合，不会吞掉同次削韧或硬控。
            if (target.TryAcceptControlExecution(_request.ExecutionId, _request.TargetId) == false)
            {
                return default;
            }

            // 两个状态写入之间不发布事件，因此同一请求不会被同步回调拆开或重入。
            float appliedToughnessDamage = toughnessComponent.ApplyResolvedDamage(
                resolvedToughnessDamage,
                _applicationTime,
                out bool didStagger);
            HardControlApplicationStatus hardControlStatus =
                hardControlComponent.ApplyResolvedDuration(
                    effectiveHardControlDuration,
                    _applicationTime);

            return new EnemyControlApplicationResult(
                appliedToughnessDamage,
                didStagger,
                hardControlStatus);
        }

        private static bool IsFiniteNonNegative(float _value)
        {
            return float.IsNaN(_value) == false &&
                float.IsInfinity(_value) == false &&
                _value >= 0f;
        }
    }
}
