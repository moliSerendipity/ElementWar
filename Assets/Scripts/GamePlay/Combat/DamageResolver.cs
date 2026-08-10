using Game.Definition.Combat;
using Game.Foundation.Events;
using Game.Gameplay.Character;
using Game.Gameplay.Combat.Events;
using Game.Gameplay.Enemy;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 伤害域唯一裁决点，按确定性公式计算伤害并把结果提交给目标生命组件。
    /// </summary>
    public static class DamageResolver
    {
        /// <summary>
        /// 执行完整伤害裁决；目标拒绝伤害时返回 <see cref="DamageResult.None"/>。
        /// </summary>
        /// <param name="_request">已经完成来源、目标和命中事实解析的请求。</param>
        /// <returns>已提交的伤害结果；目标不可受伤时返回空结果。</returns>
        public static DamageResult ResolveAndApply(in DamageRequest _request)
        {
            HealthComponent healthComponent = _request.Target;
            if (healthComponent == null || healthComponent.CanReceiveDamage == false)
            {
                return DamageResult.None;
            }

            ActorStatBase targetStat = healthComponent.OwnerStat;
            float hitPartMultiplier = ResolveHitPartMultiplier(_request, healthComponent);
            float defenseMultiplier = ResolveDefenseMultiplier(targetStat);
            float elementResistanceMultiplier = ResolveElementResistanceMultiplier(_request.Element, targetStat);
            float deliveryResistanceMultiplier = ResolveDeliveryResistanceMultiplier(_request.Delivery, targetStat);
            float damageTakenMultiplier = ResolveDamageTakenMultiplier(targetStat);

            // 元素轴当前只参与抗性，不在本阶段执行附着或反应。
            float finalDamage = Mathf.Max(
                0f,
                _request.BaseDamage
                * hitPartMultiplier
                * defenseMultiplier
                * elementResistanceMultiplier
                * deliveryResistanceMultiplier
                * damageTakenMultiplier);

            DamageResult result = healthComponent.ApplyResolvedDamage(
                _request.Instigator,
                _request.SourceObject,
                _request.Element,
                _request.Delivery,
                _request.HitPartType,
                finalDamage,
                _request.HitPoint,
                _request.HitNormal,
                _request.RequestTime);

            if (result.IsApplied)
            {
                PublishDamageEvents(result);
            }

            return result;
        }

        /// <summary>
        /// 解析确定性的命中部位倍率。弱点同时应用来源侧和敌人受击侧倍率。
        /// </summary>
        private static float ResolveHitPartMultiplier(in DamageRequest _request, HealthComponent _healthComponent)
        {
            return _request.HitPartType switch
            {
                HitPartType.Head => Mathf.Max(1f, _request.HeadShotDamageMultiplier),
                HitPartType.WeakPoint => ResolveWeakPointMultiplier(
                    _request.WeakPointDamageMultiplier,
                    _healthComponent),
                _ => 1f,
            };
        }

        /// <summary>
        /// 弱点倍率由攻击来源倍率与敌人弱点承伤倍率相乘；非敌人目标只使用来源倍率。
        /// </summary>
        private static float ResolveWeakPointMultiplier(
            float _requestWeakPointMultiplier,
            HealthComponent _healthComponent)
        {
            if (_healthComponent.OwnerStat is EnemyStat enemyStat)
            {
                return Mathf.Max(
                    1f,
                    _requestWeakPointMultiplier * enemyStat.WeakPointDamageMultiplier);
            }

            return Mathf.Max(1f, _requestWeakPointMultiplier);
        }

        /// <summary>
        /// 防御使用递减公式 100 / (100 + defense)。
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
        /// 根据元素轴选择抗性；<see cref="ElementType.None"/> 使用物理抗性。
        /// 抗性最终钳制为 0 至 2 倍伤害。
        /// </summary>
        private static float ResolveElementResistanceMultiplier(
            ElementType _element,
            ActorStatBase _targetStat)
        {
            if (_targetStat == null)
            {
                return 1f;
            }

            float resistance = _element switch
            {
                ElementType.Fire => _targetStat.FireResistance,
                ElementType.Water => _targetStat.WaterResistance,
                ElementType.Electric => _targetStat.ElectricResistance,
                ElementType.Ice => _targetStat.IceResistance,
                _ => _targetStat.PhysicalResistance,
            };

            return ResolveResistanceMultiplier(resistance);
        }

        /// <summary>
        /// 爆炸形态在元素抗性之外独立应用爆炸抗性；直接伤害不追加修正。
        /// </summary>
        private static float ResolveDeliveryResistanceMultiplier(
            DamageDeliveryType _delivery,
            ActorStatBase _targetStat)
        {
            if (_targetStat == null || _delivery != DamageDeliveryType.Explosion)
            {
                return 1f;
            }

            return ResolveResistanceMultiplier(_targetStat.ExplosionResistance);
        }

        /// <summary>
        /// 把 [-1, 1] 抗性转换为 [2, 0] 的伤害倍率，并防御运行时越界值。
        /// </summary>
        private static float ResolveResistanceMultiplier(float _resistance)
        {
            return Mathf.Clamp(1f - _resistance, 0f, 2f);
        }

        /// <summary>
        /// 读取目标当前承伤乘区；没有 Stat 时不修正。
        /// </summary>
        private static float ResolveDamageTakenMultiplier(ActorStatBase _targetStat)
        {
            return _targetStat != null
                ? Mathf.Max(0f, _targetStat.DamageTakenMultiplier)
                : 1f;
        }

        /// <summary>
        /// 在生命事实提交后按命中、伤害、生命变化、生命耗尽的顺序同步发布事件。
        /// </summary>
        private static void PublishDamageEvents(in DamageResult _result)
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (eventBus == null)
            {
                return;
            }

            GameObject target = _result.Target != null ? _result.Target.gameObject : null;

            eventBus.Publish(new HitConfirmedEvent(
                _result.Instigator,
                _result.SourceObject,
                target,
                _result.HitPartType,
                _result.HitPoint,
                _result.HitNormal));

            eventBus.Publish(new DamageAppliedEvent(_result));
            eventBus.Publish(new HealthChangedEvent(
                _result.Target,
                _result.RemainingHealth,
                _result.Target.MaxHealth));

            if (_result.DidDepleteHealth)
            {
                eventBus.Publish(new HealthDepletedEvent(
                    _result.Instigator,
                    _result.SourceObject,
                    _result.Target));
            }
        }
    }
}
