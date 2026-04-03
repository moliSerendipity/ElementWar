namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Gameplay 可读的只读相机视图上下文。
    /// 当前阶段只暴露角色 3C 主链真正需要的最小视图信息，避免 Gameplay 直接依赖具体相机实现。
    /// </summary>
    public readonly struct CameraViewContext
    {
        public CameraViewContext(float _yaw, float _pitch)
        {
            Yaw = _yaw;
            Pitch = _pitch;
        }

        /// <summary>
        /// 获取当前相机水平 yaw。
        /// 角色朝向与视角相对位移都应读取这里的结果。
        /// </summary>
        public float Yaw { get; }

        /// <summary>
        /// 获取当前相机俯仰 pitch。
        /// 当前阶段 Gameplay 仅作只读透传，后续可供空中瞄准与相机相关规则使用。
        /// </summary>
        public float Pitch { get; }

        public static CameraViewContext Default => new(0f, 0f);
    }
}
