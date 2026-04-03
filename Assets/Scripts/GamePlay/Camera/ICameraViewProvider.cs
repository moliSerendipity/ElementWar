namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Gameplay 侧只读相机视图提供接口。
    /// 具体实现位于 Presentation 层；Gameplay 只读取结果，不直接依赖 Cinemachine 或具体相机对象。
    /// </summary>
    public interface ICameraViewProvider
    {
        /// <summary>
        /// 尝试获取当前相机视图上下文。
        /// </summary>
        /// <param name="_cameraViewContext">输出当前相机视图上下文。</param>
        /// <returns>如果当前相机视图可用则返回 true。</returns>
        bool TryGetCameraViewContext(out CameraViewContext _cameraViewContext);
    }
}
