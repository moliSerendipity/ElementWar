using Game.Foundation.Events;
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
    /// 1. 统一处理部位倍率、暴击、防御、抗性与承伤乘区；
    /// 2. 把最终结果提交给 HealthComponent；
    /// 3. 在事实写回成功后，通过 GameEventBus 发布结果事件。
    ///
    /// 约束：
    /// 1. 运行时只读 HealthComponent 和 ActorStatBase，不再在热路径回查配置表；
    /// 2. Resolver 只裁决并提交，不缓存运行时状态。
    /// </summary>
    public static class DamageResolver
    {
        public static CombatDamageResult ResolveAndApply(in CombatDamageRequestContext _request)
        {
            HealthComponent damageReceiver = _request.HitContext.DamageReceiver;
            if (damageReceiver == null || damageReceiver.CanReceiveDamage == false)
            {
                return CombatDamageResult.None;
            }

            ActorStatBase damageReceiverStat = damageReceiver.OwnerStat;

            // 先把命中部位倍率、暴击倍率、防御倍率与抗性倍率全部裁决出来。
            float hitPartMultiplier = ResolveHitPartMultiplier(_request, damageReceiver);
            float criticalMultiplier = ResolveCriticalMultiplier(_request.CritChance, _request.CritDamageMultiplier, out bool isCritical);
            float defenseMultiplier = ResolveDefenseMultiplier(damageReceiverStat);
            float resistanceMultiplier = ResolveResistanceMultiplier(_request.DamageKind, damageReceiverStat);
            float damageTakenMultiplier = ResolveDamageTakenMultiplier(damageReceiverStat);

            // 所有乘区收敛完成后，再得到最终提交到生命事实组件的伤害值。
            float finalDamage = Mathf.Max(
                0f,
                _request.BaseDamage * hitPartMultiplier * criticalMultiplier * defenseMultiplier * resistanceMultiplier * damageTakenMultiplier);

            // 生命事实组件只负责提交已经裁决好的结果，不反向计算伤害。
            CombatDamageResult result = damageReceiver.ApplyResolvedDamage(
                _request.Attacker,
                _request.DamageKind,
                _request.HitContext.HitPartType,
                finalDamage,
                isCritical,
                _request.HitContext.HitPoint,
                _request.HitContext.HitNormal,
                _request.RequestTime);

            if (result.IsApplied)
            {
                // 只有事实写回成功后，才允许对外发布命中与伤害结果事件。
                PublishDamageEvents(result);
            }

            return result;
        }

        private static float ResolveHitPartMultiplier(in CombatDamageRequestContext _request, HealthComponent _damageReceiver)
        {
            return _request.HitContext.HitPartType switch
            {
                CombatHitPartType.Head => Mathf.Max(1f, _request.HeadShotDamageMultiplier),
                CombatHitPartType.WeakPoint => ResolveWeakPointMultiplier(_request.WeakPointDamageMultiplier, _damageReceiver),
                _ => 1f,
            };
        }

        private static float ResolveWeakPointMultiplier(float _requestWeakPointMultiplier, HealthComponent _damageReceiver)
        {
            if (_damageReceiver.OwnerStat is EnemyStat enemyStat)
            {
                // 敌人的弱点倍率由武器弱点倍率与敌人弱点受击倍率共同决定。
                return Mathf.Max(1f, _requestWeakPointMultiplier * enemyStat.WeakPointDamageMultiplier);
            }

            return Mathf.Max(1f, _requestWeakPointMultiplier);
        }

        private static float ResolveCriticalMultiplier(float _critChance, float _critDamageMultiplier, out bool _isCritical)
        {
            _isCritical = false;
            if (_critChance <= 0f)
            {
                return 1f;
            }

            // 暴击只在当前请求范围内随机一次，后续链路直接消费裁决结果。
            _isCritical = Random.value * 100 <= _critChance;
            return _isCritical ? Mathf.Max(1f, _critDamageMultiplier) : 1f;
        }

        private static float ResolveDefenseMultiplier(ActorStatBase _damageReceiverStat)
        {
            if (_damageReceiverStat == null)
            {
                return 1f;
            }

            float defense = Mathf.Max(0f, _damageReceiverStat.Defense);
            return 100f / (100f + defense);
        }

        private static float ResolveResistanceMultiplier(CombatDamageKind _damageKind, ActorStatBase _damageReceiverStat)
        {
            if (_damageReceiverStat == null)
            {
                return 1f;
            }

            float resistance = _damageKind switch
            {
                CombatDamageKind.Fire => _damageReceiverStat.FireResistance,
                CombatDamageKind.Electric => _damageReceiverStat.ElectricResistance,
                CombatDamageKind.Ice => _damageReceiverStat.IceResistance,
                CombatDamageKind.Explosion => _damageReceiverStat.ExplosionResistance,
                _ => _damageReceiverStat.PhysicalResistance,
            };

            return Mathf.Max(0f, 1f - resistance);
        }

        private static float ResolveDamageTakenMultiplier(ActorStatBase _damageReceiverStat)
        {
            return _damageReceiverStat != null ? Mathf.Max(0f, _damageReceiverStat.DamageTakenMultiplier) : 1f;
        }

        private static void PublishDamageEvents(in CombatDamageResult _result)
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (eventBus == null)
            {
                return;
            }

            GameObject target = _result.Target != null ? _result.Target.gameObject : null;

            // 先发布命中确认，再发布伤害与生命变化，让表现层按“命中 -> 伤害 -> 击杀”顺序消费。
            eventBus.Publish(new HitConfirmedEvent(_result.Attacker, target, _result.HitPartType, _result.HitPoint, _result.HitNormal));
            eventBus.Publish(new DamageAppliedEvent(_result));
            eventBus.Publish(new HealthChangedEvent(_result.Target, _result.RemainingHealth, _result.Target.MaxHealth));

            if (_result.WasKilled)
            {
                // 击杀成立后，最后再广播死亡事实。
                eventBus.Publish(new EntityDiedEvent(_result.Attacker, _result.Target));
            }
        }
    }
}
