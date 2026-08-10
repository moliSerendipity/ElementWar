using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 显式标记 HurtBox 或 HitBox 的命中部位，不依赖节点名称推断。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitBoxComponent : MonoBehaviour
    {
        [SerializeField] private HitPartType hitPartType = HitPartType.Default;

        /// <summary>该碰撞体对应的命中部位。</summary>
        public HitPartType HitPartType => hitPartType;
    }
}
