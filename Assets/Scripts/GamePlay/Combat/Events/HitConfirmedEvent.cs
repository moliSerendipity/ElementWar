using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Combat.Events
{
    /// <summary>
    /// 命中查询已确认击中合法受击目标后发布的事实事件。
    ///
    /// 说明：
    /// 1. 该事件只说明“本次命中链找到了合法受击对象”；
    /// 2. 它不表示最终一定造成了伤害，最终伤害结果以 DamageAppliedEvent 为准；
    /// 3. 该事件只允许在 Combat 域完成合法性校验后，通过 GameEventBus 对外广播。
    /// </summary>
    public readonly struct HitConfirmedEvent
    {
        public HitConfirmedEvent(
            GameObject _attacker,
            GameObject _target,
            CombatHitPartType _hitPartType,
            Vector3 _hitPoint,
            Vector3 _hitNormal)
        {
            Attacker = _attacker;
            Target = _target;
            HitPartType = _hitPartType;
            HitPoint = _hitPoint;
            HitNormal = _hitNormal;
        }

        public GameObject Attacker { get; }
        public GameObject Target { get; }
        public CombatHitPartType HitPartType { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
    }
}
