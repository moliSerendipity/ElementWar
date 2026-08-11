using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 把物理命中 Collider 统一解析为最近的活动权威战斗目标根。
    /// </summary>
    public static class CombatTargetResolver
    {
        /// <summary>
        /// 尝试从命中 Collider 向父级解析一个活动 Combatant。
        /// </summary>
        /// <param name="_collider">物理查询返回的 Collider。</param>
        /// <param name="_combatant">成功时返回权威目标根。</param>
        /// <returns>找到活动且有生命引用的目标时返回 <see langword="true"/>。</returns>
        public static bool TryResolve(Collider _collider, out Combatant _combatant)
        {
            _combatant = _collider != null
                ? _collider.GetComponentInParent<Combatant>()
                : null;

            return _combatant != null && _combatant.IsRuntimeActive;
        }
    }
}
