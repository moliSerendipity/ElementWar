public interface IInputSource
{
    /// <summary>
    /// 获取当前帧的输入数据
    /// </summary>
    /// <returns>当前帧的输入数据</returns>
    InputFrame GetInputFrame();
}
