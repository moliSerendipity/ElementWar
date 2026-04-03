using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 弱点显式标记组件。
    /// 只要命中链路读到该组件，即判定为 WeakPoint，不再依赖 Collider 名称猜测。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeakSpotComponent : MonoBehaviour
    {
        [SerializeField] private bool isEnabled = true;

        public bool IsEnabled => isEnabled;
    }
}
