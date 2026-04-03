using UnityEngine;

namespace Game.Gameplay.Combat.Events
{
    /// <summary>
    /// 死亡事实正式写回后的事件。
    /// 只有目标已经进入死亡状态后，才允许通过事件总线广播。
    /// </summary>
    public readonly struct EntityDiedEvent
    {
        public EntityDiedEvent(GameObject _attacker, HealthComponent _target)
        {
            Attacker = _attacker;
            Target = _target;
        }

        public GameObject Attacker { get; }
        public HealthComponent Target { get; }
    }
}
