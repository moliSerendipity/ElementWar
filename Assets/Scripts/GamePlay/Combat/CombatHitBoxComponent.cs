using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 明确标记 HurtBox / HitBox 的命中部位类型。
    /// 后续弱点、头部、装甲部位都应通过显式组件声明，而不是依赖节点命名猜测。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHitBoxComponent : MonoBehaviour
    {
        [SerializeField] private CombatHitPartType hitPartType = CombatHitPartType.Default;

        public CombatHitPartType HitPartType => hitPartType;
    }
}
