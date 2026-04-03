namespace Game.Definition.ConfigSystem.Core
{
    /// <summary>
    /// 配置校验消息严重级别。
    /// 当前阶段只区分信息、警告、错误三档，足够支撑 Bootstrap 阶段拦截阻塞级问题。
    /// </summary>
    public enum ConfigValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
}