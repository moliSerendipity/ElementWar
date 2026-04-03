using UnityEngine;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Gameplay 可读的只读相机瞄点上下文。
    /// 用于把“屏幕中心瞄向哪里”的结果从 Presentation 层安全透传给 Gameplay。
    /// </summary>
    public readonly struct CameraAimPointContext
    {
        public CameraAimPointContext(
            Vector3 _rayOrigin,
            Vector3 _rayDirection,
            Vector3 _aimPoint,
            bool _hasBlockingHit,
            float _distance)
        {
            RayOrigin = _rayOrigin;
            RayDirection = _rayDirection;
            AimPoint = _aimPoint;
            HasBlockingHit = _hasBlockingHit;
            Distance = _distance;
        }

        /// <summary>
        /// 获取本帧瞄准射线起点。
        /// </summary>
        public Vector3 RayOrigin { get; }

        /// <summary>
        /// 获取本帧瞄准射线方向。
        /// </summary>
        public Vector3 RayDirection { get; }

        /// <summary>
        /// 获取当前逻辑瞄点。
        /// 命中场景时为命中点，未命中时为默认远点。
        /// </summary>
        public Vector3 AimPoint { get; }

        /// <summary>
        /// 获取当前是否命中了有效遮挡或目标。
        /// </summary>
        public bool HasBlockingHit { get; }

        /// <summary>
        /// 获取当前瞄点距离射线起点的距离。
        /// </summary>
        public float Distance { get; }

        public static CameraAimPointContext Default => new(
            Vector3.zero,
            Vector3.forward,
            Vector3.zero,
            false,
            0f);
    }
}
