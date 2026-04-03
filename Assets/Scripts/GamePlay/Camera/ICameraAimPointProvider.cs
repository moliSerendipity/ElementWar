namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Gameplay 侧只读相机瞄点提供接口。
    /// 具体解析逻辑位于 Presentation 层；Gameplay 只读取结果，不直接进行屏幕中心射线求解。
    /// </summary>
    public interface ICameraAimPointProvider
    {
        /// <summary>
        /// 尝试获取当前相机瞄点上下文。
        /// </summary>
        /// <param name="_cameraAimPointContext">输出当前相机瞄点上下文。</param>
        /// <returns>如果当前瞄点结果可用则返回 true。</returns>
        bool TryGetCameraAimPointContext(out CameraAimPointContext _cameraAimPointContext);
    }
}
