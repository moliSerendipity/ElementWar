public class PlayerInputSource : IInputSource
{
    /// <summary>
    /// 获取当前帧输入数据
    /// </summary>
    /// <returns>当前帧的输入数据</returns>
    public InputFrame GetInputFrame()
    {
        return InputManager.Instance.Frame;
    }
}
